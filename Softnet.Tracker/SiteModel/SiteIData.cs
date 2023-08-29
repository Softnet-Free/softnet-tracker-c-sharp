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
    class SiteIData
    {
        public string ServiceType = null;
        public string ContractAuthor = null;
        public byte[] SiteUid = null;
        public int SiteKind = 0;
        public bool Structured = false;
        public bool SiteEnabled = false;
        public bool OwnerEligible = false;
        public byte[] SSHash = null;
        public bool GuestSupported = false;
        public bool GuestAllowed = false;
        public bool GuestEnabled = false;
        public bool StatelessGuestSupported = false;
        public bool RolesSupported = false;
        public bool EventsSupported = false;
        public List<SGItem> SGItems = null;
        public List<MUser> MUsers = null;
        public List<MRole> MRoles = null;
        public List<MUserRole> UserRoles = null;
        public List<EventData> EventList = null;
        public List<REventInstance> REventInstances = null;
        public List<QEventInstance> QEventInstances = null;
        public List<PEventInstance> PEventInstances = null;
    }
}
