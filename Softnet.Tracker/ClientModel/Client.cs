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
using System.Net;
using System.Net.Sockets;

using Softnet.Asn;
using Softnet.Tracker.Core;
using Softnet.Tracker.SiteModel;
using Softnet.ServerKit;

namespace Softnet.Tracker.ClientModel
{
    public class Client : Softnet.ServerKit.Monitorable
    {
        public long Id;
        public long UserId;
        public int UserKind;
        public MUser User;        
        public bool Online;
        public int Status;
        public byte[] ChannelId;

        public ClientSyncToken SyncToken;
        public object Tmp;        

        public Client()
        {
            Online = false;
            Status = Constants.ClientStatus.Offline;
            m_Site = null;
            m_Channel = null;
            TcpController = null;
            UdpController = null;
            RpcController = null;
            EventController = null;
            User = null;
            Tmp = null;
            SyncToken = null;
        }

        public void SetOnline(Site site, IChannel channel)
        {
            Online = true;
            Status = Constants.ClientStatus.Online;

            m_Site = site;
            m_Channel = channel;
            m_Channel.SetCompletedCallback(new Action(OnChannelCompleted));

            TcpController = new TCPController();
            TcpController.Init(site, this, channel);

            UdpController = new UDPController();
            UdpController.Init(site, this, channel);

            RpcController = new RPCController();
            RpcController.Init(site, this, channel);

            if (m_Site.EventController != null)
            {
                if (this.UserKind != Constants.UserKind.StatelessGuest)
                {
                    var eventController = new SFEventController();
                    this.EventController = eventController;
                    eventController.Init(m_Site, this, m_Channel);
                }
                else
                {
                    var eventController = new SLEventController();
                    this.EventController = eventController;
                    eventController.Init(m_Site, this, m_Channel);                
                }
            }

            Softnet.ServerKit.Monitor.Add(this);
        }

        public void SetParked(int status, Site site, IChannel channel)
        {
            Status = status;
            m_Site = site;
            m_Channel = channel;
            m_Channel.SetCompletedCallback(new Action(OnChannelCompleted));
            Softnet.ServerKit.Monitor.Add(this);
        }

        public bool IsAlive(long currentTime)
        {
            if (m_Channel.IsAlive(currentTime) == false)
                return false;

            if (this.EventController != null)
                this.EventController.Monitor(currentTime);

            return true;
        }

        Site m_Site;
        IChannel m_Channel;

        public TCPController TcpController;
        public UDPController UdpController;
        public RPCController RpcController;
        public IEventClientController EventController;

        public void Send(SoftnetMessage message)
        {            
            m_Channel.Send(message);
        }

        byte[] m_userHash;
        byte[] m_userRolesHash;

        public void SetUser(MUser user)
        {
            this.User = user;
            this.m_userHash = user.hash;
            this.m_userRolesHash = user.rolesHash;
        }

        public void UpdateUser(MUser user, List<MRole> userRoles)
        {
            if (this.User.authority.isGuest)
            {
                m_Channel.Send(EncodeMessage_User(user, userRoles));

                this.User = user;
                this.m_userHash = user.hash;
                this.m_userRolesHash = user.rolesHash;

                if (this.EventController != null)
                    this.EventController.OnAuthorityUpdated();
            }
            else if (ByteArray.Equals(user.hash, m_userHash) == false)
            {
                m_Channel.Send(EncodeMessage_User(user, userRoles));

                if (ByteArray.Equals(user.rolesHash, m_userRolesHash) == false)
                {
                    this.User = user;
                    this.m_userHash = user.hash;
                    this.m_userRolesHash = user.rolesHash;

                    if (this.EventController != null)
                        this.EventController.OnAuthorityUpdated();
                }
                else
                {
                    this.User = user;
                    this.m_userHash = user.hash;
                    this.m_userRolesHash = user.rolesHash;
                }
            }
            else
            {
                this.User = user;
                this.m_userHash = user.hash;
                this.m_userRolesHash = user.rolesHash;
            }
        }

        public void UpdateUser(MUser user)
        {
            if (user.authority.isGuest)
            {
                if (this.User.authority.isGuest == false)
                {
                    m_Channel.Send(MsgBuilder.Create(Constants.Client.Membership.ModuleId, Constants.Client.Membership.GUEST));

                    this.User = user;
                    this.m_userHash = user.hash;

                    if (this.EventController != null)
                        this.EventController.OnAuthorityUpdated();
                }
            }
            else
            {
                if (this.User.authority.isGuest)
                {
                    m_Channel.Send(EncodeMessage_User(user));

                    this.User = user;
                    this.m_userHash = user.hash;

                    if (this.EventController != null)
                        this.EventController.OnAuthorityUpdated();
                }
                else
                {
                    if (ByteArray.Equals(user.hash, m_userHash) == false)
                        m_Channel.Send(EncodeMessage_User(user));

                    this.User = user;
                    this.m_userHash = user.hash;
                }
            }
        }        

        void OnChannelCompleted()
        {
            m_Site.Uninstall(this);
        }

        public void Shutdown(int errorCode)
        {
            if (m_Channel != null)
                m_Channel.Shutdown(errorCode);
        }

        public void Remove(int errorCode)
        {
            m_Site.Uninstall(this);
            m_Channel.Shutdown(errorCode);
        }

        public void Close()
        {
            if (m_Channel != null)
                m_Channel.Close();
        }

        SoftnetMessage EncodeMessage_User(MUser user)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.Int64(user.id);
            asnSequence.IA5String(user.name);
            return MsgBuilder.Create(Constants.Client.Membership.ModuleId, Constants.Client.Membership.USER, asnEncoder);
        }

        SoftnetMessage EncodeMessage_User(MUser user, List<MRole> userRoles)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.Int64(user.id);
            asnSequence.IA5String(user.name);

            SequenceEncoder asnRoles = asnSequence.Sequence(1);
            foreach (long roleId in user.authority.roles)
            {
                MRole mRole = userRoles.Find(x => x.id == roleId);
                asnRoles.IA5String(mRole.name);
            }

            return MsgBuilder.Create(Constants.Client.Membership.ModuleId, Constants.Client.Membership.USER, asnEncoder);
        }
    }
}
