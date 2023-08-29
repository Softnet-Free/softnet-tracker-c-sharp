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
using Softnet.Tracker.ClientModel;

namespace Softnet.Tracker.ServiceModel
{
    public class RPCController
    {
        Site m_Site;
        Service m_Service;
        IChannel m_Channel;

        public void Init(Site site, Service service, IChannel channel)
        {
            m_Site = site;
            m_Service = service;
            m_Channel = channel;
            m_Channel.RegisterModule(Constants.Service.RpcController.ModuleId, OnMessageReceived);
        }

        public void SendRequest(byte[] requestUid, string procedureName, int userKind, long userId, long clientId, byte[] arguments)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var asnSequence = asnEncoder.Sequence;
            asnSequence.OctetString(requestUid);
            asnSequence.IA5String(procedureName);
            asnSequence.Int32(userKind);
            asnSequence.Int64(userId);
            asnSequence.Int64(clientId);
            asnSequence.OctetString(arguments);
            m_Channel.Send(MsgBuilder.Create(Constants.Service.RpcController.ModuleId, Constants.Service.RpcController.REQUEST, asnEncoder));

        }

        void ProcessMessage_Result(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            byte[] requestUid = asnSequence.OctetString(16);
            int userKind = asnSequence.Int32();
            long clientId = asnSequence.Int64();
            byte[] resultBytes = asnSequence.OctetString(2, 65536);
            asnSequence.End();

            if (userKind != Constants.UserKind.StatelessGuest)
            {
                Client client = m_Site.FindClient(clientId);
                if (client != null)
                    client.RpcController.SendResult(requestUid, resultBytes);
            }
            else
            {
                Client client = m_Site.FindStatelessClient(clientId);
                if (client != null)
                    client.RpcController.SendResult(requestUid, resultBytes);
            }
        }

        void ProcessMessage_SoftnetError(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            byte[] requestUid = asnSequence.OctetString(16);
            int userKind = asnSequence.Int32();
            long clientId = asnSequence.Int64();
            int errorCode = asnSequence.Int32();
            asnSequence.End();

            if (userKind != Constants.UserKind.StatelessGuest)
            {
                Client client = m_Site.FindClient(clientId);
                if (client != null)
                    client.RpcController.SendSoftnetError(requestUid, errorCode);
            }
            else
            {
                Client client = m_Site.FindStatelessClient(clientId);
                if (client != null)
                    client.RpcController.SendSoftnetError(requestUid, errorCode);
            }
        }

        void ProcessMessage_AppError(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            byte[] requestUid = asnSequence.OctetString(16);
            int userKind = asnSequence.Int32();
            long clientId = asnSequence.Int64();
            int errorCode = asnSequence.Int32();
            byte[] errorBytes = asnSequence.OctetString(2, 65536);
            asnSequence.End();

            if (userKind != Constants.UserKind.StatelessGuest)
            {
                Client client = m_Site.FindClient(clientId);
                if (client != null)
                    client.RpcController.SendAppError(requestUid, errorCode, errorBytes);
            }
            else
            {
                Client client = m_Site.FindStatelessClient(clientId);
                if (client != null)
                    client.RpcController.SendAppError(requestUid, errorCode, errorBytes);
            }
        }

        void OnMessageReceived(byte[] message)
        {
            byte messageTag = message[1];
            if (messageTag == Constants.Service.RpcController.RESULT)
            {
                ProcessMessage_Result(message);
            }
            else if (messageTag == Constants.Service.RpcController.SOFTNET_ERROR)
            {
                ProcessMessage_SoftnetError(message);
            }
            else if (messageTag == Constants.Service.RpcController.APP_ERROR)
            {
                ProcessMessage_AppError(message);
            }
            else
                throw new SoftnetException(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR);
        }
    }
}
