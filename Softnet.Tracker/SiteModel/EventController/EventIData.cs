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
    public class EventIData
    {
        public string Name = null;
        public long EventId = 0;
        public long InstanceId = 0;
        public long ServiceId = 0;
        public long ClientId = 0;
        public bool IsNull = false;
        public long CreatedTimeTicks;
        public DateTime CreatedDate;
        public byte[] Arguments = null;
    }
}
