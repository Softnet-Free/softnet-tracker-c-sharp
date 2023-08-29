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

using Softnet.Tracker.Core;
using Softnet.Tracker.ClientModel;
using Softnet.ServerKit;
using Softnet.Asn;

namespace Softnet.Tracker.SiteModel
{
    class SSEventController : IEventController
    {
        public SSEventController(Site site, List<Client> clients, List<Client> statelessClients) 
        {
            m_Site = site;
            site_mutex = site.mutex;
            m_Clients = clients;
            m_StatelessClients = statelessClients;
        }

        public void Init(SiteIData siteIData)
        {
            var rCollectors = new List<REventCollector>();
            var qCollectors = new List<QEventCollector>();
            var pCollectors = new List<PEventCollector>();

            List<EventData> eventList = siteIData.EventList;
            foreach (EventData eventData in eventList)
            {
                if (eventData.EventKind == 1)
                {
                    var collector = new REventCollector(eventData);
                    rCollectors.Add(collector);
                }
                else if (eventData.EventKind == 2)
                {
                    var collector = new QEventCollector(eventData);
                    qCollectors.Add(collector);
                }
                else if (eventData.EventKind == 4)
                {
                    var collector = new PEventCollector(eventData);
                    pCollectors.Add(collector);
                }
            }

            List<REventInstance> rEventInstances = siteIData.REventInstances;
            foreach (REventInstance eventInstance in rEventInstances)
            {
                REventCollector rCollector = rCollectors.Find(x => x.EventId == eventInstance.EventId);
                if (rCollector != null)
                {
                    if (rCollector.EventInstance != null)
                    {
                        SoftnetRegistry.EventController_DeleteREventInstance(rCollector.EventInstance.InstanceId);
                    }
                    rCollector.EventInstance = eventInstance;
                }
            }

            List<QEventInstance> qEventInstances = siteIData.QEventInstances;
            foreach (QEventInstance eventInstance in qEventInstances)
            {
                QEventCollector qCollector = qCollectors.Find(x => x.EventId == eventInstance.EventId);
                if (qCollector != null)
                {                    
                    if (qCollector.EventInstances.Count == qCollector.QueueSize)
                    {
                        SoftnetRegistry.EventController_DeleteQEventInstance(qCollector.EventInstances[0].InstanceId);
                        qCollector.EventInstances.RemoveAt(0);
                    }
                    qCollector.EventInstances.Add(eventInstance);
                }
            }

            List<PEventInstance> pEventInstances = siteIData.PEventInstances;
            foreach (PEventInstance eventInstance in pEventInstances)
            {
                PEventCollector pCollector = pCollectors.Find(x => x.EventId == eventInstance.EventId);
                if (pCollector != null)
                {
                    if (pCollector.EventInstances.Count == 1000)
                    {
                        SoftnetRegistry.EventController_DeletePEventInstance(pCollector.EventInstances[0].InstanceId);
                        pCollector.EventInstances.RemoveAt(0);
                    }
                    pCollector.EventInstances.Add(eventInstance);
                }
            }

            m_REventCollectors = rCollectors;
            m_QEventCollectors = qCollectors;
            m_PEventCollectors = pCollectors;

            m_TimeToDieTicks = 0;
            Monitor();
        }

        public void AuthorizeRSubscription(RSubscription subscription, UserAuthority userAuthority)
        {
            REventCollector collector = m_REventCollectors.Find(x => x.EventId == subscription.EventId);
            if (collector == null)
            {
                subscription.Authorized = false;
                return;
            }

            if (collector.GuestAccess == 1)
            {
                if (userAuthority.isGuest)
                {
                    subscription.Authorized = false;
                    return;
                }
            }
            else if (collector.GuestAccess == 2)
            {
                if (userAuthority.isStatelessGuest)
                {
                    subscription.Authorized = false;
                    return;
                }
            }
            else if (collector.Roles != null)
            {
                if (userAuthority.roles == null)
                {
                    subscription.Authorized = false;
                    return;
                }

                bool containsRole = false;
                foreach (long roleId in collector.Roles)
                {
                    if (userAuthority.roles.Contains(roleId))
                    {
                        containsRole = true;
                        break;
                    }
                }
                if (containsRole == false)
                {
                    subscription.Authorized = false;
                    return;
                }
            }

            subscription.Authorized = true;

            if (collector.EventInstance != null && subscription.EventInstance == null && collector.EventInstance.InstanceId > subscription.DeliveredInstanceId)
            {
                subscription.EventInstance = collector.EventInstance;
            }
        }

