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

namespace Softnet.Tracker.Core
{
    static class ErrorCodes
    {
        public const int INCOMPATIBLE_PROTOCOL_VERSION = 92;
        public const int INVALID_SERVER_ENDPOINT = 93;

        public const int SERVICE_NOT_REGISTERED = 95;
        public const int INVALID_CLIENT_CATEGORY = 96;
        public const int CLIENT_NOT_REGISTERED = 97;
	    public const int PASSWORD_NOT_MATCHED = 98;
        public const int DUPLICATED_SERVICE_UID_USAGE = 100;
        public const int DUPLICATED_CLIENT_KEY_USAGE = 101;
        public const int CONSTRAINT_VIOLATION = 102;

        public const int ENDPOINT_DATA_FORMAT_ERROR = 110;
        public const int ENDPOINT_DATA_INCONSISTENT = 111;

	    public const int SERVICE_OFFLINE = 115;
	
        public const int SERVER_BUSY = 232;
        public const int CONFIG_ERROR = 233;
        public const int DBMS_ERROR = 234;
        public const int DATA_INTEGRITY_ERROR = 238;

        public const int RESTART = 245;
    }
}
