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
using System.Threading;

using Softnet.Asn;
using Softnet.ServerKit;
using Softnet.Tracker.Core;
using Softnet.Tracker.ClientModel;
using Softnet.Tracker.ServiceModel;

namespace Softnet.Tracker.SiteModel
{
    class RBMembership : IMembership
    {
        public static RBMembership Create(
            Site site, 
            List<Service> services, 
            List<Client> clients,
            SiteIData siteIData,            
            Action<long> userRemovedCallback,
            Action<long> userDeletedCallback,
            Action clientRestartedCallback,
            Action guestAllowedCallback,
            Action guestDeniedCallback)
        {            
            var instance = new RBMembership();
            instance.m_Site = site;
            instance.site_mutex = site.mutex;
            instance.m_Services = services;
            instance.m_Clients = clients;
            instance.m_Users = siteIData.MUsers;
            instance.m_Roles = siteIData.MRoles;
            instance.c_GuestSupported = siteIData.GuestSupported;
            instance.c_StatelessGuestSupported = siteIData.StatelessGuestSupported;
            instance.UserRemovedCallback = userRemovedCallback;
            instance.UserDeletedCallback = userDeletedCallback;
            instance.ClientRestartedCallback = clientRestartedCallback;
            instance.GuestAllowedCallback = guestAllowedCallback;
            instance.GuestDeniedCallback = guestDeniedCallback;
            instance.Init(siteIData.UserRoles, siteIData.GuestEnabled, siteIData.GuestAllowed);
            return instance;
        }

        private RBMembership() { }

        private void Init(List<MUserRole> userRoles, bool guestEnabled, bool guestAllowed)
        {
            foreach (MUser mUser in m_Users)
            {
                List<long> roles = new List<long>();                
                foreach (MUserRole mur in userRoles)
                {
                    if (mur.userId == mUser.id)
                        roles.Add(mur.roleId);
                }
                mUser.authority = UserAuthority.CreateInstance(roles);
                ComputeUserHash(mUser);
            }

            m_Roles.Sort();
            ComputeHash();

            if (c_GuestSupported)
            {
                if (guestEnabled && guestAllowed)
                    m_GuestAllowed = true;
                else
                    m_GuestAllowed = false;
                
                m_Guest = new MUser(0, null, 4);
                m_Guest.authority = UserAuthority.Guest;

                if (c_StatelessGuestSupported)
                {
                    m_StatelessGuest = new MUser(0, null, 5);
                    m_StatelessGuest.authority = UserAuthority.StatelessGuest;
                }
            }
            else
                m_GuestAllowed = false;

            m_userlist_state = 0;
            m_guest_state = 0;
        }

        Site m_Site;
        object site_mutex;

        bool c_GuestSupported;
        bool c_StatelessGuestSupported;

        bool m_GuestAllowed;
        MUser m_Guest;
        MUser m_StatelessGuest;

        List<Service> m_Services;
        List<Client> m_Clients;

        List<MUser> m_Users;
        List<MRole> m_Roles;
        int m_userlist_state;
        int m_guest_state;

        byte[] m_Hash;

        Action<long> UserRemovedCallback;
        Action<long> UserDeletedCallback;
        Action ClientRestartedCallback;
        Action GuestAllowedCallback;
        Action GuestDeniedCallback;        

        public int AuthorizeClient(Softnet.Tracker.ClientModel.ClientInstaller clientInstaller)
        {
            if (m_userlist_state != 0)
                return -1;

            MUser mUser = m_Users.Find(x => x.id == clientInstaller.UserId);
            if (mUser == null)
            {
                if (m_guest_state != 0)
                    return -1;

                if (m_GuestAllowed)
                {
                    clientInstaller.SetUser(m_Guest);
                    return 0;
                }
                else
                    return 1;
            }
            else
            {
                if (mUser.confirmed && mUser.state == 0)
                {
                    clientInstaller.SetUser(mUser, m_Roles);
                    return 0;
                }
                else
                    return -1;
            }
        }

        public int AuthorizeGuest(Softnet.Tracker.ClientModel.ClientInstaller clientInstaller)
        {
            if (m_guest_state != 0)
                return -1;

            if (m_GuestAllowed)
            {
                clientInstaller.SetUser(m_Guest);
                return 0;
            }
            else
                return 1;
        }

