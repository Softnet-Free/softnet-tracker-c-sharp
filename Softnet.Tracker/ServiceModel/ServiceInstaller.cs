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
using System.Xml;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using Softnet.ServerKit;
using Softnet.Tracker.Core;
using Softnet.Tracker.SiteModel;
using Softnet.Asn;

namespace Softnet.Tracker.ServiceModel
{
    public class ServiceInstaller
    {
        public Guid SiteUid;
        public long SiteId;
        public long ServiceId;

        public string ServiceType = "";
        public string ContractAuthor = "";
        public string Version = "";
        public SiteStructure SiteStructure = null;
        public byte[] SSHash = null;
        public bool IsStructureUpdated = false;

        public byte[] ReceivedUserListHash = null;
        public byte[] ReceivedHostnameHash = null;
        public bool ReceivedGuestAllowed = false;

        public bool ChannelRestored;
        public byte[] ChannelId;

        public Guid StorageUid = Guid.Empty;

        IChannel m_Channel;
        Service m_Service;
        Site m_Site;
        ScheduledTask m_TimeoutControlTask;
        public int ErrorCode = 0;

        public ServiceInstaller(IChannel channel)
        {
            m_Channel = channel;
            m_Service = null;
            m_Site = null;
        }

        public void Start()
        {
            m_Channel.Init(this, new Action(OnChannelEstablished));
            m_Channel.SetCompletedCallback(new Action(OnChannelCompleted));
            m_Channel.Start();

            m_TimeoutControlTask = new ScheduledTask(OnTimeoutExpired, null);
            TaskScheduler.Add(m_TimeoutControlTask, 60);
        }

        void Install()
        {
            try
            {
                m_Site = SoftnetTracker.GetSite(this.SiteId);
                m_Service = new Service();
                m_Service.Id = this.ServiceId;
                m_Service.ChannelId = this.ChannelId;
                m_Site.Install(m_Service, this);
            }
            catch (SoftnetException ex)
            {
                m_Channel.Shutdown(ex.ErrorCode);
            }
        }

        public void Send(SoftnetMessage message)
        {
            m_Channel.Send(message);
        }

        public void SetOnline()
        {
            if (m_Site.EventController != null && this.StorageUid != Guid.Empty)
                m_Channel.Send(EncodeMessage_LastStorageUid(this.StorageUid));

            Softnet.ServerKit.Monitor.Add(m_Channel);
            m_Channel.Send(MsgBuilder.Create(Constants.Service.Installer.ModuleId, Constants.Service.Installer.ONLINE));
            m_Channel.RemoveModule(Constants.Service.Installer.ModuleId);
            m_Service.SetOnline(m_Site, m_Channel);
        }

        public void SetParked(int status)
        {
            Softnet.ServerKit.Monitor.Add(m_Channel);
            m_Channel.Send(EncodeMessage_Parked(status));
            m_Channel.RemoveModule(Constants.Service.Installer.ModuleId);
            m_Service.SetParked(status, m_Site, m_Channel);
        }

        void OnChannelEstablished()
        {
            m_Channel.RegisterModule(Constants.Service.Installer.ModuleId, OnMessageReceived);

            if (string.IsNullOrEmpty(this.ServiceType) == false)
            {
                expectedMessage = Constants.Service.Installer.STATE;
                m_Channel.Send(MsgBuilder.Create(Constants.Service.Installer.ModuleId, Constants.Service.Installer.GET_STATE));
            }
            else
            {
                expectedMessage = Constants.Service.Installer.SITE_STRUCTURE;
                m_Channel.Send(MsgBuilder.Create(Constants.Service.Installer.ModuleId, Constants.Service.Installer.GET_SITE_STRUCTURE));
            }
        }

        public void Shutdown(int errorCode)
        {
            m_Channel.Shutdown(errorCode);
        }

        void OnChannelCompleted()
        {
            m_TimeoutControlTask.Cancel();
            if (m_Site != null)
                m_Site.Uninstall(m_Service);
        }

        void OnTimeoutExpired(object noData)
        {
            m_Channel.Close();
        }

