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

namespace Softnet.Tracker.SiteModel
{
    public class SiteStructure
    {
        public SiteStructure(SSDataset ssDataset)
        {            
            if (string.IsNullOrEmpty(ssDataset.serviceType) || ssDataset.serviceType.Length > 256)
                throw new FormatException();

            if (string.IsNullOrEmpty(ssDataset.contractAuthor) || ssDataset.contractAuthor.Length > 256)
                throw new FormatException();

            if (!(0 <= ssDataset.guestSupport && ssDataset.guestSupport <= 2))
                throw new FormatException();
            
            m_serviceType = ssDataset.serviceType;
            m_contractAuthor = ssDataset.contractAuthor;
            m_guestSupport = ssDataset.guestSupport;

            if (ssDataset.roles != null && ssDataset.roles.Count > 0)
            {
                List<string> roles = ssDataset.roles;
                for (int i = 0; i < roles.Count; i++)
                {
                    if (string.IsNullOrEmpty(roles[i]) || roles[i].Length > 256)
                        throw new FormatException();

                    for (int j = i + 1; j < roles.Count; j++)
                    {
                        if (roles[i].Equals(roles[j], StringComparison.OrdinalIgnoreCase))
                            throw new FormatException();
                    }
                }

                m_roles = roles;

                if (string.IsNullOrEmpty(ssDataset.ownerRole) == false)
                {
                    if(m_roles.Find(x => x.Equals(ssDataset.ownerRole)) == null)
                        throw new FormatException();
                    m_ownerRole = ssDataset.ownerRole;
                }
            }

            if (ssDataset.REvents != null && ssDataset.REvents.Count > 0)
            {                
                foreach (REvent evt in ssDataset.REvents)
                {
                    if (string.IsNullOrEmpty(evt.name) || evt.name.Length > 256)
                        throw new FormatException();
                    
                    if (evt.guestAccess != 0)
                    { 
                        if(evt.guestAccess < 0 || evt.guestAccess > 2)
                            throw new FormatException();
                    }
                    else if (evt.roles != null)
                    {
                        if (m_roles == null || evt.roles.Count == 0)
                            throw new FormatException();

                        foreach (string role in evt.roles)
                        { 
                            if(m_roles.Find(x => x.Equals(role)) == null)
                                throw new FormatException();
                        }
                    }
                }
                m_REvents = ssDataset.REvents;
            }

            if (ssDataset.QEvents != null && ssDataset.QEvents.Count > 0)
            {
                foreach (QEvent evt in ssDataset.QEvents)
                {
                    if (string.IsNullOrEmpty(evt.name) || evt.name.Length > 256)
                        throw new FormatException();

                    if (evt.lifeTime < 60 || evt.lifeTime > 2592000)
                        throw new FormatException();

                    if (evt.queueSize < 1 || evt.queueSize > 1000)
                        throw new FormatException();

                    if (evt.guestAccess != 0)
                    {
                        if (evt.guestAccess < 0 || evt.guestAccess > 2)
                            throw new FormatException();
                    }
                    else if (evt.roles != null)
                    {
                        if (m_roles == null || evt.roles.Count == 0)
                            throw new FormatException();

                        foreach (string role in evt.roles)
                        {
                            if (m_roles.Find(x => x.Equals(role)) == null)
                                throw new FormatException();
                        }
                    }
                }
                m_QEvents = ssDataset.QEvents;
            }            

            if (ssDataset.PEvents != null && ssDataset.PEvents.Count > 0)
            { 
                foreach (PEvent evt in ssDataset.PEvents)
                {
                    if (string.IsNullOrEmpty(evt.name) || evt.name.Length > 256)
                        throw new FormatException();

                    if (evt.lifeTime < 60 || evt.lifeTime > 2592000)
                        throw new FormatException();
                }
                m_PEvents = ssDataset.PEvents;
            }
        }
        
        string m_serviceType;
        string m_contractAuthor;
        int m_guestSupport;

