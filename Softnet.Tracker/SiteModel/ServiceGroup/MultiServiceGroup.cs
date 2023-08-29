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
using System.Threading;

using Softnet.Asn;
using Softnet.ServerKit;
using Softnet.Tracker.Core;
using Softnet.Tracker.ClientModel;
using Softnet.Tracker.ServiceModel;

namespace Softnet.Tracker.SiteModel
{
    public class MultiServiceGroup : IServiceGroup
    {
        private MultiServiceGroup() { }
        
        public static MultiServiceGroup Create(Site site, List<SGItem> sgItems, List<Service> services, List<Client> clients, List<Client> statelessClients, Action<long> serviceDeletedCallback, Action<long> serviceDisabledCallback)
        {
            var instance = new MultiServiceGroup();
            instance.m_Site = site;
            instance.site_mutex = site.mutex;
            instance.m_SGItems = sgItems;
            instance.m_Services = services;
            instance.m_Clients = clients;
            instance.m_StatelessClients = statelessClients;
            instance.ServiceDeletedCallback = serviceDeletedCallback;
            instance.ServiceStatusChangedCallback = serviceDisabledCallback;
            instance.c_ClientsSupported = true;
            instance.ComputeHash();
            return instance;
        }

        public static MultiServiceGroup Create(Site site, List<SGItem> sgItems, List<Service> services, Action<long> serviceDeletedCallback, Action<long> serviceDisabledCallback)
        {
            var instance = new MultiServiceGroup();
            instance.m_Site = site;
            instance.site_mutex = site.mutex;
            instance.m_Services = services;
            instance.m_SGItems = sgItems;
            instance.ServiceDeletedCallback = serviceDeletedCallback;
            instance.ServiceStatusChangedCallback = serviceDisabledCallback;
            instance.c_ClientsSupported = false;
            return instance;
        }

        Site m_Site;
        bool c_ClientsSupported;

        object site_mutex;
        List<Service> m_Services;
        List<Client> m_Clients;
        List<Client> m_StatelessClients;
        List<SGItem> m_SGItems;

        byte[] m_Hash;
        Action<long> ServiceDeletedCallback;
        Action<long> ServiceStatusChangedCallback;

        public int OnServiceInstalled(ServiceModel.ServiceInstaller serviceInstaller)
        {
            SGItem item = m_SGItems.Find(x => x.serviceId == serviceInstaller.ServiceId);
            if (item == null)
                return -1;

            byte[] hostnameHash = null;
            if (string.IsNullOrEmpty(item.hostname) == false)
            {
                byte[] nameBytes = Encoding.ASCII.GetBytes(item.hostname);
                hostnameHash = ByteConverter.GetBytes(Fnv1a.Get32BitHash(nameBytes));
            }

            if (ByteArray.Equals(serviceInstaller.ReceivedHostnameHash, hostnameHash) == false)
                serviceInstaller.Send(EncodeServiceMessage_HostnameChanged(item));

            if (item.enabled == false)
                return 1;
            return 0;
        }

        public void OnServiceOnline(ServiceModel.ServiceInstaller serviceInstaller)
        {
            SGItem item = m_SGItems.Find(x => x.serviceId == serviceInstaller.ServiceId);
            if (item != null)
            {
                if (serviceInstaller.Version.Equals(item.version))
                {
                    BroadcastMessage(EncodeMessage_ServiceOnline(item));
                }
                else
                {
                    item.version = serviceInstaller.Version;
                    BroadcastMessage(EncodeMessage_ServiceOnline2(item));
                }
            }
        }
        
        public void SyncClient(ClientInstaller clientInstaller)
        {
            if (ByteArray.Equals(clientInstaller.ReceivedServiceGroupHash, m_Hash) == false)
                clientInstaller.Send(EncodeMessage_ServicesUpdated());

            SoftnetMessage message = EncodeMessage_ServicesOnline();
            if (message != null)
                clientInstaller.Send(message);
        }

        public void OnServiceUninstalled(Service service)
        {
            if (c_ClientsSupported && service.Online)
            {
                BroadcastMessage(EncodeMessage_ServiceOffline(service));
            }
        }

        public void OnServiceCreated(long serviceId, string hostname)
        {
            SGItem item = new SGItem(serviceId, hostname);
            m_SGItems.Add(item);

            if (c_ClientsSupported)
            {
                ComputeHash();
                BroadcastMessage(EncodeMessage_ServiceIncluded(item));
            }
        }

        public void OnServiceDeleted(long serviceId)
        {
            SGItem item = m_SGItems.Find(x => x.serviceId == serviceId);
            if (item == null)
                return;

            m_SGItems.Remove(item);
            item.hostname_state = 0;
            item.enabled_state = 0;

            ServiceDeletedCallback(item.serviceId);

            if (c_ClientsSupported)
            {
                ComputeHash();
                BroadcastMessage(EncodeMessage_ServiceRemoved(item));
            }
        }

        public void OnHostnameChanged(long serviceId)
        {
            SGItem item = m_SGItems.Find(x => x.serviceId == serviceId);
            if (item == null)
                return;

            if (item.hostname_state == 0)
            {
                item.hostname_state = 1;
                ThreadPool.QueueUserWorkItem(delegate { ResetHostname(item); });
            }
            else
                item.hostname_state = 2;
        }

        public void OnEnabledStatusChanged(long serviceId)
        {
            SGItem item = m_SGItems.Find(x => x.serviceId == serviceId);
            if (item == null)
                return;

            if (item.enabled_state == 0)
            {
                item.enabled_state = 1;
                ThreadPool.QueueUserWorkItem(delegate { ResetEnabledStatus(item); });
            }
            else
                item.enabled_state = 2;
        }