        public void AuthorizeQSubscription(QSubscription subscription, UserAuthority userAuthority)
        {
            QEventCollector collector = m_QEventCollectors.Find(x => x.EventId == subscription.EventId);
            if (collector == null)
            {
                subscription.Authorized = false;
                return;
            }

            if (collector.GuestAccess == 1)
            {
                if (userAuthority.isGuest)
                {
                    subscription.Authorized = false;
                    return;
                }
            }
            else if (collector.GuestAccess == 2)
            {
                if (userAuthority.isStatelessGuest)
                {
                    subscription.Authorized = false;
                    return;
                }
            }
            else if (collector.Roles != null)
            {
                if (userAuthority.roles == null)
                {
                    subscription.Authorized = false;
                    return;
                }

                bool containsRole = false;
                foreach (long roleId in collector.Roles)
                {
                    if (userAuthority.roles.Contains(roleId))
                    {
                        containsRole = true;
                        break;
                    }
                }
                if (containsRole == false)
                {
                    subscription.Authorized = false;
                    return;
                }
            }

            subscription.Authorized = true;

            if (subscription.EventInstance == null && collector.EventInstances.Count > 0)
            {
                foreach (QEventInstance eventInstance in collector.EventInstances)
                {
                    if (eventInstance.InstanceId > subscription.DeliveredInstanceId)
                    {
                        subscription.EventInstance = eventInstance;
                        break;
                    }
                }                    
            }
        }

        public void InitPSubscription(PSubscription subscription, long clientId)
        {
            PEventCollector collector = m_PEventCollectors.Find(x => x.EventId == subscription.EventId);
            if (collector == null)
                return;

            if (collector.EventInstances.Count == 0)
                return;

            foreach (PEventInstance eventInstance in collector.EventInstances)
            {
                if (eventInstance.ClientId == clientId && eventInstance.InstanceId > subscription.DeliveredInstanceId)
                {
                    subscription.EventInstance = eventInstance;
                    break;
                }
            }            
        }

