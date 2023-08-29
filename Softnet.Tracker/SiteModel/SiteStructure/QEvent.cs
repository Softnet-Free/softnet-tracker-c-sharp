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
    public class QEvent
    {
        public readonly string name;
        public readonly int lifeTime;
        public readonly int queueSize;
        public readonly int guestAccess;
        public readonly List<string> roles;

        public QEvent(string name, int lifeTime, int queueSize)
        {
            this.name = name;
            this.lifeTime = lifeTime;
            this.queueSize = queueSize;
            this.guestAccess = 0;
            this.roles = null;
        }
        public QEvent(string name, int lifeTime, int queueSize, List<string> roles)
        {
            this.name = name;
            this.lifeTime = lifeTime;
            this.queueSize = queueSize;
            this.guestAccess = 0;
            this.roles = roles;
        }
        public QEvent(string name, int lifeTime, int queueSize, int guestAccess)
        {
            this.name = name;
            this.lifeTime = lifeTime;
            this.queueSize = queueSize;
            this.guestAccess = guestAccess;
            this.roles = null;
        }
    }
}
