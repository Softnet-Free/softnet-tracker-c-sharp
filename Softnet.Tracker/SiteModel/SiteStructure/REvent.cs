﻿/*
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
    public class REvent
    {
        public readonly string name;
        public readonly int guestAccess;
        public readonly List<string> roles;

        public REvent(string name)
        {
            this.name = name;
            this.guestAccess = 0;
            this.roles = null;
        }
        public REvent(string name, List<string> roles)
        {
            this.name = name;
            this.guestAccess = 0;
            this.roles = roles;
        }
        public REvent(string name, int guestAccess)
        {
            this.name = name;
            this.guestAccess = guestAccess;
            this.roles = null;
        }    	
    }
}