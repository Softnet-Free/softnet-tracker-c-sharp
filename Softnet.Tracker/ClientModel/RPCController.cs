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
using Softnet.Tracker.SiteModel;
using Softnet.Tracker.ServiceModel;

namespace Softnet.Tracker.ClientModel
{
    public class RPCController
    {
        Site m_Site;
        Client m_Client;
        IChannel m_Channel;

        public void Init(Site site, Client client, IChannel channel)
        {
            m_Site = site;
            m_Client = client;
            m_Channel = channel;
            m_Channel.RegisterModule(Constants.Client.RpcController.ModuleId, OnMessageReceived);
        }

        public void SendResult(byte[] requestUid, byte[] resultBytes)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var asnSequence = asnEncoder.Sequence;
            asnSequence.OctetString(requestUid);
            asnSequence.OctetString(resultBytes);
            m_Channel.Send(MsgBuilder.Create(Constants.Client.RpcController.ModuleId, Constants.Client.RpcController.RESULT, asnEncoder));
        }

        public void SendSoftnetError(byte[] requestUid, int errorCode)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var asnSequence = asnEncoder.Sequence;
            asnSequence.OctetString(requestUid);
            asnSequence.Int32(errorCode);
            m_Channel.Send(MsgBuilder.Create(Constants.Client.RpcController.ModuleId, Constants.Client.RpcController.SOFTNET_ERROR, asnEncoder));
        }

        public void SendAppError(byte[] requestUid, int errorCode, byte[] errorBytes)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var asnSequence = asnEncoder.Sequence;
            asnSequence.OctetString(requestUid);
            asnSequence.Int32(errorCode);
            asnSequence.OctetString(errorBytes);
            m_Channel.Send(MsgBuilder.Create(Constants.Client.RpcController.ModuleId, Constants.Client.RpcController.APP_ERROR, asnEncoder));
        }

        void ProcessMessage_Request(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            byte[] requestUid = asnSequence.OctetString(16);
            long serviceId = asnSequence.Int64();
            string procedureName = asnSequence.IA5String(1, 256);
            byte[] arguments = asnSequence.OctetString(2, 65536);
            asnSequence.End();

            Service service = m_Site.FindService(serviceId);
            if (service != null && service.Online)
            {
                service.RpcController.SendRequest(requestUid, procedureName, m_Client.UserKind, m_Client.UserId, m_Client.Id, arguments);
            }
            else
            {
                SendSoftnetError(requestUid, ErrorCodes.SERVICE_OFFLINE);
            }
        }

        void OnMessageReceived(byte[] message)
        {
            if (message[1] == Constants.Client.RpcController.REQUEST)
            {
                ProcessMessage_Request(message);
            }
            else
            {
                m_Client.Remove(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR);
            }
        }
    }
}