        void ResetHostname(SGItem item)
        {
            try
            {
                SGItemData itemData = new SGItemData();
                SoftnetRegistry.ServiceGroup_GetItemData(item.serviceId, itemData);

                lock (site_mutex)
                {
                    if (item.hostname_state == 1)
                    {
                        item.hostname = itemData.hostname;
                        if (c_ClientsSupported && item.enabled)
                        {
                            ComputeHash();
                            BroadcastMessage(EncodeMessage_ServiceUpdated(item));
                        }

                        Service service = m_Services.Find(x => x.Id == item.serviceId);
                        if (service != null && service.Status != Constants.ServiceStatus.Offline)
                            service.Send(EncodeServiceMessage_HostnameChanged(item));

                        item.hostname_state = 0;
                    }
                    else if (item.hostname_state == 2)
                    {
                        item.hostname_state = 1;
                        ThreadPool.QueueUserWorkItem(delegate { ResetHostname(item); });
                    }
                }
            }
            catch (SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }
        }

        void ResetEnabledStatus(SGItem item)
        {
            try
            {
                SGItemData itemData = new SGItemData();
                SoftnetRegistry.ServiceGroup_GetItemData(item.serviceId, itemData);
                
                lock (site_mutex)
                {
                    if (item.enabled_state == 1)
                    {
                        if (item.enabled != itemData.enabled)
                        {
                            item.enabled = itemData.enabled;
                            ServiceStatusChangedCallback(item.serviceId);

                            if (c_ClientsSupported)
                            {
                                ComputeHash();
                                if (item.enabled)
                                {
                                    BroadcastMessage(EncodeMessage_ServiceIncluded(item));
                                }
                                else
                                {
                                    BroadcastMessage(EncodeMessage_ServiceRemoved(item));                                        
                                }
                            }                                
                        }

                        item.enabled_state = 0;
                    }
                    else if (item.enabled_state == 2)
                    {
                        item.enabled_state = 1;
                        ThreadPool.QueueUserWorkItem(delegate { ResetEnabledStatus(item); });
                    }
                }
            }
            catch (SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }        
        }

        void BroadcastMessage(SoftnetMessage message)
        {
            foreach (Client client in m_Clients)
                if (client.Online)
                    client.Send(message);
            foreach (Client client in m_StatelessClients)
                if (client.Online)
                    client.Send(message);
        }

        void ComputeHash()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;

            m_SGItems.Sort();
            foreach (SGItem item in m_SGItems)
            {
                if (item.enabled)
                {
                    SequenceEncoder asnService = asnRootSequence.Sequence();
                    asnService.Int64(item.serviceId);
                    asnService.IA5String(item.hostname);
                    asnService.IA5String(item.version);
                }
            }

            if (asnRootSequence.Count > 0)
                m_Hash = SHA1Hash.Compute(asnEncoder.GetEncoding());
            else
                m_Hash = null;
        }

        SoftnetMessage EncodeMessage_ServicesUpdated()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            foreach (SGItem item in m_SGItems)
            {
                if (item.enabled)
                {
                    SequenceEncoder asnService = asnRootSequence.Sequence();
                    asnService.Int64(item.serviceId);
                    asnService.IA5String(item.hostname);
                    asnService.IA5String(item.version);
                }
            }
            return MsgBuilder.Create(Constants.Client.MultiServiceGroup.ModuleId, Constants.Client.MultiServiceGroup.SERVICES_UPDATED, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ServicesOnline()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;

            foreach (Service service in m_Services)
            {
                if (service.Online)
                    asnRootSequence.Int64(service.Id);
            }

            if (asnRootSequence.Count == 0)
                return null;
            return MsgBuilder.Create(Constants.Client.MultiServiceGroup.ModuleId, Constants.Client.MultiServiceGroup.SERVICES_ONLINE, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ServiceIncluded(SGItem item)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.Int64(item.serviceId);
            asnRootSequence.IA5String(item.hostname);
            asnRootSequence.IA5String(item.version);
            return MsgBuilder.Create(Constants.Client.MultiServiceGroup.ModuleId, Constants.Client.MultiServiceGroup.SERVICE_INCLUDED, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ServiceRemoved(SGItem item)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.Int64(item.serviceId);
            return MsgBuilder.Create(Constants.Client.MultiServiceGroup.ModuleId, Constants.Client.MultiServiceGroup.SERVICE_REMOVED, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ServiceUpdated(SGItem item)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.Int64(item.serviceId);
            asnRootSequence.IA5String(item.version);
            asnRootSequence.IA5String(item.hostname);
            return MsgBuilder.Create(Constants.Client.MultiServiceGroup.ModuleId, Constants.Client.MultiServiceGroup.SERVICE_UPDATED, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ServiceOnline(SGItem item)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.Int64(item.serviceId);
            return MsgBuilder.Create(Constants.Client.MultiServiceGroup.ModuleId, Constants.Client.MultiServiceGroup.SERVICE_ONLINE, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ServiceOnline2(SGItem item)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.Int64(item.serviceId);
            asnRootSequence.IA5String(item.version);
            return MsgBuilder.Create(Constants.Client.MultiServiceGroup.ModuleId, Constants.Client.MultiServiceGroup.SERVICE_ONLINE_2, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ServiceOffline(Service service)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.Int64(service.Id);
            return MsgBuilder.Create(Constants.Client.MultiServiceGroup.ModuleId, Constants.Client.MultiServiceGroup.SERVICE_OFFLINE, asnEncoder);
        }

        SoftnetMessage EncodeServiceMessage_HostnameChanged(SGItem item)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var sequence = asnEncoder.Sequence;
            sequence.IA5String(item.hostname);
            return MsgBuilder.Create(Constants.Service.SyncController.ModuleId, Constants.Service.SyncController.HOSTNAME_CHANGED, asnEncoder);
        }
    }
}
