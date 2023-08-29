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

using Softnet.Tracker.SiteModel;
using Softnet.ServerKit;
using Softnet.Tracker.Core;

namespace Softnet.Tracker.ServiceModel
{
    public class Service : IEquatable<Service>, Softnet.ServerKit.Monitorable
    {
        public long Id;
        public bool Online;
        public int Status;
        public byte[] ChannelId;

        public ServiceSyncToken SyncToken;
        public object Tmp;

        public Service()
        {
            Online = false;
            Status = Constants.ServiceStatus.Offline;
            m_Site = null;
            m_Channel = null;
            TcpController = null;
            RpcController = null;
            EventController = null;
            Tmp = null;
            SyncToken = null;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Service objAsService = obj as Service;
            if (objAsService == null) return false;
            else return Equals(objAsService);
        }

        public bool Equals(Service other)
        {
            if (other == null) return false;
            return (this.Id == other.Id);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
        
        public void SetOnline(Site site, IChannel channel)
        {
            Online = true;
            Status = Constants.ServiceStatus.Online;

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
                EventController = new EventServiceController();
                EventController.Init(site, this, channel);
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
            return true;
        }

        Site m_Site;
        IChannel m_Channel;

        public void Send(SoftnetMessage message)
        {
            m_Channel.Send(message);
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

        public TCPController TcpController;
        public UDPController UdpController;
        public RPCController RpcController;
        public EventServiceController EventController;
    }
}