        public int AuthorizeStatelessGuest(Softnet.Tracker.ClientModel.ClientInstaller clientInstaller)
        {
            if (c_StatelessGuestSupported == false)
                return 1;

            if (m_guest_state != 0)
                return -1;

            if (m_GuestAllowed)
            {
                clientInstaller.SetUser(m_StatelessGuest);
                return 0;
            }
            else
                return 1;
        }

        public void SyncService(ServiceInstaller serviceInstaller)
        {
            if (serviceInstaller.IsStructureUpdated == false)
            {
                if (ByteArray.Equals(serviceInstaller.ReceivedUserListHash, m_Hash) == false)
                {
                    serviceInstaller.Send(EncodeMessage_UserList());
                }

                if (c_GuestSupported)
                {
                    if (serviceInstaller.ReceivedGuestAllowed != m_GuestAllowed)
                    {
                        if (m_GuestAllowed)
                        {
                            serviceInstaller.Send(MsgBuilder.Create(Constants.Service.RBMembership.ModuleId, Constants.Service.RBMembership.GUEST_ALLOWED));
                        }
                        else
                        {
                            serviceInstaller.Send(MsgBuilder.Create(Constants.Service.RBMembership.ModuleId, Constants.Service.RBMembership.GUEST_DENIED));
                        }
                    }
                }
            }
            else 
            {
                serviceInstaller.Send(EncodeMessage_UserList());

                if (c_GuestSupported)
                {
                    if (m_GuestAllowed)
                    {
                        serviceInstaller.Send(MsgBuilder.Create(Constants.Service.RBMembership.ModuleId, Constants.Service.RBMembership.GUEST_ALLOWED));
                    }
                    else
                    {
                        serviceInstaller.Send(MsgBuilder.Create(Constants.Service.RBMembership.ModuleId, Constants.Service.RBMembership.GUEST_DENIED));
                    }
                }
            }
        }

        public void OnUserUpdated(long userId)
        { 
            if (m_userlist_state == 0)
            {
                MUser mUser = m_Users.Find(x => x.id == userId);
                if (mUser == null)
                {
                    mUser = new MUser(userId);
                    m_Users.Add(mUser);
                    mUser.state = 1;
                    ThreadPool.QueueUserWorkItem(delegate { ResetUser(mUser); });
                }
                else if (mUser.state == 0)
                {
                    mUser.state = 1;
                    ThreadPool.QueueUserWorkItem(delegate { ResetUser(mUser); });
                }
                else // mUser.state >= 1
                {
                    mUser.state = 2;
                }
            }
            else // m_userlist_state >= 1
            {
                m_userlist_state = 2;
            }
        }

        public void OnUserDeleted(long userId)
        {
            if (m_userlist_state == 0)
            {
                MUser mUser = m_Users.Find(x => x.id == userId);
                if (mUser != null)
                {
                    m_Users.Remove(mUser);
                    mUser.state = 0;

                    if (mUser.confirmed)
                    {
                        ComputeHash();
                        BroadcastMessage(EncodeMessage_UserRemoved(mUser));
                        UserDeletedCallback(mUser.id);
                    }
                }
            }
            else // m_userlist_state >= 1
            {
                m_userlist_state = 2;
            }
        }

        public void OnUsersUpdated()
        {
            if (m_userlist_state == 0)
            {
                m_userlist_state = 1;
                ThreadPool.QueueUserWorkItem(delegate { ResetUserlist(); });
            }
            else
            {
                m_userlist_state = 2;
            }
        }

        public void OnGuestStatusChanged()
        {
            if (c_GuestSupported == false)
                return;
            
            if (m_guest_state == 0)
            {
                m_guest_state = 1;
                ThreadPool.QueueUserWorkItem(delegate { ResetGuestStatus(); });
            }
            else
            {
                m_guest_state = 2;
            }            
        }

        public void OnContactDisabled(long contactId)
        {
            foreach (MUser mUser in m_Users)
            {
                if (mUser.userKind == 3 && mUser.contactId == contactId)
                {
                    if (m_userlist_state >= 1)
                    {
                        m_userlist_state = 2;
                        return;
                    }

                    if (mUser.state == 0)
                    {
                        mUser.state = 1;
                        ThreadPool.QueueUserWorkItem(delegate { ResetUser(mUser); });
                    }
                    else // mUser.state >= 1
                    {
                        mUser.state = 2;
                    }
                }
            }
        }

