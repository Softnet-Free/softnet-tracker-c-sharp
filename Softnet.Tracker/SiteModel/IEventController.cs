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

namespace Softnet.Tracker.SiteModel
{
    public interface IEventController
    {
        void AuthorizeRSubscription(RSubscription subscription, UserAuthority userAuthority);
        void AuthorizeQSubscription(QSubscription subscription, UserAuthority userAuthority);
        void InitPSubscription(PSubscription subscription, long clientId);
        bool AuthorizeSCSubscription(SCSubscriptionData subsData);

        void AcceptREvent(EventIData eventIData);
        void AcceptQEvent(EventIData eventIData);
        void AcceptPEvent(EventIData eventIData);

        REventInstance GetNextEvent(REventInstance eventInstance);
        QEventInstance GetNextEvent(QEventInstance eventInstance);
        PEventInstance GetNextEvent(PEventInstance eventInstance, long clientId);

        object GetMutex();
        void Monitor();
    }
}
