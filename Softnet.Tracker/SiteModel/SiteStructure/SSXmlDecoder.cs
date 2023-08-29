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
using System.Xml;
using System.IO;

namespace Softnet.Tracker.SiteModel
{
    public class SSXmlDecoder
    {
        public static SiteStructure exec(string ssXml)
        {
            try
            {
                SSDataset ssDataset = new SSDataset();
                XmlReader xmlReader = XmlReader.Create(new StringReader(ssXml));

                xmlReader.Read();
                if (!(xmlReader.NodeType == XmlNodeType.XmlDeclaration && xmlReader.Name == "xml"))
                    throw new FormatException();

                xmlReader.Read();
                if (!(xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "SiteStructure"))
                    throw new FormatException();                

                xmlReader.Read();
                if (!(xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "ServiceType"))
                    throw new FormatException();

                xmlReader.Read();
                if (!(xmlReader.NodeType == XmlNodeType.Text))
                    throw new FormatException();

                ssDataset.serviceType = xmlReader.Value;                

                xmlReader.Read();
                if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "ServiceType"))
                    throw new FormatException();

                xmlReader.Read();
                if (!(xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "ContractAuthor"))
                    throw new FormatException();

                xmlReader.Read();
                if (!(xmlReader.NodeType == XmlNodeType.Text))
                    throw new FormatException();

                ssDataset.contractAuthor = xmlReader.Value;