        public void OnContactDeleted(long contactId)
        {
            for (int i = m_Users.Count - 1; i >= 0; i--)
            {
                MUser mUser = m_Users[i];
                if (mUser.userKind == 3 && mUser.contactId == contactId)
                {
                    if (m_userlist_state >= 1)
                    {
                        m_userlist_state = 2;
                        return;
                    }

                    m_Users.RemoveAt(i);
                    mUser.state = 0;

                    if (mUser.confirmed)
                    {                        
                        BroadcastMessage(EncodeMessage_UserRemoved(mUser));
                        UserDeletedCallback(mUser.id);
                    }
                }
            }

            ComputeHash();
        }

        public void OnConsumerDisabled(long consumerId)
        {
            foreach (MUser mUser in m_Users)
            {
                if (mUser.userKind == 3 && mUser.consumerId == consumerId)
                {
                    if (m_userlist_state >= 1)
                    {
                        m_userlist_state = 2;
                        return;
                    }

                    if (mUser.state == 0)
                    {
                        mUser.state = 1;
                        ThreadPool.QueueUserWorkItem(delegate { ResetUser(mUser); });
                    }
                    else // mUser.state >= 1
                    {
                        mUser.state = 2;
                    }                    
                }
            }
        }

        public void OnConsumerDeleted(long consumerId)
        {
            for (int i = m_Users.Count - 1; i >= 0; i--)
            {
                MUser mUser = m_Users[i];
                if (mUser.userKind == 3 && mUser.consumerId == consumerId)
                {
                    if (m_userlist_state >= 1)
                    {
                        m_userlist_state = 2;
                        return;
                    }

                    m_Users.RemoveAt(i);
                    mUser.state = 0;

                    if (mUser.confirmed)
                    {
                        BroadcastMessage(EncodeMessage_UserRemoved(mUser));
                        UserDeletedCallback(mUser.id);
                    }
                }
            }

            ComputeHash();
        }

        void ResetUser(MUser mUser)
        {
            try
            {
                MUserData userData = new MUserData();
                int user_status_code = SoftnetRegistry.RBMemebership_GetUserData(mUser.id, m_Site.Id, userData);
                
                lock (site_mutex)
                {
                    if (m_userlist_state != 0)
                        return;

                    if (user_status_code == 0)
                    {
                        if (mUser.state == 1)
                        {
                            mUser.state = 0;
                            mUser.name = userData.name;
                            mUser.authority = UserAuthority.CreateInstance(userData.roles);
                            ComputeUserHash(mUser);

                            if (mUser.confirmed)
                            {
                                BroadcastMessage(EncodeMessage_UserUpdated(mUser));
                            }
                            else
                            {
                                mUser.userKind = userData.userKind;
                                mUser.contactId = userData.contactId;
                                mUser.consumerId = userData.consumerId;
                                mUser.confirmed = true;
                                BroadcastMessage(EncodeMessage_UserIncluded(mUser));
                            }

                            ComputeHash();

                            for (int i = m_Clients.Count - 1; i >= 0; i--)
                            {
                                Client client = m_Clients[i];
                                if (client.UserId == mUser.id)
                                {
                                    if (client.Online)
                                        client.UpdateUser(mUser, m_Roles);
                                    else
                                    {
                                        m_Clients.RemoveAt(i);
                                        client.Shutdown(ErrorCodes.RESTART);
                                        ClientRestartedCallback();
                                    }
                                }
                            }
                        }
                        else if (mUser.state == 2)
                        {
                            mUser.state = 1;
                            ThreadPool.QueueUserWorkItem(delegate { ResetUser(mUser); });
                        }
                    }
                    else if (user_status_code == 1)
                    {
                        if (mUser.state == 1)
                        {
                            m_Users.Remove(mUser);
                            mUser.state = 0;

                            if (mUser.confirmed)
                            {
                                ComputeHash();
                                BroadcastMessage(EncodeMessage_UserRemoved(mUser));

                                if (m_GuestAllowed)
                                {
                                    foreach (Client client in m_Clients)
                                    {
                                        if (client.UserId == mUser.id)
                                            client.UpdateUser(m_Guest);
                                    }
                                }
                                else
                                {
                                    UserRemovedCallback(mUser.id);
                                }
                            }
                        }
                        else if (mUser.state == 2)
                        {
                            mUser.state = 1;
                            ThreadPool.QueueUserWorkItem(delegate { ResetUser(mUser); });
                        }
                    }
                    else if(user_status_code == -1)
                    {
                        if (mUser.state > 0)
                        {
                            m_Users.Remove(mUser);
                            mUser.state = 0;

                            if (mUser.confirmed)
                            {
                                ComputeHash();
                                BroadcastMessage(EncodeMessage_UserRemoved(mUser));
                                UserDeletedCallback(mUser.id);
                            }
                        }
                    }
                }
            }
            catch (SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }
        }

