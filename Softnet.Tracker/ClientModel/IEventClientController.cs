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

using Softnet.Tracker.SiteModel;

namespace Softnet.Tracker.ClientModel
{
    public interface IEventClientController
    {
        void OnAuthorityUpdated();
        void Monitor(long currentTime);
        void OnREventRaised(REventInstance eventInstance, Softnet.ServerKit.SoftnetMessage message, byte[] transferUid);
        void OnQEventRaised(QEventInstance eventInstance, Softnet.ServerKit.SoftnetMessage message, byte[] transferUid);
        void OnPEventRaised(PEventInstance eventInstance, byte[] arguments);
    }
}
