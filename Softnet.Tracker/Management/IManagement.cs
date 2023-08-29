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
using System.ServiceModel;

namespace Softnet.Tracker.Management
{
    [ServiceContract]
    public interface IManagement
    {
        [OperationContract]
        int deleteDomain(long domainId);
        [OperationContract]
        int createPrivateUser(long domainId, string userName, bool dedicatedStatus);
        [OperationContract]
        int createContactUser(long domainId, long contactId, string userName, bool dedicatedStatus, bool enabledStatus);
        [OperationContract]
        int updateUser(long domainId, int userKind, long userId, bool enabledStatus);
        [OperationContract]
        int updateUser2(long domainId, long userId, string userName, bool enabledStatus, bool dedicatedStatus);
        [OperationContract]
        int deleteUser(long domainId, long userId);
        [OperationContract]
        int allowGuest(long siteId);
        [OperationContract]
        int denyGuest(long siteId);
        [OperationContract]
        int allowImplicitUsers(long siteId);
        [OperationContract]
        int denyImplicitUsers(long siteId);
        [OperationContract]
        int addSiteUser(long siteId, long userId);
        [OperationContract]
        int removeSiteUser(long siteId, long userId);
        [OperationContract]
        int setDefaultRole(long siteId, long roleId);
        [OperationContract]
        int removeDefaultRole(long siteId);
        [OperationContract]
        int updateUserRoles(long siteId, long userId, List<long> roles);
        [OperationContract]
        int addService(long siteId, string hostname);
        [OperationContract]
        int setServicePassword(long siteId, long serviceId, string salt, string saltedPassword);
        [OperationContract]
        int changeHostname(long siteId, long serviceId, string hostname);
        [OperationContract]
        int applyStructure(long siteId, long serviceId);
        [OperationContract]
        int setServicePingPeriod(long siteId, long serviceId, int pingPeriod);
        [OperationContract]
        int enableService(long siteId, long serviceId);
        [OperationContract]
        int disableService(long siteId, long serviceId);
        [OperationContract]
        int deleteService(long siteId, long serviceId);
        [OperationContract]
        int deleteSite(long siteId);
        [OperationContract]
        int enableSite(long siteId);
        [OperationContract]
        int disableSite(long siteId);
        [OperationContract]
        int setSiteAsMultiservice(long siteId);
        [OperationContract]
        int setClientPassword(long siteId, long clientId, string salt, string saltedPassword);
        [OperationContract]
        int setClientPingPeriod(long siteId, long clientId, int pingPeriod);
        [OperationContract]
        int deleteClient(long siteId, long clientId);
        [OperationContract]
        int deleteContact(long contactId);
        [OperationContract]
        int restoreContact(long ownerId, long selectedUserId);
        [OperationContract]
        int assignRoleProvider(long ownerId);
        [OperationContract]
        int removeRoleProvider(long ownerId);
        [OperationContract]
        int enableOwner(long ownerId);
        [OperationContract]
        int disableOwner(long ownerId);
        [OperationContract]
        int deleteOwner(long ownerId);
    }
}
