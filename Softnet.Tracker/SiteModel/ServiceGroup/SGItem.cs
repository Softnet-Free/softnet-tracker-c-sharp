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

namespace Softnet.Tracker.SiteModel
{
    public class SGItem : IComparable<SGItem>, IEquatable<SGItem>
    {
        public long serviceId;
        public string hostname;
        public bool enabled;
        public string version;

        public int hostname_state;
        public int enabled_state;

        public SGItem(long serviceId)
        {
            this.serviceId = serviceId;
            hostname = "";
            version = "";
            enabled = true;

            hostname_state = 0;
            enabled_state = 0;
        }

        public SGItem(long serviceId, string hostname)
        {
            this.serviceId = serviceId;
            this.hostname = hostname;
            this.version = "";
            this.enabled = true;

            hostname_state = 0;
            enabled_state = 0;
        }

        public SGItem(long serviceId, string hostname, string version, bool enabled)
        {
            this.serviceId = serviceId;
            this.hostname = hostname;
            this.version = version;
            this.enabled = enabled;

            hostname_state = 0;
            enabled_state = 0;
        }

        public int CompareTo(SGItem other)
        {
            return this.serviceId.CompareTo(other.serviceId);
        }

        public bool Equals(SGItem other)
        {
            return (this.serviceId == other.serviceId);
        }
    }
}
