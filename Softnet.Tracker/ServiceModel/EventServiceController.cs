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

namespace Softnet.Tracker.ServiceModel
{
    public class EventServiceController
    {
        SiteModel.Site m_Site;
        Service m_Service;
        IChannel m_Channel;

        public void Init(SiteModel.Site site, Service service, IChannel channel)
        {
            m_Site = site;
            m_Service = service;
            m_Channel = channel;
            m_Channel.RegisterModule(Constants.Service.EventController.ModuleId, OnMessageReceived);
        }

        void ProcessMessage_ReplacingEvent(byte[] message)
        {
            var eventIData = new EventIData();
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            eventIData.Name = asnSequence.IA5String(1, 256);
            int eventIndex = asnSequence.Int32();
            byte[] instanceUid = asnSequence.OctetString(16);
            if (asnSequence.Exists(1))
                eventIData.Arguments = asnSequence.OctetString(2, 2048);
            asnSequence.End();

            eventIData.IsNull = false;
            eventIData.ServiceId = m_Service.Id;

            int resultCode = SoftnetRegistry.Service_SaveEventAcknowledgment(eventIData, m_Site.Id, m_Service.Id, instanceUid);
            if (resultCode == 0)
            {
                m_Site.EventController.AcceptREvent(eventIData);
                m_Channel.Send(EncodeMessage_EventAck(eventIndex, instanceUid));
            }
            else if (resultCode == 1)
            {
                m_Channel.Send(EncodeMessage_EventAck(eventIndex, instanceUid));
            }
            else
            {
                m_Channel.Send(EncodeMessage_IllegalEventName(eventIndex, instanceUid));
            }
        }

        void ProcessMessage_ReplacingNullEvent(byte[] message)
        {
            var eventIData = new EventIData();
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            eventIData.Name = asnSequence.IA5String(1, 256);
            int eventIndex = asnSequence.Int32();
            byte[] instanceUid = asnSequence.OctetString(16);
            asnSequence.End();

            eventIData.IsNull = true;
            eventIData.ServiceId = m_Service.Id;

            int resultCode = SoftnetRegistry.Service_SaveEventAcknowledgment(eventIData, m_Site.Id, m_Service.Id, instanceUid);
            if (resultCode == 0)
            {
                m_Site.EventController.AcceptREvent(eventIData);
                m_Channel.Send(EncodeMessage_EventAck(eventIndex, instanceUid));
            }
            else if (resultCode == 1)
            {
                m_Channel.Send(EncodeMessage_EventAck(eventIndex, instanceUid));
            }
            else
            {
                m_Channel.Send(EncodeMessage_IllegalEventName(eventIndex, instanceUid));
            }
        }

        void ProcessMessage_QueueingEvent(byte[] message)
        {
            var eventIData = new SiteModel.EventIData();
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            eventIData.Name = asnSequence.IA5String(1, 256);
            int eventIndex = asnSequence.Int32();
            byte[] instanceUid = asnSequence.OctetString(16);
            if (asnSequence.Exists(1))
                eventIData.Arguments = asnSequence.OctetString(2, 2048);
            asnSequence.End();

            eventIData.ServiceId = m_Service.Id;

            int resultCode = SoftnetRegistry.Service_SaveEventAcknowledgment(eventIData, m_Site.Id, m_Service.Id, instanceUid);
            if (resultCode == 0)
            {
                m_Site.EventController.AcceptQEvent(eventIData);
                m_Channel.Send(EncodeMessage_EventAck(eventIndex, instanceUid));
            }
            else if (resultCode == 1)
            {
                m_Channel.Send(EncodeMessage_EventAck(eventIndex, instanceUid));
            }
            else
            {
                m_Channel.Send(EncodeMessage_IllegalEventName(eventIndex, instanceUid));
            }
        }        

        void ProcessMessage_PrivateEvent(byte[] message)
        {
            var eventIData = new SiteModel.EventIData();
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            eventIData.Name = asnSequence.IA5String(1, 256);
            int eventIndex = asnSequence.Int32();
            byte[] instanceUid = asnSequence.OctetString(16);
            eventIData.ClientId = asnSequence.Int64();
            if (asnSequence.Exists(1))
                eventIData.Arguments = asnSequence.OctetString(2, 2048);
            asnSequence.End();

            eventIData.ServiceId = m_Service.Id;

            int resultCode = SoftnetRegistry.Service_SaveEventAcknowledgment(eventIData, m_Site.Id, m_Service.Id, instanceUid);
            if (resultCode == 0)
            {
                m_Site.EventController.AcceptPEvent(eventIData);
                m_Channel.Send(EncodeMessage_EventAck(eventIndex, instanceUid));
            }
            else if (resultCode == 1)
            {
                m_Channel.Send(EncodeMessage_EventAck(eventIndex, instanceUid));
            }
            else
            {
                m_Channel.Send(EncodeMessage_IllegalEventName(eventIndex, instanceUid));
            }
        }

        void ProcessMessage_NewStorageUid(byte[] message)
        {            
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            Guid storageUid = ByteConverter.ToGuid(asnSequence.OctetString(16));
            asnSequence.End();

            SoftnetRegistry.Service_UpdateStorageUid(m_Service.Id, storageUid);
        }

        SoftnetMessage EncodeMessage_EventAck(int eventIndex, byte[] instanceUid)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.Int32(eventIndex);
            asnSequence.OctetString(instanceUid);
            return MsgBuilder.Create(Constants.Service.EventController.ModuleId, Constants.Service.EventController.EVENT_ACK, asnEncoder);
        }

        SoftnetMessage EncodeMessage_IllegalEventName(int eventIndex, byte[] instanceUid)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.Int32(eventIndex);
            asnSequence.OctetString(instanceUid);
            return MsgBuilder.Create(Constants.Service.EventController.ModuleId, Constants.Service.EventController.ILLEGAL_EVENT_NAME, asnEncoder);
        }

        void OnMessageReceived(byte[] message)
        {
            byte messageTag = message[1];
            if (messageTag == Constants.Service.EventController.REPLACING_EVENT)
            {
                ProcessMessage_ReplacingEvent(message);
            }
            else if (messageTag == Constants.Service.EventController.REPLACING_NULL_EVENT)
            {
                ProcessMessage_ReplacingNullEvent(message);
            }
            else if (messageTag == Constants.Service.EventController.QUEUEING_EVENT)
            {
                ProcessMessage_QueueingEvent(message);
            }
            else if (messageTag == Constants.Service.EventController.PRIVATE_EVENT)
            {
                ProcessMessage_PrivateEvent(message);
            }
            else if (messageTag == Constants.Service.EventController.NEW_STORAGE_UID)
            {
                ProcessMessage_NewStorageUid(message);
            }
            else
                throw new SoftnetException(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR);
        }
    }
}
