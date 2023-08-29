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
    public class MUser : IComparable<MUser>, IEquatable<MUser>
    {
        public long id;
        public string name;
        public int userKind;
        public long contactId;
        public long consumerId;

        public UserAuthority authority;
        public byte[] hash;
        public byte[] rolesHash;

        public bool confirmed;
        public int state;       

        public MUser(long userId, string name, int userKind)
        {
            this.id = userId;
            this.name = name;
            this.userKind = userKind;
            contactId = 0;
            consumerId = 0;

            authority = null;
            hash = null;
            rolesHash = null;

            confirmed = true;
            state = 0;            
        }

        public MUser(long userId, string name, long contactId, long consumerId)
        {
            this.id = userId;
            this.name = name;
            this.userKind = 3;
            this.contactId = contactId;
            this.consumerId = consumerId;

            authority = null;
            hash = null;
            rolesHash = null;

            confirmed = true;
            state = 0;
        }

        public MUser(long userId)
        {
            this.id = userId;
            name = "";
            userKind = 0;
            contactId = 0;
            consumerId = 0;

            authority = null;
            hash = null;
            rolesHash = null;

            confirmed = false;
            state = 0;            
        }

        public int CompareTo(MUser other)
        {
            return this.id.CompareTo(other.id);
        }

        public bool Equals(MUser other)
        {
            return (this.id == other.id);
        }
    }
}
