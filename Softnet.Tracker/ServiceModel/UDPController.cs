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
using Softnet.Tracker.ClientModel;

namespace Softnet.Tracker.ServiceModel
{
    public class UDPController
    {
        Site m_Site;
        Service m_Service;
        IChannel m_Channel;

        public void Init(Site site, Service service, IChannel channel)
        {
            m_Site = site;
            m_Service = service;
            m_Channel = channel;
            m_Channel.RegisterModule(Constants.Service.UdpController.ModuleId, OnMessageReceived);
        }

        public void SendRequest(byte[] requestUid, int virtualPort, Client client)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var asnSequence = asnEncoder.Sequence;
            asnSequence.OctetString(requestUid);
            asnSequence.Int32(virtualPort);
            asnSequence.Int32(client.UserKind);
            asnSequence.Int64(client.UserId);
            asnSequence.Int64(client.Id);
            m_Channel.Send(MsgBuilder.Create(Constants.Service.UdpController.ModuleId, Constants.Service.UdpController.REQUEST, asnEncoder));
        }

        void SendRzvData(byte[] requestUid, int virtualPort, Client client)
        {
            byte[] connectionUid = Guid.NewGuid().ToByteArray();
            ProxyParams ProxyParams = NetworkResources.GetProxy();

            client.UdpController.SendRzvData(requestUid, connectionUid, ProxyParams);

            if (m_Channel.GetAddressFamily() == System.Net.Sockets.AddressFamily.InterNetworkV6)
                m_Channel.Send(EncodeMessage_RzvData(requestUid, connectionUid, ProxyParams.ServerId, ProxyParams.IPv6, virtualPort, client));
            else
                m_Channel.Send(EncodeMessage_RzvData(requestUid, connectionUid, ProxyParams.ServerId, ProxyParams.IPv4, virtualPort, client));
        }

        void ProcessMessage_RequestOk(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Sequence(message, 2);
            byte[] requestUid = asnSequence.OctetString(16);
            int virtualPort = asnSequence.Int32();
            int userKind = asnSequence.Int32();
            long clientId = asnSequence.Int64();
            asnSequence.End();

            if (userKind != Constants.UserKind.StatelessGuest)
            {
                Client client = m_Site.FindClient(clientId);
                if (client != null)
                    SendRzvData(requestUid, virtualPort, client);
            }
            else
            {
                Client client = m_Site.FindStatelessClient(clientId);
                if (client != null)
                    SendRzvData(requestUid, virtualPort, client);
            }
        }

        void ProcessMessage_RequestError(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Sequence(message, 2);
            byte[] requestUid = asnSequence.OctetString(16);
            int errorCode = asnSequence.Int32();
            int userKind = asnSequence.Int32();
            long clientId = asnSequence.Int64();
            asnSequence.End();

            if (userKind != Constants.UserKind.StatelessGuest)
            {
                Client client = m_Site.FindClient(clientId);
                if (client != null)
                    client.UdpController.SendRequestError(requestUid, errorCode);
            }
            else
            {
                Client client = m_Site.FindStatelessClient(clientId);
                if (client != null)
                    client.UdpController.SendRequestError(requestUid, errorCode);
            }
        }

        void ProcessMessage_AuthKey(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Sequence(message, 2);
            int virtualPort = asnSequence.Int32();
            byte[] connectionUid = asnSequence.OctetString(16);
            int serverId = asnSequence.Int32();
            byte[] authKey = asnSequence.OctetString(20);
            asnSequence.End();

            ProxyParams proxyParams = NetworkResources.FindProxy(serverId);
            if (proxyParams != null)
            {
                byte[] authKey2 = Randomizer.ByteString(20);
                byte[] authHash = PasswordHash.Compute(authKey, authKey2, proxyParams.SecretKey);
                m_Channel.Send(EncodeMessage_AuthHash(virtualPort, connectionUid, authHash, authKey2));
            }
            else
            {
                m_Channel.Send(EncodeMessage_AuthError(virtualPort, connectionUid));
            }
        }

        SoftnetMessage EncodeMessage_RzvData(byte[] requestUid, byte[] connectionUid, long serverId, IPAddress serverIP, int virtualPort, Client client)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var asnSequence = asnEncoder.Sequence;
            asnSequence.OctetString(requestUid);
            asnSequence.OctetString(connectionUid);
            asnSequence.Int64(serverId);
            asnSequence.OctetString(serverIP.GetAddressBytes());
            asnSequence.Int32(virtualPort);
            asnSequence.Int32(client.UserKind);
            asnSequence.Int64(client.UserId);
            asnSequence.Int64(client.Id);
            return MsgBuilder.Create(Constants.Service.UdpController.ModuleId, Constants.Service.UdpController.RZV_DATA, asnEncoder);
        }

        SoftnetMessage EncodeMessage_AuthHash(int virtualPort, byte[] connectionUid, byte[] authHash, byte[] authKey2)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.Int32(virtualPort);
            asnSequence.OctetString(connectionUid);
            asnSequence.OctetString(authHash);
            asnSequence.OctetString(authKey2);
            return MsgBuilder.Create(Constants.Service.UdpController.ModuleId, Constants.Service.UdpController.AUTH_HASH, asnEncoder);
        }

        SoftnetMessage EncodeMessage_AuthError(int virtualPort, byte[] connectionUid)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.Int32(virtualPort);
            asnSequence.OctetString(connectionUid);
            return MsgBuilder.Create(Constants.Service.UdpController.ModuleId, Constants.Service.UdpController.AUTH_ERROR, asnEncoder);
        }

        void OnMessageReceived(byte[] message)
        {
            byte messageTag = message[1];
            if (messageTag == Constants.Service.UdpController.REQUEST_OK)
            {
                ProcessMessage_RequestOk(message);
            }
            else if (messageTag == Constants.Service.UdpController.REQUEST_ERROR)
            {
                ProcessMessage_RequestError(message);
            }
            else if (messageTag == Constants.Service.UdpController.AUTH_KEY)
            {
                ProcessMessage_AuthKey(message);
            }
            else
            {
                m_Service.Remove(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR);
            }
        }
    }
}
