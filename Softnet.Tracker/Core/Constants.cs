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

namespace Softnet.Tracker.Core
{
    class Constants
    {
        public const int ProtocolVersion = 1;
        public const int Tracker_TcpPort = 7740;        
        public const int Balancer_TcpPort = 7737;
        public const int Balancer_SaeaBufferSize = 128;

        public const int SiteStructure_MaxUserRoles = 30;
        public const int SiteStructure_MaxAppEvents = 100;

        public static class SiteKind
        {
            public const int SingleService = 1;
            public const int MultiService = 2;
        }

        public static class UserKind
        {
            public const int Owner = 1;
            public const int Private = 2;
            public const int Contact = 3;
            public const int Guest = 4;
            public const int StatelessGuest = 5;
        }

        public static class ClientCategory
        { 
            public const int SingleService = 1;
	        public const int MultiService = 2;
            public const int SingleServiceStateless = 3;
            public const int MultiServiceStateless = 4;
        }

        public static class EventKind
        {
            public const int Replacing = 1;
            public const int Queueing = 2;
            public const int Private = 4;
        }

        public class ClientStatus
        {            
            public const int Offline = 0;
            public const int Online = 1;
            public const int AccessDenied = 2;
            public const int ServiceDisabled = 3;
            public const int ServiceOwnerDisabled = 4;
            public const int ServiceTypeConflict = 5;
            public const int SiteBlank = 6;
        }

        public class ServiceStatus
        {            
            public const int Offline = 0;
            public const int Online = 1;
            public const int Disabled = 2;
            public const int SiteDisabled = 3;
            public const int OwnerDisabled = 4;
            public const int SiteStrutureMismatch = 5;
            public const int ServiceTypeConflict = 6;
            public const int SiteBlank = 7;
        }

        public static class Balancer
        {
            // Input
            public const int CLIENT_S_KEY = 1;
    	    public const int CLIENT_M_KEY = 2;
    	    public const int CLIENT_SS_KEY = 3;
    	    public const int CLIENT_MS_KEY = 4;
            public const int SERVICE_UID = 5;

            // Output
    	    public const byte SUCCESS = 1;
            public const byte ERROR = 2;
            public const byte IP_V6 = 6;
    	    public const byte IP_V4 = 4;
        }
        
        public class Service
        {
            public const byte EndpointType = 1;            

            public class Channel
            {
                public const byte ModuleId = 1;
                // Input
                public const byte OPEN = 1;
                public const byte RESTORE = 2;
                public const byte HASH_AND_KEY2 = 3;
                // Output
                public const byte SALT_AND_KEY1 = 1;
                public const byte OPEN_OK = 2;
                public const byte RESTORE_OK = 3;
                public const byte ERROR = 4;
            }

            public class ChannelMonitor
            {
                public const byte ModuleId = 2;
                // Input
                public const byte PING = 1;
                public const byte KEEP_ALIVE = 2;
                // Output
                public const byte PONG = 1;
            }

            public static class Installer
            {
                public const byte ModuleId = 5;
                // Input
                public const byte STATE = 1;
                public const byte SITE_STRUCTURE = 2;
                // Output
                public const byte GET_STATE = 1;
                public const byte GET_SITE_STRUCTURE = 2;
                public const byte ONLINE = 3;
                public const byte PARKED = 4;
            }

            public static class SyncController
            {
                public const byte ModuleId = 6;
                // Output
                public const byte PARAMS = 1;
        	    public const byte HOSTNAME_CHANGED = 2;
                public const byte SET_PING_PERIOD = 3;
            }

            public static class RBMembership
            {
                public const byte ModuleId = 7;
                // Output
                public const byte USER_LIST = 1;
                public const byte USER_INCLUDED = 2;
                public const byte USER_UPDATED = 3;
                public const byte USER_REMOVED = 4;
                public const byte GUEST_ALLOWED = 5;
                public const byte GUEST_DENIED = 6;
            }

            public static class UBMembership
            {
                public const byte ModuleId = 8;
                // Output
                public const byte USER_LIST = 1;
                public const byte USER_INCLUDED = 2;
                public const byte USER_UPDATED = 3;
                public const byte USER_REMOVED = 4;
                public const byte GUEST_ALLOWED = 5;
                public const byte GUEST_DENIED = 6;
            }

            public static class TcpController
            {
                public const byte ModuleId = 10;
                // Input
                public const byte REQUEST_OK = 1;
                public const byte REQUEST_ERROR = 2;
                public const byte CONNECTION_ACCEPTED = 3;
                public const byte AUTH_KEY = 7;
                // Output
                public const byte REQUEST = 1;
                public const byte RZV_DATA = 2;
                public const byte AUTH_HASH = 7;
                public const byte AUTH_ERROR = 8;
            }

            public static class UdpController
            {
                public const byte ModuleId = 11;
                // Input
                public const byte REQUEST_OK = 1;
                public const byte REQUEST_ERROR = 2;
                public const byte CONNECTION_ACCEPTED = 3;
                public const byte AUTH_KEY = 7;
                // Output
                public const byte REQUEST = 1;
                public const byte RZV_DATA = 2;
                public const byte AUTH_HASH = 7;
                public const byte AUTH_ERROR = 8;
            }

