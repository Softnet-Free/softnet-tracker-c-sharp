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
using Softnet.Tracker.SiteModel;

namespace Softnet.Tracker.ClientModel
{
    class SLEventController : IEventClientController
    {
        Site m_Site;
        Client m_Client;
        IChannel m_Channel;

        object site_mutex;
        object cont_mutex;

        List<RSubscription> m_RSubscriptions;
        List<QSubscription> m_QSubscriptions;
        bool m_Synchronized;
        int m_deliveringEventsCounter;

        public SLEventController()
        {
            m_RSubscriptions = new List<RSubscription>();
            m_QSubscriptions = new List<QSubscription>();
            m_Synchronized = false;
            m_deliveringEventsCounter = 0;
        }

        public void Init(Site site, Client client, IChannel channel)
        {
            m_Site = site;
            m_Client = client;
            m_Channel = channel;

            site_mutex = m_Site.mutex;
            cont_mutex = m_Site.EventController.GetMutex();

            m_Channel.RegisterModule(Constants.Client.EventController.ModuleId, OnMessageReceived);
            m_Channel.Send(MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.SYNC));
        }

        public void OnAuthorityUpdated()
        {
            lock (cont_mutex)
            {
                foreach (RSubscription subscription in m_RSubscriptions)
                {
                    if (subscription.EventInstance == null)
                    {
                        m_Site.EventController.AuthorizeRSubscription(subscription, m_Client.User.authority);

                        if (subscription.EventInstance != null)
                        {
                            m_deliveringEventsCounter++;
                            subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                            subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                            ThreadPool.QueueUserWorkItem(delegate { SendREvent(subscription); });
                        }
                    }
                    else
                    {
                        m_Site.EventController.AuthorizeRSubscription(subscription, m_Client.User.authority);
                    }
                }

                foreach (QSubscription subscription in m_QSubscriptions)
                {
                    if (subscription.EventInstance == null)
                    {
                        m_Site.EventController.AuthorizeQSubscription(subscription, m_Client.User.authority);

                        if (subscription.EventInstance != null)
                        {
                            m_deliveringEventsCounter++;
                            subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                            subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                            ThreadPool.QueueUserWorkItem(delegate { SendQEvent(subscription); });
                        }
                    }
                    else
                    {
                        m_Site.EventController.AuthorizeQSubscription(subscription, m_Client.User.authority);
                    }
                }
            }
        }

        public void Monitor(long currentTime)
        {
            lock (cont_mutex)
            {
                if (m_deliveringEventsCounter == 0)
                    return;

                foreach (RSubscription subscription in m_RSubscriptions)
                {
                    if (subscription.EventInstance != null && subscription.DeliveryExpirationTime < currentTime)
                    {
                        subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                        subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                        ThreadPool.QueueUserWorkItem(delegate { SendREvent(subscription); });
                    }
                }

                foreach (QSubscription subscription in m_QSubscriptions)
                {
                    if (subscription.EventInstance != null && subscription.DeliveryExpirationTime < currentTime)
                    {
                        subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                        subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                        ThreadPool.QueueUserWorkItem(delegate { SendQEvent(subscription); });
                    }
                }
            }
        }

        public void OnREventRaised(REventInstance eventInstance, SoftnetMessage message, byte[] transactionUid)
        {
            if (m_Synchronized == false)
                return;

            RSubscription subscription = m_RSubscriptions.Find(x => x.EventId == eventInstance.EventId);
            if (!(subscription != null && subscription.Authorized))
                return;
            if (subscription.EventInstance != null)
                return;

            m_deliveringEventsCounter++;
            subscription.EventInstance = eventInstance;
            subscription.TransactionUid = transactionUid;
            subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
            m_Channel.Send(message);
        }

        public void OnQEventRaised(QEventInstance eventInstance, SoftnetMessage message, byte[] transactionUid)
        {
            if (m_Synchronized == false)
                return;

            QSubscription subscription = m_QSubscriptions.Find(x => x.EventId == eventInstance.EventId);
            if (!(subscription != null && subscription.Authorized))
                return;
            if (subscription.EventInstance != null)
                return;

            m_deliveringEventsCounter++;
            subscription.EventInstance = eventInstance;
            subscription.TransactionUid = transactionUid;
            subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
            m_Channel.Send(message);
        }