        void ProcessMessage_State(byte[] message)
        {
            SequenceDecoder asnRootDecoder = ASNDecoder.Create(message, 2);

            SequenceDecoder asnSiteProfile = asnRootDecoder.Sequence();
            string receivedServiceType = asnSiteProfile.IA5String(1, 256);
            string receivedContractAuthor = asnSiteProfile.IA5String(1, 256);

            if (!(receivedServiceType.Equals(this.ServiceType) && receivedContractAuthor.Equals(this.ContractAuthor)))
            {
                expectedMessage = Constants.Service.Installer.SITE_STRUCTURE;
                m_Channel.Send(MsgBuilder.Create(Constants.Service.Installer.ModuleId, Constants.Service.Installer.GET_SITE_STRUCTURE));
                return;
            }

            byte[] receivedSSHash = asnSiteProfile.OctetString(20);

            if (ByteArray.Equals(receivedSSHash, this.SSHash) == false)
            {
                expectedMessage = Constants.Service.Installer.SITE_STRUCTURE;
                m_Channel.Send(MsgBuilder.Create(Constants.Service.Installer.ModuleId, Constants.Service.Installer.GET_SITE_STRUCTURE));
                return;
            }

            if (m_TimeoutControlTask.Cancel() == false)
                return;

            string receivedVersion = "";
            if (asnRootDecoder.Exists(1))
                receivedVersion = asnRootDecoder.IA5String(1, 64);

            SequenceDecoder asnMembershipState = asnRootDecoder.Sequence();
            if (asnMembershipState.Exists(1))
                this.ReceivedUserListHash = asnMembershipState.OctetString(20);

            if (asnMembershipState.Exists(2))
                this.ReceivedGuestAllowed = asnMembershipState.Boolean();
            asnMembershipState.End();

            if (asnRootDecoder.Exists(2))
                this.ReceivedHostnameHash = asnRootDecoder.OctetString(4);
            asnRootDecoder.End();

            if (receivedVersion.Equals(this.Version) == false)
            {
                SoftnetRegistry.Service_UpdateVersion(this.ServiceId, receivedVersion);
                this.Version = receivedVersion;
            }

            Install();
        }

