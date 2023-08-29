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

namespace Softnet.Tracker.Core
{
    static class NetworkResources
    {
        static ProxyParams[] ProxyServers = null;
        static int CurrentIndex = 0;

        public static void Load()
        {
            Softnet.Tracker.TrackerConfig trackerConfig = (Softnet.Tracker.TrackerConfig)ConfigurationManager.GetSection("tracker");
            Softnet.Tracker.ProxyServerCollection proxyServerCollection = trackerConfig.ProxyServers;

            if (proxyServerCollection.Count > 0)
            {                
                ProxyServers = new ProxyParams[proxyServerCollection.Count];

                for (int i = 0; i < proxyServerCollection.Count; i++)
                {
                    Softnet.Tracker.ProxyServerConfig proxyServerConfig = proxyServerCollection[i];
                    byte[] secretKey = Encoding.BigEndianUnicode.GetBytes(proxyServerConfig.SecretKey);
                    ProxyServers[i] = new ProxyParams(proxyServerConfig.ServerId, secretKey, proxyServerConfig.IPv6, proxyServerConfig.IPv4);
                }
            }
            else
            {
                throw new ConfigurationErrorsException("No proxy servers are specified in the configuration file");
            }
        }

        public static ProxyParams GetProxy()
        {
            lock (ProxyServers)
            {
                if (ProxyServers.Length == 1)
                {
                    return ProxyServers[0];
                }
                else
                {
                    CurrentIndex++;
                    if (CurrentIndex >= ProxyServers.Length)
                        CurrentIndex = 0;
                    return ProxyServers[CurrentIndex];
                }
            }
        }

        public static ProxyParams FindProxy(int serverId)
        {
            lock (ProxyServers)
            {
                foreach (ProxyParams proxyParams in ProxyServers)
                {
                    if (proxyParams.ServerId == serverId)
                        return proxyParams;
                }
                return null;
            }
        }
    }
}