        public void OnPEventRaised(PEventInstance eventInstance, byte[] arguments) { }

        void SendREvent(RSubscription subscription)
        {
            REventInstance eventInstance = subscription.EventInstance;
            long eventAge = (eventInstance.SetupTimeTicks - eventInstance.CreatedTimeTicks) * 10 + (SystemClock.Seconds - eventInstance.SetupTimeSeconds);

            if (eventInstance.IsNull)
            {
                m_Channel.Send(EncodeMessage_ReplacingNullEvent(subscription, eventInstance, eventAge));
            }
            else if (eventInstance.HasArguments)
            {
                if (eventInstance.Arguments != null)
                {
                    m_Channel.Send(EncodeMessage_ReplacingEvent(subscription, eventInstance, eventAge, eventInstance.Arguments));
                }
                else
                {
                    try
                    {
                        Container<byte[]> eventArgs = new Container<byte[]>();
                        int resultCode = SoftnetRegistry.Client_GetREventArguments(eventInstance.InstanceId, eventArgs);
                        if (resultCode == 0)
                        {
                            m_Channel.Send(EncodeMessage_ReplacingEvent(subscription, eventInstance, eventAge, eventArgs.Obj));
                        }
                        else
                        {
                            lock (cont_mutex)
                            {
                                if (subscription.Removed)
                                    return;

                                subscription.DeliveredInstanceId = eventInstance.InstanceId;

                                m_deliveringEventsCounter--;
                                subscription.EventInstance = null;
                                subscription.TransactionUid = null;

                                if (subscription.Authorized == false)
                                    return;

                                subscription.EventInstance = m_Site.EventController.GetNextEvent(eventInstance);
                                if (subscription.EventInstance == null)
                                    return;

                                m_deliveringEventsCounter++;
                                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                            }

                            ThreadPool.QueueUserWorkItem(delegate { SendREvent(subscription); });
                        }
                    }
                    catch (SoftnetException ex)
                    {
                        m_Client.Remove(ex.ErrorCode);
                    }
                }
            }
            else
            {
                m_Channel.Send(EncodeMessage_ReplacingEvent(subscription, eventInstance, eventAge, null));
            }
        }

        void SendQEvent(QSubscription subscription)
        {
            QEventInstance eventInstance = subscription.EventInstance;
            long eventAge = (eventInstance.SetupTimeTicks - eventInstance.CreatedTimeTicks) * 10 + (SystemClock.Seconds - eventInstance.SetupTimeSeconds);

            if (eventInstance.HasArguments)
            {
                if (eventInstance.Arguments != null)
                {
                    m_Channel.Send(EncodeMessage_QueueingEvent(subscription, eventInstance, eventAge, eventInstance.Arguments));
                }
                else
                {
                    try
                    {
                        Container<byte[]> eventArgs = new Container<byte[]>();
                        int resultCode = SoftnetRegistry.Client_GetQEventArguments(eventInstance.InstanceId, eventArgs);
                        if (resultCode == 0)
                        {
                            m_Channel.Send(EncodeMessage_QueueingEvent(subscription, eventInstance, eventAge, eventArgs.Obj));
                        }
                        else
                        {
                            lock (cont_mutex)
                            {
                                if (subscription.Removed)
                                    return;

                                subscription.DeliveredInstanceId = eventInstance.InstanceId;

                                m_deliveringEventsCounter--;
                                subscription.EventInstance = null;
                                subscription.TransactionUid = null;

                                if (subscription.Authorized == false)
                                    return;

                                subscription.EventInstance = m_Site.EventController.GetNextEvent(eventInstance);
                                if (subscription.EventInstance == null)
                                    return;

                                m_deliveringEventsCounter++;
                                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                            }

                            ThreadPool.QueueUserWorkItem(delegate { SendQEvent(subscription); });
                        }
                    }
                    catch (SoftnetException ex)
                    {
                        m_Client.Remove(ex.ErrorCode);
                    }
                }
            }
            else
            {
                m_Channel.Send(EncodeMessage_QueueingEvent(subscription, eventInstance, eventAge, null));
            }
        }        