            public static class RpcController
            {
                public const byte ModuleId = 14;
                // Output
                public const byte REQUEST = 1;
                // Input
                public const byte RESULT = 1;
                public const byte APP_ERROR = 2;
                public const byte SOFTNET_ERROR = 3;
            }

            public static class EventController
            {
                public const byte ModuleId = 15;
        	    // Input
        	    public const byte REPLACING_EVENT = 1;
                public const byte QUEUEING_EVENT = 2;
                public const byte PRIVATE_EVENT = 4;
                public const byte REPLACING_NULL_EVENT = 5;
                public const byte NEW_STORAGE_UID = 9;
                // Output
        	    public const byte EVENT_ACK = 1;
                public const byte ILLEGAL_EVENT_NAME = 2;
                public const byte LAST_STORAGE_UID = 9;
            }
        }

        public class Client
        {
            public const byte EndpointType = 2;

            public class Channel
            {
                public const byte ModuleId = 1;
                // Input
                public const byte OPEN = 1;
                public const byte RESTORE = 2;
                public const byte HASH_AND_KEY2 = 3;
                // Output
                public const byte SALT_AND_KEY1 = 1;
                public const byte OPEN_OK = 2;
                public const byte OPEN_OK2 = 3;
                public const byte RESTORE_OK = 4;
                public const byte ERROR = 5;
            }

            public class ChannelMonitor
            {
                public const byte ModuleId = 2;
                // Input
                public const byte PING = 1;
                public const byte KEEP_ALIVE = 2;
                // Output
                public const byte PONG = 1;
            }

            public static class Installer
            {
                public const byte ModuleId = 5;
                // Input
                public const byte STATE = 1;
                // Output
                public const byte GET_STATE = 1;
                public const byte ONLINE = 2;
                public const byte PARKED = 3;
            }

            public static class StateController
            {
                public const byte ModuleId = 6;
                // Output
                public const byte PARAMS = 1;
                public const byte SET_PING_PERIOD = 2;
            }

            public static class SingleServiceGroup
            {
                public const byte ModuleId = 7;
                // Output
                public const byte SERVICE_UPDATED = 1;
                public const byte SERVICE_ONLINE = 2;
                public const byte SERVICE_ONLINE_2 = 3;
                public const byte SERVICE_OFFLINE = 4;
            }

            public static class MultiServiceGroup
            {
                public const byte ModuleId = 8;
                // Output
                public const byte SERVICES_UPDATED = 1;
                public const byte SERVICES_ONLINE = 2;
                public const byte SERVICE_INCLUDED = 3;
                public const byte SERVICE_REMOVED = 4;
                public const byte SERVICE_UPDATED = 5;
                public const byte SERVICE_ONLINE = 6;
                public const byte SERVICE_ONLINE_2 = 7;
                public const byte SERVICE_OFFLINE = 8;
            }

            public static class Membership
            {
                public const byte ModuleId = 9;
                // Output
                public const byte GUEST = 1;
                public const byte USER = 2;
            }

            public static class TcpController
            {
                public const byte ModuleId = 10;
                // Input
                public const byte REQUEST = 1;
                public const byte AUTH_KEY = 7;
                // Output
                public const byte RZV_DATA = 1;
                public const byte REQUEST_ERROR = 2;
                public const byte CONNECTION_ACCEPTED = 3;
                public const byte AUTH_HASH = 7;
                public const byte AUTH_ERROR = 8;
            }

            public static class UdpController
            {
                public const byte ModuleId = 11;
                // Input
                public const byte REQUEST = 1;
                public const byte AUTH_KEY = 7;
                // Output
                public const byte RZV_DATA = 1;
                public const byte REQUEST_ERROR = 2;
                public const byte CONNECTION_ACCEPTED = 3;
                public const byte AUTH_HASH = 7;
                public const byte AUTH_ERROR = 8;
            }

            public static class RpcController
            {
                public const byte ModuleId = 14;
                // Input
                public const byte REQUEST = 1;
                // Output
                public const byte RESULT = 1;
                public const byte APP_ERROR = 2;
                public const byte SOFTNET_ERROR = 3;
            }

            public static class EventController
            {
                public const byte ModuleId = 15;
                // Input
                public const byte REPLACING_EVENT_ACK = 1;
                public const byte QUEUEING_EVENT_ACK = 2;
                public const byte DATAGRAM_EVENT_ACK = 3;
                public const byte PRIVATE_EVENT_ACK = 4;
                public const byte EVENT_REJECTED = 5;

                public const byte SYNC_OK = 6;
                public const byte SUBSCRIPTIONS = 7;
                public const byte ADD_SUBSCRIPTION = 8;
                public const byte REMOVE_SUBSCRIPTION = 9;

                // Output
                public const byte REPLACING_EVENT = 1;
                public const byte QUEUEING_EVENT = 2;
                public const byte DATAGRAM_EVENT = 3;
                public const byte PRIVATE_EVENT = 4;
                public const byte REPLACING_NULL_EVENT = 5;

                public const byte SYNC = 6;
                public const byte ILLEGAL_SUBSCRIPTION = 7;
            }
        }
    }
}
