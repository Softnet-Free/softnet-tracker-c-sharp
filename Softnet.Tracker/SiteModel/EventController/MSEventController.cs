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
    class MSEventController : IEventController
    {
        public MSEventController(Site site, List<Client> clients, List<Client> statelessClients) 
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
                    REventInstance existingEventInstance = rCollector.EventInstances.Find(x => x.ServiceId == eventInstance.ServiceId);
                    if (existingEventInstance != null)
                    {
                        SoftnetRegistry.EventController_DeleteREventInstance(existingEventInstance.InstanceId);
                    }
                    rCollector.EventInstances.Add(eventInstance);
                }
            }

            List<QEventInstance> qEventInstances = siteIData.QEventInstances;
            foreach (QEventInstance eventInstance in qEventInstances)
            {
                QEventCollector qCollector = qCollectors.Find(x => x.EventId == eventInstance.EventId);
                if (qCollector != null)
                {
                    BiLinked<QEventInstance> bilinkedQEventInstance = qCollector.EventInstances.Add(eventInstance);
                    QEventPort eventPort = qCollector.EventPorts.Find(x => x.ServiceId == eventInstance.ServiceId);
                    if (eventPort == null)           
                    {
                        eventPort = new QEventPort(eventInstance.ServiceId);
                        qCollector.EventPorts.Add(eventPort);
                    }                    
                    else if(eventPort.EventInstances.Count == qCollector.QueueSize)
                    {
                        SoftnetRegistry.EventController_DeleteQEventInstance(eventPort.EventInstances[0].Data.InstanceId);
                        qCollector.EventInstances.Remove(eventPort.EventInstances[0]);
                        eventPort.EventInstances.RemoveAt(0);
                    }

                    eventPort.EventInstances.Add(bilinkedQEventInstance);                    
                }
            }

            List<PEventInstance> pEventInstances = siteIData.PEventInstances;
            foreach (PEventInstance eventInstance in pEventInstances)
            {
                PEventCollector pCollector = pCollectors.Find(x => x.EventId == eventInstance.EventId);
                if (pCollector != null)
                {
                    BiLinked<PEventInstance> bilinkedPEventInstance = pCollector.EventInstances.Add(eventInstance);
                    PEventPort eventPort = pCollector.EventPorts.Find(x => x.ServiceId == eventInstance.ServiceId);
                    if (eventPort == null)
                    {
                        eventPort = new PEventPort(eventInstance.ServiceId);
                        pCollector.EventPorts.Add(eventPort);
                    }
                    else if (eventPort.EventInstances.Count == 1000)
                    {
                        SoftnetRegistry.EventController_DeletePEventInstance(eventPort.EventInstances[0].Data.InstanceId);
                        pCollector.EventInstances.Remove(eventPort.EventInstances[0]);
                        eventPort.EventInstances.RemoveAt(0);
                    }
                    
                    eventPort.EventInstances.Add(bilinkedPEventInstance);
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
            
            if (subscription.EventInstance == null && collector.EventInstances.Count > 0)
            {
                foreach (REventInstance eventInstance in collector.EventInstances)
                {
                    if (eventInstance.InstanceId > subscription.DeliveredInstanceId)
                    {
                        subscription.EventInstance = eventInstance;
                        break;
                    }
                }
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

            if (subscription.EventInstance == null && collector.EventInstances.IsEmpty == false)
            {
                IEnumerator<QEventInstance> eventEnumerator = collector.EventInstances.GetEnumerator();
                while (eventEnumerator.MoveNext())
                {
                    if (eventEnumerator.Current.InstanceId > subscription.DeliveredInstanceId)
                    {
                        subscription.EventInstance = eventEnumerator.Current;
                        return;
                    }
                }
            }
        }

        public void InitPSubscription(PSubscription subscription, long clientId)
        {
            PEventCollector collector = m_PEventCollectors.Find(x => x.EventId == subscription.EventId);
            if (collector == null)
                return;

            if (collector.EventInstances.IsEmpty)
                return;
            
            IEnumerator<PEventInstance> eventEnumerator = collector.EventInstances.GetEnumerator();
            while (eventEnumerator.MoveNext())
            {
                if (eventEnumerator.Current.ClientId == clientId && eventEnumerator.Current.InstanceId > subscription.DeliveredInstanceId)
                {
                    subscription.EventInstance = eventEnumerator.Current;
                    return;
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

                        if (collector.EventInstances.Count > 0)
                        {
                            foreach (REventInstance eventInstance in collector.EventInstances)
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
            else if (subscription.EventKind == Constants.EventKind.Queueing)
            {
                QEventCollector collector = m_QEventCollectors.Find(x => x.EventName.Equals(subscription.EventName));
                if (collector != null)
                {
                    subscription.EventId = collector.EventId;

                    if (collector.Roles == null && collector.GuestAccess == 0)
                    {
                        subscription.Authorized = true;

                        if (collector.EventInstances.IsEmpty == false)
                        {
                            IEnumerator<QEventInstance> eventEnumerator = collector.EventInstances.GetEnumerator();
                            while (eventEnumerator.MoveNext())
                            {
                                if (eventEnumerator.Current.InstanceId > subscription.DeliveredInstanceId)
                                {
                                    subscription.EventInstance = eventEnumerator.Current;
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
                REventInstance existingInstance = collector.EventInstances.Find(x => x.ServiceId == eventIData.ServiceId);
                if (existingInstance != null)
                {
                    if (eventIData.IsNull && existingInstance.IsNull)
                        return;
                    invalidInstanceId = existingInstance.InstanceId;
                    collector.EventInstances.Remove(existingInstance);
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
                            collector.EventInstances.Add(eventInstance);

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
                            collector.EventInstances.Add(eventInstance);

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
                QEventPort eventPort = collector.EventPorts.Find(x => x.ServiceId == eventIData.ServiceId);
                if (eventPort != null)
                {
                    if (eventPort.EventInstances.Count >= collector.QueueSize)
                    {
                        collector.EventInstances.Remove((eventPort.EventInstances[0]));
                        invalidInstanceId = eventPort.EventInstances[0].Data.InstanceId;
                        eventPort.EventInstances.RemoveAt(0);
                        instanceInvalidated = true;
                    }
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
                        if (collector.EventInstances.IsEmpty)
                        {
                            long timeToDieTicks = eventInstance.CreatedTimeTicks + collector.LifeTicks;
                            if (timeToDieTicks < m_TimeToDieTicks)
                                m_TimeToDieTicks = timeToDieTicks;
                        }

                        BiLinked<QEventInstance> bilinkedQEventInstance = collector.EventInstances.Add(eventInstance);

                        QEventPort eventPort = collector.EventPorts.Find(x => x.ServiceId == eventIData.ServiceId);
                        if (eventPort == null)
                        {
                            eventPort = new QEventPort(eventIData.ServiceId);
                            collector.EventPorts.Add(eventPort);
                        }
                        eventPort.EventInstances.Add(bilinkedQEventInstance);                        

                        foreach (Client client in m_Clients)
                            if (client.Online)
                                client.EventController.OnQEventRaised(eventInstance, message, transactionUid);

                        foreach (Client client in m_StatelessClients)
                            if (client.Online)
                                client.EventController.OnQEventRaised(eventInstance, message, transactionUid);
                    }
                }
            }
            catch (SoftnetException ex)
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
                PEventPort eventPort = collector.EventPorts.Find(x => x.ServiceId == eventIData.ServiceId);
                if (eventPort != null)
                {
                    if (eventPort.EventInstances.Count >= 1000)
                    {
                        collector.EventInstances.Remove((eventPort.EventInstances[0]));
                        invalidInstanceId = eventPort.EventInstances[0].Data.InstanceId;
                        eventPort.EventInstances.RemoveAt(0);
                        instanceInvalidated = true;
                    }
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
                        if (collector.EventInstances.IsEmpty)
                        {
                            long timeToDieTicks = eventInstance.CreatedTimeTicks + collector.LifeTicks;
                            if (timeToDieTicks < m_TimeToDieTicks)
                                m_TimeToDieTicks = timeToDieTicks;                        
                        }

                        BiLinked<PEventInstance> bilinkedPEventInstance = collector.EventInstances.Add(eventInstance);

                        PEventPort eventPort = collector.EventPorts.Find(x => x.ServiceId == eventIData.ServiceId);
                        if (eventPort == null)
                        {
                            eventPort = new PEventPort(eventIData.ServiceId);
                            collector.EventPorts.Add(eventPort);
                        }
                        eventPort.EventInstances.Add(bilinkedPEventInstance);

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

            foreach (REventInstance eventInstance in collector.EventInstances)
            {
                if (eventInstance.InstanceId > deliveredEventInstance.InstanceId)
                    return eventInstance;
            }

            return null;
        }

        public QEventInstance GetNextEvent(QEventInstance deliveredEventInstance)
        {
            QEventCollector collector = m_QEventCollectors.Find(x => x.EventId == deliveredEventInstance.EventId);
            if (collector == null)
                return null;

            IEnumerator<QEventInstance> eventEnumerator = collector.EventInstances.GetEnumerator();
            while (eventEnumerator.MoveNext())
            {
                if (eventEnumerator.Current.InstanceId > deliveredEventInstance.InstanceId)
                    return eventEnumerator.Current;
            }

            return null;
        }

        public PEventInstance GetNextEvent(PEventInstance deliveredEventInstance, long clientId)
        {
            PEventCollector collector = m_PEventCollectors.Find(x => x.EventId == deliveredEventInstance.EventId);
            if (collector == null)
                return null;

            IEnumerator<PEventInstance> eventEnumerator = collector.EventInstances.GetEnumerator();
            while (eventEnumerator.MoveNext())
            {
                if (eventEnumerator.Current.InstanceId == deliveredEventInstance.InstanceId)
                {
                    PEventPort eventPort = collector.EventPorts.Find(x => x.ServiceId == deliveredEventInstance.ServiceId);
                    if (eventPort != null)
                    {
                        BiLinked<PEventInstance> bilinkedPEventInstance = eventPort.EventInstances.Find(x => x.Data.InstanceId == deliveredEventInstance.InstanceId);
                        if (bilinkedPEventInstance != null)
                        {
                            collector.EventInstances.Remove(bilinkedPEventInstance);
                            eventPort.EventInstances.Remove(bilinkedPEventInstance);

                            System.Threading.ThreadPool.QueueUserWorkItem(delegate
                            {
                                try
                                {
                                    SoftnetRegistry.EventController_DeletePEventInstance(deliveredEventInstance.InstanceId);
                                }
                                catch(SoftnetException ex)
                                {
                                    m_Site.Remove(ex.ErrorCode);
                                }
                            });  
                        }
                    }

                    break;
                }
            }

            eventEnumerator = collector.EventInstances.GetEnumerator();
            while (eventEnumerator.MoveNext())
            {
                if (eventEnumerator.Current.ClientId == clientId && eventEnumerator.Current.InstanceId > deliveredEventInstance.InstanceId)
                    return eventEnumerator.Current;
            }
            return null;
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
                    if (collector.EventInstances.IsEmpty == false)
                    {
                        foreach (QEventPort eventPort in collector.EventPorts)
                        {
                            if (eventPort.EventInstances.Count > 0)
                            {
                                int eventCount = eventPort.EventInstances.Count;
                                for (int i = 0; i < eventCount; i++)
                                {
                                    long timeToDieTicks = eventPort.EventInstances[0].Data.CreatedTimeTicks + collector.LifeTicks;
                                    if (timeToDieTicks <= currentTimeTicks)
                                    {
                                        BiLinked<QEventInstance> biLinkedEventInstance = eventPort.EventInstances[0];
                                        QEventRemover.Add(biLinkedEventInstance.Data);
                                        eventPort.EventInstances.RemoveAt(0);
                                        collector.EventInstances.Remove(biLinkedEventInstance);
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

                foreach (PEventCollector collector in m_PEventCollectors)
                {
                    if (collector.EventInstances.IsEmpty == false)
                    {
                        foreach (PEventPort eventPort in collector.EventPorts)
                        {
                            if (eventPort.EventInstances.Count > 0)
                            {
                                int eventCount = eventPort.EventInstances.Count;
                                for (int i = 0; i < eventCount; i++)
                                {
                                    long timeToDieTicks = eventPort.EventInstances[0].Data.CreatedTimeTicks + collector.LifeTicks;
                                    if (timeToDieTicks <= currentTimeTicks)
                                    {
                                        BiLinked<PEventInstance> biLinkedEventInstance = eventPort.EventInstances[0];
                                        PEventRemover.Add(biLinkedEventInstance.Data);
                                        eventPort.EventInstances.RemoveAt(0);
                                        collector.EventInstances.Remove(biLinkedEventInstance);
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
            public readonly int GuestAccess;
            public readonly List<long> Roles;
            public readonly List<REventInstance> EventInstances;

            public REventCollector(EventData eventData)
            {
                EventId = eventData.EventId;
                EventName = eventData.EventName;
                GuestAccess = eventData.GuestAccess;
                Roles = eventData.Roles;
                EventInstances = new List<REventInstance>();
            }
        }

        class QEventPort
        {
            public readonly long ServiceId;
            public readonly List<BiLinked<QEventInstance>> EventInstances;
            public QEventPort(long serviceId)
            {
                ServiceId = serviceId;
                EventInstances = new List<BiLinked<QEventInstance>>();
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
            public readonly List<QEventPort> EventPorts;
            public readonly BiLinkedList<QEventInstance> EventInstances;

            public QEventCollector(EventData eventData)
            {
                EventId = eventData.EventId;
                EventName = eventData.EventName;
                LifeTicks = eventData.LifeTicks;
                QueueSize = eventData.QueueSize;
                GuestAccess = eventData.GuestAccess;
                Roles = eventData.Roles;
                EventPorts = new List<QEventPort>();
                EventInstances = new BiLinkedList<QEventInstance>();
            }
        }

        class PEventPort
        {
            public readonly long ServiceId;
            public readonly List<BiLinked<PEventInstance>> EventInstances;
            public PEventPort(long serviceId)
            {
                ServiceId = serviceId;
                EventInstances = new List<BiLinked<PEventInstance>>();                
            }
        }

        class PEventCollector
        {
            public readonly long EventId;
            public readonly string EventName;
            public readonly int LifeTicks;
            public readonly List<PEventPort> EventPorts;
            public readonly BiLinkedList<PEventInstance> EventInstances;

            public PEventCollector(EventData eventData)
            {
                EventId = eventData.EventId;
                EventName = eventData.EventName;
                LifeTicks = eventData.LifeTicks;
                EventPorts = new List<PEventPort>();
                EventInstances = new BiLinkedList<PEventInstance>();
            }
        }
    }
}