        void ProcessMessage_SiteStructure(byte[] message)
        {
            if (m_TimeoutControlTask.Cancel() == false)
                return;

            SSDataset ssDataset = new SSDataset();

            SequenceDecoder asnData = ASNDecoder.Create(message, 2);
            SequenceDecoder asnSiteStructure = asnData.Sequence();            
            ssDataset.serviceType = asnSiteStructure.IA5String(1, 256);
            ssDataset.contractAuthor = asnSiteStructure.IA5String(1, 256);
            ssDataset.guestSupport = asnSiteStructure.Int32(0, 2);

            if (asnSiteStructure.Exists(1))
            {
                ssDataset.roles = new List<string>();
                SequenceDecoder asnRoles = asnSiteStructure.Sequence();
                SequenceDecoder asnRoleNames = asnRoles.Sequence();
                while (asnRoleNames.HasNext())
                    ssDataset.roles.Add(asnRoleNames.IA5String(1, 256));
                asnRoleNames.End();
                if (asnRoles.Exists(1))
                    ssDataset.ownerRole = asnRoles.IA5String(1, 256);
                asnRoles.End();
            }

            if (asnSiteStructure.Exists(2))
            {
                SequenceDecoder asnEventsDefinition = asnSiteStructure.Sequence();
                if (asnEventsDefinition.Exists(1))
                {
                    ssDataset.REvents = new List<REvent>();
                    SequenceDecoder asnReplacingEvents = asnEventsDefinition.Sequence();
                    
                    while (asnReplacingEvents.HasNext())
                    {
                        SequenceDecoder asnEvent = asnReplacingEvents.Sequence();
                        string eventName = asnEvent.IA5String(1, 256);                        

                        if (asnEvent.Exists(1))
                        {
                            int guestAccess = asnEvent.Int32(1, 2);
                            ssDataset.REvents.Add(new REvent(eventName, guestAccess));
                        }
                        else if (asnEvent.Exists(2))
                        {
                            List<string> roles = new List<string>();
                            SequenceDecoder asnRoles = asnEvent.Sequence();
                            while (asnRoles.HasNext())
                            {
                                string roleName = asnRoles.IA5String(1, 256);                                
                                roles.Add(roleName);
                            }
                            asnRoles.End();
                            ssDataset.REvents.Add(new REvent(eventName, roles));
                        }
                        else
                            ssDataset.REvents.Add(new REvent(eventName));
                        asnEvent.End();
                    }
                    asnReplacingEvents.End();
                }

                if (asnEventsDefinition.Exists(2))
                {
                    ssDataset.QEvents = new List<QEvent>();
                    SequenceDecoder asnQueueingEvents = asnEventsDefinition.Sequence();
                    
                    while (asnQueueingEvents.HasNext())
                    {
                        SequenceDecoder asnEvent = asnQueueingEvents.Sequence();
                        string eventName = asnEvent.IA5String(1, 256);
                        int lifeTime = asnEvent.Int32(60, 2592000);
                        int queueSize = asnEvent.Int32(1, 1000);

                        if (asnEvent.Exists(1))
                        {
                            int guestAccess = asnEvent.Int32(1, 2);
                            ssDataset.QEvents.Add(new QEvent(eventName, lifeTime, queueSize, guestAccess));
                        }
                        else if (asnEvent.Exists(2))
                        {
                            List<string> roles = new List<string>();
                            SequenceDecoder asnRoles = asnEvent.Sequence();
                            while (asnRoles.HasNext())
                                roles.Add(asnRoles.IA5String(1, 256));
                            asnRoles.End();
                            ssDataset.QEvents.Add(new QEvent(eventName, lifeTime, queueSize, roles));
                        }
                        else
                            ssDataset.QEvents.Add(new QEvent(eventName, lifeTime, queueSize));
                        asnEvent.End();
                    }
                    asnQueueingEvents.End();
                }

                if (asnEventsDefinition.Exists(4))
                {
                    ssDataset.PEvents = new List<PEvent>();
                    SequenceDecoder asnPrivateEvents = asnEventsDefinition.Sequence();
                    while (asnPrivateEvents.HasNext())
                    {
                        SequenceDecoder asnEvent = asnPrivateEvents.Sequence();
                        string eventName = asnEvent.IA5String(1, 256);
                        int lifeTime = asnEvent.Int32(60, 2592000);
                        ssDataset.PEvents.Add(new PEvent(eventName, lifeTime));
                    }
                    asnPrivateEvents.End();
                }
                asnEventsDefinition.End();
            }
            asnSiteStructure.End();

            if (asnData.Exists(1))
                this.Version = asnData.IA5String(1, 64);
            asnData.End();

            if (ssDataset.roles != null && ssDataset.roles.Count > Constants.SiteStructure_MaxUserRoles)
                throw new SoftnetException(ErrorCodes.CONSTRAINT_VIOLATION);

            int events_count = 0;
            if (ssDataset.REvents != null)
                events_count = ssDataset.REvents.Count;
            if (ssDataset.QEvents != null)
                events_count += ssDataset.QEvents.Count;
            if (ssDataset.PEvents != null)
                events_count += ssDataset.PEvents.Count;
            if (events_count > Constants.SiteStructure_MaxAppEvents)
                throw new SoftnetException(ErrorCodes.CONSTRAINT_VIOLATION);

            this.ServiceType = ssDataset.serviceType;
            this.ContractAuthor = ssDataset.contractAuthor;

            SiteStructure siteStructure = new SiteStructure(ssDataset);
            byte[] ssHash = SSHashBuilder.exec(siteStructure);

            this.SiteStructure = siteStructure;
            this.SSHash = ssHash;
            SoftnetRegistry.Service_SaveStructure(this);

            this.IsStructureUpdated = true;
            Install();
        }
       
        SoftnetMessage EncodeMessage_Parked(int endpointStatus)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var sequence = asnEncoder.Sequence;
            sequence.Int32(endpointStatus);
            return MsgBuilder.Create(Constants.Service.Installer.ModuleId, Constants.Service.Installer.PARKED, asnEncoder);
        }

        SoftnetMessage EncodeMessage_LastStorageUid(Guid value)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var sequence = asnEncoder.Sequence;
            sequence.OctetString(ByteConverter.GetBytes(value));
            return MsgBuilder.Create(Constants.Service.EventController.ModuleId, Constants.Service.EventController.LAST_STORAGE_UID, asnEncoder);
        }

        int expectedMessage = -1;

        void OnMessageReceived(byte[] message)
        {            
            int messageTag = message[1];
            if (messageTag == expectedMessage)
            {
                if (messageTag == Constants.Service.Installer.STATE)
                {
                    ProcessMessage_State(message);
                    return;
                }

                if (messageTag == Constants.Service.Installer.SITE_STRUCTURE)
                {
                    ProcessMessage_SiteStructure(message);
                    return;
                }
            }
            else
            {
                m_Channel.Shutdown(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR);
                expectedMessage = -1;
            }
        }
    }
}
