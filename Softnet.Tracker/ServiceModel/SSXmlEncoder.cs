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
using System.IO;
using System.Xml;

using Softnet.Tracker.Core;
using Softnet.Tracker.SiteModel;

namespace Softnet.Tracker.ServiceModel
{
    class SSXmlEncoder
    {        
        public static string exec(SiteModel.SiteStructure siteStructure)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = false;
            settings.CloseOutput = true;

            StringBuilder stringBuilder = new StringBuilder();
            UTF8StringWriter stringWriter = new UTF8StringWriter(stringBuilder);

            XmlWriter writer = XmlWriter.Create(stringWriter, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("SiteStructure");            
            writer.WriteElementString("ServiceType", siteStructure.getServiceType());
            writer.WriteElementString("ContractAuthor", siteStructure.getContractAuthor());
            writer.WriteElementString("GuestSupport", siteStructure.getGuestSupport().ToString());

            if (siteStructure.rolesSupported())
            {
                writer.WriteStartElement("RolesDefinition");

                writer.WriteStartElement("Roles");
                foreach (string role in siteStructure.getRoles())
                    writer.WriteElementString("Role", role);
                writer.WriteEndElement();

                if (siteStructure.containsOwnerRole())
                    writer.WriteElementString("OwnerRole", siteStructure.getOwnerRole());

                writer.WriteEndElement();
            }

            if (siteStructure.eventsSupported())
            {
                writer.WriteStartElement("EventsDefinition");
                if (siteStructure.containsREvents())
                {
                    writer.WriteStartElement("ReplacingEvents");
                    foreach (REvent rEvent in siteStructure.getREvents())
                    {
                        writer.WriteStartElement("Event");
                        writer.WriteAttributeString("Name", rEvent.name);                        
                        if (rEvent.guestAccess > 0)
                        {
                            writer.WriteAttributeString("GuestAccess", rEvent.guestAccess.ToString());
                        }
                        else if (rEvent.roles != null)
                        {
                            writer.WriteStartElement("Roles");
                            foreach (string role in rEvent.roles)
                            {
                                writer.WriteElementString("Role", role);
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }

                if (siteStructure.containsQEvents())
                {
                    writer.WriteStartElement("QueueingEvents");
                    foreach (QEvent qEvent in siteStructure.getQEvents())
                    {
                        writer.WriteStartElement("Event");
                        writer.WriteAttributeString("Name", qEvent.name);
                        writer.WriteAttributeString("LifeTime", qEvent.lifeTime.ToString());
                        writer.WriteAttributeString("QueueSize", qEvent.queueSize.ToString());
                        if (qEvent.guestAccess > 0)
                        {
                            writer.WriteAttributeString("GuestAccess", qEvent.guestAccess.ToString());
                        }
                        else if (qEvent.roles != null)
                        {
                            writer.WriteStartElement("Roles");
                            foreach (string role in qEvent.roles)
                            {
                                writer.WriteElementString("Role", role);
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }

                if (siteStructure.containsPEvents())
                {
                    writer.WriteStartElement("PrivateEvents");
                    foreach (PEvent pEvent in siteStructure.getPEvents())
                    {
                        writer.WriteStartElement("Event");
                        writer.WriteAttributeString("Name", pEvent.name);
                        writer.WriteAttributeString("LifeTime", pEvent.lifeTime.ToString());
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();

            return stringBuilder.ToString();
        }

        class UTF8StringWriter : StringWriter
        {
            public UTF8StringWriter(StringBuilder sb)
                : base(sb) { }
            public override Encoding Encoding
            {
                get
                {
                    return Encoding.UTF8;
                }
            }
        }
    }    
}

/*
<?xml version="1.0" encoding="utf-8"?>
<ServiceDefinition>  
    <SiteKind>2</SiteKind>
    <ServiceType>Baby Monitor</ServiceType>
    <ContractAuthor>KRA</ContractAuthor>
    <GuestSupport>2</GuestSupport>
    <RolesDefinition>
	    <Roles>
		    <Role>Administrator</Role>
		    <Role>SuperUser</Role>
		    <Role>Mounter</Role>
		    <Role>User</Role>
	    </Roles>
	    <OwnerRole>Administrator</OwnerRole>
    </RolesDefinition>
    <EventsDefinition>
	    <ReplacingEvents>
		    <Event Name="AirTempAbnormal" LifeTime="259200" Index="1">
			    <Roles>
				    <Role>Administrator</Role>
				    <Role>SuperUser</Role>
			    </Roles>
		    </Event>
		    <Event Name="AirTemp" LifeTime="600" Index="2">
			    <Roles>
				    <Role>Administrator</Role>
				    <Role>SuperUser</Role>
				    <Role>User</Role>
			    </Roles>
		    </Event>
	    </ReplacingEvents>
	    <QueueingEvents>
		    <Event Name="BoilerTemp" LifeTime="600" QueueSize="60" Index="3">
			    <Roles>
				    <Role>Administrator</Role>
				    <Role>SuperUser</Role>
				    <Role>User</Role>
			    </Roles>
		    </Event>
		    <Event Name="WaterTemp" LifeTime="600" QueueSize="60" Index="4" GuestAccess="2" />
	    </QueueingEvents>
	    <DatagramEvents>
		    <Event Name="OilTemp" Index="5">
			    <Roles>
				    <Role>Administrator</Role>
				    <Role>SuperUser</Role>
			    </Roles>
		    </Event>
		    <Event Name="OilPressure" Index="6" GuestAccess="1" />
	    </DatagramEvents>
	    <PrivateEvents>
		    <Event Name="FilesAccepted" LifeTime="864000" Index="7" />
		    <Event Name="FilesRejected" LifeTime="864000" Index="8" />
	    </PrivateEvents>
    </EventsDefinition>
</ServiceDefinition>
*/
