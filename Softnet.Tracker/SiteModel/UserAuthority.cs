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
    public class UserAuthority
    {
        public readonly bool isGuest;
        public readonly bool isStatelessGuest;
        public readonly List<long> roles;        

        public static readonly UserAuthority Guest;
        public static readonly UserAuthority StatelessGuest;

        static UserAuthority()
        {
            Guest = new UserAuthority(true, false);
            StatelessGuest = new UserAuthority(true, true);
        }

        private UserAuthority(bool isGuest, bool isStatelessGuest)
        {
            this.isGuest = isGuest;
            this.isStatelessGuest = isStatelessGuest;
            roles = null;            
        }

        private UserAuthority()
        {
            isGuest = false;
            isStatelessGuest = false;
            roles = null;           
        }

        private UserAuthority(List<long> roles)
        {
            isGuest = false;
            isStatelessGuest = false;
            this.roles = roles;
        }

        public static UserAuthority CreateInstance(List<long> roles)
        {
            return new UserAuthority(roles);
        }

        public static UserAuthority CreateInstance()
        {
            return new UserAuthority();
        }
    }
}