        List<string> m_roles = null;
        string m_ownerRole = null;

        List<REvent> m_REvents = null;
        List<QEvent> m_QEvents = null;        
        List<PEvent> m_PEvents = null;
        
        public string getServiceType()
        {
            return m_serviceType;
        }

        public string getContractAuthor()
        {
            return m_contractAuthor;
        }

        public int getGuestSupport()
        {
            return m_guestSupport;
        }

        public bool rolesSupported()
        {
            return m_roles != null;
        }

        public List<string> getRoles()
        {
            return m_roles;
        }

        public List<string> getSortedRoles()
        {
            List<string> roles = m_roles.ToList();
            roles.Sort();
            return roles;
        }

        public bool containsOwnerRole()
        {
            return string.IsNullOrEmpty(m_ownerRole) == false;
        }

        public string getOwnerRole()
        {
            return m_ownerRole;
        }

        public bool eventsSupported()
        {
            return m_REvents != null || m_QEvents != null || m_PEvents != null; 
        }

        public bool containsREvents()
        {
            return m_REvents != null;
        }

        public bool containsREvent(string eventName)
        {
            if (m_REvents == null)
                return false;
            return m_REvents.Find(x => x.name.Equals(eventName)) != null;
        }

        public List<REvent> getREvents()
        {
            return m_REvents;
        }

        public List<REvent> getSortedREvents()
        {
            if (m_REvents == null)
                return null;

            List<REvent> events = new List<REvent>(m_REvents.Count);
            foreach(REvent evt in m_REvents)
            {
                if (evt.roles != null)
                {
                    List<string> roles = evt.roles.ToList();
                    roles.Sort();
                    events.Add(new REvent(evt.name, roles));
                }
                else
                    events.Add(evt);
            }
            events.Sort((x, y) => x.name.CompareTo(y.name));
            return events;
        }

        public bool containsQEvents()
        {
            return m_QEvents != null;
        }

        public bool containsQEvent(string eventName)
        {
            if (m_QEvents == null)
                return false;
            return m_QEvents.Find(x => x.name.Equals(eventName)) != null;
        }

        public List<QEvent> getQEvents()
        {
            return m_QEvents;
        }

        public List<QEvent> getSortedQEvents()
        {
            if (m_QEvents == null)
                return null;

            List<QEvent> events = new List<QEvent>(m_QEvents.Count);
            foreach (QEvent evt in m_QEvents)
            {
                if (evt.roles != null)
                {
                    List<string> roles = evt.roles.ToList();
                    roles.Sort();
                    events.Add(new QEvent(evt.name, evt.lifeTime, evt.queueSize, roles));
                }
                else
                    events.Add(evt);
            }
            events.Sort((x, y) => x.name.CompareTo(y.name));
            return events;
        }

        public bool containsPEvents()
        {
            return m_PEvents != null;
        }

        public bool containsPEvent(string eventName)
        {
            if (m_PEvents == null)
                return false;
            return m_PEvents.Find(x => x.name.Equals(eventName)) != null;
        }

        public List<PEvent> getPEvents()
        {            
            return m_PEvents;
        }

        public List<PEvent> getSortedPEvents()
        {
            if (m_PEvents == null)
                return null;
            List<PEvent> events = m_PEvents.ToList();
            events.Sort((x, y) => x.name.CompareTo(y.name));
            return events;
        }

        private bool eventExists(string eventName)
        {
            if (m_REvents != null)
            {
                if (m_REvents.Find(x => x.name.Equals(eventName, StringComparison.OrdinalIgnoreCase)) != null)
                    return true;
            }

            if (m_QEvents != null)
            {
                if (m_QEvents.Find(x => x.name.Equals(eventName, StringComparison.OrdinalIgnoreCase)) != null)
                    return true;
            }

            if (m_PEvents != null)
            {
                if (m_PEvents.Find(x => x.name.Equals(eventName, StringComparison.OrdinalIgnoreCase)) != null)
                    return true;
            }

            return false;
        }
    }
}
