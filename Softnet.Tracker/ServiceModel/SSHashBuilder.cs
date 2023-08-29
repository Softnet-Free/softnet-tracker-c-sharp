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
using Softnet.ServerKit;
using Softnet.Asn;

namespace Softnet.Tracker.SiteModel
{
    class SSHashBuilder
    {
        public static byte[] exec(SiteStructure siteStructure)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSiteStructure = asnEncoder.Sequence;            
            asnSiteStructure.IA5String(siteStructure.getServiceType());
            asnSiteStructure.IA5String(siteStructure.getContractAuthor());
   		    asnSiteStructure.Int32(siteStructure.getGuestSupport());

            if(siteStructure.rolesSupported())
            {
                List<string> roles = siteStructure.getSortedRoles();
                SequenceEncoder asnRolesDefinition = asnSiteStructure.Sequence(1);
	            foreach(string role in roles)
                    asnRolesDefinition.IA5String(role);
            }

            if(siteStructure.eventsSupported())
            {
            	SequenceEncoder asnEventsDefinition = asnSiteStructure.Sequence(2);
                if (siteStructure.containsREvents())
                {
                    List<REvent> events = siteStructure.getSortedREvents();
                    SequenceEncoder asnReplacingEvents = asnEventsDefinition.Sequence(1);
                    foreach (REvent evt in events)
        		    {
        			    SequenceEncoder asnEvent = asnReplacingEvents.Sequence();
                        asnEvent.IA5String(evt.name);
        			    if(evt.guestAccess > 0)
        			    {
        				    if(evt.guestAccess == 1)
        					    asnEvent.Int32(1, 1);
        				    else
        					    asnEvent.Int32(1, 2);
        			    }
        			    else if(evt.roles != null)
        			    {
        				    SequenceEncoder asnRoles = asnEvent.Sequence(2);
                            foreach (string role in evt.roles)
                                asnRoles.IA5String(role);
        			    }        		
                    }
                }

                if(siteStructure.containsQEvents())
        	    {
                    List<QEvent> events = siteStructure.getSortedQEvents();
        		    SequenceEncoder asnQueueingEvents = asnEventsDefinition.Sequence(2);
                    foreach (QEvent evt in events)
        		    {
        			    SequenceEncoder asnEvent = asnQueueingEvents.Sequence();
                        asnEvent.IA5String(evt.name);
        			    asnEvent.Int32(evt.lifeTime);
        			    asnEvent.Int32(evt.queueSize);
        			    if(evt.guestAccess > 0)
        			    {
        				    if(evt.guestAccess == 1)
        					    asnEvent.Int32(1, 1);
        				    else
        					    asnEvent.Int32(1, 2);        					
        			    }
        			    else if(evt.roles != null)
        			    {
        				    SequenceEncoder asnRoles = asnEvent.Sequence(2);
        				    foreach(string role in evt.roles)
                                asnRoles.IA5String(role);
        			    }        				
        		    }
        	    }

                if(siteStructure.containsPEvents())
        	    {
                    List<PEvent> events = siteStructure.getSortedPEvents();
        		    SequenceEncoder asnPrivateEvents = asnEventsDefinition.Sequence(4);
                    foreach (PEvent evt in events)
        		    {
        			    SequenceEncoder asnEvent = asnPrivateEvents.Sequence();
                        asnEvent.IA5String(evt.name);
        			    asnEvent.Int32(evt.lifeTime);
        		    }
        	    }
            }

            byte[] encoding = asnEncoder.GetEncoding();
            return SHA1Hash.Compute(encoding);
        }
    }
}