        void ResetUserlist()
        { 
            try
            {
                MUserList userList = new MUserList();
                SoftnetRegistry.RBMemebership_GetUserList(m_Site.Id, userList);

                lock (site_mutex)
                {
                    if (m_userlist_state == 1)
                    {
                        m_userlist_state = 0;
                        foreach (MUser mUser in m_Users)
                            mUser.state = 0;

                        m_Users = userList.users;
                        m_Roles = userList.roles;

                        foreach (MUser mUser in m_Users)
                        {
                            List<long> roles = new List<long>();
                            foreach (MUserRole mur in userList.userRoles)
                            {
                                if (mur.userId == mUser.id)
                                    roles.Add(mur.roleId);
                            }
                            mUser.authority = UserAuthority.CreateInstance(roles);
                            ComputeUserHash(mUser);
                        }

                        m_Roles.Sort();
                        ComputeHash();

                        BroadcastMessage(EncodeMessage_UserList());  

                        for(int i = m_Clients.Count - 1; i >= 0; i--)
                        {
                            Client client = m_Clients[i];
                            if (client.UserKind == Constants.UserKind.Guest)
                                continue;

                            MUser mUser = m_Users.Find(x => x.id == client.UserId);
                            if (mUser != null)
                            {
                                if (client.Online)
                                    client.UpdateUser(mUser, m_Roles);
                                else 
                                {
                                    m_Clients.RemoveAt(i);
                                    client.Shutdown(ErrorCodes.RESTART);
                                    ClientRestartedCallback();
                                }
                            }
                            else if (m_GuestAllowed)
                            {
                                if (client.Online)
                                    client.UpdateUser(m_Guest);
                                else
                                {
                                    m_Clients.RemoveAt(i);
                                    client.Shutdown(ErrorCodes.RESTART);
                                    ClientRestartedCallback();
                                }
                            }
                            else
                            {
                                m_Clients.RemoveAt(i);
                                client.Shutdown(ErrorCodes.RESTART);
                                ClientRestartedCallback();
                            }
                        }
                    }
                    else
                    {
                        m_userlist_state = 1;
                        ThreadPool.QueueUserWorkItem(delegate { ResetUserlist(); });
                    }
                }
            }
            catch (SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }
        }

        void ResetGuestStatus()
        { 
            try
            {                
                bool guestAllowed = SoftnetRegistry.RBMemebership_IsGuestAllowed(m_Site.Id);
                lock (site_mutex)
                {
                    if (m_guest_state == 1)
                    {
                        m_guest_state = 0;

                        if (guestAllowed)
                        {
                            if (m_GuestAllowed != guestAllowed)
                            {
                                GuestAllowedCallback();
                                BroadcastMessage(MsgBuilder.Create(Constants.Service.RBMembership.ModuleId, Constants.Service.RBMembership.GUEST_ALLOWED));
                            }
                            m_GuestAllowed = guestAllowed;
                        }
                        else
                        {
                            if (m_GuestAllowed != guestAllowed)
                            {
                                GuestDeniedCallback();
                                BroadcastMessage(MsgBuilder.Create(Constants.Service.RBMembership.ModuleId, Constants.Service.RBMembership.GUEST_DENIED));
                            }
                            m_GuestAllowed = guestAllowed;
                        }
                    }
                    else
                    {
                        m_guest_state = 1;
                        ThreadPool.QueueUserWorkItem(delegate { ResetGuestStatus(); });
                    }                    
                }                
            }
            catch (SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }        
        }

        void ComputeHash()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;

            SequenceEncoder asnRoles = asnRootSequence.Sequence();
            foreach (MRole mRole in m_Roles)
            {
                SequenceEncoder asnRole = asnRoles.Sequence();
                asnRole.Int64(mRole.id);
                asnRole.IA5String(mRole.name);
            }

