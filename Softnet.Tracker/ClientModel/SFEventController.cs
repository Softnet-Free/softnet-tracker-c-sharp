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
    class SFEventController : IEventClientController
    {
        Site m_Site;
        Client m_Client;
        IChannel m_Channel;

        object site_mutex;
        object cont_mutex;

        List<SubscriptionData> m_SubscriptionList;

        List<RSubscription> m_RSubscriptions;
        List<QSubscription> m_QSubscriptions;
        List<PSubscription> m_PSubscriptions;
        bool m_Synchronized;
        int m_DeliveringEventsCounter;

        public SFEventController()
        {
            m_RSubscriptions = new List<RSubscription>();
            m_QSubscriptions = new List<QSubscription>();
            m_PSubscriptions = new List<PSubscription>();
            m_Synchronized = false;
            m_DeliveringEventsCounter = 0;
        }

        public void Init(Site site, Client client, IChannel channel)
        {
            m_Site = site;
            m_Client = client;
            m_Channel = channel;

            site_mutex = m_Site.mutex;
            cont_mutex = m_Site.EventController.GetMutex();

            ThreadPool.QueueUserWorkItem(delegate { Load(); });
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
                            m_DeliveringEventsCounter++;
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
                            m_DeliveringEventsCounter++;
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
                if (m_DeliveringEventsCounter == 0)
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

                foreach (PSubscription subscription in m_PSubscriptions)
                {
                    if (subscription.EventInstance != null && subscription.DeliveryExpirationTime < currentTime)
                    {
                        subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                        subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                        ThreadPool.QueueUserWorkItem(delegate { SendPEvent(subscription); });
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

            m_DeliveringEventsCounter++;
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

            m_DeliveringEventsCounter++;
            subscription.EventInstance = eventInstance;
            subscription.TransactionUid = transactionUid;
            subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
            m_Channel.Send(message);
        }

        public void OnPEventRaised(PEventInstance eventInstance, byte[] arguments)
        {
            if (m_Synchronized == false)
                return;

            PSubscription subscription = m_PSubscriptions.Find(x => x.EventId == eventInstance.EventId);
            if (subscription == null)
                return;
            if (subscription.EventInstance != null)
                return;

            m_DeliveringEventsCounter++;
            subscription.EventInstance = eventInstance;
            subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
            subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
            m_Channel.Send(EncodeMessage_PrivateEvent(subscription, eventInstance, 0, arguments));
        }

        void Load()
        {
            try
            {
                m_SubscriptionList = new List<SubscriptionData>();
                SoftnetRegistry.Client_GetSubscriptions(m_Client.Id, m_SubscriptionList);

                m_Channel.RegisterModule(Constants.Client.EventController.ModuleId, OnMessageReceived);

                if (m_SubscriptionList.Count > 0)
                {
                    m_SubscriptionList.Sort();

                    ASNEncoder asnEncoder = new ASNEncoder();
                    SequenceEncoder asnRoot = asnEncoder.Sequence;
                    foreach (SubscriptionData subsData in m_SubscriptionList)
                    {
                        SequenceEncoder asnSubscription = asnRoot.Sequence();
                        asnSubscription.Int32(subsData.EventKind);
                        asnSubscription.IA5String(subsData.EventName);
                    }
                    byte[] hash = SHA1Hash.Compute(asnEncoder.GetEncoding());

                    asnEncoder = new ASNEncoder();
                    asnEncoder.Sequence.OctetString(1, hash);
                    m_Channel.Send(MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.SYNC, asnEncoder));
                }
                else
                {
                    ASNEncoder asnEncoder = new ASNEncoder();
                    m_Channel.Send(MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.SYNC, asnEncoder));
                }
            }
            catch(SoftnetException ex)
            {
                m_Client.Remove(ex.ErrorCode);
            }
        }

        void SetupSubscriptions()
        {
            lock (site_mutex)
            {
                lock (cont_mutex)
                {
                    foreach (SubscriptionData subsData in m_SubscriptionList)
                    {
                        if (subsData.EventKind == 1)
                        {
                            RSubscription subscription = new RSubscription(subsData);
                            m_RSubscriptions.Add(subscription);
                            m_Site.EventController.AuthorizeRSubscription(subscription, m_Client.User.authority);

                            if (subscription.EventInstance != null)
                            {
                                m_DeliveringEventsCounter++;
                                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                                ThreadPool.QueueUserWorkItem(delegate { SendREvent(subscription); });
                            }
                        }
                        else if (subsData.EventKind == 2)
                        {
                            QSubscription subscription = new QSubscription(subsData);
                            m_QSubscriptions.Add(subscription);
                            m_Site.EventController.AuthorizeQSubscription(subscription, m_Client.User.authority);

                            if (subscription.EventInstance != null)
                            {
                                m_DeliveringEventsCounter++;
                                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                                ThreadPool.QueueUserWorkItem(delegate { SendQEvent(subscription); });
                            }
                        }                        
                        else // subsData.EventKind == 4
                        {
                            PSubscription subscription = new PSubscription(subsData);
                            m_PSubscriptions.Add(subscription);
                            m_Site.EventController.InitPSubscription(subscription, m_Client.Id);

                            if (subscription.EventInstance != null)
                            {
                                m_DeliveringEventsCounter++;
                                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                                ThreadPool.QueueUserWorkItem(delegate { SendPEvent(subscription); });
                            }
                        }
                    }

                    m_Synchronized = true;
                }
            }

            m_SubscriptionList = null;
        }

        void SendREvent(RSubscription subscription)
        {
            REventInstance eventInstance = subscription.EventInstance;
            long eventAge = (eventInstance.SetupTimeTicks - eventInstance.CreatedTimeTicks) * 10 + (SystemClock.Seconds - eventInstance.SetupTimeSeconds);

            if (eventInstance.IsNull)
            {
                m_Channel.Send(EncodeMessage_ReplacingNullEvent(subscription, eventInstance, eventAge));
            }
            else if(eventInstance.HasArguments)
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
                            SoftnetRegistry.Client_SaveEventAcknowledgment(m_Client.Id, eventInstance.EventId, eventInstance.InstanceId);

                            lock (cont_mutex)
                            {
                                if (subscription.Removed)
                                    return;

                                subscription.EventInstance = null;
                                subscription.TransactionUid = null;
                                m_DeliveringEventsCounter--;

                                subscription.DeliveredInstanceId = eventInstance.InstanceId;

                                if (subscription.Authorized == false)
                                    return;

                                subscription.EventInstance = m_Site.EventController.GetNextEvent(eventInstance);
                                if (subscription.EventInstance == null)
                                    return;

                                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                                m_DeliveringEventsCounter++;
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
                            SoftnetRegistry.Client_SaveEventAcknowledgment(m_Client.Id, eventInstance.EventId, eventInstance.InstanceId);

                            lock (cont_mutex)
                            {
                                if (subscription.Removed)
                                    return;

                                subscription.DeliveredInstanceId = eventInstance.InstanceId;

                                subscription.TransactionUid = null;
                                subscription.EventInstance = null;
                                m_DeliveringEventsCounter--;

                                if (subscription.Authorized == false)
                                    return;

                                subscription.EventInstance = m_Site.EventController.GetNextEvent(eventInstance);
                                if (subscription.EventInstance == null)
                                    return;

                                m_DeliveringEventsCounter++;
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

        void SendPEvent(PSubscription subscription)
        {
            PEventInstance eventInstance = subscription.EventInstance;
            long eventAge = (eventInstance.SetupTimeTicks - eventInstance.CreatedTimeTicks) * 10 + (SystemClock.Seconds - eventInstance.SetupTimeSeconds);

            if (eventInstance.HasArguments)
            {
                if (eventInstance.Arguments != null)
                {
                    m_Channel.Send(EncodeMessage_PrivateEvent(subscription, eventInstance, eventAge, eventInstance.Arguments));
                }
                else
                {
                    try
                    {
                        Container<byte[]> eventArgs = new Container<byte[]>();
                        int resultCode = SoftnetRegistry.Client_GetPEventArguments(eventInstance.InstanceId, eventArgs);
                        if (resultCode == 0)
                        {
                            m_Channel.Send(EncodeMessage_PrivateEvent(subscription, eventInstance, eventAge, eventArgs.Obj));
                        }
                        else
                        {
                            SoftnetRegistry.Client_SaveEventAcknowledgment(m_Client.Id, eventInstance.EventId, eventInstance.InstanceId);

                            lock (cont_mutex)
                            {
                                if (subscription.Removed)
                                    return;

                                subscription.DeliveredInstanceId = eventInstance.InstanceId;

                                m_DeliveringEventsCounter--;
                                subscription.TransactionUid = null;
                                subscription.EventInstance = null;

                                subscription.EventInstance = m_Site.EventController.GetNextEvent(eventInstance, m_Client.Id);
                                if (subscription.EventInstance == null)
                                    return;

                                m_DeliveringEventsCounter++;
                                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                            }

                            ThreadPool.QueueUserWorkItem(delegate { SendPEvent(subscription); });                            
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
                m_Channel.Send(EncodeMessage_PrivateEvent(subscription, eventInstance, eventAge, null));
            }
        }

        void ProcessMessage_SyncOk(byte[] message)
        {
            SetupSubscriptions();
        }

        void ProcessMessage_Subscriptions(byte[] message)
        {
            List<SubscriptionData> receivedSubscriptionList = new List<SubscriptionData>();

            SequenceDecoder asnRoot = ASNDecoder.Create(message, 2);
            while (asnRoot.HasNext())
            {
                SequenceDecoder asnSubscription = asnRoot.Sequence();
                int eventKind = asnSubscription.Int32();
                string eventName = asnSubscription.IA5String(1, 256);
                asnSubscription.End();

                if (receivedSubscriptionList.Find(x => x.EventName.Equals(eventName)) != null)
                    throw new SoftnetException(ErrorCodes.ENDPOINT_DATA_INCONSISTENT);
                
                SubscriptionData subsData = new SubscriptionData();
                subsData.EventKind = eventKind;
                subsData.EventName = eventName;
                receivedSubscriptionList.Add(subsData);
            }
            asnRoot.End();

            for (int i = m_SubscriptionList.Count - 1; i >= 0; i--)
            {
                SubscriptionData receivedSubscription = receivedSubscriptionList.Find(x => x.EventKind == m_SubscriptionList[i].EventKind && x.EventName.Equals(m_SubscriptionList[i].EventName));
                if (receivedSubscription != null)
                {
                    receivedSubscriptionList.Remove(receivedSubscription);
                }
                else
                {
                    SoftnetRegistry.Client_RemoveSubscription(m_Client.Id, m_SubscriptionList[i].EventId);
                    m_SubscriptionList.RemoveAt(i);
                }
            }

            foreach (SubscriptionData subsData in receivedSubscriptionList)
            {
                int resultCode = SoftnetRegistry.Client_AddSubscription(m_Site.Id, m_Client.Id, subsData);
                if (resultCode == 0)
                {
                    m_SubscriptionList.Add(subsData);
                }
                else
                {
                    ASNEncoder asnEncoder = new ASNEncoder();
                    asnEncoder.Sequence.IA5String(subsData.EventName);
                    m_Channel.Send(MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.ILLEGAL_SUBSCRIPTION, asnEncoder));
                }
            }

            SetupSubscriptions();
        }

        void ProcessMessage_AddSubscription(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            int eventKind = asnSequence.Int32(1, 4);
            string eventName = asnSequence.IA5String(1, 256);
            asnSequence.End();

            lock (cont_mutex)
            {
                if (eventKind == 1 && m_RSubscriptions.Find(x => x.EventName.Equals(eventName)) != null)
                    return;
                if (eventKind == 2 && m_QSubscriptions.Find(x => x.EventName.Equals(eventName)) != null)
                    return;
                if (eventKind == 4 && m_PSubscriptions.Find(x => x.EventName.Equals(eventName)) != null)
                    return;
            }

            SubscriptionData subsData = new SubscriptionData();
            subsData.EventKind = eventKind;
            subsData.EventName = eventName;       
         
            int resultCode = SoftnetRegistry.Client_AddSubscription(m_Site.Id, m_Client.Id, subsData);
            if (resultCode == 0)
            {
                if (subsData.EventKind == 1)
                {
                    lock (site_mutex)
                    {
                        lock (cont_mutex)
                        {
                            RSubscription subscription = new RSubscription(subsData);
                            m_RSubscriptions.Add(subscription);
                            m_Site.EventController.AuthorizeRSubscription(subscription, m_Client.User.authority);

                            if (subscription.EventInstance != null)
                            {
                                m_DeliveringEventsCounter++;
                                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                                ThreadPool.QueueUserWorkItem(delegate { SendREvent(subscription); });
                            }
                        }
                    }
                }
                else if (subsData.EventKind == 2)
                {
                    lock (site_mutex)
                    {
                        lock (cont_mutex)
                        {
                            QSubscription subscription = new QSubscription(subsData);
                            m_QSubscriptions.Add(subscription);
                            m_Site.EventController.AuthorizeQSubscription(subscription, m_Client.User.authority);

                            if (subscription.EventInstance != null)
                            {
                                m_DeliveringEventsCounter++;
                                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                                ThreadPool.QueueUserWorkItem(delegate { SendQEvent(subscription); });
                            }
                        }
                    }
                }                
                else // subsData.EventKind == 4
                {
                    lock (cont_mutex)
                    {
                        PSubscription subscription = new PSubscription(subsData);
                        m_PSubscriptions.Add(subscription);
                        m_Site.EventController.InitPSubscription(subscription, m_Client.Id);

                        if (subscription.EventInstance != null)
                        {
                            m_DeliveringEventsCounter++;
                            subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                            subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
                            ThreadPool.QueueUserWorkItem(delegate { SendPEvent(subscription); });
                        }
                    }
                }
            }
            else
            {
                ASNEncoder asnEncoder = new ASNEncoder();
                asnEncoder.Sequence.IA5String(eventName);
                m_Channel.Send(MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.ILLEGAL_SUBSCRIPTION, asnEncoder));
            }            
        }

        void ProcessMessage_RemoveSubscription(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            int eventKind = asnSequence.Int32(1, 4);
            string eventName = asnSequence.IA5String(1, 256);
            asnSequence.End();

            if (eventKind == Constants.EventKind.Replacing)
            {
                RSubscription subscription = null;
                lock (cont_mutex)
                {
                    subscription = m_RSubscriptions.Find(x => x.EventName.Equals(eventName));
                    if (subscription == null)
                        return;
                        
                    m_RSubscriptions.Remove(subscription);
                    subscription.Removed = true;

                    if (subscription.EventInstance != null)
                        m_DeliveringEventsCounter--;
                }

                SoftnetRegistry.Client_RemoveSubscription(m_Client.Id, subscription.EventId);
            }
            else if (eventKind == Constants.EventKind.Queueing)
            {
                QSubscription subscription = null;
                lock (cont_mutex)
                {
                    subscription = m_QSubscriptions.Find(x => x.EventName.Equals(eventName));
                    if (subscription == null)
                        return;
                    
                    m_QSubscriptions.Remove(subscription);
                    subscription.Removed = true;

                    if (subscription.EventInstance != null)
                        m_DeliveringEventsCounter--;                    
                }

                SoftnetRegistry.Client_RemoveSubscription(m_Client.Id, subscription.EventId);
            }
            else if (eventKind == Constants.EventKind.Private)
            {
                PSubscription subscription = null;
                lock (cont_mutex)
                {
                    subscription = m_PSubscriptions.Find(x => x.EventName.Equals(eventName));
                    if (subscription == null)
                        return;
                    
                    m_PSubscriptions.Remove(subscription);
                    subscription.Removed = true;

                    if (subscription.EventInstance != null)
                        m_DeliveringEventsCounter--;
                }

                SoftnetRegistry.Client_RemoveSubscription(m_Client.Id, subscription.EventId);
            }
        }

        void ProcessMessage_ReplacingEventAck(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
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
            }

            REventInstance deliveredEventInstance = subscription.EventInstance;
            SoftnetRegistry.Client_SaveEventAcknowledgment(m_Client.Id, deliveredEventInstance.EventId, deliveredEventInstance.InstanceId);

            lock (cont_mutex)
            {
                if (subscription.Removed)
                    return;

                subscription.DeliveredInstanceId = deliveredEventInstance.InstanceId;

                m_DeliveringEventsCounter--;
                subscription.TransactionUid = null;
                subscription.EventInstance = null;

                if (subscription.Authorized == false)
                    return;

                subscription.EventInstance = m_Site.EventController.GetNextEvent(deliveredEventInstance);
                if (subscription.EventInstance == null)
                    return;

                m_DeliveringEventsCounter++;
                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
            }

            SendREvent(subscription);
        }

        void ProcessMessage_QueueingEventAck(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
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
            }

            QEventInstance deliveredEventInstance = subscription.EventInstance;
            SoftnetRegistry.Client_SaveEventAcknowledgment(m_Client.Id, deliveredEventInstance.EventId, deliveredEventInstance.InstanceId);

            lock (cont_mutex)
            {
                if (subscription.Removed)
                    return;

                subscription.DeliveredInstanceId = deliveredEventInstance.InstanceId;

                m_DeliveringEventsCounter--;
                subscription.TransactionUid = null;
                subscription.EventInstance = null;

                if (subscription.Authorized == false)
                    return;

                subscription.EventInstance = m_Site.EventController.GetNextEvent(deliveredEventInstance);
                if (subscription.EventInstance == null)                
                    return;

                m_DeliveringEventsCounter++;
                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
            }

            SendQEvent(subscription);
        }

        void ProcessMessage_PrivateEventAck(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
            long eventId = asnSequence.Int64();
            byte[] transactionUid = asnSequence.OctetString(16);
            asnSequence.End();

            PSubscription subscription = null;
            lock (cont_mutex)
            {
                subscription = m_PSubscriptions.Find(x => x.EventId == eventId);
                if (subscription == null)
                    return;

                if (!(subscription.EventInstance != null && ByteArray.Equals(transactionUid, subscription.TransactionUid)))
                    return;
            }

            PEventInstance deliveredEventInstance = subscription.EventInstance;
            SoftnetRegistry.Client_SaveEventAcknowledgment(m_Client.Id, deliveredEventInstance.EventId, deliveredEventInstance.InstanceId);

            lock (cont_mutex)
            {
                if (subscription.Removed)
                    return;

                subscription.DeliveredInstanceId = deliveredEventInstance.InstanceId;

                m_DeliveringEventsCounter--;
                subscription.TransactionUid = null;
                subscription.EventInstance = null;

                subscription.EventInstance = m_Site.EventController.GetNextEvent(deliveredEventInstance, m_Client.Id);
                if (subscription.EventInstance == null)                
                    return;

                m_DeliveringEventsCounter++;
                subscription.TransactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                subscription.DeliveryExpirationTime = SystemClock.Seconds + 300;
            }

            SendPEvent(subscription);
        }

        void ProcessMessage_EventRejected(byte[] message)
        {
            SequenceDecoder asnSequence = ASNDecoder.Create(message, 2);
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
                    m_DeliveringEventsCounter--;
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
                    m_DeliveringEventsCounter--;
                }
            }
            else if (eventKind == Constants.EventKind.Private)
            {
                lock (cont_mutex)
                {
                    PSubscription subscription = m_PSubscriptions.Find(x => x.EventId == eventId);
                    if (subscription == null)
                        return;

                    if (!(subscription.EventInstance != null && ByteArray.Equals(transactionUid, subscription.TransactionUid)))
                        return;

                    m_PSubscriptions.Remove(subscription);
                    subscription.Removed = true;
                    m_DeliveringEventsCounter--;
                }
            }
            else
                throw new FormatException();
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

        SoftnetMessage EncodeMessage_PrivateEvent(PSubscription subscription, PEventInstance eventInstance, long eventAge, byte[] arguments)
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
            return MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.PRIVATE_EVENT, asnEncoder);
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
            else if (messageTag == Constants.Client.EventController.PRIVATE_EVENT_ACK)
            {
                ProcessMessage_PrivateEventAck(message);
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
            else if (messageTag == Constants.Client.EventController.SYNC_OK)
            {
                ProcessMessage_SyncOk(message);
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