                xmlReader.Read();
                if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "ContractAuthor"))
                    throw new FormatException();

                xmlReader.Read();
                if (!(xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "GuestSupport"))
                    throw new FormatException();

                xmlReader.Read();
                if (!(xmlReader.NodeType == XmlNodeType.Text))
                    throw new FormatException();

                ssDataset.guestSupport = int.Parse(xmlReader.Value);

                xmlReader.Read();
                if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "GuestSupport"))
                    throw new FormatException();

                xmlReader.Read();
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "RolesDefinition")
                {
                    xmlReader.Read();
                    if (!(xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "Roles"))
                        throw new FormatException();

                    ssDataset.roles = new List<string>();

                    xmlReader.Read();
                    while (true)
                    {
                        if (!(xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "Role"))
                            throw new FormatException();

                        xmlReader.Read();
                        if (!(xmlReader.NodeType == XmlNodeType.Text))
                            throw new FormatException();

                        ssDataset.roles.Add(xmlReader.Value);

                        xmlReader.Read();
                        if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "Role"))
                            throw new FormatException();

                        xmlReader.Read();
                        if (xmlReader.NodeType == XmlNodeType.EndElement)
                        {
                            if (xmlReader.Name != "Roles")
                                throw new FormatException();
                            break;
                        }
                    }

                    xmlReader.Read();
                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "OwnerRole")
                    {
                        xmlReader.Read();
                        if (!(xmlReader.NodeType == XmlNodeType.Text))
                            throw new FormatException();

                        ssDataset.ownerRole = xmlReader.Value;

                        xmlReader.Read();
                        if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "OwnerRole"))
                            throw new FormatException();

                        xmlReader.Read();
                    }                    
                    
                    if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "RolesDefinition"))
                        throw new FormatException();

                    xmlReader.Read();
                }

                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "EventsDefinition")
                {
                    xmlReader.Read();
                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "ReplacingEvents")
                    {
                        ssDataset.REvents = new List<REvent>();

                        xmlReader.Read();
                        while (true)
                        {
                            if (!(xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "Event"))
                                throw new FormatException();

                            if (xmlReader.MoveToFirstAttribute() == false)
                                throw new FormatException();
                            if (xmlReader.Name != "Name")
                                throw new FormatException();

                            string eventName = xmlReader.Value;                            

                            if (xmlReader.MoveToNextAttribute())
                            {
                                if (xmlReader.Name != "GuestAccess")
                                    throw new FormatException();

                                int guestAccess = int.Parse(xmlReader.Value);

                                ssDataset.REvents.Add(new REvent(eventName, guestAccess));
                                xmlReader.Read();
                            }
                            else
                            {
                                xmlReader.Read();
                                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "Roles")
                                {
                                    List<string> roles = new List<string>();

                                    xmlReader.Read();
                                    while (true)
                                    {
                                        if (!(xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "Role"))
                                            throw new FormatException();

                                        xmlReader.Read();
                                        if (!(xmlReader.NodeType == XmlNodeType.Text))
                                            throw new FormatException();                                        

                                        roles.Add(xmlReader.Value);

                                        xmlReader.Read();
                                        if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "Role"))
                                            throw new FormatException();

                                        xmlReader.Read();
                                        if (xmlReader.NodeType == XmlNodeType.EndElement)
                                        {
                                            if (xmlReader.Name != "Roles")
                                                throw new FormatException();
                                            break;
                                        }
                                    }

                                    xmlReader.Read();
                                    if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "Event"))
                                        throw new FormatException();

                                    ssDataset.REvents.Add(new REvent(eventName, roles));
                                    xmlReader.Read();
                                }
                                else
                                {
                                    ssDataset.REvents.Add(new REvent(eventName));
                                }
                            }

                            if (xmlReader.NodeType == XmlNodeType.EndElement)
                            {
                                if (xmlReader.Name != "ReplacingEvents")
                                    throw new FormatException();
                                break;
                            }
                        }

                        xmlReader.Read();
                    }

                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "QueueingEvents")
                    {
                        ssDataset.QEvents = new List<QEvent>();

                        xmlReader.Read();
                        while (true)
                        {
                            if (!(xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "Event"))
                                throw new FormatException();

                            if (xmlReader.MoveToFirstAttribute() == false)
                                throw new FormatException();
                            if (xmlReader.Name != "Name")
                                throw new FormatException();

                            string eventName = xmlReader.Value;

                            if (xmlReader.MoveToNextAttribute() == false)
                                throw new FormatException();
                            if (xmlReader.Name != "LifeTime")
                                throw new FormatException();

                            int lifeTime = int.Parse(xmlReader.Value);

                            if (xmlReader.MoveToNextAttribute() == false)
                                throw new FormatException();
                            if (xmlReader.Name != "QueueSize")
                                throw new FormatException();

                            int queueSize = int.Parse(xmlReader.Value);

                            if (xmlReader.MoveToNextAttribute())
                            {
                                if (xmlReader.Name != "GuestAccess")
                                    throw new FormatException();

                                int guestAccess = int.Parse(xmlReader.Value);

                                ssDataset.QEvents.Add(new QEvent(eventName, lifeTime, queueSize, guestAccess));
                                xmlReader.Read();
                            }
                            else
                            {
                                xmlReader.Read();
                                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "Roles")
                                {
                                    List<string> roles = new List<string>();

                                    xmlReader.Read();
                                    while (true)
                                    {
                                        if (!(xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "Role"))
                                            throw new FormatException();

                                        xmlReader.Read();
                                        if (!(xmlReader.NodeType == XmlNodeType.Text))
                                            throw new FormatException();

                                        roles.Add(xmlReader.Value);

                                        xmlReader.Read();
                                        if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "Role"))
                                            throw new FormatException();

                                        xmlReader.Read();
                                        if (xmlReader.NodeType == XmlNodeType.EndElement)
                                        {
                                            if (xmlReader.Name != "Roles")
                                                throw new FormatException();
                                            break;
                                        }
                                    }

                                    xmlReader.Read();
                                    if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "Event"))
                                        throw new FormatException();

                                    ssDataset.QEvents.Add(new QEvent(eventName, lifeTime, queueSize, roles));
                                    xmlReader.Read();
                                }
                                else
                                {
                                    ssDataset.QEvents.Add(new QEvent(eventName, lifeTime, queueSize));
                                }
                            }
                            
                            if (xmlReader.NodeType == XmlNodeType.EndElement)
                            {
                                if (xmlReader.Name != "QueueingEvents")
                                    throw new FormatException();
                                break;
                            }
                        }

                        xmlReader.Read();
                    }

                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "PrivateEvents")
                    {
                        ssDataset.PEvents = new List<PEvent>();

                        xmlReader.Read();
                        while (true)
                        {
                            if (!(xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "Event"))
                                throw new FormatException();

                            if (xmlReader.MoveToFirstAttribute() == false)
                                throw new FormatException();
                            if (xmlReader.Name != "Name")
                                throw new FormatException();

                            string eventName = xmlReader.Value;

                            if (xmlReader.MoveToNextAttribute() == false)
                                throw new FormatException();
                            if (xmlReader.Name != "LifeTime")
                                throw new FormatException();

                            int lifeTime = int.Parse(xmlReader.Value);
                            ssDataset.PEvents.Add(new PEvent(eventName, lifeTime));

                            xmlReader.Read();
                            if (xmlReader.NodeType == XmlNodeType.EndElement)
                            {
                                if (xmlReader.Name != "PrivateEvents")
                                    throw new FormatException();
                                break;
                            }
                        }

                        xmlReader.Read();
                    }

                    if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "EventsDefinition"))
                        throw new FormatException();

                    xmlReader.Read();
                }

                if (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "SiteStructure"))
                    throw new FormatException();

                if (xmlReader.Read())
                    throw new FormatException();

                return new SiteStructure(ssDataset);
            }
            catch (ArgumentException)
            {
                throw new FormatException();
            }
            catch (OverflowException)
            {
                throw new FormatException();
            }
        }
    }
}
