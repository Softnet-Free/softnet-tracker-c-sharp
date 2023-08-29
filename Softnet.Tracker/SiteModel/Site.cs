/*
*   Copyright 2023 Robert Koifman
*   
*   Licensed under the Apache License, Version 2.0 (the "License");
*   you may not use this file except in compliance with the License.
*   You may obtain a copy of the License at
*
*   http://www.apache.org/licenses/LICENSE-2.0
*
*   Unless required by applicable law or agreed to in writing, software
*   distributed under the License is distributed on an "AS IS" BASIS,
*   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*   See the License for the specific language governing permissions and
*   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Softnet.Asn;
using Softnet.ServerKit;
using Softnet.Tracker.Core;
using Softnet.Tracker.ServiceModel;
using Softnet.Tracker.ClientModel;

namespace Softnet.Tracker.SiteModel
{
    public class Site : Monitorable
    {
        public long Id;
        public byte[] Uid;

        public Site(long siteId)
        {
            this.Id = siteId;
            m_Services = new List<Service>();
            m_Clients = new List<Client>();
            m_StatelessClients = new List<Client>();
            m_EndpointsCount = 0;
            c_SiteKind = 0;
            m_State = StateEnum.Initial;
            m_ServiceGroup = null;
            m_Membership = null;
            EventController = null;            
        }

        long m_LastAcquiredTime; 
        public void SetLastAcquiredTime()
        {
            m_LastAcquiredTime = SystemClock.Seconds;
        }

        public bool HasBeenAcquired()
        {
            if (SystemClock.Seconds - m_LastAcquiredTime <= 300)
                return true;
            return false;
        }

        public bool IsAlive(long currentTime)
        {
            if (EventController != null)
                EventController.Monitor();

            if (m_EndpointsCount > 0)
                return true;

            if (m_State == StateEnum.Completed)
                return false;
            if (currentTime - m_LastAcquiredTime <= 300)
                return true;
            if (SoftnetTracker.RemoveOnExpiration(this))
                return false;

            return true;
        }
        
        int c_SiteKind;
        string c_ServiceType;
        string c_ContractAuthor;
        byte[] c_SSHash;
        bool c_SiteEnabled;
        bool c_OwnerEligible;

        public readonly object mutex = new object();

        enum StateEnum { Initial, Loading, Running, Blank, Completed };
        StateEnum m_State;

        public IServiceGroup m_ServiceGroup;
        public IMembership m_Membership;
        public IEventController EventController;

        ServiceSyncController m_ServiceSyncController;
        ClientSyncController m_ClientSyncController;
        List<Service> m_Services;
        List<Client> m_Clients;
        List<Client> m_StatelessClients;
        int m_EndpointsCount;

        public Service FindService(long serviceId)
        {
            lock (mutex)
            {
                if (c_SiteKind == Constants.SiteKind.SingleService)
                {
                    if(m_Services.Count == 1)                                            
                        return m_Services[0];
                    return null;
                }
                else
                {
                    return m_Services.Find(x => x.Id == serviceId);
                }
            }
        }

        public Client FindClient(long clientId)
        {
            lock (mutex)
            {
                return m_Clients.Find(x => x.Id == clientId);
            }
        }

        public Client FindStatelessClient(long clientId)
        {
            lock (mutex)
            {
                return m_StatelessClients.Find(x => x.Id == clientId);
            }
        }

        public int Load()
        {
            lock (mutex)
            {
                if (m_State != StateEnum.Initial)
                    return ErrorCodes.RESTART;
                m_State = StateEnum.Loading;
            }

            try
            {
                var siteIData = new SiteIData();
                SoftnetRegistry.Site_GetIData(this.Id, siteIData);

                m_ServiceSyncController = ServiceSyncController.Create(this);
                m_ClientSyncController = ClientSyncController.Create(this);

                this.Uid = siteIData.SiteUid;
                c_SiteKind = siteIData.SiteKind;
                c_ServiceType = siteIData.ServiceType;
                c_ContractAuthor = siteIData.ContractAuthor;
                c_SiteEnabled = siteIData.SiteEnabled;
                c_OwnerEligible = siteIData.OwnerEligible;

                if (siteIData.Structured)
                {
                    c_SSHash = siteIData.SSHash;

                    if (c_SiteEnabled && c_OwnerEligible)
                    {
                        if (c_SiteKind == Constants.SiteKind.SingleService)
                            m_ServiceGroup = SingleServiceGroup.Create(this, siteIData.SGItems[0], m_Services, m_Clients, m_StatelessClients);
                        else
                            m_ServiceGroup = MultiServiceGroup.Create(this, siteIData.SGItems, m_Services, m_Clients, m_StatelessClients, new Action<long>(ServiceGroup_OnServiceDeleted), new Action<long>(ServiceGroup_OnServiceStatusChanged));

                        if (siteIData.RolesSupported)
                        {
                            m_Membership = RBMembership.Create(
                                this,
                                m_Services,
                                m_Clients,
                                siteIData,
                                new Action<long>(Membership_OnUserRemoved),
                                new Action<long>(Membership_OnUserDeleted),
                                new Action(Membership_OnClientRestarted),
                                new Action(Membership_OnGuestAllowed),
                                new Action(Membership_OnGuestDenied));
                        }
                        else
                        {
                            m_Membership = UBMembership.Create(
                                this,
                                m_Services,
                                m_Clients,
                                siteIData,
                                new Action<long>(Membership_OnUserRemoved),
                                new Action<long>(Membership_OnUserDeleted),
                                new Action(Membership_OnClientRestarted),
                                new Action(Membership_OnGuestAllowed),
                                new Action(Membership_OnGuestDenied));
                        }

                        if (siteIData.EventsSupported)
                        {
                            if (c_SiteKind == Constants.SiteKind.SingleService)
                            {
                                SSEventController eventController = new SSEventController(this, m_Clients, m_StatelessClients);
                                eventController.Init(siteIData);
                                EventController = eventController;
                            }
                            else
                            {
                                MSEventController eventController = new MSEventController(this, m_Clients, m_StatelessClients);
                                eventController.Init(siteIData);
                                EventController = eventController;
                            }
                        }

                        lock (mutex)
                        {
                            if (m_State == StateEnum.Completed)
                                return ErrorCodes.RESTART;
                            m_State = StateEnum.Running;

                            if (m_EndpointsCount > 0)
                                RunInstalledEndpoints();
                        }
                    }
                    else
                    {
                        lock (mutex)
                        {
                            if (m_State == StateEnum.Completed)
                                return ErrorCodes.RESTART;
                            m_State = StateEnum.Running;

                            if (c_SiteKind == Constants.SiteKind.SingleService)
                                m_ServiceGroup = SingleServiceGroup.Create(this, siteIData.SGItems[0], m_Services);
                            else
                                m_ServiceGroup = MultiServiceGroup.Create(this, siteIData.SGItems, m_Services, new Action<long>(ServiceGroup_OnServiceDeleted), new Action<long>(ServiceGroup_OnServiceStatusChanged));

                            if (m_EndpointsCount > 0)
                                RunInstalledEndpoints();
                        }
                    }
                }
                else
                {
                    lock (mutex)
                    {
                        if (m_State == StateEnum.Completed)
                            return ErrorCodes.RESTART;
                        m_State = StateEnum.Blank;

                        if (siteIData.SiteKind == Constants.SiteKind.SingleService)
                            m_ServiceGroup = SingleServiceGroup.Create(this, siteIData.SGItems[0], m_Services);
                        else
                            m_ServiceGroup = MultiServiceGroup.Create(this, siteIData.SGItems, m_Services, new Action<long>(ServiceGroup_OnServiceDeleted), new Action<long>(ServiceGroup_OnServiceStatusChanged));

                        if (m_EndpointsCount > 0)
                            RunInstalledEndpoints();
                    }
                }

                Softnet.ServerKit.Monitor.Add(this);
                return 0;
            }
            catch (SoftnetException ex)
            {
                return ex.ErrorCode;
            }
        }

        void RunInstalledEndpoints()
        {
            if (m_Services.Count > 0)
            {
                foreach (Service service in m_Services)
                {
                    RunService(service, (ServiceInstaller)service.Tmp);
                    service.Tmp = null;
                }
            }

            if (m_Clients.Count > 0)
            {
                foreach (Client client in m_Clients)
                {
                    if (client.UserKind != Constants.UserKind.Guest)
                        RunClient(client, (ClientInstaller)client.Tmp);
                    else
                        RunGuestClient(client, (ClientInstaller)client.Tmp);
                    client.Tmp = null;
                }
            }

            if (m_StatelessClients.Count > 0)
            {
                foreach (Client client in m_StatelessClients)
                {
                    RunStatelessClient(client, (ClientInstaller)client.Tmp);
                    client.Tmp = null;
                }
            }
        }

        bool m_removed = false;
        public void Remove(int errorCode)
        {
            lock (mutex)
            {
                if (m_removed)
                    return;                
                m_removed = true;
                m_State = StateEnum.Completed;

                if (this.m_EndpointsCount > 0)
                {
                    foreach (Service service in m_Services)
                        service.Shutdown(errorCode);
                    foreach (Client client in m_Clients)
                        client.Shutdown(errorCode);
                    foreach (Client client in m_StatelessClients)
                        client.Shutdown(errorCode);
                }
            }

            SoftnetTracker.Remove(this);
        }

        public void OnDeleted()
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Completed)
                    return;
                m_State = StateEnum.Completed;

                if (this.m_EndpointsCount > 0)
                {
                    foreach (Service service in m_Services)
                        service.Shutdown(ErrorCodes.SERVICE_NOT_REGISTERED);
                    foreach (Client client in m_Clients)
                        client.Shutdown(ErrorCodes.CLIENT_NOT_REGISTERED);
                    foreach (Client client in m_StatelessClients)
                        client.Shutdown(ErrorCodes.CLIENT_NOT_REGISTERED);
                }
            }

            SoftnetTracker.Remove(this);
        }

        public void Terminate()
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Completed)
                    return;
                m_State = StateEnum.Completed;

                if (this.m_EndpointsCount > 0)
                {
                    foreach (Service service in m_Services)
                        service.Close();
                    m_Services.Clear();
                    foreach (Client client in m_Clients)
                        client.Close();
                    m_Clients.Clear();
                    foreach (Client client in m_StatelessClients)
                        client.Close();
                    m_StatelessClients.Clear();
                }
            }
        }

        public void InstallClient(Client client, ClientInstaller clientInstaller)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Completed)
                {
                    clientInstaller.Shutdown(ErrorCodes.RESTART);
                    return;
                }

                Client prevInstance = m_Clients.Find(x => x.Id == client.Id);
                if (prevInstance == null)
                {
                    this.m_EndpointsCount++;
                }
                else
                {
                    m_Clients.Remove(prevInstance);                    

                    if (clientInstaller.ChannelRestored)
                    {
                        if (clientInstaller.ChannelId.SequenceEqual(prevInstance.ChannelId))
                        {
                            prevInstance.Close();
                        }
                        else
                        {
                            prevInstance.Shutdown(ErrorCodes.DUPLICATED_CLIENT_KEY_USAGE);
                        }
                    }
                    else
                    {
                        prevInstance.Close();
                    }
                }

                m_Clients.Add(client);

                if (m_State == StateEnum.Running)
                {
                    RunClient(client, clientInstaller);
                }
                else if (m_State == StateEnum.Blank)
                {
                    clientInstaller.SetParked(Constants.ClientStatus.SiteBlank);
                }
                else if (m_State == StateEnum.Initial || m_State == StateEnum.Loading)
                {
                    client.Tmp = clientInstaller;
                }
            }           
        }

        void RunClient(Client client, ClientInstaller clientInstaller)
        {
            if (clientInstaller.ServiceType.Equals(c_ServiceType) == false || clientInstaller.ContractAuthor.Equals(c_ContractAuthor) == false)
            {
                clientInstaller.SetParked(Constants.ClientStatus.ServiceTypeConflict);
            }
            else if (c_OwnerEligible == false)
            {
                clientInstaller.SetParked(Constants.ClientStatus.ServiceOwnerDisabled);
            }
            else if (c_SiteEnabled == false)
            {
                clientInstaller.SetParked(Constants.ClientStatus.ServiceDisabled);
            }
            else
            {
                int resultCode = m_Membership.AuthorizeClient(clientInstaller);
                if (resultCode == 0)
                {
                    m_ServiceGroup.SyncClient(clientInstaller);
                    clientInstaller.SetOnline();
                }
                else if (resultCode == 1)
                {
                    clientInstaller.SetParked(Constants.ClientStatus.AccessDenied);
                }
                else // resultCode == -1
                {
                    if (m_Clients.Remove(client))
                        this.m_EndpointsCount--;
                    clientInstaller.Shutdown(ErrorCodes.RESTART);
                    return;
                }
            }

            m_ClientSyncController.SyncParams(client);
        }

        public void InstallGuestClient(Client client, ClientInstaller clientInstaller)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Completed)
                {
                    clientInstaller.Shutdown(ErrorCodes.RESTART);
                    return;
                }

                Client prevInstance = m_Clients.Find(x => x.Id == client.Id);
                if (prevInstance == null)
                {
                    m_EndpointsCount++;
                }
                else
                {
                    m_Clients.Remove(prevInstance);

                    if (clientInstaller.ChannelRestored)
                    {
                        if (clientInstaller.ChannelId.SequenceEqual(prevInstance.ChannelId))
                        {
                            prevInstance.Close();
                        }
                        else
                        {
                            prevInstance.Shutdown(ErrorCodes.DUPLICATED_CLIENT_KEY_USAGE);
                        }
                    }
                    else
                    {
                        prevInstance.Close();
                    }
                }

                m_Clients.Add(client);

                if (m_State == StateEnum.Running)
                {
                    RunGuestClient(client, clientInstaller);
                }
                else if (m_State == StateEnum.Blank)
                {
                    clientInstaller.SetParked(Constants.ClientStatus.SiteBlank);
                }
                else if (m_State == StateEnum.Initial || m_State == StateEnum.Loading)
                {
                    client.Tmp = clientInstaller;
                }
            }
        }

        void RunGuestClient(Client client, ClientInstaller clientInstaller)
        {
            if (clientInstaller.ServiceType.Equals(c_ServiceType) == false || clientInstaller.ContractAuthor.Equals(c_ContractAuthor) == false)
            {
                clientInstaller.SetParked(Constants.ClientStatus.ServiceTypeConflict);
            }
            else if (c_OwnerEligible == false)
            {
                clientInstaller.SetParked(Constants.ClientStatus.ServiceOwnerDisabled);
            }
            else if (c_SiteEnabled == false)
            {
                clientInstaller.SetParked(Constants.ClientStatus.ServiceDisabled);
            }
            else
            {
                int resultCode = m_Membership.AuthorizeGuest(clientInstaller);
                if (resultCode == 0)
                {
                    m_ServiceGroup.SyncClient(clientInstaller);
                    clientInstaller.SetOnline();
                }
                else if (resultCode == 1)
                {
                    clientInstaller.SetParked(Constants.ClientStatus.AccessDenied);
                }
                else // resultCode == -1
                {
                    if (m_Clients.Remove(client))
                        this.m_EndpointsCount--;
                    clientInstaller.Shutdown(ErrorCodes.RESTART);
                    return;
                }
            }

            m_ClientSyncController.SyncParams(client);
        }

        public void InstallStatelessClient(Client client, ClientInstaller clientInstaller)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Completed)
                {
                    clientInstaller.Shutdown(ErrorCodes.RESTART);
                    return;
                }

                m_EndpointsCount++;
                m_StatelessClients.Add(client);

                if (m_State == StateEnum.Running)
                {
                    RunStatelessClient(client, clientInstaller);
                }
                else if (m_State == StateEnum.Blank)
                {
                    clientInstaller.SetParked(Constants.ClientStatus.SiteBlank);
                }
                else if (m_State == StateEnum.Initial || m_State == StateEnum.Loading)
                {
                    client.Tmp = clientInstaller;
                }
            }            
        }

        void RunStatelessClient(Client client, ClientInstaller clientInstaller)
        {
            if (clientInstaller.ServiceType.Equals(c_ServiceType) == false || clientInstaller.ContractAuthor.Equals(c_ContractAuthor) == false)
            {
                clientInstaller.SetParked(Constants.ClientStatus.ServiceTypeConflict);
            }
            else if (c_OwnerEligible == false)
            {
                clientInstaller.SetParked(Constants.ClientStatus.ServiceOwnerDisabled);
            }
            else if (c_SiteEnabled == false)
            {
                clientInstaller.SetParked(Constants.ClientStatus.ServiceDisabled);
            }
            else
            {
                int resultCode = m_Membership.AuthorizeStatelessGuest(clientInstaller);
                if (resultCode == 0)
                {
                    m_ServiceGroup.SyncClient(clientInstaller);
                    clientInstaller.SetOnline();
                }
                else if (resultCode == 1)
                {
                    clientInstaller.SetParked(Constants.ClientStatus.AccessDenied);
                }
                else // resultCode == -1
                {
                    if (m_StatelessClients.Remove(client))
                        m_EndpointsCount--;
                    clientInstaller.Shutdown(ErrorCodes.RESTART);
                }
            }            
        }

        public void Uninstall(Client client)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Completed)
                    return;
                if (client.UserKind != Constants.UserKind.StatelessGuest)
                {
                    if (m_Clients.Remove(client))
                        m_EndpointsCount--;
                }
                else
                {
                    if (m_StatelessClients.Remove(client))
                        m_EndpointsCount--;
                }
            }
        }        

        public void Install(Service service, ServiceInstaller serviceInstaller)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Completed)
                {
                    serviceInstaller.Shutdown(ErrorCodes.RESTART);
                    return;
                }

                if (!(m_State == StateEnum.Blank && c_SiteEnabled && c_OwnerEligible))
                {
                    Service prevInstance = m_Services.Find(x => x.Id == service.Id);
                    if (prevInstance == null)
                    {
                        m_EndpointsCount++;
                    }
                    else
                    {
                        m_Services.Remove(prevInstance);

                        if (serviceInstaller.ChannelRestored)
                        {
                            if (serviceInstaller.ChannelId.SequenceEqual(prevInstance.ChannelId))
                            {
                                prevInstance.Close();
                            }
                            else
                            {
                                prevInstance.Shutdown(ErrorCodes.DUPLICATED_SERVICE_UID_USAGE);
                            }
                        }
                        else
                        {
                            prevInstance.Close();
                        }
                    }

                    m_Services.Add(service);

                    if (c_SiteKind == Constants.SiteKind.SingleService && m_Services.Count > 1)
                    {
                        this.Remove(ErrorCodes.DATA_INTEGRITY_ERROR);
                        return;
                    }

                    if (m_State == StateEnum.Running)
                    {
                        RunService(service, serviceInstaller);
                        return;
                    }

                    if (m_State == StateEnum.Initial || m_State == StateEnum.Loading)
                    {
                        service.Tmp = serviceInstaller;
                        return;
                    }

                    // m_State == StateEnum.Blank && (c_SiteEnabled == false || c_OwnerEligible == false)                    
                    RunService(service, serviceInstaller);
                    return;
                }

                m_State = StateEnum.Completed;
            }

            int completionCode = ErrorCodes.RESTART;
            try
            {
                if (serviceInstaller.SiteStructure != null)
                {
                    if (serviceInstaller.SiteStructure.rolesSupported())
                        SoftnetRegistry.Site_ConstructRBSite(this.Id, serviceInstaller.SiteStructure, Convert.ToBase64String(serviceInstaller.SSHash));
                    else
                        SoftnetRegistry.Site_ConstructUBSite(this.Id, serviceInstaller.SiteStructure, Convert.ToBase64String(serviceInstaller.SSHash));
                }
                else
                {
                    SSRawData ssRawData = new SSRawData();
                    SoftnetRegistry.Site_GetSSData(serviceInstaller.ServiceId, ssRawData);
                    try
                    {
                        SiteStructure siteStructure = SSXmlDecoder.exec(ssRawData.xml);
                        if (siteStructure.rolesSupported())
                            SoftnetRegistry.Site_ConstructRBSite(this.Id, siteStructure, ssRawData.hash);
                        else
                            SoftnetRegistry.Site_ConstructUBSite(this.Id, siteStructure, ssRawData.hash);
                    }
                    catch (FormatException)
                    {
                        completionCode = ErrorCodes.DATA_INTEGRITY_ERROR;
                    }
                }
            }
            catch (SoftnetException ex)
            {
                completionCode = ex.ErrorCode;
            }
            finally
            {
                serviceInstaller.Shutdown(completionCode);
                Remove(ErrorCodes.RESTART);
            }
        }

        void RunService(Service service, ServiceInstaller serviceInstaller)
        {
            int resultCode = m_ServiceGroup.OnServiceInstalled(serviceInstaller);
            if (resultCode == 0)
            {
                if (m_State == StateEnum.Running)
                {
                    if (serviceInstaller.ServiceType.Equals(c_ServiceType) == false || serviceInstaller.ContractAuthor.Equals(c_ContractAuthor) == false)
                    {
                        serviceInstaller.SetParked(Constants.ServiceStatus.ServiceTypeConflict);
                    }
                    else if (ByteArray.Equals(serviceInstaller.SSHash, c_SSHash) == false)
                    {
                        serviceInstaller.SetParked(Constants.ServiceStatus.SiteStrutureMismatch);
                    }
                    else if (c_OwnerEligible == false)
                    {
                        serviceInstaller.SetParked(Constants.ServiceStatus.OwnerDisabled);                    
                    }
                    else if (c_SiteEnabled == false)
                    {
                        serviceInstaller.SetParked(Constants.ServiceStatus.SiteDisabled);
                    }
                    else
                    {
                        m_Membership.SyncService(serviceInstaller);
                        serviceInstaller.SetOnline();
                        m_ServiceGroup.OnServiceOnline(serviceInstaller);
                    }
                }
                else // m_State == StateEnum.Blank
                {
                    serviceInstaller.SetParked(Constants.ServiceStatus.SiteBlank);
                }

                m_ServiceSyncController.SyncParams(service);
            }
            else if (resultCode == 1) // service disabled
            {
                if (m_State == StateEnum.Running)
                {
                    if (serviceInstaller.ServiceType.Equals(c_ServiceType) == false || serviceInstaller.ContractAuthor.Equals(c_ContractAuthor) == false)
                    {
                        serviceInstaller.SetParked(Constants.ServiceStatus.ServiceTypeConflict);
                    }
                    else if (ByteArray.Equals(serviceInstaller.SSHash, c_SSHash) == false)
                    {
                        serviceInstaller.SetParked(Constants.ServiceStatus.SiteStrutureMismatch);
                    }
                    else
                    {
                        serviceInstaller.SetParked(Constants.ServiceStatus.Disabled);
                    }
                }
                else // m_State == StateEnum.Blank
                {
                    serviceInstaller.SetParked(Constants.ServiceStatus.SiteBlank);
                }

                m_ServiceSyncController.SyncParams(service);
            }
            else // resultCode == -1
            {
                if (m_Services.Remove(service))
                    this.m_EndpointsCount--;
                serviceInstaller.Shutdown(ErrorCodes.SERVICE_NOT_REGISTERED);
            }             
        }
        
        public void Uninstall(Service service)
        {
            lock (mutex)
            {                
                if (m_Services.Remove(service))
                {
                    m_EndpointsCount--;
                    m_ServiceGroup.OnServiceUninstalled(service);
                }
            }
        }

        void ServiceGroup_OnServiceDeleted(long serviceId)
        {
            Service service = m_Services.Find(x => x.Id == serviceId);
            if (service != null)
            {
                if (m_Services.Remove(service))
                {
                    m_EndpointsCount--;
                    service.Shutdown(ErrorCodes.SERVICE_NOT_REGISTERED);
                }
            }
        }

        void ServiceGroup_OnServiceStatusChanged(long serviceId)
        {
            Service service = m_Services.Find(x => x.Id == serviceId);
            if (service != null)
            {
                if (m_Services.Remove(service))
                {
                    m_EndpointsCount--;
                    service.Shutdown(ErrorCodes.RESTART);
                }
            }
        }

        void Membership_OnUserRemoved(long userId)
        {
            for (int i = m_Clients.Count - 1; i >= 0; i--)
            {
                Client client = m_Clients[i];
                if (client.UserId == userId)
                {                    
                    m_Clients.RemoveAt(i);
                    m_EndpointsCount--;
                    client.Shutdown(ErrorCodes.RESTART);
                }
            }
        }

        void Membership_OnUserDeleted(long userId)
        {
            for (int i = m_Clients.Count - 1; i >= 0; i--)
            {
                Client client = m_Clients[i];
                if (client.UserId == userId)
                {
                    m_Clients.RemoveAt(i);
                    m_EndpointsCount--;
                    client.Shutdown(ErrorCodes.CLIENT_NOT_REGISTERED);
                }
            }
        }

        void Membership_OnClientRestarted()
        {
            m_EndpointsCount--;
        }

        void Membership_OnGuestAllowed()
        {
            if (m_Clients.Count > 0)
            {
                for (int i = m_Clients.Count - 1; i >= 0; i--)
                {
                    Client client = m_Clients[i];
                    if (client.UserKind == 4 || client.User == null)
                    {
                        m_Clients.RemoveAt(i);
                        m_EndpointsCount--;
                        client.Shutdown(ErrorCodes.RESTART);
                    }
                }
            }

            if (m_StatelessClients.Count > 0)
            {
                foreach (Client client in m_StatelessClients)
                {
                    m_EndpointsCount--;
                    client.Shutdown(ErrorCodes.RESTART);
                }
                m_StatelessClients.Clear();
            }
        }

        void Membership_OnGuestDenied()
        {            
            if (m_Clients.Count > 0)
            {
                for (int i = m_Clients.Count - 1; i >= 0; i--)
                {
                    Client client = m_Clients[i];
                    if (client.UserKind == 4 || (client.User != null && client.User.authority.isGuest))
                    {
                        m_Clients.RemoveAt(i);
                        m_EndpointsCount--;
                        client.Shutdown(ErrorCodes.RESTART);
                    }
                }                    
            }

            if (m_StatelessClients.Count > 0)
            {
                foreach (Client client in m_StatelessClients)
                {
                    m_EndpointsCount--;
                    client.Shutdown(ErrorCodes.RESTART);
                }
                m_StatelessClients.Clear();
            }
        }

        public void Mgt_ConstructSite(long serviceId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Loading || m_State == StateEnum.Completed)
                    throw new SoftnetException(ErrorCodes.RESTART);
                m_State = StateEnum.Completed;
            }

            try
            {
                SSRawData ssRawData = new SSRawData();
                SoftnetRegistry.Site_GetSSData(serviceId, ssRawData);
                SiteStructure siteStructure = SSXmlDecoder.exec(ssRawData.xml);

                SiteParams siteParams = new SiteParams();
                SoftnetRegistry.Mgt_GetSiteParams(this.Id, siteParams);

                if (siteParams.structured)
                {
                    if (siteStructure.getServiceType().Equals(siteParams.serviceType) && siteStructure.getContractAuthor().Equals(siteParams.ContractAuthor))
                    {
                        if (siteStructure.rolesSupported())
                        {
                            if (siteParams.rolesSupported)
                                SoftnetRegistry.Mgt_ReconstructRBSite(this.Id, siteStructure, ssRawData.hash);
                            else
                                SoftnetRegistry.Mgt_ReconstructRBSite2(this.Id, siteStructure, ssRawData.hash);
                        }
                        else
                            SoftnetRegistry.Mgt_ReconstructUBSite(this.Id, siteStructure, ssRawData.hash);
                    }
                    else
                    {
                        if (siteStructure.rolesSupported())
                            SoftnetRegistry.Mgt_ConstructRBSite(this.Id, siteStructure, ssRawData.hash);
                        else
                            SoftnetRegistry.Mgt_ConstructUBSite(this.Id, siteStructure, ssRawData.hash);
                    }
                }
                else
                {
                    if (siteStructure.rolesSupported())
                        SoftnetRegistry.Mgt_ConstructRBSite(this.Id, siteStructure, ssRawData.hash);
                    else
                        SoftnetRegistry.Mgt_ConstructUBSite(this.Id, siteStructure, ssRawData.hash);
                }
            }
            catch (FormatException)
            {
                throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
            }
            finally
            {
                Remove(ErrorCodes.RESTART);
            }
        }

        public void Mgt_OnServiceCreated(long serviceId, string hostname)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running || m_State == StateEnum.Blank)
                {
                    ((MultiServiceGroup)m_ServiceGroup).OnServiceCreated(serviceId, hostname);
                }
                else if (m_State == StateEnum.Loading)
                { 
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnServiceDeleted(long serviceId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running || m_State == StateEnum.Blank)
                {
                    ((MultiServiceGroup)m_ServiceGroup).OnServiceDeleted(serviceId);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnServiceEnabledStatusChanged(long serviceId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running || m_State == StateEnum.Blank)
                {
                    ((MultiServiceGroup)m_ServiceGroup).OnEnabledStatusChanged(serviceId);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnHostnameChanged(long serviceId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running || m_State == StateEnum.Blank)
                {
                    m_ServiceGroup.OnHostnameChanged(serviceId);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnServicePingPeriodChanged(long serviceId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running || m_State == StateEnum.Blank)
                {
                    Service service = m_Services.Find(x => x.Id == serviceId);
                    if (service != null)
                        m_ServiceSyncController.OnPingPeriodChanged(service);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnClientPingPeriodChanged(long clientId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running || m_State == StateEnum.Blank)
                {
                    Client client = m_Clients.Find(x => x.Id == clientId);
                    if (client != null)
                        m_ClientSyncController.OnPingPeriodChanged(client);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnUserCreated(long userId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running)
                {
                    m_Membership.OnUserUpdated(userId);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnUserUpdated(long userId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running)
                {
                    m_Membership.OnUserUpdated(userId);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnUserDeleted(long userId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running)
                {
                    m_Membership.OnUserDeleted(userId);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnUsersUpdated()
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running)
                {
                    m_Membership.OnUsersUpdated();
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnGuestStatusChanged()
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running)
                {
                    m_Membership.OnGuestStatusChanged();
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnContactDisabled(long contactId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running)
                {
                    m_Membership.OnContactDisabled(contactId);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnContactDeleted(long contactId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running)
                {
                    m_Membership.OnContactDeleted(contactId);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnConsumerDisabled(long consumerId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running)
                {
                    m_Membership.OnConsumerDisabled(consumerId);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_OnConsumerDeleted(long consumerId)
        {
            lock (mutex)
            {
                if (m_State == StateEnum.Running)
                {
                    m_Membership.OnConsumerDeleted(consumerId);
                }
                else if (m_State == StateEnum.Loading)
                {
                    Remove(ErrorCodes.RESTART);
                }
            }
        }

        public void Mgt_RestartService(long serviceId)
        {
            lock (mutex)
            {
                Service service = m_Services.Find(x => x.Id == serviceId);
                if (service != null)
                {
                    if (m_Services.Remove(service))
                    {
                        m_EndpointsCount--;
                        service.Shutdown(ErrorCodes.RESTART);
                    }
                }
            }
        }

        public void Mgt_RestartClient(long clientId)
        {
            lock (mutex)
            {
                Client client = m_Clients.Find(x => x.Id == clientId);
                if (client != null)
                {
                    if (m_Clients.Remove(client))
                    {
                        m_EndpointsCount--;
                        client.Shutdown(ErrorCodes.RESTART);
                    }
                }
            }
        }

        public void Mgt_OnClientDeleted(long clientId)
        {
            lock (mutex)
            {
                Client client = m_Clients.Find(x => x.Id == clientId);
                if (client != null)
                {
                    if (m_Clients.Remove(client))
                    {
                        m_EndpointsCount--;
                        client.Shutdown(ErrorCodes.CLIENT_NOT_REGISTERED);
                    }
                }
            }
        }       
    }
}
