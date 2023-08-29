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
    public class SingleServiceGroup : IServiceGroup
    {
        private SingleServiceGroup() { }

        public static SingleServiceGroup Create(Site site, SGItem sgItem, List<Service> services, List<Client> clients, List<Client> statelessClients)
        {
            var instance = new SingleServiceGroup();
            instance.m_Site = site;
            instance.site_mutex = site.mutex;
            instance.m_SGItem = sgItem;
            instance.m_Services = services;
            instance.m_Clients = clients;
            instance.m_StatelessClients = statelessClients;
            instance.c_ClientsSupported = true;
            instance.ComputeHash();
            return instance;
        }

        public static SingleServiceGroup Create(Site site, SGItem sgItem, List<Service> services)
        {
            var instance = new SingleServiceGroup();
            instance.m_Site = site;
            instance.site_mutex = site.mutex;
            instance.m_Services = services;
            instance.m_SGItem = sgItem;
            instance.c_ClientsSupported = false;
            return instance;
        }

        Site m_Site;
        bool c_ClientsSupported;

        object site_mutex;
        List<Service> m_Services;
        List<Client> m_Clients;
        List<Client> m_StatelessClients;
        SGItem m_SGItem;

        byte[] m_Hash;

        public int OnServiceInstalled(ServiceModel.ServiceInstaller serviceInstaller)
        {
            byte[] hostnameHash = null;
            if (string.IsNullOrEmpty(m_SGItem.hostname) == false)
            {
                byte[] nameBytes = Encoding.ASCII.GetBytes(m_SGItem.hostname);
                hostnameHash = ByteConverter.GetBytes(Fnv1a.Get32BitHash(nameBytes));
            }

            if (ByteArray.Equals(serviceInstaller.ReceivedHostnameHash, hostnameHash) == false)
                serviceInstaller.Send(EncodeServiceMessage_HostnameChanged());

            return 0;
        }

        public void OnServiceOnline(ServiceInstaller serviceInstaller)
        {
            if (serviceInstaller.Version.Equals(m_SGItem.version))
                BroadcastMessage(EncodeMessage_ServiceOnline());
            else
            {
                m_SGItem.version = serviceInstaller.Version;
                BroadcastMessage(EncodeMessage_ServiceOnline2());
            }
        }

        public void SyncClient(ClientInstaller clientInstaller)
        {
            if (ByteArray.Equals(clientInstaller.ReceivedServiceGroupHash, m_Hash) == false)
                clientInstaller.Send(EncodeMessage_ServiceUpdated());

            if (m_Services.Count == 1 && m_Services[0].Online)
                clientInstaller.Send(EncodeMessage_ServiceOnline());
        }

        public void OnServiceUninstalled(ServiceModel.Service service)
        {
            if (c_ClientsSupported && service.Online)
                BroadcastMessage(EncodeMessage_ServiceOffline());
        }

        public void OnHostnameChanged(long serviceId)
        {
            if (m_SGItem.hostname_state == 0)
            {
                m_SGItem.hostname_state = 1;
                ThreadPool.QueueUserWorkItem(delegate { ResetHostname(); });
            }
            else
                m_SGItem.hostname_state = 2;
        }

        void ResetHostname()
        {
            try
            {
                SGItemData itemData = new SGItemData();
                SoftnetRegistry.ServiceGroup_GetItemData(m_SGItem.serviceId, itemData);
                
                lock (site_mutex)
                {
                    if (m_SGItem.hostname_state == 1)
                    {
                        m_SGItem.hostname = itemData.hostname;

                        if (c_ClientsSupported)
                        {
                            ComputeHash();
                            BroadcastMessage(EncodeMessage_ServiceUpdated());
                        }

                        if (m_Services.Count == 1 && m_Services[0].Status != Constants.ServiceStatus.Offline)
                            m_Services[0].Send(EncodeServiceMessage_HostnameChanged());

                        m_SGItem.hostname_state = 0;
                    }
                    else if (m_SGItem.hostname_state == 2)
                    {
                        m_SGItem.hostname_state = 1;
                        ThreadPool.QueueUserWorkItem(delegate { ResetHostname(); });
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
                if(client.Online)
                    client.Send(message);
            foreach (Client client in m_StatelessClients)
                if (client.Online)
                    client.Send(message);
        }

        void ComputeHash()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.IA5String(m_SGItem.hostname);
            asnRootSequence.IA5String(m_SGItem.version);
            m_Hash = SHA1Hash.Compute(asnEncoder.GetEncoding());
        }

        SoftnetMessage EncodeMessage_ServiceUpdated()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.IA5String(m_SGItem.version);
            asnRootSequence.IA5String(m_SGItem.hostname);
            return MsgBuilder.Create(Constants.Client.SingleServiceGroup.ModuleId, Constants.Client.SingleServiceGroup.SERVICE_UPDATED, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ServiceOnline()
        {
            return MsgBuilder.Create(Constants.Client.SingleServiceGroup.ModuleId, Constants.Client.SingleServiceGroup.SERVICE_ONLINE);
        }

        SoftnetMessage EncodeMessage_ServiceOnline2()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.IA5String(m_SGItem.version);
            return MsgBuilder.Create(Constants.Client.SingleServiceGroup.ModuleId, Constants.Client.SingleServiceGroup.SERVICE_ONLINE_2, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ServiceOffline()
        {
            return MsgBuilder.Create(Constants.Client.SingleServiceGroup.ModuleId, Constants.Client.SingleServiceGroup.SERVICE_OFFLINE);
        }

        SoftnetMessage EncodeServiceMessage_HostnameChanged()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var sequence = asnEncoder.Sequence;
            sequence.IA5String(m_SGItem.hostname);
            return MsgBuilder.Create(Constants.Service.SyncController.ModuleId, Constants.Service.SyncController.HOSTNAME_CHANGED, asnEncoder);
        }
    }
}