        void ProcessMessage_Subscriptions(byte[] message)
        {
            List<SCSubscriptionData> subscriptions = new List<SCSubscriptionData>();

            SequenceDecoder asnRoot = ASNDecoder.Sequence(message, 2);
            while (asnRoot.HasNext())
            {
                SCSubscriptionData subsData = new SCSubscriptionData();
                SequenceDecoder asnSubscription = asnRoot.Sequence();
                subsData.EventKind = asnSubscription.Int32(1, 3);
                subsData.EventName = asnSubscription.IA5String(1, 256);
                if (asnSubscription.Exists(1))
                    subsData.DeliveredInstanceId = asnSubscription.Int64();
                asnSubscription.End();
                subscriptions.Add(subsData);
            }
            asnRoot.End();

            lock (site_mutex)
            {
                lock (cont_mutex)
                {
                    foreach (SCSubscriptionData subsData in subscriptions)
                    {
                        if (m_Site.EventController.AuthorizeSCSubscription(subsData))
                        {
                            if (subsData.EventKind == 1)
                            {
                                RSubscription subscription = new RSubscription(subsData);
                                m_RSubscriptions.Add(subscription);

                                if (subscription.EventInstance != null)
                                {
                                    m_deliveringEventsCounter++;
                                    subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                                    subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                                    ThreadPool.QueueUserWorkItem(delegate { SendREvent(subscription); });
                                }
                            }
                            else if (subsData.EventKind == 2)
                            {
                                QSubscription subscription = new QSubscription(subsData);
                                m_QSubscriptions.Add(subscription);

                                if (subscription.EventInstance != null)
                                {
                                    m_deliveringEventsCounter++;
                                    subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                                    subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                                    ThreadPool.QueueUserWorkItem(delegate { SendQEvent(subscription); });
                                }
                            }                            
                        }
                        else
                        {
                            ASNEncoder asnEncoder = new ASNEncoder();
                            asnEncoder.Sequence.IA5String(subsData.EventName);
                            m_Channel.Send(MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.ILLEGAL_SUBSCRIPTION, asnEncoder));                        
                        }
                    }

                    m_Synchronized = true;
                }
            }
        }

        void ProcessMessage_ReplacingEventAck(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Sequence(message, 2);
            long eventId = asnSequence.Int64();
            byte[] transactionUid = asnSequence.OctetString(16);
            asnSequence.End();

            RSubscription subscription = null;
            lock (cont_mutex)
            {
                subscription = m_RSubscriptions.Find(x => x.EventId == eventId);
                if (subscription == null)
                    return;

                if (!(subscription.EventInstance != null && ByteArray.Equals(transactionUid, subscription.TransactionUid)))
                    return;

                REventInstance deliveredEventInstance = subscription.EventInstance;
                subscription.DeliveredInstanceId = deliveredEventInstance.InstanceId;

                m_deliveringEventsCounter--;
                subscription.TransactionUid = null;
                subscription.EventInstance = null;

                if (subscription.Authorized == false)                
                    return;

                subscription.EventInstance = m_Site.EventController.GetNextEvent(deliveredEventInstance);
                if (subscription.EventInstance == null)
                    return;

                m_deliveringEventsCounter++;
                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
            }

            SendREvent(subscription);
        }

        void ProcessMessage_QueueingEventAck(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Sequence(message, 2);
            long eventId = asnSequence.Int64();
            byte[] transactionUid = asnSequence.OctetString(16);
            asnSequence.End();

            QSubscription subscription = null;
            lock (cont_mutex)
            {
                subscription = m_QSubscriptions.Find(x => x.EventId == eventId);
                if (subscription == null)
                    return;

                if (!(subscription.EventInstance != null && ByteArray.Equals(transactionUid, subscription.TransactionUid)))
                    return;

                QEventInstance deliveredEventInstance = subscription.EventInstance;
                subscription.DeliveredInstanceId = deliveredEventInstance.InstanceId;

                m_deliveringEventsCounter--;
                subscription.TransactionUid = null;
                subscription.EventInstance = null;

                if (subscription.Authorized == false)
                    return;

                subscription.EventInstance = m_Site.EventController.GetNextEvent(deliveredEventInstance);
                if (subscription.EventInstance == null)
                    return;

                m_deliveringEventsCounter++;
                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
            }

            SendQEvent(subscription);
        }

        void ProcessMessage_EventRejected(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Sequence(message, 2);
            int eventKind = asnSequence.Int32();
            long eventId = asnSequence.Int64();
            byte[] transactionUid = asnSequence.OctetString(16);
            asnSequence.End();

            if (eventKind == Constants.EventKind.Replacing)
            {
                lock (cont_mutex)
                {
                    RSubscription subscription = m_RSubscriptions.Find(x => x.EventId == eventId);
                    if (subscription == null)
                        return;

                    if (!(subscription.EventInstance != null && ByteArray.Equals(transactionUid, subscription.TransactionUid)))
                        return;

                    m_RSubscriptions.Remove(subscription);
                    subscription.Removed = true;

                    if (subscription.EventInstance != null)
                        m_deliveringEventsCounter--;
                }
            }
            else if (eventKind == Constants.EventKind.Queueing)
            {
                lock (cont_mutex)
                {
                    QSubscription subscription = m_QSubscriptions.Find(x => x.EventId == eventId);
                    if (subscription == null)
                        return;

                    if (!(subscription.EventInstance != null && ByteArray.Equals(transactionUid, subscription.TransactionUid)))
                        return;

                    m_QSubscriptions.Remove(subscription);
                    subscription.Removed = true;

                    if (subscription.EventInstance != null)
                        m_deliveringEventsCounter--;
                }
            }
            else
                throw new FormatException();
        }

        void ProcessMessage_AddSubscription(byte[] message)
        {
            SCSubscriptionData subsData = new SCSubscriptionData();

            SequenceDecoder asnSequence = ASNDecoder.Sequence(message, 2);
            subsData.EventKind = asnSequence.Int32();
            subsData.EventName = asnSequence.IA5String(1, 256);
            if (asnSequence.Exists(1))
                subsData.DeliveredInstanceId = asnSequence.Int64();
            asnSequence.End();

            lock (cont_mutex)
            {
                if (subsData.EventKind == Constants.EventKind.Replacing && m_RSubscriptions.Find(x => x.EventName.Equals(subsData.EventName)) != null)
                    return;
                if (subsData.EventKind == Constants.EventKind.Queueing && m_QSubscriptions.Find(x => x.EventName.Equals(subsData.EventName)) != null)
                    return;                

                if (m_Site.EventController.AuthorizeSCSubscription(subsData))
                {
                    if (subsData.EventKind == 1)
                    {
                        RSubscription subscription = new RSubscription(subsData);
                        m_RSubscriptions.Add(subscription);

                        if (subscription.EventInstance != null)
                        {
                            m_deliveringEventsCounter++;
                            subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                            subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                            ThreadPool.QueueUserWorkItem(delegate { SendREvent(subscription); });
                        }
                    }
                    else if (subsData.EventKind == 2)
                    {
                        QSubscription subscription = new QSubscription(subsData);
                        m_QSubscriptions.Add(subscription);

                        if (subscription.EventInstance != null)
                        {
                            m_deliveringEventsCounter++;
                            subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                            subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                            ThreadPool.QueueUserWorkItem(delegate { SendQEvent(subscription); });
                        }
                    }                    
                }
                else
                {
                    ASNEncoder asnEncoder = new ASNEncoder();
                    asnEncoder.Sequence.IA5String(subsData.EventName);
                    m_Channel.Send(MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.ILLEGAL_SUBSCRIPTION, asnEncoder));                
                }
            }
        }

        void ProcessMessage_RemoveSubscription(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Sequence(message, 2);
            int eventKind = asnSequence.Int32();
            string eventName = asnSequence.IA5String(1, 256);
            asnSequence.End();

            lock (cont_mutex)
            {
                if (eventKind == Constants.EventKind.Replacing)
                {
                    RSubscription subscription = m_RSubscriptions.Find(x => x.EventName.Equals(eventName));
                    if (subscription == null)
                        return;
                    
                    m_RSubscriptions.Remove(subscription);
                    subscription.Removed = true;

                    if (subscription.EventInstance != null)
                        m_deliveringEventsCounter--;
                }
                else if (eventKind == Constants.EventKind.Queueing)
                {
                    QSubscription subscription = m_QSubscriptions.Find(x => x.EventName.Equals(eventName));
                    if (subscription == null)
                        return;
                    
                    m_QSubscriptions.Remove(subscription);
                    subscription.Removed = true;

                    if (subscription.EventInstance != null)
                        m_deliveringEventsCounter--;
                }                
            }
        }

        SoftnetMessage EncodeMessage_ReplacingEvent(RSubscription subscription, REventInstance eventInstance, long eventAge, byte[] arguments)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.IA5String(subscription.EventName);
            asnSequence.Int64(eventInstance.EventId);
            asnSequence.OctetString(subscription.TransactionUid);
            asnSequence.Int64(eventInstance.InstanceId);
            asnSequence.Int64(eventInstance.ServiceId);
            asnSequence.Int64(eventAge);
            asnSequence.GndTime(eventInstance.CreatedDate);
            if (arguments != null)
                asnSequence.OctetString(1, arguments);
            return MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.REPLACING_EVENT, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ReplacingNullEvent(RSubscription subscription, REventInstance eventInstance, long eventAge)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.IA5String(subscription.EventName);
            asnSequence.Int64(eventInstance.EventId);
            asnSequence.OctetString(subscription.TransactionUid);
            asnSequence.Int64(eventInstance.InstanceId);
            asnSequence.Int64(eventInstance.ServiceId);
            asnSequence.Int64(eventAge);
            asnSequence.GndTime(eventInstance.CreatedDate);
            return MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.REPLACING_NULL_EVENT, asnEncoder);
        }

        SoftnetMessage EncodeMessage_QueueingEvent(QSubscription subscription, QEventInstance eventInstance, long eventAge, byte[] arguments)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.IA5String(subscription.EventName);
            asnSequence.Int64(eventInstance.EventId);
            asnSequence.OctetString(subscription.TransactionUid);
            asnSequence.Int64(eventInstance.InstanceId);
            asnSequence.Int64(eventInstance.ServiceId);
            asnSequence.Int64(eventAge);
            asnSequence.GndTime(eventInstance.CreatedDate);
            if (arguments != null)
                asnSequence.OctetString(1, arguments);
            return MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.QUEUEING_EVENT, asnEncoder);
        }

        void OnMessageReceived(byte[] message)
        {
            byte messageTag = message[1];
            if (messageTag == Constants.Client.EventController.REPLACING_EVENT_ACK)
            {
                ProcessMessage_ReplacingEventAck(message);
            }
            else if (messageTag == Constants.Client.EventController.QUEUEING_EVENT_ACK)
            {
                ProcessMessage_QueueingEventAck(message);
            }
            else if (messageTag == Constants.Client.EventController.ADD_SUBSCRIPTION)
            {
                ProcessMessage_AddSubscription(message);
            }
            else if (messageTag == Constants.Client.EventController.REMOVE_SUBSCRIPTION)
            {
                ProcessMessage_RemoveSubscription(message);
            }
            else if (messageTag == Constants.Client.EventController.EVENT_REJECTED)
            {
                ProcessMessage_EventRejected(message);
            }
            else if (messageTag == Constants.Client.EventController.SUBSCRIPTIONS)
            {
                ProcessMessage_Subscriptions(message);
            }
            else
                throw new SoftnetException(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR);
        }
    }
}
