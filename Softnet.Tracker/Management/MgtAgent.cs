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
using System.Configuration;
using Softnet.ServerKit;
using Softnet.Tracker.Core;
using System.Data.SqlClient;

namespace Softnet.Tracker.Management
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple)]
    class MgtAgent : IManagement
    {
        public int deleteDomain(long domainId)
        {
            try
            {
                List<long> siteIdentities = new List<long>();
                SoftnetRegistry.Mgt_GetSiteIdentitiesForDomain(domainId, siteIdentities);
                try
                {
                    SoftnetRegistry.Mgt_DeleteDomain(domainId);
                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.OnDeleted();
                    }
                    return 0;   
                }
                catch (SqlException e)
                {
                    AppLog.WriteLine(e.Message);
                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Remove(ErrorCodes.RESTART);
                    }
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int createPrivateUser(long domainId, string userName, bool dedicatedStatus)
        {            
            try
            {
                List<long> siteIdentities = new List<long>();
                SoftnetRegistry.Mgt_GetSiteIdentitiesForDomain(domainId, siteIdentities);
                try
                {
                    Container<long> resultData = new Container<long>();
                    int resultCode = SoftnetRegistry.Mgt_CreatePrivateUser(domainId, userName, dedicatedStatus, resultData);
                    if (resultCode != 0)
                        return resultCode;

                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Mgt_OnUserCreated(resultData.Obj);
                    }
                    return 0;    
                }
                catch (SqlException e)
                {
                    AppLog.WriteLine(e.Message);
                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Remove(ErrorCodes.RESTART);
                    }
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int createContactUser(long domainId, long contactId, string userName, bool dedicatedStatus, bool enabledStatus)
        {
            try
            {
                List<long> siteIdentities = new List<long>();
                SoftnetRegistry.Mgt_GetSiteIdentitiesForDomain(domainId, siteIdentities);
                try
                {
                    Container<long> resultData = new Container<long>();
                    int resultCode = SoftnetRegistry.Mgt_CreateContactUser(domainId, contactId, userName, dedicatedStatus, enabledStatus, resultData);
                    if (resultCode != 0)
                        return resultCode;

                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Mgt_OnUserCreated(resultData.Obj);
                    }
                    return 0;
                }
                catch (SqlException e)
                {
                    AppLog.WriteLine(e.Message);
                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Remove(ErrorCodes.RESTART);
                    }
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int updateUser(long domainId, int userKind, long userId, bool enabledStatus)
        {
            try
            {
                List<long> siteIdentities = new List<long>();
                SoftnetRegistry.Mgt_GetSiteIdentitiesForDomain(domainId, siteIdentities);
                try
                {
                    if (userKind == Constants.UserKind.Owner)
                    {
                        SoftnetRegistry.Mgt_UpdateUser(userId, enabledStatus);
                        foreach (long siteId in siteIdentities)
                        {
                            SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                            if (site != null)
                                site.Mgt_OnUserUpdated(userId);
                        }
                    }
                    else // userKind == Constants.UserKind.Guest
                    {
                        SoftnetRegistry.Mgt_UpdateUser(userId, enabledStatus);
                        foreach (long siteId in siteIdentities)
                        {
                            SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                            if (site != null)
                                site.Mgt_OnGuestStatusChanged();
                        }
                    }
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Remove(ErrorCodes.RESTART);
                    }
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int updateUser2(long domainId, long userId, string userName, bool enabledStatus, bool dedicatedStatus)
        {
            try
            {
                List<long> siteIdentities = new List<long>();
                SoftnetRegistry.Mgt_GetSiteIdentitiesForDomain(domainId, siteIdentities);
                try
                {
                    SoftnetRegistry.Mgt_UpdateUser(userId, userName, enabledStatus, dedicatedStatus);
                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Mgt_OnUserUpdated(userId);
                    }
                    return 0;
                }
                catch (SqlException e)
                {
                    AppLog.WriteLine(e.Message);
                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Remove(ErrorCodes.RESTART);
                    }
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int deleteUser(long domainId, long userId)
        {
            try
            {
                List<long> siteIdentities = new List<long>();
                SoftnetRegistry.Mgt_GetSiteIdentitiesForDomain(domainId, siteIdentities);
                try
                {
                    SoftnetRegistry.Mgt_DeleteUser(userId);
                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Mgt_OnUserDeleted(userId);
                    }
                    return 0;
                }
                catch (SqlException e)
                {
                    AppLog.WriteLine(e.Message);
                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Remove(ErrorCodes.RESTART);
                    }
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int allowGuest(long siteId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_AllowGuest(siteId);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnGuestStatusChanged();                    
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int denyGuest(long siteId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_DenyGuest(siteId);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnGuestStatusChanged();
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int allowImplicitUsers(long siteId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_AllowImplicitUsers(siteId);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnUsersUpdated();
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int denyImplicitUsers(long siteId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_DenyImplicitUsers(siteId);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnUsersUpdated();
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int addSiteUser(long siteId, long userId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_AddSiteUser(siteId, userId);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnUserUpdated(userId);
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int removeSiteUser(long siteId, long userId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_RemoveSiteUser(siteId, userId);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnUserUpdated(userId);
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int setDefaultRole(long siteId, long roleId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_SetDefaultRole(siteId, roleId);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnUsersUpdated();
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int removeDefaultRole(long siteId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_RemoveDefaultRole(siteId);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnUsersUpdated();
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int updateUserRoles(long siteId, long userId, List<long> roles)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_UpdateUserRoles(siteId, userId, roles);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnUserUpdated(userId);
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int addService(long siteId, string hostname)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    Pair<long, string> dto = new Pair<long, string>();
                    dto.Second = hostname;
                    int returnCode = SoftnetRegistry.Mgt_AddService(siteId, dto);
                    if (returnCode != 0)
                        return returnCode;

                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnServiceCreated(dto.First, dto.Second);
                    return 0;
                }
                catch (SqlException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int deleteService(long siteId, long serviceId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_DeleteService(siteId, serviceId);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnServiceDeleted(serviceId);
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_RestartService(serviceId);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int setServicePassword(long siteId, long serviceId, string salt, string saltedPassword)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_SetServicePassword(serviceId, salt, saltedPassword);
                }
                finally
                {
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_RestartService(serviceId);
                }
                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int changeHostname(long siteId, long serviceId, string hostname)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_ChangeHostname(serviceId, hostname);
                }
                finally
                {
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnHostnameChanged(serviceId);
                }
                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int applyStructure(long siteId, long serviceId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                SiteModel.Site site = SoftnetTracker.GetSiteToConstruct(siteId);
                site.Mgt_ConstructSite(serviceId);
                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                if (e.ErrorCode == ErrorCodes.DATA_INTEGRITY_ERROR)
                    return -5;
                return 1;
            }
        }

        public int setServicePingPeriod(long siteId, long serviceId, int pingPeriod)
        {
            try
            {
                try
                {
                    SoftnetRegistry.ServiceParams_SetPingPeriod(serviceId, pingPeriod);
                }
                finally
                {
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnServicePingPeriodChanged(serviceId);
                }
                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int enableService(long siteId, long serviceId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_EnableService(serviceId);
                }
                finally
                {
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnServiceEnabledStatusChanged(serviceId);
                }
                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int disableService(long siteId, long serviceId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_DisableService(serviceId);
                }
                finally
                {
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnServiceEnabledStatusChanged(serviceId);
                }
                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int deleteSite(long siteId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_DeleteSite(siteId);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.OnDeleted();
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int enableSite(long siteId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_EnableSite(siteId);
                }
                finally
                {
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                }
                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int disableSite(long siteId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_DisableSite(siteId);
                }
                finally
                {
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                }
                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int setSiteAsMultiservice(long siteId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_SetSiteAsMultiService(siteId);
                }
                finally
                {
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Remove(ErrorCodes.RESTART);
                }
                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int setClientPassword(long siteId, long clientId, string salt, string saltedPassword)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_SetClientPassword(clientId, salt, saltedPassword);
                }
                finally
                {
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_RestartClient(clientId);
                }
                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }
        
        public int setClientPingPeriod(long siteId, long clientId, int pingPeriod)
        {
            try
            {
                try
                {
                    SoftnetRegistry.ClientParams_SetPingPeriod(clientId, pingPeriod);
                }
                finally
                {
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnClientPingPeriodChanged(clientId);
                }
                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int deleteClient(long siteId, long clientId)
        {
            try
            {
                SoftnetRegistry.Mgt_ValidateConnection();
                try
                {
                    SoftnetRegistry.Mgt_DeleteClient(clientId);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_OnClientDeleted(clientId);
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                    if (site != null)
                        site.Mgt_RestartClient(clientId);
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int deleteContact(long contactId)
        {
            try
            {
                Pair<int, long> result = new Pair<int,long>();
                List<long> mySites = new List<long>();
                List<long> peerSites = new List<long>();
                SoftnetRegistry.Mgt_GetSiteIdentitiesForContact(contactId, result, mySites, peerSites);

                try
                {
                    SoftnetRegistry.Mgt_DeleteContact(contactId);
                    foreach (long siteId in mySites)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Mgt_OnContactDeleted(contactId);
                    }
                    if (result.First == 2)
                    {
                        foreach (long siteId in peerSites)
                        {
                            SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                            if (site != null)
                                site.Mgt_OnContactDisabled(result.Second);
                        }
                    }
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    foreach (long siteId in mySites)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Mgt_OnContactDisabled(contactId);
                    }
                    if (result.First == 2)
                    {
                        foreach (long siteId in peerSites)
                        {
                            SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                            if (site != null)
                                site.Mgt_OnContactDisabled(result.Second);
                        }
                    }
                    return 1;
                }
            }
            catch(SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int restoreContact(long ownerId, long partnerId)
        {
            try
            {
                List<Pair<long, long>> userIdentities = new List<Pair<long,long>>();
                SoftnetRegistry.Mgt_GetUserIdentitiesForPeerContact(ownerId, partnerId, userIdentities);

                try
                {
                    SoftnetRegistry.Mgt_RestoreContact(ownerId, partnerId);
                }
                finally
                {
                    foreach (Pair<long, long> item in userIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(item.First);
                        if (site != null)
                            site.Mgt_OnUserUpdated(item.Second);
                    }
                }

                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int assignRoleProvider(long ownerId)
        {
            try
            {
                List<long> siteIdentities = new List<long>();
                SoftnetRegistry.MgtUsers_GetSiteIdentitiesForProvider(ownerId, siteIdentities);

                try
                {
                    SoftnetRegistry.MgtUsers_AssignRoleProvider(ownerId);
                }
                finally
                {
                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Remove(ErrorCodes.RESTART);
                    }
                }

                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int removeRoleProvider(long ownerId)
        {
            try
            {
                List<long> siteIdentities = new List<long>();
                SoftnetRegistry.MgtUsers_GetSiteIdentitiesForProvider(ownerId, siteIdentities);

                try
                {
                    SoftnetRegistry.MgtUsers_RemoveRoleProvider(ownerId);
                }
                finally
                {
                    foreach (long siteId in siteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Remove(ErrorCodes.RESTART);
                    }
                }

                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int enableOwner(long ownerId)
        {
            try
            {
                List<long> ownedSiteIdentities = new List<long>();
                List<Pair<long, long>> userIdentities = new List<Pair<long, long>>();
                SoftnetRegistry.MgtUsers_GetIdentitiesForEnabledOwner(ownerId, ownedSiteIdentities, userIdentities);

                try
                {
                    SoftnetRegistry.MgtUsers_EnableOwner(ownerId);
                }
                finally
                {
                    foreach (long siteId in ownedSiteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Remove(ErrorCodes.RESTART);
                    }
                    foreach (Pair<long, long> item in userIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(item.First);
                        if (site != null)
                            site.Mgt_OnUserUpdated(item.Second);
                    }
                }

                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int disableOwner(long ownerId)
        {
            try
            {
                List<long> ownedSiteIdentities = new List<long>();
                List<long> consumedSiteIdentities = new List<long>();
                SoftnetRegistry.MgtUsers_GetSiteIdentitiesForOwner(ownerId, ownedSiteIdentities, consumedSiteIdentities);

                try
                {
                    SoftnetRegistry.MgtUsers_DisableOwner(ownerId);
                }
                finally
                {
                    foreach (long siteId in ownedSiteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Remove(ErrorCodes.RESTART);
                    }
                    foreach (long siteId in consumedSiteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Mgt_OnConsumerDisabled(ownerId);
                    }
                }

                return 0;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        public int deleteOwner(long ownerId)
        {
            try
            {
                List<long> ownedSiteIdentities = new List<long>();
                List<long> consumedSiteIdentities = new List<long>();
                SoftnetRegistry.MgtUsers_GetSiteIdentitiesForOwner(ownerId, ownedSiteIdentities, consumedSiteIdentities);
                try
                {
                    SoftnetRegistry.MgtUsers_DeleteOwner(ownerId);
                    foreach (long siteId in ownedSiteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.OnDeleted();
                    }
                    foreach (long siteId in consumedSiteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Mgt_OnConsumerDeleted(ownerId);
                    }
                    return 0;
                }
                catch (SoftnetException e)
                {
                    AppLog.WriteLine(e.Message);
                    foreach (long siteId in ownedSiteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Remove(e.ErrorCode);
                    }
                    foreach (long siteId in consumedSiteIdentities)
                    {
                        SiteModel.Site site = SoftnetTracker.FindSite(siteId);
                        if (site != null)
                            site.Mgt_OnConsumerDisabled(ownerId);
                    }
                    return 1;
                }
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                return 1;
            }
        }

        static ServiceHost s_managementHost = null;

        public static void Start()
        {
            try
            {
                if (s_managementHost != null)
                {
                    s_managementHost.Abort();
                    s_managementHost = null;
                }

                TrackerConfig trackerConfig = (TrackerConfig)ConfigurationManager.GetSection("tracker");
                ManagementEndpointConfig managementEndpoint = trackerConfig.ManagementEndpoint;
                string managementUri = "net.tcp://" + string.Format("{0}:{1}", managementEndpoint.IP, managementEndpoint.Port);
                NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);

                ServiceHost managementHost = new ServiceHost(typeof(MgtAgent));
                managementHost.AddServiceEndpoint(typeof(IManagement), binding, managementUri);
                managementHost.Open();

                s_managementHost = managementHost;
            }
            catch (CommunicationException ex)
            {
                throw new SoftnetException(ErrorCodes.CONFIG_ERROR, ex.Message);
            }
        }

        public static void Close()
        {
            try
            {
                if (s_managementHost != null)
                {
                    s_managementHost.Close(TimeSpan.FromSeconds(20));
                    s_managementHost = null;
                }
            }
            catch (Exception) { }
        }
    }
}
