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
using System.Net;

namespace Softnet.Tracker.Core
{
    public class ProxyParams
    {
        public readonly int ServerId;
        public readonly byte[] SecretKey;
        public readonly IPAddress IPv6;
        public readonly IPAddress IPv4;

        public ProxyParams(int serverId, byte[] secretKey, IPAddress ipv6, IPAddress ipv4)
        {
            ServerId = serverId;
            SecretKey = secretKey;
            IPv6 = ipv6;
            IPv4 = ipv4;
        }
    }
}
