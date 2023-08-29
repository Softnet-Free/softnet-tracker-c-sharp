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
    public class PSubscription
    {
        public readonly long EventId;
        public readonly string EventName;
        public PEventInstance EventInstance;
        public long DeliveredInstanceId;
        public byte[] TransactionUid;
        public long DeliveryExpirationTime;
        public bool Removed;

        public PSubscription(SubscriptionData sData)
        {
            EventId = sData.EventId;
            EventName = sData.EventName;
            DeliveredInstanceId = sData.InstanceId;
            EventInstance = null;
            TransactionUid = null;
            DeliveryExpirationTime = 0;
            Removed = false;
        }
    }
}