        public bool AuthorizeSCSubscription(SCSubscriptionData subscription)
        {
            if (subscription.EventKind == Constants.EventKind.Replacing)
            {
                REventCollector collector = m_REventCollectors.Find(x => x.EventName.Equals(subscription.EventName));
                if (collector != null)
                {
                    subscription.EventId = collector.EventId;

                    if (collector.Roles == null && collector.GuestAccess == 0)
                    {
                        subscription.Authorized = true;

                        if (collector.EventInstance != null && collector.EventInstance.InstanceId > subscription.DeliveredInstanceId)
                        {
                            subscription.EventInstance = collector.EventInstance;
                        }
                    }
                    else
                    {
                        subscription.Authorized = false;
                    }

                    return true;
                }
            }
            else if (subscription.EventKind == Constants.EventKind.Queueing)
            {
                QEventCollector collector = m_QEventCollectors.Find(x => x.EventName.Equals(subscription.EventName));
                if (collector != null)
                {
                    subscription.EventId = collector.EventId;

                    if (collector.Roles == null && collector.GuestAccess == 0)
                    {
                        subscription.Authorized = true;

                        if (collector.EventInstances.Count > 0)
                        {
                            foreach (QEventInstance eventInstance in collector.EventInstances)
                            {
                                if (eventInstance.InstanceId > subscription.DeliveredInstanceId)
                                {
                                    subscription.EventInstance = eventInstance;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        subscription.Authorized = false;
                    }

                    return true;
                }
            }            

            return false;
        }

        public void AcceptREvent(EventIData eventIData)
        {
            REventCollector collector = m_REventCollectors.Find(x => x.EventName.Equals(eventIData.Name));
            if (collector == null)
                return;

            bool instanceInvalidated = false;
            long invalidInstanceId = 0;
            lock (cont_mutex)
            {
                if (collector.EventInstance != null)
                {
                    if (eventIData.IsNull && collector.EventInstance.IsNull)
                        return;
                    invalidInstanceId = collector.EventInstance.InstanceId;
                    collector.EventInstance = null;
                    instanceInvalidated = true;
                }
            }

            try
            {
                eventIData.EventId = collector.EventId;

                if (instanceInvalidated)
                    SoftnetRegistry.EventController_SaveREventInstance(eventIData, invalidInstanceId);
                else
                    SoftnetRegistry.EventController_SaveREventInstance(eventIData);
                
                REventInstance eventInstance = REventInstance.InstantiateNewEvent(eventIData);
                
                if (eventInstance.IsNull == false)
                {
                    byte[] transactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                    SoftnetMessage message = EncodeMessage_ReplacingEvent(eventIData, transactionUid);

                    lock (site_mutex)
                    {
                        lock (cont_mutex)
                        {
                            collector.EventInstance = eventInstance;

                            foreach (Client client in m_Clients)
                                if (client.Online)
                                    client.EventController.OnREventRaised(eventInstance, message, transactionUid);

                            foreach (Client client in m_StatelessClients)
                                if (client.Online)
                                    client.EventController.OnREventRaised(eventInstance, message, transactionUid);
                        }
                    }
                }
                else
                {
                    byte[] transactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                    SoftnetMessage message = EncodeMessage_ReplacingNullEvent(eventIData, transactionUid);

                    lock (site_mutex)
                    {
                        lock (cont_mutex)
                        {
                            collector.EventInstance = eventInstance;

                            foreach (Client client in m_Clients)
                                if (client.Online)
                                    client.EventController.OnREventRaised(eventInstance, message, transactionUid);

                            foreach (Client client in m_StatelessClients)
                                if (client.Online)
                                    client.EventController.OnREventRaised(eventInstance, message, transactionUid);
                        }
                    }
                }
            }
            catch (SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }
        }

        public void AcceptQEvent(EventIData eventIData)
        {
            QEventCollector collector = m_QEventCollectors.Find(x => x.EventName.Equals(eventIData.Name));
            if (collector == null)
                return;

            bool instanceInvalidated = false;
            long invalidInstanceId = 0;
            lock (cont_mutex)
            {
                if (collector.EventInstances.Count >= collector.QueueSize)
                {
                    invalidInstanceId = collector.EventInstances[0].InstanceId;
                    collector.EventInstances.RemoveAt(0);
                    instanceInvalidated = true;
                }
            }

            try
            {
                eventIData.EventId = collector.EventId;

                if (instanceInvalidated)
                    SoftnetRegistry.EventController_SaveQEventInstance(eventIData, invalidInstanceId);
                else
                    SoftnetRegistry.EventController_SaveQEventInstance(eventIData);

                QEventInstance eventInstance = QEventInstance.InstantiateNewEvent(eventIData);

                byte[] transactionUid = ByteConverter.GetBytes(Guid.NewGuid());
                SoftnetMessage message = EncodeMessage_QueueingEvent(eventIData, transactionUid);

                lock (site_mutex)
                {
                    lock (cont_mutex)
                    {
                        if (collector.EventInstances.Count == 0)
                        {
                            long timeToDieTicks = eventInstance.CreatedTimeTicks + collector.LifeTicks;
                            if (timeToDieTicks < m_TimeToDieTicks)
                                m_TimeToDieTicks = timeToDieTicks;                        
                        }

                        collector.EventInstances.Add(eventInstance);

                        foreach (Client client in m_Clients)
                            if (client.Online)
                                client.EventController.OnQEventRaised(eventInstance, message, transactionUid);

                        foreach (Client client in m_StatelessClients)
                            if (client.Online)
                                client.EventController.OnQEventRaised(eventInstance, message, transactionUid);
                    }
                }
            }
            catch(SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }
        }

        public void AcceptPEvent(EventIData eventIData)
        {
            PEventCollector collector = m_PEventCollectors.Find(x => x.EventName.Equals(eventIData.Name));
            if (collector == null)
                return;

            bool instanceInvalidated = false;
            long invalidInstanceId = 0;
            lock (cont_mutex)
            {
                if (collector.EventInstances.Count >= 1000)
                {
                    invalidInstanceId = collector.EventInstances[0].InstanceId;
                    collector.EventInstances.RemoveAt(0);
                    instanceInvalidated = true;
                }
            }

            try
            {
                eventIData.EventId = collector.EventId;

                if (instanceInvalidated)
                    SoftnetRegistry.EventController_SavePEventInstance(eventIData, invalidInstanceId);
                else
                    SoftnetRegistry.EventController_SavePEventInstance(eventIData);

                PEventInstance eventInstance = PEventInstance.InstantiateNewEvent(eventIData);
                lock (site_mutex)
                {
                    lock (cont_mutex)
                    {
                        collector.EventInstances.Add(eventInstance);

                        long timeToDieTicks = eventInstance.CreatedTimeTicks + collector.LifeTicks;
                        if (timeToDieTicks < m_TimeToDieTicks)
                            m_TimeToDieTicks = timeToDieTicks;

                        Client client = m_Clients.Find(x => x.Id == eventIData.ClientId);
                        if (client != null)
                        {
                            if (client.Online)
                                client.EventController.OnPEventRaised(eventInstance, eventIData.Arguments);
                            return;
                        }
                    }
                }
            }
            catch (SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }
        }

        public REventInstance GetNextEvent(REventInstance deliveredEventInstance)
        {
            REventCollector collector = m_REventCollectors.Find(x => x.EventId == deliveredEventInstance.EventId);
            if (collector == null)
                return null;

            if (collector.EventInstance == null)
                return null;

            if (collector.EventInstance.InstanceId > deliveredEventInstance.InstanceId)
                return collector.EventInstance;

            return null;
        }

        public QEventInstance GetNextEvent(QEventInstance deliveredEventInstance)
        {
            QEventCollector collector = m_QEventCollectors.Find(x => x.EventId == deliveredEventInstance.EventId);
            if (collector == null)
                return null;

            foreach (QEventInstance eventInstance in collector.EventInstances)
            {
                if (eventInstance.InstanceId > deliveredEventInstance.InstanceId)
                    return eventInstance;
            }

            return null;
        }

        public PEventInstance GetNextEvent(PEventInstance deliveredEventInstance, long clientId)
        {
            PEventCollector collector = m_PEventCollectors.Find(x => x.EventId == deliveredEventInstance.EventId);
            if (collector == null)
                return null;

            int index = collector.EventInstances.FindIndex(x => x.InstanceId == deliveredEventInstance.InstanceId);
            if (index >= 0)
            {
                PEventInstance nextEventInstance = null;
                
                List<PEventInstance> eventInstances = collector.EventInstances;
                for (int i = index + 1; i < eventInstances.Count; i++)
                {
                    if (eventInstances[i].ClientId == clientId)
                    {
                        nextEventInstance = eventInstances[i];
                        break;
                    }
                }

                collector.EventInstances.RemoveAt(index);

                System.Threading.ThreadPool.QueueUserWorkItem(delegate 
                {
                    try
                    {
                        SoftnetRegistry.EventController_DeletePEventInstance(deliveredEventInstance.InstanceId);
                    }
                    catch (SoftnetException ex)
                    {
                        m_Site.Remove(ex.ErrorCode);
                    }
                });                

                return nextEventInstance;
            }
            else
            {
                foreach (PEventInstance eventInstance in collector.EventInstances)
                {
                    if (eventInstance.ClientId == clientId && eventInstance.InstanceId > deliveredEventInstance.InstanceId)
                        return eventInstance;
                }

                return null;
            }            
        }

        public object GetMutex() { return cont_mutex; }

        public void Monitor()
        {
            long currentTimeTicks = AppClock.getTicks();
            if (currentTimeTicks < m_TimeToDieTicks)
                return;

            lock (site_mutex)
            {
                m_TimeToDieTicks = long.MaxValue;

                foreach (QEventCollector collector in m_QEventCollectors)
                {
                    if (collector.EventInstances.Count > 0)
                    {
                        int eventCount = collector.EventInstances.Count;
                        for (int i = 0; i < eventCount; i++)
                        {
                            long timeToDieTicks = collector.EventInstances[0].CreatedTimeTicks + collector.LifeTicks;
                            if (timeToDieTicks <= currentTimeTicks)
                            {
                                QEventRemover.Add(collector.EventInstances[0]);
                                collector.EventInstances.RemoveAt(0);
                            }
                            else 
                            {
                                if (timeToDieTicks < m_TimeToDieTicks)
                                    m_TimeToDieTicks = timeToDieTicks;
                                break;
                            }
                        }
                    }
                }

                foreach (PEventCollector collector in m_PEventCollectors)
                {
                    if (collector.EventInstances.Count > 0)
                    {
                        int eventCount = collector.EventInstances.Count;
                        for (int i = 0; i < eventCount; i++)
                        {
                            long timeToDieTicks = collector.EventInstances[0].CreatedTimeTicks + collector.LifeTicks;
                            if (timeToDieTicks <= currentTimeTicks)
                            {
                                PEventRemover.Add(collector.EventInstances[0]);
                                collector.EventInstances.RemoveAt(0);
                            }
                            else
                            {
                                if (timeToDieTicks < m_TimeToDieTicks)
                                    m_TimeToDieTicks = timeToDieTicks;
                                break;
                            }
                        }
                    }
                }
            }
        }

        object cont_mutex = new object();
        object site_mutex;
        
        Site m_Site;
        List<Client> m_Clients;
        List<Client> m_StatelessClients;

        List<REventCollector> m_REventCollectors;
        List<QEventCollector> m_QEventCollectors;
        List<PEventCollector> m_PEventCollectors;
        long m_TimeToDieTicks;

        SoftnetMessage EncodeMessage_ReplacingEvent(EventIData eventIData, byte[] transactionUid)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.IA5String(eventIData.Name);
            asnSequence.Int64(eventIData.EventId);
            asnSequence.OctetString(transactionUid);
            asnSequence.Int64(eventIData.InstanceId);
            asnSequence.Int64(eventIData.ServiceId);
            asnSequence.Int64(0); // Event is newborn. Age is 0.
            asnSequence.GndTime(eventIData.CreatedDate);
            if (eventIData.Arguments != null)
                asnSequence.OctetString(1, eventIData.Arguments);
            return MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.REPLACING_EVENT, asnEncoder);
        }

        SoftnetMessage EncodeMessage_ReplacingNullEvent(EventIData eventIData, byte[] transactionUid)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.IA5String(eventIData.Name);
            asnSequence.Int64(eventIData.EventId);
            asnSequence.OctetString(transactionUid);
            asnSequence.Int64(eventIData.InstanceId);
            asnSequence.Int64(eventIData.ServiceId);
            asnSequence.Int64(0); // Event is newborn. Age is 0.
            asnSequence.GndTime(eventIData.CreatedDate);
            return MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.REPLACING_NULL_EVENT, asnEncoder);
        }

