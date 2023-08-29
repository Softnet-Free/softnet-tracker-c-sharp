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
using System.Configuration;
using System.ComponentModel;
using System.Globalization;

namespace Softnet.Tracker
{
    class TrackerConfig : ConfigurationSection
    {
        [ConfigurationProperty("trackerId", IsRequired = true)]
        public int TrackerId
        {
            get
            {
                return (int)this["trackerId"];
            }
        }

        [ConfigurationProperty("capacity", IsRequired = true)]
        public int Capacity
        {
            get
            {
                return (int)this["capacity"];
            }
        }

        [ConfigurationProperty("maxPendingConnections", IsRequired = true)]
        public int MaxPendingConnections
        {
            get
            {
                return (int)this["maxPendingConnections"];
            }
        }

        [ConfigurationProperty("managementEndpoint", IsRequired = true)]
        public ManagementEndpointConfig ManagementEndpoint
        {
            get
            {
                return (ManagementEndpointConfig)this["managementEndpoint"];
            }
        }

        [ConfigurationProperty("proxyServers", IsRequired = true)]
        public ProxyServerCollection ProxyServers
        {
            get { return (ProxyServerCollection)base["proxyServers"]; }
        }
    }

    public class ManagementEndpointConfig : ConfigurationElement
    {
        [ConfigurationProperty("IP", IsRequired = true)]
        [TypeConverter(typeof(IPConverter))]
        public System.Net.IPAddress IP
        {
            get
            {
                return (System.Net.IPAddress)base["IP"];
            }
        }

        [ConfigurationProperty("port", IsRequired = true)]
        public int Port
        {
            get
            {
                return (int)this["port"];
            }
        }
    }

    [ConfigurationCollection(typeof(ProxyServerConfig), CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class ProxyServerCollection : ConfigurationElementCollection
    {
        static ConfigurationPropertyCollection m_properties;

        static ProxyServerCollection()
        {
            m_properties = new ConfigurationPropertyCollection();
        }

        public ProxyServerCollection() { }

        #region Properties

        protected override ConfigurationPropertyCollection Properties
        {
            get { return m_properties; }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "proxyServer"; }
        }

        #endregion

        #region Indexers

        public ProxyServerConfig this[int index]
        {
            get { return (ProxyServerConfig)base.BaseGet(index); }
        }

        new public ProxyServerConfig this[string name]
        {
            get { return (ProxyServerConfig)base.BaseGet(name); }
        }

        #endregion

        #region Overrides

        protected override ConfigurationElement CreateNewElement()
        {
            return new ProxyServerConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as ProxyServerConfig).SecretKey;
        }

        #endregion
    }

    public class ProxyServerConfig : ConfigurationElement
    {
        [ConfigurationProperty("serverId", IsRequired = true)]
        public int ServerId
        {
            get
            {
                return (int)this["serverId"];
            }
        }

        [ConfigurationProperty("secretKey", IsRequired = true)]
        public string SecretKey
        {
            get
            {
                return (string)this["secretKey"];
            }
        }

        [ConfigurationProperty("IPv6", IsRequired = false)]
        [TypeConverter(typeof(IPConverter))]
        public System.Net.IPAddress IPv6
        {
            get
            {
                System.Net.IPAddress IP = (System.Net.IPAddress)base["IPv6"];
                if (IP == null)
                {
                    return null;
                }
                else
                {
                    if (IP.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
                        throw new ConfigurationErrorsException("Invalid address family");
                    return IP;
                }
            }
        }

        [ConfigurationProperty("IPv4", IsRequired = false)]
        [TypeConverter(typeof(IPConverter))]
        public System.Net.IPAddress IPv4
        {
            get
            {
                System.Net.IPAddress IP = (System.Net.IPAddress)base["IPv4"];
                if (IP == null)
                {
                    return null;
                }
                else
                {
                    if (IP.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                        throw new ConfigurationErrorsException("Invalid address family");
                    return IP;
                }
            }
        }
    }

    public sealed class IPConverter : ConfigurationConverterBase
    {
        public override bool CanConvertTo(ITypeDescriptorContext ctx, Type type)
        {
            return (type == typeof(string));
        }

        public override bool CanConvertFrom(ITypeDescriptorContext ctx, Type type)
        {
            return (type == typeof(string));
        }

        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            return ((System.Net.IPAddress)value).ToString();
        }

        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object value)
        {
            string str_ip = (string)value;
            if (str_ip == string.Empty)
                return null;
            return System.Net.IPAddress.Parse(str_ip);
        }
    }
}