            SequenceEncoder asnUsers = asnRootSequence.Sequence();
            if (m_Users.Count > 0)
            {
                m_Users.Sort();
                foreach (MUser mUser in m_Users)
                {
                    if (mUser.confirmed)
                    {
                        var asnUser = asnUsers.Sequence();
                        asnUser.Int64(mUser.id);
                        asnUser.IA5String(mUser.name);
                        SequenceEncoder asnUserRoles = asnUser.Sequence();
                        mUser.authority.roles.Sort();
                        foreach (long roleId in mUser.authority.roles)
                        {
                            asnUserRoles.Int64(roleId);
                        }
                    }
                }
            }

            m_Hash = SHA1Hash.Compute(asnEncoder.GetEncoding());
        }

        void ComputeUserHash(MUser mUser)
        {
            int i = 0; string[] roles = new string[mUser.authority.roles.Count];
            foreach (long roleId in mUser.authority.roles)
            {
                MRole mRole = m_Roles.Find(x => x.id == roleId);
                if (mRole == null)
                    throw new SoftnetException(ErrorCodes.RESTART);
                roles[i] = mRole.name;
                i++;
            }

            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnSequence = asnEncoder.Sequence;            
            Array.Sort(roles);
            foreach (string role in roles)
                asnSequence.IA5String(role);
            byte[] rolesEncoding = asnEncoder.GetEncoding();
            mUser.rolesHash = SHA1Hash.Compute(rolesEncoding);

            asnEncoder = new ASNEncoder();
            asnSequence = asnEncoder.Sequence;
            asnSequence.Int64(mUser.id);
            asnSequence.IA5String(mUser.name);
            asnSequence.OctetString(1, rolesEncoding);
            mUser.hash = SHA1Hash.Compute(asnEncoder.GetEncoding());
        }

        void BroadcastMessage(SoftnetMessage message)
        {
            foreach (Service service in m_Services)
                if (service.Online)
                    service.Send(message);
        }

        SoftnetMessage EncodeMessage_UserList()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;

            SequenceEncoder asnRoles = asnRootSequence.Sequence();
            foreach (MRole mRole in m_Roles)
            {
                SequenceEncoder asnRole = asnRoles.Sequence();
                asnRole.Int64(mRole.id);
                asnRole.IA5String(mRole.name);
            }

            SequenceEncoder asnUsers = asnRootSequence.Sequence();
            if (m_Users.Count > 0)
            {
                foreach (MUser mUser in m_Users)
                {
                    if (mUser.confirmed)
                    {
                        var asnUser = asnUsers.Sequence();
                        asnUser.Int64(mUser.id);
                        asnUser.IA5String(mUser.name);
                        SequenceEncoder asnUserRoles = asnUser.Sequence();
                        foreach (long roleId in mUser.authority.roles)
                            asnUserRoles.Int64(roleId);
                    }
                }
            }

            return MsgBuilder.Create(Constants.Service.RBMembership.ModuleId, Constants.Service.RBMembership.USER_LIST, asnEncoder);
        }

        SoftnetMessage EncodeMessage_UserIncluded(MUser user)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.Int64(user.id);
            asnRootSequence.IA5String(user.name);
            SequenceEncoder asnUserRoles = asnRootSequence.Sequence();
            foreach (long roleId in user.authority.roles)
                asnUserRoles.Int64(roleId);
            return MsgBuilder.Create(Constants.Service.RBMembership.ModuleId, Constants.Service.RBMembership.USER_INCLUDED, asnEncoder);
        }

        SoftnetMessage EncodeMessage_UserRemoved(MUser user)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.Int64(user.id);
            return MsgBuilder.Create(Constants.Service.RBMembership.ModuleId, Constants.Service.RBMembership.USER_REMOVED, asnEncoder);
        }

        SoftnetMessage EncodeMessage_UserUpdated(MUser user)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder asnRootSequence = asnEncoder.Sequence;
            asnRootSequence.Int64(user.id);
            asnRootSequence.IA5String(user.name);
            SequenceEncoder asnUserRoles = asnRootSequence.Sequence();
            foreach (long roleId in user.authority.roles)
                asnUserRoles.Int64(roleId);
            return MsgBuilder.Create(Constants.Service.RBMembership.ModuleId, Constants.Service.RBMembership.USER_UPDATED, asnEncoder);
        }
    }
}