        SoftnetMessage EncodeMessage_QueueingEvent(EventIData eventIData, byte[] transactionUid)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;
            asnSequence.IA5String(eventIData.Name);
            asnSequence.Int64(eventIData.EventId);
            asnSequence.OctetString(transactionUid);
            asnSequence.Int64(eventIData.InstanceId);
            asnSequence.Int64(eventIData.ServiceId);
            asnSequence.Int64(0); // Event is newborn. Age is 0.
            asnSequence.GndTime(eventIData.CreatedDate);
            if (eventIData.Arguments != null)
                asnSequence.OctetString(1, eventIData.Arguments);
            return MsgBuilder.Create(Constants.Client.EventController.ModuleId, Constants.Client.EventController.QUEUEING_EVENT, asnEncoder);
        }

        class REventCollector
        {
            public readonly long EventId;
            public readonly string EventName;
            public readonly int LifeTime;
            public readonly int GuestAccess;
            public readonly List<long> Roles;

            public REventInstance EventInstance;

            public REventCollector(EventData eventData)
            {
                EventId = eventData.EventId;
                EventName = eventData.EventName;
                LifeTime = eventData.LifeTicks;
                GuestAccess = eventData.GuestAccess;
                Roles = eventData.Roles;
                EventInstance = null;
            }
        }

        class QEventCollector
        {
            public readonly long EventId;
            public readonly string EventName;
            public readonly int LifeTicks;
            public readonly int QueueSize;
            public readonly int GuestAccess;
            public readonly List<long> Roles;

            public List<QEventInstance> EventInstances;

            public QEventCollector(EventData eventData)
            {
                EventId = eventData.EventId;
                EventName = eventData.EventName;
                LifeTicks = eventData.LifeTicks;
                QueueSize = eventData.QueueSize;
                GuestAccess = eventData.GuestAccess;
                Roles = eventData.Roles;
                EventInstances = new List<QEventInstance>();
            }
        }

        class PEventCollector
        {
            public readonly long EventId;
            public readonly string EventName;
            public readonly int LifeTicks;

            public List<PEventInstance> EventInstances;

            public PEventCollector(EventData eventData)
            {
                EventId = eventData.EventId;
                EventName = eventData.EventName;
                LifeTicks = eventData.LifeTicks;
                EventInstances = new List<PEventInstance>();
            }
        }
    }
}
