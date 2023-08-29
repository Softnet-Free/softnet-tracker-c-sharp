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

namespace Softnet.Tracker.SiteModel
{
    public class REventInstance
    {
        public readonly long EventId;
        public readonly long InstanceId;
        public readonly long ServiceId;
        public readonly long CreatedTimeTicks;
        public readonly long SetupTimeTicks;
        public readonly long SetupTimeSeconds;
        public readonly DateTime CreatedDate;
        public readonly bool IsNull;
        public readonly bool HasArguments;
        public readonly byte[] Arguments;

        public REventInstance(long instanceId, long eventId, long serviceId, long createdTimeTicks, long setupTimeTicks, long setupTimeSeconds, DateTime createdDate, bool isNull, bool hasArguments, byte[] arguments)
        {
            InstanceId = instanceId;
            EventId = eventId;
            ServiceId = serviceId;
            CreatedTimeTicks = createdTimeTicks;
            SetupTimeTicks = setupTimeTicks;
            SetupTimeSeconds = setupTimeSeconds;
            CreatedDate = createdDate;
            IsNull = isNull;
            if (IsNull)
            {
                HasArguments = false;
                Arguments = null;
            }
            else
            {
                HasArguments = hasArguments;
                Arguments = arguments;
            }
        }

        private REventInstance(EventIData eventIData, long setupTimeTicks, long setupTimeSeconds, bool hasArguments, byte[] arguments)
        {
            InstanceId = eventIData.InstanceId;
            EventId = eventIData.EventId;
            ServiceId = eventIData.ServiceId;
            CreatedTimeTicks = eventIData.CreatedTimeTicks;
            SetupTimeTicks = setupTimeTicks;
            SetupTimeSeconds = setupTimeSeconds;
            CreatedDate = eventIData.CreatedDate;
            IsNull = eventIData.IsNull;
            if (IsNull)
            {
                HasArguments = false;
                Arguments = null;
            }
            else
            {
                HasArguments = hasArguments;
                Arguments = arguments;
            }
        }

        public static REventInstance InstantiateNewEvent(EventIData eventIData)
        {
            if (eventIData.Arguments != null)
            {
                if (eventIData.Arguments.Length <= 128)
                    return new REventInstance(eventIData, eventIData.CreatedTimeTicks, SystemClock.Seconds, true, eventIData.Arguments);
                else
                    return new REventInstance(eventIData, eventIData.CreatedTimeTicks, SystemClock.Seconds, true, null);
            }
            else
            {
                return new REventInstance(eventIData, eventIData.CreatedTimeTicks, SystemClock.Seconds, false, null);
            }
        }
    }
}
