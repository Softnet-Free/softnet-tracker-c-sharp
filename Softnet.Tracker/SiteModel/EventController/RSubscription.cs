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
    public class RSubscription
    {
        public readonly long EventId;
        public readonly string EventName;
        public REventInstance EventInstance;
        public long DeliveredInstanceId;
        public byte[] TransactionUid;
        public long DeliveryExpirationTime;
        public bool Authorized;
        public bool Removed;

        public RSubscription(SubscriptionData sData)
        {
            EventId = sData.EventId;
            EventName = sData.EventName;
            DeliveredInstanceId = sData.InstanceId;
            EventInstance = null;
            Authorized = false;
            TransactionUid = null;
            DeliveryExpirationTime = 0;
            Removed = false;
        }

        public RSubscription(SCSubscriptionData sData)
        {
            EventId = sData.EventId;
            EventName = sData.EventName;
            DeliveredInstanceId = sData.DeliveredInstanceId;
            EventInstance = (REventInstance)sData.EventInstance;
            Authorized = sData.Authorized;
            TransactionUid = null;
            DeliveryExpirationTime = 0;
            Removed = false;
        }
    }
}
