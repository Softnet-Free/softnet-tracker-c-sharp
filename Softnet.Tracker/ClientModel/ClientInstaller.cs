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
using System.Net;
using System.Net.Sockets;

using Softnet.ServerKit;
using Softnet.Tracker.Core;
using Softnet.Tracker.SiteModel;
using Softnet.Asn;

namespace Softnet.Tracker.ClientModel
{
    public class ClientInstaller
    {
        public long SiteId;
        public long ClientId;
        public string ClientKey;
        public long UserId;
        public int UserKind;
        public int SiteKind;

        public string ServiceType = "";
        public string ContractAuthor = "";
        public string ClientDescription = "";

        public byte[] ReceivedServiceGroupHash = null;
        public byte[] ReceivedUserHash = null;

        public bool ChannelRestored;
        public byte[] ChannelId;

        public int ErrorCode;

        Site m_Site; 
        Client m_Client;
        IChannel m_Channel;
        ScheduledTask m_TimeoutControlTask;

        public ClientInstaller(IChannel channel)
        { 
            m_Channel = channel;
            m_Site = null;
            m_Client = null;
        }

        public void Start()
        {
            m_Channel.Init(this, new Action(OnChannelEstablished));
            m_Channel.SetCompletedCallback(new Action(OnChannelCompleted));
            m_Channel.Start();

            m_TimeoutControlTask = new ScheduledTask(OnTimeoutExpired, null);
            TaskScheduler.Add(m_TimeoutControlTask, 60);
        }

        void OnChannelEstablished()
        {
            m_Channel.RegisterModule(Constants.Client.Installer.ModuleId, OnMessageReceived);
            m_Channel.Send(MsgBuilder.Create(Constants.Client.Installer.ModuleId, Constants.Client.Installer.GET_STATE));
        }

        static long s_LastStatelessClientId = 0;

        void Install()
        {
            try
            {
                m_Site = SoftnetTracker.GetSite(this.SiteId);
                
                m_Client = new Client();
                m_Client.UserId = this.UserId;
                m_Client.UserKind = this.UserKind;

                if (this.UserKind == Constants.UserKind.StatelessGuest)
                {
                    m_Client.Id = System.Threading.Interlocked.Increment(ref s_LastStatelessClientId);
                    m_Site.InstallStatelessClient(m_Client, this);
                }
                else if (this.UserKind == Constants.UserKind.Guest)
                {
                    m_Client.Id = this.ClientId;
                    m_Client.ChannelId = this.ChannelId;
                    m_Site.InstallGuestClient(m_Client, this);
                }
                else
                {
                    m_Client.Id = this.ClientId;
                    m_Client.ChannelId = this.ChannelId;
                    m_Site.InstallClient(m_Client, this);
                }
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

        public void SetUser(MUser user, List<MRole> siteRoles)
        {
            m_Client.SetUser(user);
            
            if (ByteArray.Equals(this.ReceivedUserHash, user.hash) == false)
                m_Channel.Send(EncodeMessage_User(user, siteRoles));
        }

        public void SetUser(MUser user)
        {
            m_Client.SetUser(user);

            if (this.UserKind == Constants.UserKind.Guest || this.UserKind == Constants.UserKind.StatelessGuest)
                return;

            if (ByteArray.Equals(this.ReceivedUserHash, user.hash) == false)
            {
                if (user.authority.isGuest)
                    m_Channel.Send(MsgBuilder.Create(Constants.Client.Membership.ModuleId, Constants.Client.Membership.GUEST));
                else
                    m_Channel.Send(EncodeMessage_User(user));
            }
        }        

        public void SetOnline()
        {
            m_Channel.Send(EncodeMessage_Online());
            m_Channel.RemoveModule(Constants.Client.Installer.ModuleId);
            m_Client.SetOnline(m_Site, m_Channel);
        }

        public void SetParked(int status)
        {
            m_Channel.Send(EncodeMessage_Parked(status));
            m_Channel.RemoveModule(Constants.Client.Installer.ModuleId);
            m_Client.SetParked(status, m_Site, m_Channel);
        }

        public void Shutdown(int errorCode)
        {
            m_Channel.Shutdown(errorCode);
        }

        void OnChannelCompleted()
        {
            m_TimeoutControlTask.Cancel();
            if (m_Site != null)
                m_Site.Uninstall(m_Client);
        }

        void OnTimeoutExpired(object noData)
        {
            m_Channel.Close();
        }

        void ProcessMessage_State(byte[] message)
        {
            if (m_TimeoutControlTask.Cancel() == false)
                return;

            SequenceDecoder asnRootSequence = ASNDecoder.Sequence(message, 2);
            string receivedServiceType = asnRootSequence.IA5String(1, 256);
            string receivedContractAuthor = asnRootSequence.IA5String(1, 256);
            string receivedClientDescription = "";
            if (asnRootSequence.Exists(1))
                receivedClientDescription = asnRootSequence.IA5String(1, 256);            
            if (asnRootSequence.Exists(2))
                this.ReceivedServiceGroupHash = asnRootSequence.OctetString(20);
            if (asnRootSequence.Exists(3))
                this.ReceivedUserHash = asnRootSequence.OctetString(20);            
            asnRootSequence.End();

            if (this.UserKind != Constants.UserKind.StatelessGuest)
            {
                if (receivedServiceType.Equals(this.ServiceType) == false ||
                    receivedContractAuthor.Equals(this.ContractAuthor) == false ||
                    receivedClientDescription.Equals(this.ClientDescription) == false)
                {
                    this.ServiceType = receivedServiceType;
                    this.ContractAuthor = receivedContractAuthor;
                    this.ClientDescription = receivedClientDescription;

                    SoftnetRegistry.Client_SaveSoftwareProps(this);
                }
            }
            else
            {
                this.ServiceType = receivedServiceType;
                this.ContractAuthor = receivedContractAuthor;
            }

            Install();
        }

        SoftnetMessage EncodeMessage_Online()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var sequence = asnEncoder.Sequence;
            sequence.OctetString(m_Site.Uid);
            return MsgBuilder.Create(Constants.Client.Installer.ModuleId, Constants.Client.Installer.ONLINE, asnEncoder);
        }

        SoftnetMessage EncodeMessage_Parked(int endpointStatus)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var sequence = asnEncoder.Sequence;
            sequence.OctetString(m_Site.Uid);
            sequence.Int32(endpointStatus);
            return MsgBuilder.Create(Constants.Client.Installer.ModuleId, Constants.Client.Installer.PARKED, asnEncoder);
        }

        SoftnetMessage EncodeMessage_User(MUser user)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.Int64(user.id);
            asnSequence.IA5String(user.name);
            return MsgBuilder.Create(Constants.Client.Membership.ModuleId, Constants.Client.Membership.USER, asnEncoder);
        }

        SoftnetMessage EncodeMessage_User(MUser user, List<MRole> siteRoles)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.Int64(user.id);
            asnSequence.IA5String(user.name);
            SequenceEncoder asnRoles = asnSequence.Sequence(1);
            foreach (long roleId in user.authority.roles)
            {
                MRole mRole = siteRoles.Find(x => x.id == roleId);
                asnRoles.IA5String(mRole.name);
            }
            return MsgBuilder.Create(Constants.Client.Membership.ModuleId, Constants.Client.Membership.USER, asnEncoder);
        }

        void OnMessageReceived(byte[] message)
        {
            if (message[1] == Constants.Client.Installer.STATE)
            {
                ProcessMessage_State(message);
            }
            else
            {
                m_Channel.Shutdown(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR);
            }
        }
    }
}
