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

using Softnet.Asn;
using Softnet.ServerKit;
using Softnet.Tracker.Core;
using Softnet.Tracker.SiteModel;
using Softnet.Tracker.ServiceModel;

namespace Softnet.Tracker.ClientModel
{
    public class UDPController
    {
        Site m_Site;
        Client m_Client;
        IChannel m_Channel;

        public void Init(Site site, Client client, IChannel channel)
        {
            m_Site = site;
            m_Client = client;
            m_Channel = channel;
            m_Channel.RegisterModule(Constants.Client.UdpController.ModuleId, OnMessageReceived);
        }

        public void SendRzvData(byte[] requestUid, byte[] connectionUid, ProxyParams proxyParams)
        {
            if (m_Channel.GetAddressFamily() == System.Net.Sockets.AddressFamily.InterNetworkV6)
                m_Channel.Send(EncodeMessage_RzvData(requestUid, connectionUid, proxyParams.ServerId, proxyParams.IPv6));
            else
                m_Channel.Send(EncodeMessage_RzvData(requestUid, connectionUid, proxyParams.ServerId, proxyParams.IPv4));
        }

        public void SendRequestError(byte[] requestUid, int errorCode)
        {
            m_Channel.Send(EncodeMessage_RequestError(requestUid, errorCode));
        }

        public void SendConnectionAccepted(byte[] requestUid)
        {
            m_Channel.Send(EncodeMessage_ConnectionAccepted(requestUid));
        }

        void ProcessMessage_Request(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Sequence(message, 2);
            byte[] requestUid = asnSequence.OctetString(16);
            long serviceId = asnSequence.Int64();
            int virtualPort = asnSequence.Int32();
            byte[] sessionTag = null;
            if (asnSequence.Exists(1))
                sessionTag = asnSequence.OctetString(2, 64);
            asnSequence.End();

            Service service = m_Site.FindService(serviceId);
            if (service != null && service.Online)
            {
                service.UdpController.SendRequest(requestUid, virtualPort, m_Client, sessionTag);
            }
            else
            {
                m_Channel.Send(EncodeMessage_RequestError(requestUid, ErrorCodes.SERVICE_OFFLINE));
            }
        }

        void ProcessMessage_AuthKey(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Sequence(message, 2);
            byte[] requestUid = asnSequence.OctetString(16);
            int serverId = asnSequence.Int32();
            byte[] authKey = asnSequence.OctetString(20);
            asnSequence.End();

            ProxyParams proxyParams = NetworkResources.FindProxy(serverId);
            if (proxyParams != null)
            {
                byte[] authKey2 = Randomizer.ByteString(20);
                byte[] authHash = PasswordHash.Compute(authKey, authKey2, proxyParams.SecretKey);
                m_Channel.Send(EncodeMessage_AuthHash(requestUid, authHash, authKey2));
            }
            else
            {
                m_Channel.Send(EncodeMessage_AuthError(requestUid));
            }
        }

        SoftnetMessage EncodeMessage_RzvData(byte[] requestUid, byte[] connectionUid, int serverId, IPAddress serverIP)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var asnSequence = asnEncoder.Sequence;
            asnSequence.OctetString(requestUid);
            asnSequence.OctetString(connectionUid);
            asnSequence.Int32(serverId);
            asnSequence.OctetString(serverIP.GetAddressBytes());
            return MsgBuilder.Create(Constants.Client.UdpController.ModuleId, Constants.Client.UdpController.RZV_DATA, asnEncoder);
        }

        SoftnetMessage EncodeMessage_RequestError(byte[] requestUid, int errorCode)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var asnSequence = asnEncoder.Sequence;
            asnSequence.OctetString(requestUid);
            asnSequence.Int32(errorCode);
            return MsgBuilder.Create(Constants.Client.UdpController.ModuleId, Constants.Client.UdpController.REQUEST_ERROR, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ConnectionAccepted(byte[] requestUid)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var asnSequence = asnEncoder.Sequence;
            asnSequence.OctetString(requestUid);
            return MsgBuilder.Create(Constants.Client.UdpController.ModuleId, Constants.Client.UdpController.CONNECTION_ACCEPTED, asnEncoder);
        }

        SoftnetMessage EncodeMessage_AuthHash(byte[] requestUid, byte[] authHash, byte[] authKey2)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.OctetString(requestUid);
            asnSequence.OctetString(authHash);
            asnSequence.OctetString(authKey2);
            return MsgBuilder.Create(Constants.Client.UdpController.ModuleId, Constants.Client.UdpController.AUTH_HASH, asnEncoder);
        }

        SoftnetMessage EncodeMessage_AuthError(byte[] requestUid)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.OctetString(requestUid);
            return MsgBuilder.Create(Constants.Client.UdpController.ModuleId, Constants.Client.UdpController.AUTH_ERROR, asnEncoder);
        }

        void OnMessageReceived(byte[] message)
        {
            byte messageTag = message[1];
            if (messageTag == Constants.Client.UdpController.REQUEST)
            {
                ProcessMessage_Request(message);
            }
            else if (messageTag == Constants.Client.UdpController.AUTH_KEY)
            {
                ProcessMessage_AuthKey(message);
            }
            else
            {
                m_Client.Remove(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR);
            }
        }
    }
}
