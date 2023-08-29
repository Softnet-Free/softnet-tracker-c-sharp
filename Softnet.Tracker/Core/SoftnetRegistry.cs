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
using System.Text.RegularExpressions;
using System.Configuration;
using System.Threading;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.IO;

using Softnet.ServerKit;
using Softnet.Tracker.SiteModel;
using Softnet.Tracker.ClientModel;
using Softnet.Tracker.ServiceModel;

namespace Softnet.Tracker.Core
{
    class SoftnetRegistry
    {
        public static void Mgt_ValidateConnection()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("select 1", connection);
                    command.ExecuteScalar();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void MgtUsers_EnableOwner(long ownerId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_MgtUsers_EnableOwner";

                    command.Parameters.Add("@OwnerId", SqlDbType.BigInt);
                    command.Parameters["@OwnerId"].Direction = ParameterDirection.Input;
                    command.Parameters["@OwnerId"].Value = ownerId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void MgtUsers_DisableOwner(long ownerId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_MgtUsers_DisableOwner";

                    command.Parameters.Add("@OwnerId", SqlDbType.BigInt);
                    command.Parameters["@OwnerId"].Direction = ParameterDirection.Input;
                    command.Parameters["@OwnerId"].Value = ownerId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void MgtUsers_DeleteOwner(long ownerId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_MgtUsers_DeleteOwner";

                    command.Parameters.Add("@OwnerId", SqlDbType.BigInt);
                    command.Parameters["@OwnerId"].Direction = ParameterDirection.Input;
                    command.Parameters["@OwnerId"].Value = ownerId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void MgtUsers_GetSiteIdentitiesForOwner(long ownerId, List<long> ownedSiteIdentities, List<long> consumedSiteIdentities)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_MgtUsers_GetSiteIdentitiesForOwner";

                    command.Parameters.Add("@OwnerId", SqlDbType.BigInt);
                    command.Parameters["@OwnerId"].Direction = ParameterDirection.Input;
                    command.Parameters["@OwnerId"].Value = ownerId;

                    SqlDataReader dataReader = command.ExecuteReader();
                    try
                    {
                        while (dataReader.Read())
                        {
                            long siteId = (long)dataReader[0];
                            ownedSiteIdentities.Add(siteId);
                        }

                        if (dataReader.NextResult() == false)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                        while (dataReader.Read())
                        {
                            long siteId = (long)dataReader[0];
                            consumedSiteIdentities.Add(siteId);
                        }
                    }
                    finally
                    {
                        dataReader.Close();
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void MgtUsers_GetIdentitiesForEnabledOwner(long ownerId, List<long> ownedSiteIdentities, List<Pair<long, long>> userIdentities)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_MgtUsers_GetIdentitiesForEnabledOwner";

                    command.Parameters.Add("@OwnerId", SqlDbType.BigInt);
                    command.Parameters["@OwnerId"].Direction = ParameterDirection.Input;
                    command.Parameters["@OwnerId"].Value = ownerId;

                    SqlDataReader dataReader = command.ExecuteReader();
                    try
                    {
                        while (dataReader.Read())
                        {
                            long siteId = (long)dataReader[0];
                            ownedSiteIdentities.Add(siteId);
                        }

                        if (dataReader.NextResult() == false)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                        while (dataReader.Read())
                        {
                            long siteId = (long)dataReader[0];
                            long userId = (long)dataReader[1];
                            userIdentities.Add(new Pair<long, long>(siteId, userId));
                        }
                    }
                    finally
                    {
                        dataReader.Close();
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void MgtUsers_GetSiteIdentitiesForProvider(long ownerId, List<long> siteIdentities)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_MgtUsers_GetSiteIdentitiesForProvider";

                    command.Parameters.Add("@OwnerId", SqlDbType.BigInt);
                    command.Parameters["@OwnerId"].Direction = ParameterDirection.Input;
                    command.Parameters["@OwnerId"].Value = ownerId;

                    SqlDataReader dataReader = command.ExecuteReader();
                    while (dataReader.Read())
                    {
                        long siteId = (long)dataReader[0];
                        siteIdentities.Add(siteId);
                    }
                    dataReader.Close();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void MgtUsers_AssignRoleProvider(long ownerId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_MgtUsers_AssignRoleProvider";

                    command.Parameters.Add("@OwnerId", SqlDbType.BigInt);
                    command.Parameters["@OwnerId"].Direction = ParameterDirection.Input;
                    command.Parameters["@OwnerId"].Value = ownerId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void MgtUsers_RemoveRoleProvider(long ownerId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_MgtUsers_RemoveRoleProvider";

                    command.Parameters.Add("@OwnerId", SqlDbType.BigInt);
                    command.Parameters["@OwnerId"].Direction = ParameterDirection.Input;
                    command.Parameters["@OwnerId"].Value = ownerId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_GetSiteIdentitiesForContact(long contactId, Pair<int,long> result, List<long> mySites, List<long> peerSites)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_GetSiteIdentitiesForContact";

                    command.Parameters.Add("@ContactId", SqlDbType.BigInt);
                    command.Parameters["@ContactId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ContactId"].Value = contactId;

                    command.Parameters.Add("@PeerContactId", SqlDbType.BigInt);
                    command.Parameters["@PeerContactId"].Direction = ParameterDirection.Output;

                    SqlDataReader dataReader = command.ExecuteReader();
                    try
                    {
                        while (dataReader.Read())
                            mySites.Add((long)dataReader[0]);

                        if (dataReader.NextResult())
                        {
                            while (dataReader.Read())
                                peerSites.Add((long)dataReader[0]);
                            result.First = 2;
                        }
                        else
                            result.First = 1;
                    }
                    finally
                    {
                        dataReader.Close();
                    }

                    if(result.First == 2)
                        result.Second = (long)command.Parameters["@PeerContactId"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_GetUserIdentitiesForPeerContact(long consumerId, long providerId, List<Pair<long,long>> userIdentities)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_GetUserIdentitiesForPeerContact";

                    command.Parameters.Add("@ConsumerId", SqlDbType.BigInt);
                    command.Parameters["@ConsumerId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ConsumerId"].Value = consumerId;

                    command.Parameters.Add("@ProviderId", SqlDbType.BigInt);
                    command.Parameters["@ProviderId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ProviderId"].Value = providerId;

                    SqlDataReader dataReader = command.ExecuteReader();
                    try
                    {
                        while (dataReader.Read())
                        {
                            long siteId = (long)dataReader[0];
                            long userId = (long)dataReader[1];
                            userIdentities.Add(new Pair<long, long>(siteId, userId));
                        }
                    }
                    finally
                    {
                        dataReader.Close();
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_DeleteContact(long contactId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_DeleteContact";

                    command.Parameters.Add("@ContactId", SqlDbType.BigInt);
                    command.Parameters["@ContactId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ContactId"].Value = contactId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_RestoreContact(long ownerId, long selectedUserId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_RestoreContact";

                    command.Parameters.Add("@OwnerId", SqlDbType.BigInt);
                    command.Parameters["@OwnerId"].Direction = ParameterDirection.Input;
                    command.Parameters["@OwnerId"].Value = ownerId;

                    command.Parameters.Add("@SelectedUserId", SqlDbType.BigInt);
                    command.Parameters["@SelectedUserId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SelectedUserId"].Value = selectedUserId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }     
        }

        public static void Mgt_DeleteDomain(long domainId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_DeleteDomain";

                    command.Parameters.Add("@DomainId", SqlDbType.BigInt);
                    command.Parameters["@DomainId"].Direction = ParameterDirection.Input;
                    command.Parameters["@DomainId"].Value = domainId;

                    command.ExecuteNonQuery();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_GetSiteIdentitiesForDomain(long domainId, List<long> siteIdentities)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_GetSiteIdentitiesForDomain";

                    command.Parameters.Add("@DomainId", SqlDbType.BigInt);
                    command.Parameters["@DomainId"].Direction = ParameterDirection.Input;
                    command.Parameters["@DomainId"].Value = domainId;

                    SqlDataReader dataReader = command.ExecuteReader();
                    while (dataReader.Read())
                    {
                        long siteId = (long)dataReader[0];
                        siteIdentities.Add(siteId);
                    }
                    dataReader.Close();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Mgt_CreatePrivateUser(long domainId, string userName, bool dedicatedStatus, Container<long> resultData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_CreatePrivateUser";

                    command.Parameters.Add("@DomainId", SqlDbType.BigInt);
                    command.Parameters["@DomainId"].Direction = ParameterDirection.Input;
                    command.Parameters["@DomainId"].Value = domainId;

                    command.Parameters.Add("@UserName", SqlDbType.NVarChar, 256);
                    command.Parameters["@UserName"].Direction = ParameterDirection.Input;
                    command.Parameters["@UserName"].Value = userName;

                    command.Parameters.Add("@DedicatedStatus", SqlDbType.Bit);
                    command.Parameters["@DedicatedStatus"].Direction = ParameterDirection.Input;
                    command.Parameters["@DedicatedStatus"].Value = dedicatedStatus;

                    command.Parameters.Add("@UserId", SqlDbType.BigInt);
                    command.Parameters["@UserId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode == 0)
                        resultData.Obj = (long)command.Parameters["@UserId"].Value;

                    return resultCode;   
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Mgt_CreateContactUser(long domainId, long contactId, string userName, bool dedicatedStatus, bool enabledStatus, Container<long> resultData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_CreateContactUser";

                    command.Parameters.Add("@DomainId", SqlDbType.BigInt);
                    command.Parameters["@DomainId"].Direction = ParameterDirection.Input;
                    command.Parameters["@DomainId"].Value = domainId;

                    command.Parameters.Add("@ContactId", SqlDbType.BigInt);
                    command.Parameters["@ContactId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ContactId"].Value = contactId;

                    command.Parameters.Add("@UserName", SqlDbType.NVarChar, 256);
                    command.Parameters["@UserName"].Direction = ParameterDirection.Input;
                    command.Parameters["@UserName"].Value = userName;

                    command.Parameters.Add("@DedicatedStatus", SqlDbType.Bit);
                    command.Parameters["@DedicatedStatus"].Direction = ParameterDirection.Input;
                    command.Parameters["@DedicatedStatus"].Value = dedicatedStatus;

                    command.Parameters.Add("@EnabledStatus", SqlDbType.Bit);
                    command.Parameters["@EnabledStatus"].Direction = ParameterDirection.Input;
                    command.Parameters["@EnabledStatus"].Value = enabledStatus;

                    command.Parameters.Add("@UserId", SqlDbType.BigInt);
                    command.Parameters["@UserId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();
                    
                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode == 0)                    
                        resultData.Obj = (long)command.Parameters["@UserId"].Value;
                    
                    return resultCode;
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_UpdateUser(long userId, bool enabledStatus)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_UpdateUser";

                    command.Parameters.Add("@UserId", SqlDbType.BigInt);
                    command.Parameters["@UserId"].Direction = ParameterDirection.Input;
                    command.Parameters["@UserId"].Value = userId;

                    command.Parameters.Add("@EnabledStatus", SqlDbType.Bit);
                    command.Parameters["@EnabledStatus"].Direction = ParameterDirection.Input;
                    command.Parameters["@EnabledStatus"].Value = enabledStatus;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_UpdateUser(long userId, string userName, bool enabledStatus, bool dedicatedStatus)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_UpdateUser2";

                    command.Parameters.Add("@UserId", SqlDbType.BigInt);
                    command.Parameters["@UserId"].Direction = ParameterDirection.Input;
                    command.Parameters["@UserId"].Value = userId;

                    command.Parameters.Add("@UserName", SqlDbType.NVarChar, 256);
                    command.Parameters["@UserName"].Direction = ParameterDirection.Input;
                    command.Parameters["@UserName"].Value = userName;

                    command.Parameters.Add("@EnabledStatus", SqlDbType.Bit);
                    command.Parameters["@EnabledStatus"].Direction = ParameterDirection.Input;
                    command.Parameters["@EnabledStatus"].Value = enabledStatus;

                    command.Parameters.Add("@DedicatedStatus", SqlDbType.Bit);
                    command.Parameters["@DedicatedStatus"].Direction = ParameterDirection.Input;
                    command.Parameters["@DedicatedStatus"].Value = dedicatedStatus;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();                        
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_DeleteUser(long userId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_DeleteUser";

                    command.Parameters.Add("@UserId", SqlDbType.BigInt);
                    command.Parameters["@UserId"].Direction = ParameterDirection.Input;
                    command.Parameters["@UserId"].Value = userId;

                    command.ExecuteNonQuery();                    
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_SetClientPassword(long clientId, string salt, string saltedPassword)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand Command = new SqlCommand();
                    Command.Connection = Connection;
                    Command.CommandType = CommandType.StoredProcedure;
                    Command.CommandText = "Softnet_Mgt_SetClientPassword";

                    Command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    Command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    Command.Parameters["@ClientId"].Value = clientId;

                    Command.Parameters.Add("@Salt", SqlDbType.VarChar, 64);
                    Command.Parameters["@Salt"].Direction = ParameterDirection.Input;
                    Command.Parameters["@Salt"].Value = salt;

                    Command.Parameters.Add("@SaltedPassword", SqlDbType.VarChar, 64);
                    Command.Parameters["@SaltedPassword"].Direction = ParameterDirection.Input;
                    Command.Parameters["@SaltedPassword"].Value = saltedPassword;

                    Command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_DeleteClient(long clientId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand Command = new SqlCommand();
                    Command.Connection = Connection;
                    Command.CommandType = CommandType.StoredProcedure;
                    Command.CommandText = "Softnet_Mgt_DeleteClient";

                    Command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    Command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    Command.Parameters["@ClientId"].Value = clientId;

                    Command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_DeleteSite(long siteId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_DeleteSite";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_EnableSite(long siteId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_EnableSite";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_DisableSite(long siteId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_DisableSite";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_SetSiteAsMultiService(long siteId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_SetSiteAsMultiService";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_AllowGuest(long siteId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_AllowGuest";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_DenyGuest(long siteId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_DenyGuest";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_AllowImplicitUsers(long siteId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_AllowImplicitUsers";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_DenyImplicitUsers(long siteId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_DenyImplicitUsers";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_AddSiteUser(long siteId, long userId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_AddSiteUser";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@UserId", SqlDbType.BigInt);
                    command.Parameters["@UserId"].Direction = ParameterDirection.Input;
                    command.Parameters["@UserId"].Value = userId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_RemoveSiteUser(long siteId, long userId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_RemoveSiteUser";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@UserId", SqlDbType.BigInt);
                    command.Parameters["@UserId"].Direction = ParameterDirection.Input;
                    command.Parameters["@UserId"].Value = userId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_SetDefaultRole(long siteId, long roleId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_SetDefaultRole";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@RoleId", SqlDbType.BigInt);
                    command.Parameters["@RoleId"].Direction = ParameterDirection.Input;
                    command.Parameters["@RoleId"].Value = roleId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_RemoveDefaultRole(long siteId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_RemoveDefaultRole";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_UpdateUserRoles(long siteId, long userId, List<long> roles)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_ClearUserRoles";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@UserId", SqlDbType.BigInt);
                    command.Parameters["@UserId"].Direction = ParameterDirection.Input;
                    command.Parameters["@UserId"].Value = userId;

                    command.ExecuteNonQuery();

                    if (roles.Count > 0)
                    {
                        command = new SqlCommand();
                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "Softnet_Mgt_AddUserRole";

                        command.Parameters.Add("@UserId", SqlDbType.BigInt);
                        command.Parameters["@UserId"].Direction = ParameterDirection.Input;
                        command.Parameters["@UserId"].Value = userId;

                        command.Parameters.Add("@RoleId", SqlDbType.BigInt);
                        command.Parameters["@RoleId"].Direction = ParameterDirection.Input;

                        foreach (long roleId in roles)
                        {
                            command.Parameters["@RoleId"].Value = roleId;
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Mgt_AddService(long siteId, Pair<long, string> dto)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_AddService";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@CreatedDate", SqlDbType.DateTime);
                    command.Parameters["@CreatedDate"].Direction = ParameterDirection.Input;
                    command.Parameters["@CreatedDate"].Value = DateTime.Now;

                    command.Parameters.Add("@Hostname", SqlDbType.NVarChar, 256);
                    command.Parameters["@Hostname"].Direction = ParameterDirection.InputOutput;
                    command.Parameters["@Hostname"].Value = dto.Second;

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int returnCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (returnCode != 0)
                        return returnCode;

                    dto.First = (long)command.Parameters["@ServiceId"].Value;
                    dto.Second = (string)command.Parameters["@Hostname"].Value;
                    return 0;
                }
            }            
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_SetServicePassword(long serviceId, string salt, string saltedPassword)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand Command = new SqlCommand();
                    Command.Connection = Connection;
                    Command.CommandType = CommandType.StoredProcedure;
                    Command.CommandText = "Softnet_Mgt_SetServicePassword";

                    Command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    Command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    Command.Parameters["@ServiceId"].Value = serviceId;

                    Command.Parameters.Add("@Salt", SqlDbType.VarChar, 64);
                    Command.Parameters["@Salt"].Direction = ParameterDirection.Input;
                    Command.Parameters["@Salt"].Value = salt;

                    Command.Parameters.Add("@SaltedPassword", SqlDbType.VarChar, 64);
                    Command.Parameters["@SaltedPassword"].Direction = ParameterDirection.Input;
                    Command.Parameters["@SaltedPassword"].Value = saltedPassword;

                    Command.ExecuteNonQuery();                            
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_ChangeHostname(long serviceId, string hostname)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_ChangeHostname";

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.Parameters.Add("@Hostname", SqlDbType.NVarChar, 256);
                    command.Parameters["@Hostname"].Direction = ParameterDirection.Input;
                    command.Parameters["@Hostname"].Value = hostname;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_EnableService(long serviceId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_EnableService";

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_DisableService(long serviceId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_DisableService";

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_DeleteService(long siteId, long serviceId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Mgt_DeleteService";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_GetSiteParams(long siteId, Softnet.Tracker.SiteModel.SiteParams siteParams)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_GetSiteParams";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@Structured", SqlDbType.Bit);
                    command.Parameters["@Structured"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ServiceType", SqlDbType.NVarChar, 256);
                    command.Parameters["@ServiceType"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ContractAuthor", SqlDbType.NVarChar, 256);
                    command.Parameters["@ContractAuthor"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@RolesSupported", SqlDbType.Bit);
                    command.Parameters["@RolesSupported"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                    command.Parameters["@EventsSupported"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();
                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode != 0)
                    {
                        if (resultCode == -1)
                            throw new SoftnetException(ErrorCodes.RESTART);
                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                    }

                    siteParams.structured = (bool)command.Parameters["@Structured"].Value;
                    if (command.Parameters["@ServiceType"].Value != DBNull.Value)
                        siteParams.serviceType = (string)command.Parameters["@ServiceType"].Value;
                    if (command.Parameters["@ContractAuthor"].Value != DBNull.Value)
                        siteParams.ContractAuthor = (string)command.Parameters["@ContractAuthor"].Value;
                    siteParams.rolesSupported = (bool)command.Parameters["@RolesSupported"].Value;
                    siteParams.eventsSupported = (bool)command.Parameters["@EventsSupported"].Value;                    
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_ConstructRBSite(long siteId, SiteStructure siteStructure, string ssHash)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_RBSiteBegin";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_AddRole";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@RoleName", SqlDbType.NVarChar, 256);
                    command.Parameters["@RoleName"].Direction = ParameterDirection.Input;

                    command.Parameters.Add("@Index", SqlDbType.Int);
                    command.Parameters["@Index"].Direction = ParameterDirection.Input;

                    int index = 0;
                    foreach (string roleName in siteStructure.getRoles())
                    {
                        index++;
                        command.Parameters["@RoleName"].Value = roleName;
                        command.Parameters["@Index"].Value = index;
                        command.ExecuteNonQuery();
                    }

                    if (siteStructure.eventsSupported())
                    {
                        SqlCommand command2 = new SqlCommand();
                        command2.Connection = connection;
                        command2.CommandType = CommandType.StoredProcedure;
                        command2.CommandText = "Softnet_SiteConstruction_AddEventRole";

                        command2.Parameters.Add("@EventId", SqlDbType.BigInt);
                        command2.Parameters["@EventId"].Direction = ParameterDirection.Input;

                        command2.Parameters.Add("@SiteId", SqlDbType.BigInt);
                        command2.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                        command2.Parameters["@SiteId"].Value = siteId;

                        command2.Parameters.Add("@RoleName", SqlDbType.NVarChar, 256);
                        command2.Parameters["@RoleName"].Direction = ParameterDirection.Input;

                        if (siteStructure.containsREvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddREvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.REvent evt in siteStructure.getREvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                command.ExecuteNonQuery();

                                if (evt.roles != null)
                                {
                                    long eventId = (long)command.Parameters["@EventId"].Value;
                                    foreach (string roleName in evt.roles)
                                    {
                                        command2.Parameters["@EventId"].Value = eventId;
                                        command2.Parameters["@RoleName"].Value = roleName;
                                        command2.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        if (siteStructure.containsQEvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddQEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@QueueSize", SqlDbType.Int);
                            command.Parameters["@QueueSize"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.QEvent evt in siteStructure.getQEvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                command.Parameters["@QueueSize"].Value = evt.queueSize;
                                command.ExecuteNonQuery();

                                if (evt.roles != null)
                                {
                                    long eventId = (long)command.Parameters["@EventId"].Value;
                                    foreach (string roleName in evt.roles)
                                    {
                                        command2.Parameters["@EventId"].Value = eventId;
                                        command2.Parameters["@RoleName"].Value = roleName;
                                        command2.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        if (siteStructure.containsPEvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddPEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            foreach (Softnet.Tracker.SiteModel.PEvent evt in siteStructure.getPEvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_RBSiteEnd";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@ServiceType", SqlDbType.NVarChar, 256);
                    command.Parameters["@ServiceType"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceType"].Value = siteStructure.getServiceType();

                    command.Parameters.Add("@ContractAuthor", SqlDbType.NVarChar, 256);
                    command.Parameters["@ContractAuthor"].Direction = ParameterDirection.Input;
                    command.Parameters["@ContractAuthor"].Value = siteStructure.getContractAuthor();

                    command.Parameters.Add("@SSHash", SqlDbType.VarChar, 64);
                    command.Parameters["@SSHash"].Direction = ParameterDirection.Input;
                    command.Parameters["@SSHash"].Value = ssHash;

                    if (siteStructure.getGuestSupport() > 0)
                    {
                        command.Parameters.Add("@GuestSupported", SqlDbType.Bit);
                        command.Parameters["@GuestSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@GuestSupported"].Value = true;

                        if (siteStructure.getGuestSupport() == 2)
                        {
                            command.Parameters.Add("@StatelessGuestSupported", SqlDbType.Bit);
                            command.Parameters["@StatelessGuestSupported"].Direction = ParameterDirection.Input;
                            command.Parameters["@StatelessGuestSupported"].Value = true;
                        }
                    }

                    if (siteStructure.eventsSupported())
                    {
                        command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                        command.Parameters["@EventsSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@EventsSupported"].Value = true;
                    }

                    if (siteStructure.containsOwnerRole())
                    {
                        command.Parameters.Add("@OwnerDefaultRole", SqlDbType.NVarChar, 256);
                        command.Parameters["@OwnerDefaultRole"].Direction = ParameterDirection.Input;
                        command.Parameters["@OwnerDefaultRole"].Value = siteStructure.getOwnerRole();
                    }

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_ConstructUBSite(long siteId, SiteStructure siteStructure, string ssHash)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_UBSiteBegin";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();

                    if (siteStructure.eventsSupported())
                    {
                        if (siteStructure.containsREvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddREvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.REvent evt in siteStructure.getREvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                command.ExecuteNonQuery();
                            }
                        }

                        if (siteStructure.containsQEvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddQEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@QueueSize", SqlDbType.Int);
                            command.Parameters["@QueueSize"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.QEvent evt in siteStructure.getQEvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                command.Parameters["@QueueSize"].Value = evt.queueSize;
                                command.ExecuteNonQuery();
                            }
                        }

                        if (siteStructure.containsPEvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddPEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            foreach (Softnet.Tracker.SiteModel.PEvent evt in siteStructure.getPEvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_UBSiteEnd";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@ServiceType", SqlDbType.NVarChar, 256);
                    command.Parameters["@ServiceType"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceType"].Value = siteStructure.getServiceType();

                    command.Parameters.Add("@ContractAuthor", SqlDbType.NVarChar, 256);
                    command.Parameters["@ContractAuthor"].Direction = ParameterDirection.Input;
                    command.Parameters["@ContractAuthor"].Value = siteStructure.getContractAuthor();

                    command.Parameters.Add("@SSHash", SqlDbType.VarChar, 64);
                    command.Parameters["@SSHash"].Direction = ParameterDirection.Input;
                    command.Parameters["@SSHash"].Value = ssHash;

                    if (siteStructure.getGuestSupport() > 0)
                    {
                        command.Parameters.Add("@GuestSupported", SqlDbType.Bit);
                        command.Parameters["@GuestSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@GuestSupported"].Value = true;

                        if (siteStructure.getGuestSupport() == 2)
                        {
                            command.Parameters.Add("@StatelessGuestSupported", SqlDbType.Bit);
                            command.Parameters["@StatelessGuestSupported"].Direction = ParameterDirection.Input;
                            command.Parameters["@StatelessGuestSupported"].Value = true;
                        }
                    }

                    if (siteStructure.eventsSupported())
                    {
                        command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                        command.Parameters["@EventsSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@EventsSupported"].Value = true;
                    }

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_ReconstructRBSite(long siteId, SiteStructure siteStructure, string ssHash)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteReconstruction_RBSiteBegin";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                    command.Parameters["@EventsSupported"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventsSupported"].Value = siteStructure.eventsSupported();

                    command.ExecuteNonQuery();

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteReconstruction_SelectRoles";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    SqlDataReader dataReader = command.ExecuteReader();
                    List<SiteRole> existingRoles = new List<SiteRole>();
                    while (dataReader.Read())
                    {
                        long roleId = (long)dataReader[0];
                        string roleName = (string)dataReader[1];
                        existingRoles.Add(new SiteRole(roleId, roleName));
                    }
                    dataReader.Close();

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteReconstruction_DeleteRole";

                    command.Parameters.Add("@RoleId", SqlDbType.BigInt);
                    command.Parameters["@RoleId"].Direction = ParameterDirection.Input;

                    List<string> newRoles = siteStructure.getRoles();
                    foreach (SiteRole sr in existingRoles)
                    {
                        if (newRoles.Exists(x => x.Equals(sr.name)) == false)
                        {
                            command.Parameters["@RoleId"].Value = sr.id;
                            command.ExecuteNonQuery();
                        }
                    }

                    SqlCommand addRoleCommand = new SqlCommand();
                    addRoleCommand.Connection = connection;
                    addRoleCommand.CommandType = CommandType.StoredProcedure;
                    addRoleCommand.CommandText = "Softnet_SiteConstruction_AddRole";

                    addRoleCommand.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    addRoleCommand.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    addRoleCommand.Parameters["@SiteId"].Value = siteId;

                    addRoleCommand.Parameters.Add("@RoleName", SqlDbType.NVarChar, 256);
                    addRoleCommand.Parameters["@RoleName"].Direction = ParameterDirection.Input;

                    addRoleCommand.Parameters.Add("@Index", SqlDbType.Int);
                    addRoleCommand.Parameters["@Index"].Direction = ParameterDirection.Input;

                    SqlCommand updateRoleCommand = new SqlCommand();
                    updateRoleCommand.Connection = connection;
                    updateRoleCommand.CommandType = CommandType.StoredProcedure;
                    updateRoleCommand.CommandText = "Softnet_SiteReconstruction_UpdateRoleIndex";

                    updateRoleCommand.Parameters.Add("@RoleId", SqlDbType.BigInt);
                    updateRoleCommand.Parameters["@RoleId"].Direction = ParameterDirection.Input;

                    updateRoleCommand.Parameters.Add("@Index", SqlDbType.Int);
                    updateRoleCommand.Parameters["@Index"].Direction = ParameterDirection.Input;

                    int index = 0;
                    foreach (string roleName in newRoles)
                    {
                        index++;
                        SiteRole siteRole = existingRoles.Find(x => x.name.Equals(roleName));
                        if (siteRole != null)
                        {
                            updateRoleCommand.Parameters["@RoleId"].Value = siteRole.id;
                            updateRoleCommand.Parameters["@Index"].Value = index;
                            updateRoleCommand.ExecuteNonQuery();
                        }
                        else
                        {
                            addRoleCommand.Parameters["@RoleName"].Value = roleName;
                            addRoleCommand.Parameters["@Index"].Value = index;
                            addRoleCommand.ExecuteNonQuery();
                        }
                    }

                    if (siteStructure.eventsSupported())
                    {
                        command = new SqlCommand();
                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "Softnet_SiteReconstruction_SelectEvents";

                        command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                        command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                        command.Parameters["@SiteId"].Value = siteId;

                        dataReader = command.ExecuteReader();
                        List<Event> existingEvents = new List<Event>();
                        while (dataReader.Read())
                        {
                            long eventId = (long)dataReader[0];
                            int eventKind = (int)dataReader[1];
                            string eventName = (string)dataReader[2];
                            existingEvents.Add(new Event(eventId, eventKind, eventName));
                        }
                        dataReader.Close();

                        command = new SqlCommand();
                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "Softnet_SiteReconstruction_DeleteEvent";

                        command.Parameters.Add("@EventId", SqlDbType.BigInt);
                        command.Parameters["@EventId"].Direction = ParameterDirection.Input;

                        foreach (Event evt in existingEvents)
                        {
                            if (evt.eventKind == 1 && siteStructure.containsREvent(evt.name))
                                continue;
                            if (evt.eventKind == 2 && siteStructure.containsQEvent(evt.name))
                                continue;
                            if (evt.eventKind == 4 && siteStructure.containsPEvent(evt.name))
                                continue;

                            command.Parameters["@EventId"].Value = evt.eventId;
                            command.ExecuteNonQuery();
                        }

                        SqlCommand addEventRoleCommand = new SqlCommand();
                        addEventRoleCommand.Connection = connection;
                        addEventRoleCommand.CommandType = CommandType.StoredProcedure;
                        addEventRoleCommand.CommandText = "Softnet_SiteConstruction_AddEventRole";

                        addEventRoleCommand.Parameters.Add("@EventId", SqlDbType.BigInt);
                        addEventRoleCommand.Parameters["@EventId"].Direction = ParameterDirection.Input;

                        addEventRoleCommand.Parameters.Add("@SiteId", SqlDbType.BigInt);
                        addEventRoleCommand.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                        addEventRoleCommand.Parameters["@SiteId"].Value = siteId;

                        addEventRoleCommand.Parameters.Add("@RoleName", SqlDbType.NVarChar, 256);
                        addEventRoleCommand.Parameters["@RoleName"].Direction = ParameterDirection.Input;

                        if (siteStructure.containsREvents())
                        {
                            SqlCommand updateEventCommand = new SqlCommand();
                            updateEventCommand.Connection = connection;
                            updateEventCommand.CommandType = CommandType.StoredProcedure;
                            updateEventCommand.CommandText = "Softnet_SiteReconstruction_UpdateREvent";

                            updateEventCommand.Parameters.Add("@EventId", SqlDbType.BigInt);
                            updateEventCommand.Parameters["@EventId"].Direction = ParameterDirection.Input;

                            updateEventCommand.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            updateEventCommand.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddREvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.REvent evt in siteStructure.getREvents())
                            {
                                long eventId;
                                Event existingEvent = existingEvents.Find(x => x.eventKind == 1 && x.name.Equals(evt.name));
                                if (existingEvent != null)
                                {
                                    updateEventCommand.Parameters["@EventId"].Value = existingEvent.eventId;
                                    updateEventCommand.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    updateEventCommand.ExecuteNonQuery();
                                    eventId = existingEvent.eventId;
                                }
                                else
                                {
                                    command.Parameters["@EventName"].Value = evt.name;
                                    command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    command.ExecuteNonQuery();
                                    eventId = (long)command.Parameters["@EventId"].Value;
                                }

                                if (evt.roles != null)
                                {
                                    foreach (string roleName in evt.roles)
                                    {
                                        addEventRoleCommand.Parameters["@EventId"].Value = eventId;
                                        addEventRoleCommand.Parameters["@RoleName"].Value = roleName;
                                        addEventRoleCommand.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        if (siteStructure.containsQEvents())
                        {
                            SqlCommand updateEventCommand = new SqlCommand();
                            updateEventCommand.Connection = connection;
                            updateEventCommand.CommandType = CommandType.StoredProcedure;
                            updateEventCommand.CommandText = "Softnet_SiteReconstruction_UpdateQEvent";

                            updateEventCommand.Parameters.Add("@EventId", SqlDbType.BigInt);
                            updateEventCommand.Parameters["@EventId"].Direction = ParameterDirection.Input;

                            updateEventCommand.Parameters.Add("@LifeTime", SqlDbType.Int);
                            updateEventCommand.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            updateEventCommand.Parameters.Add("@QueueSize", SqlDbType.Int);
                            updateEventCommand.Parameters["@QueueSize"].Direction = ParameterDirection.Input;

                            updateEventCommand.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            updateEventCommand.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddQEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@QueueSize", SqlDbType.Int);
                            command.Parameters["@QueueSize"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.QEvent evt in siteStructure.getQEvents())
                            {
                                long eventId;
                                Event existingEvent = existingEvents.Find(x => x.eventKind == 2 && x.name.Equals(evt.name));
                                if (existingEvent != null)
                                {
                                    updateEventCommand.Parameters["@EventId"].Value = existingEvent.eventId;
                                    updateEventCommand.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    updateEventCommand.Parameters["@QueueSize"].Value = evt.queueSize;
                                    updateEventCommand.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    updateEventCommand.ExecuteNonQuery();
                                    eventId = existingEvent.eventId;
                                }
                                else
                                {
                                    command.Parameters["@EventName"].Value = evt.name;
                                    command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    command.Parameters["@QueueSize"].Value = evt.queueSize;
                                    command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    command.ExecuteNonQuery();
                                    eventId = (long)command.Parameters["@EventId"].Value;
                                }

                                if (evt.roles != null)
                                {
                                    foreach (string roleName in evt.roles)
                                    {
                                        addEventRoleCommand.Parameters["@EventId"].Value = eventId;
                                        addEventRoleCommand.Parameters["@RoleName"].Value = roleName;
                                        addEventRoleCommand.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        if (siteStructure.containsPEvents())
                        {
                            SqlCommand updateEventCommand = new SqlCommand();
                            updateEventCommand.Connection = connection;
                            updateEventCommand.CommandType = CommandType.StoredProcedure;
                            updateEventCommand.CommandText = "Softnet_SiteReconstruction_UpdatePEvent";

                            updateEventCommand.Parameters.Add("@EventId", SqlDbType.BigInt);
                            updateEventCommand.Parameters["@EventId"].Direction = ParameterDirection.Input;

                            updateEventCommand.Parameters.Add("@LifeTime", SqlDbType.Int);
                            updateEventCommand.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddPEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            foreach (Softnet.Tracker.SiteModel.PEvent evt in siteStructure.getPEvents())
                            {
                                Event existingEvent = existingEvents.Find(x => x.eventKind == 4 && x.name.Equals(evt.name));
                                if (existingEvent != null)
                                {
                                    updateEventCommand.Parameters["@EventId"].Value = existingEvent.eventId;
                                    updateEventCommand.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    updateEventCommand.ExecuteNonQuery();
                                }
                                else
                                {
                                    command.Parameters["@EventName"].Value = evt.name;
                                    command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteReconstruction_RBSiteEnd";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@SSHash", SqlDbType.VarChar, 64);
                    command.Parameters["@SSHash"].Direction = ParameterDirection.Input;
                    command.Parameters["@SSHash"].Value = ssHash;

                    if (siteStructure.getGuestSupport() > 0)
                    {
                        command.Parameters.Add("@GuestSupported", SqlDbType.Bit);
                        command.Parameters["@GuestSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@GuestSupported"].Value = true;

                        if (siteStructure.getGuestSupport() == 2)
                        {
                            command.Parameters.Add("@StatelessGuestSupported", SqlDbType.Bit);
                            command.Parameters["@StatelessGuestSupported"].Direction = ParameterDirection.Input;
                            command.Parameters["@StatelessGuestSupported"].Value = true;
                        }
                    }

                    if (siteStructure.eventsSupported())
                    {
                        command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                        command.Parameters["@EventsSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@EventsSupported"].Value = true;
                    }                   

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_ReconstructRBSite2(long siteId, SiteStructure siteStructure, string ssHash)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteReconstruction_RBSiteBegin2";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                    command.Parameters["@EventsSupported"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventsSupported"].Value = siteStructure.eventsSupported();

                    command.ExecuteNonQuery();

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_AddRole";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@RoleName", SqlDbType.NVarChar, 256);
                    command.Parameters["@RoleName"].Direction = ParameterDirection.Input;

                    command.Parameters.Add("@Index", SqlDbType.Int);
                    command.Parameters["@Index"].Direction = ParameterDirection.Input;
            
                    int index = 0;
                    foreach (string roleName in siteStructure.getRoles())
                    {
                        index++;
                        command.Parameters["@RoleName"].Value = roleName;
                        command.Parameters["@Index"].Value = index;
                        command.ExecuteNonQuery();                
                    }

                    if (siteStructure.eventsSupported())
                    {
                        command = new SqlCommand();
                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "Softnet_SiteReconstruction_SelectEvents";

                        command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                        command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                        command.Parameters["@SiteId"].Value = siteId;

                        SqlDataReader dataReader = command.ExecuteReader();
                        List<Event> existingEvents = new List<Event>();
                        while (dataReader.Read())
                        {
                            long eventId = (long)dataReader[0];
                            int eventKind = (int)dataReader[1];
                            string eventName = (string)dataReader[2];
                            existingEvents.Add(new Event(eventId, eventKind, eventName));
                        }
                        dataReader.Close();

                        command = new SqlCommand();
                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "Softnet_SiteReconstruction_DeleteEvent";

                        command.Parameters.Add("@EventId", SqlDbType.BigInt);
                        command.Parameters["@EventId"].Direction = ParameterDirection.Input;

                        foreach (Event evt in existingEvents)
                        {
                            if (evt.eventKind == 1 && siteStructure.containsREvent(evt.name))
                                continue;
                            if (evt.eventKind == 2 && siteStructure.containsQEvent(evt.name))
                                continue;
                            if (evt.eventKind == 4 && siteStructure.containsPEvent(evt.name))
                                continue;

                            command.Parameters["@EventId"].Value = evt.eventId;
                            command.ExecuteNonQuery();
                        }

                        SqlCommand command2 = new SqlCommand();
                        command2.Connection = connection;
                        command2.CommandType = CommandType.StoredProcedure;
                        command2.CommandText = "Softnet_SiteConstruction_AddEventRole";

                        command2.Parameters.Add("@EventId", SqlDbType.BigInt);
                        command2.Parameters["@EventId"].Direction = ParameterDirection.Input;

                        command2.Parameters.Add("@SiteId", SqlDbType.BigInt);
                        command2.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                        command2.Parameters["@SiteId"].Value = siteId;

                        command2.Parameters.Add("@RoleName", SqlDbType.NVarChar, 256);
                        command2.Parameters["@RoleName"].Direction = ParameterDirection.Input;

                        if (siteStructure.containsREvents())
                        {
                            SqlCommand command3 = new SqlCommand();
                            command3.Connection = connection;
                            command3.CommandType = CommandType.StoredProcedure;
                            command3.CommandText = "Softnet_SiteReconstruction_UpdateREvent";

                            command3.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command3.Parameters["@EventId"].Direction = ParameterDirection.Input;

                            command3.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command3.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddREvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.REvent evt in siteStructure.getREvents())
                            {
                                long eventId;
                                Event existingEvent = existingEvents.Find(x => x.eventKind == 1 && x.name.Equals(evt.name));
                                if (existingEvent != null)
                                {
                                    command3.Parameters["@EventId"].Value = existingEvent.eventId;
                                    command3.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    command3.ExecuteNonQuery();
                                    eventId = existingEvent.eventId;
                                }
                                else
                                {
                                    command.Parameters["@EventName"].Value = evt.name;
                                    command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    command.ExecuteNonQuery();
                                    eventId = (long)command.Parameters["@EventId"].Value;
                                }

                                if (evt.roles != null)
                                {
                                    foreach (string roleName in evt.roles)
                                    {
                                        command2.Parameters["@EventId"].Value = eventId;
                                        command2.Parameters["@RoleName"].Value = roleName;
                                        command2.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        if (siteStructure.containsQEvents())
                        {
                            SqlCommand command3 = new SqlCommand();
                            command3.Connection = connection;
                            command3.CommandType = CommandType.StoredProcedure;
                            command3.CommandText = "Softnet_SiteReconstruction_UpdateQEvent";

                            command3.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command3.Parameters["@EventId"].Direction = ParameterDirection.Input;

                            command3.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command3.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command3.Parameters.Add("@QueueSize", SqlDbType.Int);
                            command3.Parameters["@QueueSize"].Direction = ParameterDirection.Input;

                            command3.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command3.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddQEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@QueueSize", SqlDbType.Int);
                            command.Parameters["@QueueSize"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.QEvent evt in siteStructure.getQEvents())
                            {
                                long eventId;
                                Event existingEvent = existingEvents.Find(x => x.eventKind == 2 && x.name.Equals(evt.name));
                                if (existingEvent != null)
                                {
                                    command3.Parameters["@EventId"].Value = existingEvent.eventId;
                                    command3.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    command3.Parameters["@QueueSize"].Value = evt.queueSize;
                                    command3.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    command3.ExecuteNonQuery();
                                    eventId = existingEvent.eventId;
                                }
                                else
                                {
                                    command.Parameters["@EventName"].Value = evt.name;
                                    command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    command.Parameters["@QueueSize"].Value = evt.queueSize;
                                    command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    command.ExecuteNonQuery();
                                    eventId = (long)command.Parameters["@EventId"].Value;
                                }

                                if (evt.roles != null)
                                {
                                    foreach (string roleName in evt.roles)
                                    {
                                        command2.Parameters["@EventId"].Value = eventId;
                                        command2.Parameters["@RoleName"].Value = roleName;
                                        command2.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        if (siteStructure.containsPEvents())
                        {
                            SqlCommand command3 = new SqlCommand();
                            command3.Connection = connection;
                            command3.CommandType = CommandType.StoredProcedure;
                            command3.CommandText = "Softnet_SiteReconstruction_UpdatePEvent";

                            command3.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command3.Parameters["@EventId"].Direction = ParameterDirection.Input;

                            command3.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command3.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddPEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            foreach (Softnet.Tracker.SiteModel.PEvent evt in siteStructure.getPEvents())
                            {
                                Event existingEvent = existingEvents.Find(x => x.eventKind == 4 && x.name.Equals(evt.name));
                                if (existingEvent != null)
                                {
                                    command3.Parameters["@EventId"].Value = existingEvent.eventId;
                                    command3.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    command3.ExecuteNonQuery();
                                }
                                else
                                {
                                    command.Parameters["@EventName"].Value = evt.name;
                                    command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteReconstruction_RBSiteEnd2";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@SSHash", SqlDbType.VarChar, 64);
                    command.Parameters["@SSHash"].Direction = ParameterDirection.Input;
                    command.Parameters["@SSHash"].Value = ssHash;

                    if (siteStructure.getGuestSupport() > 0)
                    {
                        command.Parameters.Add("@GuestSupported", SqlDbType.Bit);
                        command.Parameters["@GuestSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@GuestSupported"].Value = true;

                        if (siteStructure.getGuestSupport() == 2)
                        {
                            command.Parameters.Add("@StatelessGuestSupported", SqlDbType.Bit);
                            command.Parameters["@StatelessGuestSupported"].Direction = ParameterDirection.Input;
                            command.Parameters["@StatelessGuestSupported"].Value = true;
                        }
                    }

                    if (siteStructure.eventsSupported())
                    {
                        command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                        command.Parameters["@EventsSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@EventsSupported"].Value = true;
                    }

                    if (siteStructure.containsOwnerRole())
                    {
                        command.Parameters.Add("@OwnerDefaultRole", SqlDbType.NVarChar, 256);
                        command.Parameters["@OwnerDefaultRole"].Direction = ParameterDirection.Input;
                        command.Parameters["@OwnerDefaultRole"].Value = siteStructure.getOwnerRole();
                    }

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Mgt_ReconstructUBSite(long siteId, SiteStructure siteStructure, string ssHash)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteReconstruction_UBSiteBegin";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                    command.Parameters["@EventsSupported"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventsSupported"].Value = siteStructure.eventsSupported();

                    command.ExecuteNonQuery();

                    if (siteStructure.eventsSupported())
                    {
                        command = new SqlCommand();
                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "Softnet_SiteReconstruction_SelectEvents";

                        command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                        command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                        command.Parameters["@SiteId"].Value = siteId;

                        SqlDataReader dataReader = command.ExecuteReader();
                        List<Event> existingEvents = new List<Event>();
                        while (dataReader.Read())
                        {
                            long eventId = (long)dataReader[0];
                            int eventKind = (int)dataReader[1];
                            string eventName = (string)dataReader[2];
                            existingEvents.Add(new Event(eventId, eventKind, eventName));
                        }
                        dataReader.Close();

                        command = new SqlCommand();
                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "Softnet_SiteReconstruction_DeleteEvent";

                        command.Parameters.Add("@EventId", SqlDbType.BigInt);
                        command.Parameters["@EventId"].Direction = ParameterDirection.Input;

                        foreach (Event evt in existingEvents)
                        {
                            if (evt.eventKind == 1 && siteStructure.containsREvent(evt.name))
                                continue;
                            if (evt.eventKind == 2 && siteStructure.containsQEvent(evt.name))
                                continue;
                            if (evt.eventKind == 4 && siteStructure.containsPEvent(evt.name))
                                continue;

                            command.Parameters["@EventId"].Value = evt.eventId;
                            command.ExecuteNonQuery();
                        }                        

                        if (siteStructure.containsREvents())
                        {
                            SqlCommand command3 = new SqlCommand();
                            command3.Connection = connection;
                            command3.CommandType = CommandType.StoredProcedure;
                            command3.CommandText = "Softnet_SiteReconstruction_UpdateREvent";

                            command3.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command3.Parameters["@EventId"].Direction = ParameterDirection.Input;

                            command3.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command3.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddREvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.REvent evt in siteStructure.getREvents())
                            {
                                Event existingEvent = existingEvents.Find(x => x.eventKind == 1 && x.name.Equals(evt.name));
                                if (existingEvent != null)
                                {
                                    command3.Parameters["@EventId"].Value = existingEvent.eventId;
                                    command3.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    command3.ExecuteNonQuery();
                                }
                                else
                                {
                                    command.Parameters["@EventName"].Value = evt.name;
                                    command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    command.ExecuteNonQuery();
                                }
                            }
                        }

                        if (siteStructure.containsQEvents())
                        {
                            SqlCommand command3 = new SqlCommand();
                            command3.Connection = connection;
                            command3.CommandType = CommandType.StoredProcedure;
                            command3.CommandText = "Softnet_SiteReconstruction_UpdateQEvent";

                            command3.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command3.Parameters["@EventId"].Direction = ParameterDirection.Input;

                            command3.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command3.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command3.Parameters.Add("@QueueSize", SqlDbType.Int);
                            command3.Parameters["@QueueSize"].Direction = ParameterDirection.Input;

                            command3.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command3.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddQEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@QueueSize", SqlDbType.Int);
                            command.Parameters["@QueueSize"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.QEvent evt in siteStructure.getQEvents())
                            {
                                Event existingEvent = existingEvents.Find(x => x.eventKind == 2 && x.name.Equals(evt.name));
                                if (existingEvent != null)
                                {
                                    command3.Parameters["@EventId"].Value = existingEvent.eventId;
                                    command3.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    command3.Parameters["@QueueSize"].Value = evt.queueSize;
                                    command3.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    command3.ExecuteNonQuery();
                                }
                                else
                                {
                                    command.Parameters["@EventName"].Value = evt.name;
                                    command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    command.Parameters["@QueueSize"].Value = evt.queueSize;
                                    command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                    command.ExecuteNonQuery();
                                }
                            }
                        }

                        if (siteStructure.containsPEvents())
                        {
                            SqlCommand command3 = new SqlCommand();
                            command3.Connection = connection;
                            command3.CommandType = CommandType.StoredProcedure;
                            command3.CommandText = "Softnet_SiteReconstruction_UpdatePEvent";

                            command3.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command3.Parameters["@EventId"].Direction = ParameterDirection.Input;

                            command3.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command3.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddPEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            foreach (Softnet.Tracker.SiteModel.PEvent evt in siteStructure.getPEvents())
                            {
                                Event existingEvent = existingEvents.Find(x => x.eventKind == 4 && x.name.Equals(evt.name));
                                if (existingEvent != null)
                                {
                                    command3.Parameters["@EventId"].Value = existingEvent.eventId;
                                    command3.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    command3.ExecuteNonQuery();
                                }
                                else
                                {
                                    command.Parameters["@EventName"].Value = evt.name;
                                    command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteReconstruction_UBSiteEnd";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@SSHash", SqlDbType.VarChar, 64);
                    command.Parameters["@SSHash"].Direction = ParameterDirection.Input;
                    command.Parameters["@SSHash"].Value = ssHash;

                    if (siteStructure.getGuestSupport() > 0)
                    {
                        command.Parameters.Add("@GuestSupported", SqlDbType.Bit);
                        command.Parameters["@GuestSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@GuestSupported"].Value = true;

                        if (siteStructure.getGuestSupport() == 2)
                        {
                            command.Parameters.Add("@StatelessGuestSupported", SqlDbType.Bit);
                            command.Parameters["@StatelessGuestSupported"].Direction = ParameterDirection.Input;
                            command.Parameters["@StatelessGuestSupported"].Value = true;
                        }
                    }

                    if (siteStructure.eventsSupported())
                    {
                        command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                        command.Parameters["@EventsSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@EventsSupported"].Value = true;
                    }

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void ServiceGroup_GetItemData(long serviceId, SiteModel.SGItemData itemData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_ServiceGroup_GetItemData";

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.Parameters.Add("@Hostname", SqlDbType.NVarChar, 256);
                    command.Parameters["@Hostname"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@EnabledStatus", SqlDbType.Bit);
                    command.Parameters["@EnabledStatus"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();
                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode == 0)
                    {
                        itemData.serviceId = serviceId;
                        itemData.hostname = (string)command.Parameters["@Hostname"].Value;
                        if (Regex.IsMatch(itemData.hostname, @"[^\x20-\x7F]", RegexOptions.None))
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                        itemData.enabled = (bool)command.Parameters["@EnabledStatus"].Value;
                    }
                    else
                        throw new SoftnetException(ErrorCodes.RESTART);
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int ServiceParams_GetData(long serviceId, ServiceSyncData syncData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_ServiceParams_GetData";

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.Parameters.Add("@PingPeriod", SqlDbType.Int);
                    command.Parameters["@PingPeriod"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode != 0)
                        return -1;

                    syncData.pingPeriod = (int)command.Parameters["@PingPeriod"].Value;
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int ServiceParams_GetPingPeriod(long serviceId, ServiceSyncData syncData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_ServiceParams_GetPingPeriod";

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.Parameters.Add("@PingPeriod", SqlDbType.Int);
                    command.Parameters["@PingPeriod"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode != 0)
                        return -1;

                    syncData.pingPeriod = (int)command.Parameters["@PingPeriod"].Value;
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void ServiceParams_SetPingPeriod(long serviceId, int pingPeriod)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_ServiceParams_SetPingPeriod";

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.Parameters.Add("@PingPeriod", SqlDbType.Int);
                    command.Parameters["@PingPeriod"].Direction = ParameterDirection.Input;
                    command.Parameters["@PingPeriod"].Value = pingPeriod;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int ClientParams_GetData(long clientId, ClientSyncData stateData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_ClientParams_GetData";

                    command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientId"].Value = clientId;

                    command.Parameters.Add("@PingPeriod", SqlDbType.Int);
                    command.Parameters["@PingPeriod"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode != 0)
                        return -1;

                    stateData.pingPeriod = (int)command.Parameters["@PingPeriod"].Value;
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int ClientParams_GetPingPeriod(long clientId, ClientSyncData syncData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_ClientParams_GetPingPeriod";

                    command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientId"].Value = clientId;

                    command.Parameters.Add("@PingPeriod", SqlDbType.Int);
                    command.Parameters["@PingPeriod"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode != 0)
                        return -1;

                    syncData.pingPeriod = (int)command.Parameters["@PingPeriod"].Value;
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void ClientParams_SetPingPeriod(long clientId, int pingPeriod)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_ClientParams_SetPingPeriod";

                    command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientId"].Value = clientId;

                    command.Parameters.Add("@PingPeriod", SqlDbType.Int);
                    command.Parameters["@PingPeriod"].Direction = ParameterDirection.Input;
                    command.Parameters["@PingPeriod"].Value = pingPeriod;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int RBMemebership_GetUserData(long userId, long siteId, SiteModel.MUserData userData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_RBMembership_GetUserData";

                    command.Parameters.Add("@UserId", SqlDbType.BigInt);
                    command.Parameters["@UserId"].Direction = ParameterDirection.Input;
                    command.Parameters["@UserId"].Value = userId;

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@UserName", SqlDbType.NVarChar, 256);
                    command.Parameters["@UserName"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@UserKind", SqlDbType.Int);
                    command.Parameters["@UserKind"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ContactId", SqlDbType.BigInt);
                    command.Parameters["@ContactId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ConsumerId", SqlDbType.BigInt);
                    command.Parameters["@ConsumerId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    List<long> roles = new List<long>();
                    SqlDataReader dataReader = command.ExecuteReader();
                    try
                    {
                        if (dataReader.FieldCount == 1)
                        {
                            while (dataReader.Read())
                            {
                                long roleId = (long)dataReader[0];
                                roles.Add(roleId);
                            }
                        }
                    }
                    finally
                    {
                        dataReader.Close();
                    }

                    int user_status_code = (int)command.Parameters["@ReturnValue"].Value;
                    if (user_status_code == 0)
                    {
                        userData.userId = userId;
                        userData.roles = roles;
                        userData.name = (string)command.Parameters["@UserName"].Value;
                        if (Regex.IsMatch(userData.name, @"[^\x20-\x7F]", RegexOptions.None))
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                        userData.userKind = (int)command.Parameters["@UserKind"].Value;
                        if (userData.userKind == 3)
                        {
                            userData.contactId = (long)command.Parameters["@ContactId"].Value;
                            userData.consumerId = (long)command.Parameters["@ConsumerId"].Value;
                        }
                    }
                    return user_status_code;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void RBMemebership_GetUserList(long siteId, SiteModel.MUserList userList)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_RBMembership_GetUserList";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    SqlDataReader dataReader = command.ExecuteReader();
                    try
                    {
                        if (dataReader.FieldCount == 0)
                            throw new SoftnetException(ErrorCodes.RESTART);

                        userList.users = new List<SiteModel.MUser>();
                        while (dataReader.Read())
                        {
                            long userId = (long)dataReader[0];
                            string userName = (string)dataReader[1];
                            if (Regex.IsMatch(userName, @"[^\x20-\x7F]", RegexOptions.None))
                                throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                            int userKind = (int)dataReader[2];
                            if (userKind == 3)
                            {
                                long contactId = (long)dataReader[3];
                                long consumerId = (long)dataReader[4];
                                userList.users.Add(new SiteModel.MUser(userId, userName, contactId, consumerId));
                            }
                            else
                                userList.users.Add(new SiteModel.MUser(userId, userName, userKind));
                        }

                        if (dataReader.NextResult() == false)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                        userList.roles = new List<SiteModel.MRole>();
                        while (dataReader.Read())
                        {
                            long roleId = (long)dataReader[0];
                            string roleName = (string)dataReader[1];
                            if (Regex.IsMatch(roleName, @"[^\x20-\x7F]", RegexOptions.None))
                                throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                            var role = new SiteModel.MRole(roleId, roleName);
                            userList.roles.Add(role);
                        }

                        if (userList.roles.Count == 0)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                        if (dataReader.NextResult() == false)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                        userList.userRoles = new List<SiteModel.MUserRole>();
                        while (dataReader.Read())
                        {
                            long userId = (long)dataReader[0];
                            long roleId = (long)dataReader[1];
                            var cur = new SiteModel.MUserRole(userId, roleId);
                            userList.userRoles.Add(cur);
                        }
                    }
                    finally
                    {
                        dataReader.Close();
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }        
        }

        public static bool RBMemebership_IsGuestAllowed(long siteId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_RBMembership_GetGuestStatus";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@Allowed", SqlDbType.Bit);
                    command.Parameters["@Allowed"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();
                    int returnCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (returnCode != 0)
                    {
                        if (returnCode == -1)
                            throw new SoftnetException(ErrorCodes.RESTART);
                        if (returnCode == -5)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                    }

                    return (bool)command.Parameters["@Allowed"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int UBMemebership_GetUserData(long userId, long siteId, SiteModel.MUserData userData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_UBMembership_GetUserData";

                    command.Parameters.Add("@UserId", SqlDbType.BigInt);
                    command.Parameters["@UserId"].Direction = ParameterDirection.Input;
                    command.Parameters["@UserId"].Value = userId;

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@UserName", SqlDbType.NVarChar, 256);
                    command.Parameters["@UserName"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@UserKind", SqlDbType.Int);
                    command.Parameters["@UserKind"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ContactId", SqlDbType.BigInt);
                    command.Parameters["@ContactId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ConsumerId", SqlDbType.BigInt);
                    command.Parameters["@ConsumerId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();
                    int user_status_code = (int)command.Parameters["@ReturnValue"].Value;
                    if (user_status_code == 0)
                    {
                        userData.userId = userId;
                        userData.name = (string)command.Parameters["@UserName"].Value;
                        if (Regex.IsMatch(userData.name, @"[^\x20-\x7F]", RegexOptions.None))
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                        userData.userKind = (int)command.Parameters["@UserKind"].Value;
                        if (userData.userKind == 3)
                        {
                            userData.contactId = (long)command.Parameters["@ContactId"].Value;
                            userData.consumerId = (long)command.Parameters["@ConsumerId"].Value;
                        }
                    }
                    return user_status_code;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void UBMemebership_GetUserList(long siteId, SiteModel.MUserList userList)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_UBMembership_GetUserList";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    SqlDataReader dataReader = command.ExecuteReader();
                    try
                    {
                        if (dataReader.FieldCount == 0)
                            throw new SoftnetException(ErrorCodes.RESTART);

                        userList.users = new List<SiteModel.MUser>();
                        while (dataReader.Read())
                        {
                            long userId = (long)dataReader[0];
                            string userName = (string)dataReader[1];
                            if (Regex.IsMatch(userName, @"[^\x20-\x7F]", RegexOptions.None))
                                throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                            int userKind = (int)dataReader[2];
                            if (userKind == 3)
                            {
                                long contactId = (long)dataReader[3];
                                long consumerId = (long)dataReader[4];
                                userList.users.Add(new SiteModel.MUser(userId, userName, contactId, consumerId));
                            }
                            else
                                userList.users.Add(new SiteModel.MUser(userId, userName, userKind));
                        }
                    }
                    finally
                    {
                        dataReader.Close();
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }       
        }

        public static bool UBMemebership_IsGuestAllowed(long siteId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_UBMembership_GetGuestStatus";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@Allowed", SqlDbType.Bit);
                    command.Parameters["@Allowed"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();
                    int returnCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (returnCode != 0)
                    {
                        if (returnCode == -1)
                            throw new SoftnetException(ErrorCodes.RESTART);
                        if(returnCode == -2)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                    }

                    return (bool)command.Parameters["@Allowed"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void EventController_SaveREventInstance(EventIData eventIData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_EventController_SaveREventInstance";

                    command.Parameters.Add("@EventId", SqlDbType.BigInt);
                    command.Parameters["@EventId"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventId"].Value = eventIData.EventId;

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = eventIData.ServiceId;

                    command.Parameters.Add("@IsNull", SqlDbType.Bit);
                    command.Parameters["@IsNull"].Direction = ParameterDirection.Input;
                    command.Parameters["@IsNull"].Value = eventIData.IsNull;

                    if (eventIData.Arguments != null)
                    {
                        command.Parameters.Add("@Arguments", SqlDbType.VarBinary, 4096);
                        command.Parameters["@Arguments"].Direction = ParameterDirection.Input;
                        command.Parameters["@Arguments"].Value = eventIData.Arguments;
                    }

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@CreatedTimeTicks", SqlDbType.BigInt);
                    command.Parameters["@CreatedTimeTicks"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@CreatedDate", SqlDbType.DateTime);
                    command.Parameters["@CreatedDate"].Direction = ParameterDirection.Output;

                    command.ExecuteNonQuery();

                    eventIData.InstanceId = (long)command.Parameters["@InstanceId"].Value;
                    eventIData.CreatedTimeTicks = (long)command.Parameters["@CreatedTimeTicks"].Value;
                    eventIData.CreatedDate = (DateTime)command.Parameters["@CreatedDate"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void EventController_SaveREventInstance(EventIData eventIData, long invalidInstanceId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_EventController_SaveREventInstance2";

                    command.Parameters.Add("@InvalidInstanceId", SqlDbType.BigInt);
                    command.Parameters["@InvalidInstanceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@InvalidInstanceId"].Value = invalidInstanceId;

                    command.Parameters.Add("@EventId", SqlDbType.BigInt);
                    command.Parameters["@EventId"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventId"].Value = eventIData.EventId;

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = eventIData.ServiceId;

                    command.Parameters.Add("@IsNull", SqlDbType.Bit);
                    command.Parameters["@IsNull"].Direction = ParameterDirection.Input;
                    command.Parameters["@IsNull"].Value = eventIData.IsNull;

                    if (eventIData.Arguments != null)
                    {
                        command.Parameters.Add("@Arguments", SqlDbType.VarBinary, 4096);
                        command.Parameters["@Arguments"].Direction = ParameterDirection.Input;
                        command.Parameters["@Arguments"].Value = eventIData.Arguments;
                    }

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@CreatedTimeTicks", SqlDbType.BigInt);
                    command.Parameters["@CreatedTimeTicks"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@CreatedDate", SqlDbType.DateTime);
                    command.Parameters["@CreatedDate"].Direction = ParameterDirection.Output;

                    command.ExecuteNonQuery();

                    eventIData.InstanceId = (long)command.Parameters["@InstanceId"].Value;
                    eventIData.CreatedTimeTicks = (long)command.Parameters["@CreatedTimeTicks"].Value;
                    eventIData.CreatedDate = (DateTime)command.Parameters["@CreatedDate"].Value;                    
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void EventController_SaveQEventInstance(EventIData eventIData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_EventController_SaveQEventInstance";

                    command.Parameters.Add("@EventId", SqlDbType.BigInt);
                    command.Parameters["@EventId"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventId"].Value = eventIData.EventId;

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = eventIData.ServiceId;

                    if (eventIData.Arguments != null)
                    {
                        command.Parameters.Add("@Arguments", SqlDbType.VarBinary, 4096);
                        command.Parameters["@Arguments"].Direction = ParameterDirection.Input;
                        command.Parameters["@Arguments"].Value = eventIData.Arguments;
                    }

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@CreatedTimeTicks", SqlDbType.BigInt);
                    command.Parameters["@CreatedTimeTicks"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@CreatedDate", SqlDbType.DateTime);
                    command.Parameters["@CreatedDate"].Direction = ParameterDirection.Output;

                    command.ExecuteNonQuery();

                    eventIData.InstanceId = (long)command.Parameters["@InstanceId"].Value;
                    eventIData.CreatedTimeTicks = (long)command.Parameters["@CreatedTimeTicks"].Value;
                    eventIData.CreatedDate = (DateTime)command.Parameters["@CreatedDate"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void EventController_SaveQEventInstance(EventIData eventIData, long invalidInstanceId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_EventController_SaveQEventInstance2";

                    command.Parameters.Add("@InvalidInstanceId", SqlDbType.BigInt);
                    command.Parameters["@InvalidInstanceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@InvalidInstanceId"].Value = invalidInstanceId;

                    command.Parameters.Add("@EventId", SqlDbType.BigInt);
                    command.Parameters["@EventId"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventId"].Value = eventIData.EventId;

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = eventIData.ServiceId;

                    if (eventIData.Arguments != null)
                    {
                        command.Parameters.Add("@Arguments", SqlDbType.VarBinary, 4096);
                        command.Parameters["@Arguments"].Direction = ParameterDirection.Input;
                        command.Parameters["@Arguments"].Value = eventIData.Arguments;
                    }

                    command.Parameters.Add("@CreatedTimeTicks", SqlDbType.BigInt);
                    command.Parameters["@CreatedTimeTicks"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@CreatedDate", SqlDbType.DateTime);
                    command.Parameters["@CreatedDate"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Output;

                    command.ExecuteNonQuery();

                    eventIData.CreatedTimeTicks = (long)command.Parameters["@CreatedTimeTicks"].Value;
                    eventIData.CreatedDate = (DateTime)command.Parameters["@CreatedDate"].Value;
                    eventIData.InstanceId = (long)command.Parameters["@InstanceId"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void EventController_SavePEventInstance(EventIData eventIData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_EventController_SavePEventInstance";

                    command.Parameters.Add("@EventId", SqlDbType.BigInt);
                    command.Parameters["@EventId"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventId"].Value = eventIData.EventId;

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = eventIData.ServiceId;

                    command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientId"].Value = eventIData.ClientId;

                    if (eventIData.Arguments != null)
                    {
                        command.Parameters.Add("@Arguments", SqlDbType.VarBinary, 4096);
                        command.Parameters["@Arguments"].Direction = ParameterDirection.Input;
                        command.Parameters["@Arguments"].Value = eventIData.Arguments;
                    }

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@CreatedTimeTicks", SqlDbType.BigInt);
                    command.Parameters["@CreatedTimeTicks"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@CreatedDate", SqlDbType.DateTime);
                    command.Parameters["@CreatedDate"].Direction = ParameterDirection.Output;

                    command.ExecuteNonQuery();

                    eventIData.InstanceId = (long)command.Parameters["@InstanceId"].Value;
                    eventIData.CreatedTimeTicks = (long)command.Parameters["@CreatedTimeTicks"].Value;
                    eventIData.CreatedDate = (DateTime)command.Parameters["@CreatedDate"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void EventController_SavePEventInstance(EventIData eventIData, long invalidInstanceId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_EventController_SavePEventInstance2";

                    command.Parameters.Add("@InvalidInstanceId", SqlDbType.BigInt);
                    command.Parameters["@InvalidInstanceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@InvalidInstanceId"].Value = invalidInstanceId;

                    command.Parameters.Add("@EventId", SqlDbType.BigInt);
                    command.Parameters["@EventId"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventId"].Value = eventIData.EventId;

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = eventIData.ServiceId;

                    command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientId"].Value = eventIData.ClientId;

                    if (eventIData.Arguments != null)
                    {
                        command.Parameters.Add("@Arguments", SqlDbType.VarBinary, 4096);
                        command.Parameters["@Arguments"].Direction = ParameterDirection.Input;
                        command.Parameters["@Arguments"].Value = eventIData.Arguments;
                    }

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@CreatedTimeTicks", SqlDbType.BigInt);
                    command.Parameters["@CreatedTimeTicks"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@CreatedDate", SqlDbType.DateTime);
                    command.Parameters["@CreatedDate"].Direction = ParameterDirection.Output;

                    command.ExecuteNonQuery();

                    eventIData.InstanceId = (long)command.Parameters["@InstanceId"].Value;
                    eventIData.CreatedTimeTicks = (long)command.Parameters["@CreatedTimeTicks"].Value;
                    eventIData.CreatedDate = (DateTime)command.Parameters["@CreatedDate"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void EventController_DeleteREventInstance(long instanceId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_EventController_DeleteREventInstance";

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@InstanceId"].Value = instanceId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void EventController_DeleteQEventInstance(long instanceId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_EventController_DeleteQEventInstance";

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@InstanceId"].Value = instanceId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void EventController_DeletePEventInstance(long instanceId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_EventController_DeletePEventInstance";

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@InstanceId"].Value = instanceId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Balancer_ResolveServiceUid(Guid serviceUid, Softnet.Tracker.Balancer.RequestHandler requestHandler)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_FS_ResolveServiceUid";

                    command.Parameters.Add("@ServiceUid", SqlDbType.UniqueIdentifier);
                    command.Parameters["@ServiceUid"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceUid"].Value = serviceUid;

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int returnCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (returnCode != 0)
                    {
                        if (returnCode == -1)
                            return ErrorCodes.SERVICE_NOT_REGISTERED;
                        return ErrorCodes.DATA_INTEGRITY_ERROR;
                    }

                    requestHandler.SiteId = (long)command.Parameters["@SiteId"].Value;
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Balancer_ResolveClientKey(int clientCategory, string clientKey, Softnet.Tracker.Balancer.RequestHandler requestHandler)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_FS_ResolveClientKey";

                    command.Parameters.Add("@ClientCategory", SqlDbType.Int);
                    command.Parameters["@ClientCategory"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientCategory"].Value = clientCategory;

                    command.Parameters.Add("@ClientKey", SqlDbType.NVarChar, 64);
                    command.Parameters["@ClientKey"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientKey"].Value = clientKey;

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int returnCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (returnCode != 0)
                    {
                        if (returnCode == -1)
                            return ErrorCodes.CLIENT_NOT_REGISTERED;
                        if (returnCode == -2)
                            return ErrorCodes.INVALID_CLIENT_CATEGORY;
                        return ErrorCodes.DATA_INTEGRITY_ERROR;
                    }

                    requestHandler.SiteId = (long)command.Parameters["@SiteId"].Value;
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Client_GetIData(ClientInstaller clientInstaller, ClientModel.AuthData authData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Client_GetIData";
                    
                    command.Parameters.Add("@ClientKey", SqlDbType.VarChar, 32);
                    command.Parameters["@ClientKey"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientKey"].Value = clientInstaller.ClientKey;

                    command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    command.Parameters["@ClientId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@UserId", SqlDbType.BigInt);
                    command.Parameters["@UserId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@UserKind", SqlDbType.Int);
                    command.Parameters["@UserKind"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@SiteKind", SqlDbType.Int);
                    command.Parameters["@SiteKind"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ServiceType", SqlDbType.NVarChar, 256);
                    command.Parameters["@ServiceType"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ContractAuthor", SqlDbType.NVarChar, 256);
                    command.Parameters["@ContractAuthor"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ClientDescription", SqlDbType.NVarChar, 256);
                    command.Parameters["@ClientDescription"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@Salt", SqlDbType.VarChar, 64);
                    command.Parameters["@Salt"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@SaltedPassword", SqlDbType.VarChar, 64);
                    command.Parameters["@SaltedPassword"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int returnCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (returnCode != 0)
                    {
                        if(returnCode == -1)
                            return ErrorCodes.CLIENT_NOT_REGISTERED;
                        return ErrorCodes.DATA_INTEGRITY_ERROR;
                    }

                    clientInstaller.ClientId = (long)command.Parameters["@ClientId"].Value;
                    clientInstaller.SiteId = (long)command.Parameters["@SiteId"].Value;
                    clientInstaller.SiteKind = (int)command.Parameters["@SiteKind"].Value;
                    clientInstaller.UserId = (long)command.Parameters["@UserId"].Value;
                    clientInstaller.UserKind = (int)command.Parameters["@UserKind"].Value;

                    if (command.Parameters["@ServiceType"].Value != DBNull.Value)
                        clientInstaller.ServiceType = (string)command.Parameters["@ServiceType"].Value;
                    if (command.Parameters["@ContractAuthor"].Value != DBNull.Value)
                        clientInstaller.ContractAuthor = (string)command.Parameters["@ContractAuthor"].Value;
                    if (command.Parameters["@ClientDescription"].Value != DBNull.Value)
                        clientInstaller.ClientDescription = (string)command.Parameters["@ClientDescription"].Value;

                    if (clientInstaller.UserKind != 5)
                    {
                        if (command.Parameters["@SaltedPassword"].Value == DBNull.Value || command.Parameters["@Salt"].Value == DBNull.Value)
                            return ErrorCodes.PASSWORD_NOT_MATCHED;

                        try
                        {
                            string saltedPassword = (string)command.Parameters["@SaltedPassword"].Value;
                            authData.SaltedPassword = Convert.FromBase64String(saltedPassword);

                            string salt = (string)command.Parameters["@Salt"].Value;
                            authData.Salt = Convert.FromBase64String(salt);
                        }
                        catch (FormatException)
                        {
                            return ErrorCodes.DATA_INTEGRITY_ERROR;
                        }                        
                    }

                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Client_GetStatelessClientIData(ClientInstaller clientInstaller)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Client_GetStatelessClientIData";

                    command.Parameters.Add("@ClientKey", SqlDbType.VarChar, 32);
                    command.Parameters["@ClientKey"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientKey"].Value = clientInstaller.ClientKey;

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@SiteKind", SqlDbType.Int);
                    command.Parameters["@SiteKind"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int returnCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (returnCode == -1)
                        return ErrorCodes.CLIENT_NOT_REGISTERED;

                    clientInstaller.SiteId = (long)command.Parameters["@SiteId"].Value;
                    clientInstaller.SiteKind = (int)command.Parameters["@SiteKind"].Value;
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Client_SaveSoftwareProps(ClientInstaller clientInstaller)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Client_SaveSoftwareProps";

                    command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientId"].Value = clientInstaller.ClientId;

                    command.Parameters.Add("@ServiceType", SqlDbType.NVarChar, 256);
                    command.Parameters["@ServiceType"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceType"].Value = clientInstaller.ServiceType;

                    command.Parameters.Add("@ContractAuthor", SqlDbType.NVarChar, 256);
                    command.Parameters["@ContractAuthor"].Direction = ParameterDirection.Input;
                    command.Parameters["@ContractAuthor"].Value = clientInstaller.ContractAuthor;

                    command.Parameters.Add("@ClientDescription", SqlDbType.NVarChar, 256);
                    command.Parameters["@ClientDescription"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientDescription"].Value = clientInstaller.ClientDescription;

                    command.ExecuteNonQuery();                    
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Client_AddSubscription(long siteId, long clientId, SubscriptionData sData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Client_AddSubscription";

                    command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientId"].Value = clientId;

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@EventKind", SqlDbType.Int);
                    command.Parameters["@EventKind"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventKind"].Value = sData.EventKind;

                    command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                    command.Parameters["@EventName"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventName"].Value = sData.EventName;

                    command.Parameters.Add("@EventId", SqlDbType.BigInt);
                    command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode != 0)
                        return -1;
                    
                    sData.EventId = (long)command.Parameters["@EventId"].Value;
                    sData.InstanceId = (long)command.Parameters["@InstanceId"].Value;                    
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Client_RemoveSubscription(long clientId, long eventId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Client_RemoveSubscription";

                    command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientId"].Value = clientId;

                    command.Parameters.Add("@EventId", SqlDbType.BigInt);
                    command.Parameters["@EventId"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventId"].Value = eventId;

                    command.ExecuteNonQuery();                    
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Client_GetSubscriptions(long clientId, List<SubscriptionData> subscriptions)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Client_GetSubscriptions";

                    command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientId"].Value = clientId;

                    SqlDataReader dataReader = command.ExecuteReader();
                    while (dataReader.Read())
                    {
                        var sd = new SubscriptionData();
                        sd.EventId = (long)dataReader[0];
                        sd.EventKind = (int)dataReader[1];
                        sd.EventName = (string)dataReader[2];
                        sd.InstanceId = (long)dataReader[3];
                        subscriptions.Add(sd);
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Client_GetREventArguments(long instanceId, Container<byte[]> eventArgs)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Client_GetREventArguments";

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@InstanceId"].Value = instanceId;

                    command.Parameters.Add("@Arguments", SqlDbType.VarBinary, 4096);
                    command.Parameters["@Arguments"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode != 0)
                        return -1;
                    
                    eventArgs.Obj = (byte[])command.Parameters["@Arguments"].Value;
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Client_GetQEventArguments(long instanceId, Container<byte[]> eventArgs)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Client_GetQEventArguments";

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@InstanceId"].Value = instanceId;

                    command.Parameters.Add("@Arguments", SqlDbType.VarBinary, 4096);
                    command.Parameters["@Arguments"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode != 0)
                        return -1;
                    
                    eventArgs.Obj = (byte[])command.Parameters["@Arguments"].Value;
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Client_GetPEventArguments(long instanceId, Container<byte[]> eventArgs)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Client_GetPEventArguments";

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@InstanceId"].Value = instanceId;

                    command.Parameters.Add("@Arguments", SqlDbType.VarBinary, 4096);
                    command.Parameters["@Arguments"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int resultCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (resultCode != 0)
                        return -1;

                    eventArgs.Obj = (byte[])command.Parameters["@Arguments"].Value;
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Client_SaveEventAcknowledgment(long clientId, long eventId, long instanceId)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Client_SaveEventAcknowledgment";

                    command.Parameters.Add("@ClientId", SqlDbType.BigInt);
                    command.Parameters["@ClientId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ClientId"].Value = clientId;

                    command.Parameters.Add("@EventId", SqlDbType.BigInt);
                    command.Parameters["@EventId"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventId"].Value = eventId;

                    command.Parameters.Add("@InstanceId", SqlDbType.BigInt);
                    command.Parameters["@InstanceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@InstanceId"].Value = instanceId;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Service_GetIData(ServiceInstaller serviceInstaller, ServiceModel.AuthData authData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Service_GetIData";

                    command.Parameters.Add("@ServiceUid", SqlDbType.UniqueIdentifier);
                    command.Parameters["@ServiceUid"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceUid"].Value = ByteConverter.ToGuid(authData.ServiceUid, 0);

                    command.Parameters.Add("@SiteUid", SqlDbType.UniqueIdentifier);
                    command.Parameters["@SiteUid"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ServiceType", SqlDbType.NVarChar, 256);
                    command.Parameters["@ServiceType"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ContractAuthor", SqlDbType.NVarChar, 256);
                    command.Parameters["@ContractAuthor"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@Version", SqlDbType.NVarChar, 64);
                    command.Parameters["@Version"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@SSHash", SqlDbType.VarChar, 64);
                    command.Parameters["@SSHash"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@StorageUid", SqlDbType.UniqueIdentifier);
                    command.Parameters["@StorageUid"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@SaltedPassword", SqlDbType.VarChar, 64);
                    command.Parameters["@SaltedPassword"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@Salt", SqlDbType.VarChar, 64);
                    command.Parameters["@Salt"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    int returnCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (returnCode != 0)
                    {
                        if (returnCode == -1)
                            return ErrorCodes.SERVICE_NOT_REGISTERED;
                        return ErrorCodes.DATA_INTEGRITY_ERROR;
                    }

                    serviceInstaller.SiteUid = (Guid)command.Parameters["@SiteUid"].Value;
                    serviceInstaller.SiteId = (long)command.Parameters["@SiteId"].Value;
                    serviceInstaller.ServiceId = (long)command.Parameters["@ServiceId"].Value;
                    serviceInstaller.Version = (string)command.Parameters["@Version"].Value;

                    if (command.Parameters["@ServiceType"].Value != DBNull.Value)
                        serviceInstaller.ServiceType = (string)command.Parameters["@ServiceType"].Value;
                    if (command.Parameters["@ContractAuthor"].Value != DBNull.Value)
                        serviceInstaller.ContractAuthor = (string)command.Parameters["@ContractAuthor"].Value;
                    if (command.Parameters["@StorageUid"].Value != DBNull.Value)
                        serviceInstaller.StorageUid = (Guid)command.Parameters["@StorageUid"].Value;

                    try
                    {
                        if (command.Parameters["@SSHash"].Value != DBNull.Value)
                        {
                            string ssHash = (string)command.Parameters["@SSHash"].Value;
                            serviceInstaller.SSHash = Convert.FromBase64String(ssHash);
                        }

                        if (command.Parameters["@SaltedPassword"].Value == DBNull.Value || command.Parameters["@Salt"].Value == DBNull.Value)
                            return ErrorCodes.PASSWORD_NOT_MATCHED;
                        
                        string saltedPassword = (string)command.Parameters["@SaltedPassword"].Value;
                        authData.SaltedPassword = Convert.FromBase64String(saltedPassword);

                        string salt = (string)command.Parameters["@Salt"].Value;
                        authData.Salt = Convert.FromBase64String(salt);
                    }
                    catch (FormatException)
                    {
                        return ErrorCodes.DATA_INTEGRITY_ERROR;
                    }                    

                    return 0;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Service_UpdateStorageUid(long serviceId, Guid storageUid)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Service_UpdateStorageUid";

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.Parameters.Add("@StorageUid", SqlDbType.UniqueIdentifier);
                    command.Parameters["@StorageUid"].Direction = ParameterDirection.Input;
                    command.Parameters["@StorageUid"].Value = storageUid;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Service_SaveStructure(ServiceInstaller si)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Service_SaveStructure";

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = si.ServiceId;

                    command.Parameters.Add("@ServiceType", SqlDbType.NVarChar, 256);
                    command.Parameters["@ServiceType"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceType"].Value = si.ServiceType;

                    command.Parameters.Add("@ContractAuthor", SqlDbType.NVarChar, 256);
                    command.Parameters["@ContractAuthor"].Direction = ParameterDirection.Input;
                    command.Parameters["@ContractAuthor"].Value = si.ContractAuthor;

                    command.Parameters.Add("@Version", SqlDbType.NVarChar, 64);
                    command.Parameters["@Version"].Direction = ParameterDirection.Input;
                    command.Parameters["@Version"].Value = si.Version;

                    command.Parameters.Add("@SSHash", SqlDbType.VarChar, 64);
                    command.Parameters["@SSHash"].Direction = ParameterDirection.Input;
                    command.Parameters["@SSHash"].Value = Convert.ToBase64String(si.SSHash);

                    command.Parameters.Add("@SSXml", SqlDbType.NVarChar, 4000);
                    command.Parameters["@SSXml"].Direction = ParameterDirection.Input;
                    command.Parameters["@SSXml"].Value = SSXmlEncoder.exec(si.SiteStructure);

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }
    
        public static void Service_UpdateVersion(long serviceId, string version)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Service_UpdateVersion";

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.Parameters.Add("@Version", SqlDbType.NVarChar, 64);
                    command.Parameters["@Version"].Direction = ParameterDirection.Input;
                    command.Parameters["@Version"].Value = version;

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static int Service_SaveEventAcknowledgment(EventIData eventIData, long siteId, long serviceId, byte[] instanceUid)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Service_SaveEventAcknowledgment";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                    command.Parameters["@EventName"].Direction = ParameterDirection.Input;
                    command.Parameters["@EventName"].Value = eventIData.Name;

                    command.Parameters.Add("@InstanceUid", SqlDbType.UniqueIdentifier);
                    command.Parameters["@InstanceUid"].Direction = ParameterDirection.Input;
                    command.Parameters["@InstanceUid"].Value = ByteConverter.ToGuid(instanceUid);

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    return (int)command.Parameters["@ReturnValue"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Site_GetIData(long siteId, SiteIData iData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_Site_GetParams";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@SiteKind", SqlDbType.Int);
                    command.Parameters["@SiteKind"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@SiteUid", SqlDbType.UniqueIdentifier);
                    command.Parameters["@SiteUid"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@Structured", SqlDbType.Bit);
                    command.Parameters["@Structured"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ServiceType", SqlDbType.NVarChar, 256);
                    command.Parameters["@ServiceType"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ContractAuthor", SqlDbType.NVarChar, 256);
                    command.Parameters["@ContractAuthor"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@SSHash", SqlDbType.VarChar, 64);
                    command.Parameters["@SSHash"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@GuestSupported", SqlDbType.Bit);
                    command.Parameters["@GuestSupported"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@GuestAllowed", SqlDbType.Bit);
                    command.Parameters["@GuestAllowed"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@GuestEnabled", SqlDbType.Bit);
                    command.Parameters["@GuestEnabled"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@StatelessGuestSupported", SqlDbType.Bit);
                    command.Parameters["@StatelessGuestSupported"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@SiteEnabled", SqlDbType.Bit);
                    command.Parameters["@SiteEnabled"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@OwnerEligible", SqlDbType.Bit);
                    command.Parameters["@OwnerEligible"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@RolesSupported", SqlDbType.Bit);
                    command.Parameters["@RolesSupported"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                    command.Parameters["@EventsSupported"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();
                    int returnCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (returnCode != 0)
                    {
                        if (returnCode == -1)
                            throw new SoftnetException(ErrorCodes.RESTART);
                        if (returnCode == -5)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);                      
                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                    }

                    iData.SiteKind = (int)command.Parameters["@SiteKind"].Value;                    
                    iData.SiteUid = ByteConverter.GetBytes((Guid)command.Parameters["@SiteUid"].Value);
                    iData.SiteEnabled = (bool)command.Parameters["@SiteEnabled"].Value;
                    iData.OwnerEligible = (bool)command.Parameters["@OwnerEligible"].Value;
                    iData.Structured = (bool)command.Parameters["@Structured"].Value;

                    if (iData.Structured)
                    {
                        if (command.Parameters["@ServiceType"].Value == DBNull.Value)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                        if (command.Parameters["@ContractAuthor"].Value == DBNull.Value)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                        if (command.Parameters["@SSHash"].Value == DBNull.Value)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                        iData.ServiceType = (string)command.Parameters["@ServiceType"].Value;
                        iData.ContractAuthor = (string)command.Parameters["@ContractAuthor"].Value;
                        iData.SSHash = Convert.FromBase64String((string)command.Parameters["@SSHash"].Value);

                        if (iData.ServiceType.Length == 0 || iData.ContractAuthor.Length == 0 || iData.SSHash.Length == 0)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                        iData.GuestSupported = (bool)command.Parameters["@GuestSupported"].Value;
                        if (iData.GuestSupported)
                        {
                            iData.GuestAllowed = (bool)command.Parameters["@GuestAllowed"].Value;
                            iData.GuestEnabled = (bool)command.Parameters["@GuestEnabled"].Value;
                            iData.StatelessGuestSupported = (bool)command.Parameters["@StatelessGuestSupported"].Value;
                        }

                        if (iData.SiteEnabled && iData.OwnerEligible)
                        {
                            iData.RolesSupported = (bool)command.Parameters["@RolesSupported"].Value;
                            iData.EventsSupported = (bool)command.Parameters["@EventsSupported"].Value;

                            command = new SqlCommand();
                            command.Connection = Connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_Site_GetDataset";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@RolesSupported", SqlDbType.Bit);
                            command.Parameters["@RolesSupported"].Direction = ParameterDirection.Input;
                            command.Parameters["@RolesSupported"].Value = iData.RolesSupported;

                            command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                            command.Parameters["@EventsSupported"].Direction = ParameterDirection.Input;
                            command.Parameters["@EventsSupported"].Value = iData.EventsSupported;

                            SqlDataReader dataReader = command.ExecuteReader();

                            try
                            {
                                if (dataReader.FieldCount == 0) // site not found
                                    throw new SoftnetException(ErrorCodes.RESTART);

                                iData.SGItems = new List<SiteModel.SGItem>();
                                while (dataReader.Read())
                                {
                                    long serviceId = (long)dataReader[0];
                                    string hostname = (string)dataReader[1];
                                    if (Regex.IsMatch(hostname, @"[^\x20-\x7F]", RegexOptions.None))
                                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                                    string version = (string)dataReader[2];
                                    if (Regex.IsMatch(version, @"[^\x20-\x7F]", RegexOptions.None))
                                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                                    bool enabled = (bool)dataReader[3];
                                    var item = new SiteModel.SGItem(serviceId, hostname, version, enabled);
                                    iData.SGItems.Add(item);
                                }

                                if (iData.SiteKind == Constants.SiteKind.SingleService && iData.SGItems.Count != 1)
                                    throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                if (iData.RolesSupported)
                                {
                                    if (dataReader.NextResult() == false)
                                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                    iData.MUsers = new List<SiteModel.MUser>();
                                    while (dataReader.Read())
                                    {
                                        long userId = (long)dataReader[0];
                                        string userName = (string)dataReader[1];
                                        if (Regex.IsMatch(userName, @"[^\x20-\x7F]", RegexOptions.None))
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                                        int userKind = (int)dataReader[2];
                                        if (userKind == 3)
                                        {
                                            long contactId = (long)dataReader[3];
                                            long consumerId = (long)dataReader[4];
                                            iData.MUsers.Add(new SiteModel.MUser(userId, userName, contactId, consumerId));
                                        }
                                        else
                                            iData.MUsers.Add(new SiteModel.MUser(userId, userName, userKind));
                                    }

                                    if (dataReader.NextResult() == false)
                                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                    iData.MRoles = new List<SiteModel.MRole>();
                                    while (dataReader.Read())
                                    {
                                        long roleId = (long)dataReader[0];
                                        string roleName = (string)dataReader[1];
                                        if (Regex.IsMatch(roleName, @"[^\x20-\x7F]", RegexOptions.None))
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                                        iData.MRoles.Add(new SiteModel.MRole(roleId, roleName));
                                    }

                                    if (iData.MRoles.Count == 0)
                                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                    if (dataReader.NextResult() == false)
                                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                    iData.UserRoles = new List<SiteModel.MUserRole>();
                                    while (dataReader.Read())
                                    {
                                        long userId = (long)dataReader[0];
                                        long roleId = (long)dataReader[1];
                                        iData.UserRoles.Add(new SiteModel.MUserRole(userId, roleId));
                                    }

                                    if (iData.EventsSupported)
                                    {
                                        if (dataReader.NextResult() == false)
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                        iData.EventList = new List<SiteModel.EventData>();
                                        while (dataReader.Read())
                                        {
                                            var eventData = new SiteModel.EventData();
                                            eventData.EventId = (long)dataReader[0];
                                            eventData.EventName = (string)dataReader[1];
                                            if (Regex.IsMatch(eventData.EventName, @"[^\x20-\x7F]", RegexOptions.None))
                                                throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                                            eventData.EventKind = (int)dataReader[2];
                                            eventData.LifeTicks = (int)dataReader[3];
                                            eventData.QueueSize = (int)dataReader[4];
                                            eventData.GuestAccess = (int)dataReader[5];
                                            iData.EventList.Add(eventData);
                                        }

                                        if (dataReader.NextResult() == false)
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                        while (dataReader.Read())
                                        {
                                            long eventId = (long)dataReader[0];
                                            long roleId = (long)dataReader[1];
                                            var eventData = iData.EventList.Find(x => x.EventId == eventId);
                                            if (eventData != null)
                                            {
                                                if (eventData.Roles == null)
                                                    eventData.Roles = new List<long>();
                                                eventData.Roles.Add(roleId);
                                            }
                                        }

                                        if (dataReader.NextResult() == false)
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                        long eventSetupTimeTicks = AppClock.getTicks();
                                        long eventSetupTimeSeconds = SystemClock.Seconds;

                                        iData.REventInstances = new List<SiteModel.REventInstance>();
                                        while (dataReader.Read())
                                        {
                                            long instanceId = (long)dataReader[0];
                                            long eventId = (long)dataReader[1];
                                            long serviceId = (long)dataReader[2];
                                            long createdTimeTicks = (long)dataReader[3];
                                            DateTime createdDate = (DateTime)dataReader[4];
                                            bool isNull = (bool)dataReader[5];
                                            bool hasArguments = (bool)dataReader[6];
                                            byte[] arguments = null;
                                            if (dataReader[7] != DBNull.Value)
                                                arguments = (byte[])dataReader[7];
                                            var eventInstance = new REventInstance(instanceId, eventId, serviceId, createdTimeTicks, eventSetupTimeTicks, eventSetupTimeSeconds, createdDate, isNull, hasArguments, arguments);
                                            iData.REventInstances.Add(eventInstance);
                                        }

                                        if (dataReader.NextResult() == false)
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                        iData.QEventInstances = new List<SiteModel.QEventInstance>();
                                        while (dataReader.Read())
                                        {
                                            long instanceId = (long)dataReader[0];
                                            long eventId = (long)dataReader[1];
                                            long serviceId = (long)dataReader[2];
                                            long createdTimeTicks = (long)dataReader[3];
                                            DateTime createdDate = (DateTime)dataReader[4];
                                            bool hasArguments = (bool)dataReader[5];
                                            byte[] arguments = null;
                                            if (dataReader[6] != DBNull.Value)
                                                arguments = (byte[])dataReader[6];
                                            var eventInstance = new SiteModel.QEventInstance(instanceId, eventId, serviceId, createdTimeTicks, eventSetupTimeTicks, eventSetupTimeSeconds, createdDate, hasArguments, arguments);
                                            iData.QEventInstances.Add(eventInstance);
                                        }

                                        if (dataReader.NextResult() == false)
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                        iData.PEventInstances = new List<SiteModel.PEventInstance>();
                                        while (dataReader.Read())
                                        {
                                            long instanceId = (long)dataReader[0];
                                            long eventId = (long)dataReader[1];
                                            long serviceId = (long)dataReader[2];
                                            long clientId = (long)dataReader[3];
                                            long createdTimeTicks = (long)dataReader[4];
                                            DateTime createdDate = (DateTime)dataReader[5];
                                            bool hasArguments = (bool)dataReader[6];
                                            byte[] arguments = null;
                                            if (dataReader[7] != DBNull.Value)
                                                arguments = (byte[])dataReader[7];
                                            var eventInstance = new SiteModel.PEventInstance(instanceId, eventId, serviceId, clientId, createdTimeTicks, eventSetupTimeTicks, eventSetupTimeSeconds, createdDate, hasArguments, arguments);
                                            iData.PEventInstances.Add(eventInstance);
                                        }
                                    }
                                }
                                else
                                {
                                    if (dataReader.NextResult() == false)
                                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                    iData.MUsers = new List<SiteModel.MUser>();
                                    while (dataReader.Read())
                                    {
                                        long userId = (long)dataReader[0];
                                        string userName = (string)dataReader[1];
                                        if (Regex.IsMatch(userName, @"[^\x20-\x7F]", RegexOptions.None))
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                                        int userKind = (int)dataReader[2];
                                        if (userKind == 3)
                                        {
                                            long contactId = (long)dataReader[3];
                                            long consumerId = (long)dataReader[4];
                                            iData.MUsers.Add(new SiteModel.MUser(userId, userName, contactId, consumerId));
                                        }
                                        else
                                            iData.MUsers.Add(new SiteModel.MUser(userId, userName, userKind));
                                    }

                                    if (iData.EventsSupported)
                                    {
                                        if (dataReader.NextResult() == false)
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                        iData.EventList = new List<SiteModel.EventData>();
                                        while (dataReader.Read())
                                        {
                                            var eventData = new SiteModel.EventData();
                                            eventData.EventId = (long)dataReader[0];
                                            eventData.EventName = (string)dataReader[1];
                                            if (Regex.IsMatch(eventData.EventName, @"[^\x20-\x7F]", RegexOptions.None))
                                                throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                                            eventData.EventKind = (int)dataReader[2];
                                            eventData.LifeTicks = (int)dataReader[3];
                                            eventData.QueueSize = (int)dataReader[4];
                                            eventData.GuestAccess = (int)dataReader[5];
                                            iData.EventList.Add(eventData);
                                        }

                                        if (dataReader.NextResult() == false)
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                        long eventSetupTimeTicks = AppClock.getTicks();
                                        long eventSetupTimeSeconds = SystemClock.Seconds;

                                        iData.REventInstances = new List<SiteModel.REventInstance>();
                                        while (dataReader.Read())
                                        {
                                            long instanceId = (long)dataReader[0];
                                            long eventId = (long)dataReader[1];
                                            long serviceId = (long)dataReader[2];
                                            long createdTimeTicks = (long)dataReader[3];
                                            DateTime createdDate = (DateTime)dataReader[4];
                                            bool isNull = (bool)dataReader[5];
                                            bool hasArguments = (bool)dataReader[6];
                                            byte[] arguments = null;
                                            if (dataReader[7] != DBNull.Value)
                                                arguments = (byte[])dataReader[7];
                                            var eventInstance = new SiteModel.REventInstance(instanceId, eventId, serviceId, createdTimeTicks, eventSetupTimeTicks, eventSetupTimeSeconds, createdDate, isNull, hasArguments, arguments);
                                            iData.REventInstances.Add(eventInstance);
                                        }

                                        if (dataReader.NextResult() == false)
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                        iData.QEventInstances = new List<SiteModel.QEventInstance>();
                                        while (dataReader.Read())
                                        {
                                            long instanceId = (long)dataReader[0];
                                            long eventId = (long)dataReader[1];
                                            long serviceId = (long)dataReader[2];
                                            long createdTimeTicks = (long)dataReader[3];
                                            DateTime createdDate = (DateTime)dataReader[4];
                                            bool hasArguments = (bool)dataReader[5];
                                            byte[] arguments = null;
                                            if (dataReader[6] != DBNull.Value)
                                                arguments = (byte[])dataReader[6];
                                            var eventInstance = new SiteModel.QEventInstance(instanceId, eventId, serviceId, createdTimeTicks, eventSetupTimeTicks, eventSetupTimeSeconds, createdDate, hasArguments, arguments);
                                            iData.QEventInstances.Add(eventInstance);
                                        }

                                        if (dataReader.NextResult() == false)
                                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                        iData.PEventInstances = new List<SiteModel.PEventInstance>();
                                        while (dataReader.Read())
                                        {
                                            long instanceId = (long)dataReader[0];
                                            long eventId = (long)dataReader[1];
                                            long serviceId = (long)dataReader[2];
                                            long clientId = (long)dataReader[3];
                                            long createdTimeTicks = (long)dataReader[4];
                                            DateTime createdDate = (DateTime)dataReader[5];
                                            bool hasArguments = (bool)dataReader[6];
                                            byte[] arguments = null;
                                            if (dataReader[7] != DBNull.Value)
                                                arguments = (byte[])dataReader[7];
                                            var eventInstance = new SiteModel.PEventInstance(instanceId, eventId, serviceId, clientId, createdTimeTicks, eventSetupTimeTicks, eventSetupTimeSeconds, createdDate, hasArguments, arguments);
                                            iData.PEventInstances.Add(eventInstance);
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                dataReader.Close();
                            }
                        }
                        else
                        {
                            command = new SqlCommand();
                            command.Connection = Connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_Site_GetServiceList";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            SqlDataReader dataReader = command.ExecuteReader();
                            try
                            {
                                if (dataReader.FieldCount == 0)
                                    throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                iData.SGItems = new List<SiteModel.SGItem>();
                                while (dataReader.Read())
                                {
                                    long serviceId = (long)dataReader[0];

                                    string hostname = (string)dataReader[1];
                                    if (Regex.IsMatch(hostname, @"[^\x20-\x7F]", RegexOptions.None))
                                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                                    
                                    string version = (string)dataReader[2];
                                    if (version.Length > 0 && Regex.IsMatch(version, @"[^\x20-\x7F]", RegexOptions.None))
                                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                                    
                                    bool enabled = (bool)dataReader[3];
                                    var item = new SiteModel.SGItem(serviceId, hostname, version, enabled);
                                    iData.SGItems.Add(item);
                                }
                            }
                            finally
                            {
                                dataReader.Close();
                            }

                            if (iData.SiteKind == Constants.SiteKind.SingleService && iData.SGItems.Count != 1)
                                throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                        }
                    }
                    else
                    {
                        command = new SqlCommand();
                        command.Connection = Connection;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "Softnet_Site_GetServiceList";

                        command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                        command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                        command.Parameters["@SiteId"].Value = siteId;                       

                        SqlDataReader dataReader = command.ExecuteReader();
                        try
                        {
                            if (dataReader.FieldCount == 0)
                                throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                            iData.SGItems = new List<SiteModel.SGItem>();
                            while (dataReader.Read())
                            {
                                long serviceId = (long)dataReader[0];

                                string hostname = (string)dataReader[1];
                                if (Regex.IsMatch(hostname, @"[^\x20-\x7F]", RegexOptions.None))
                                    throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                string version = (string)dataReader[2];
                                if(version.Length > 0 && Regex.IsMatch(version, @"[^\x20-\x7F]", RegexOptions.None))
                                    throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                                bool enabled = (bool)dataReader[3];
                                var item = new SiteModel.SGItem(serviceId, hostname, version, enabled);
                                iData.SGItems.Add(item);
                            }
                        }
                        finally
                        {
                            dataReader.Close();
                        }

                        if (iData.SiteKind == Constants.SiteKind.SingleService && iData.SGItems.Count != 1)
                            throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (FormatException)
            {
                throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
            }
            catch (ArgumentException)
            {
                throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);
            }
        }

        public static void Site_ConstructRBSite(long siteId, SiteStructure siteStructure, string ssHash)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_RBSiteBegin";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_AddRole";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@RoleName", SqlDbType.NVarChar, 256);
                    command.Parameters["@RoleName"].Direction = ParameterDirection.Input;

                    command.Parameters.Add("@Index", SqlDbType.Int);
                    command.Parameters["@Index"].Direction = ParameterDirection.Input;

                    int index = 0;
                    foreach (string roleName in siteStructure.getRoles())
                    {
                        index++;
                        command.Parameters["@RoleName"].Value = roleName;
                        command.Parameters["@Index"].Value = index;
                        command.ExecuteNonQuery();
                    }

                    if (siteStructure.eventsSupported())
                    {
                        SqlCommand commandAddEventRole = new SqlCommand();
                        commandAddEventRole.Connection = connection;
                        commandAddEventRole.CommandType = CommandType.StoredProcedure;
                        commandAddEventRole.CommandText = "Softnet_SiteConstruction_AddEventRole";

                        commandAddEventRole.Parameters.Add("@EventId", SqlDbType.BigInt);
                        commandAddEventRole.Parameters["@EventId"].Direction = ParameterDirection.Input;

                        commandAddEventRole.Parameters.Add("@SiteId", SqlDbType.BigInt);
                        commandAddEventRole.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                        commandAddEventRole.Parameters["@SiteId"].Value = siteId;

                        commandAddEventRole.Parameters.Add("@RoleName", SqlDbType.NVarChar, 256);
                        commandAddEventRole.Parameters["@RoleName"].Direction = ParameterDirection.Input;

                        if (siteStructure.containsREvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddREvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.REvent evt in siteStructure.getREvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                command.ExecuteNonQuery();

                                if (evt.roles != null)
                                {
                                    long eventId = (long)command.Parameters["@EventId"].Value;
                                    foreach (string roleName in evt.roles)
                                    {
                                        commandAddEventRole.Parameters["@EventId"].Value = eventId;
                                        commandAddEventRole.Parameters["@RoleName"].Value = roleName;
                                        commandAddEventRole.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        if (siteStructure.containsQEvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddQEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@QueueSize", SqlDbType.Int);
                            command.Parameters["@QueueSize"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.QEvent evt in siteStructure.getQEvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                command.Parameters["@QueueSize"].Value = evt.queueSize;
                                command.ExecuteNonQuery();

                                if (evt.roles != null)
                                {
                                    long eventId = (long)command.Parameters["@EventId"].Value;
                                    foreach (string roleName in evt.roles)
                                    {
                                        commandAddEventRole.Parameters["@EventId"].Value = eventId;
                                        commandAddEventRole.Parameters["@RoleName"].Value = roleName;
                                        commandAddEventRole.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        if (siteStructure.containsPEvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddPEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            foreach (Softnet.Tracker.SiteModel.PEvent evt in siteStructure.getPEvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_RBSiteEnd";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@ServiceType", SqlDbType.NVarChar, 256);
                    command.Parameters["@ServiceType"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceType"].Value = siteStructure.getServiceType();

                    command.Parameters.Add("@ContractAuthor", SqlDbType.NVarChar, 256);
                    command.Parameters["@ContractAuthor"].Direction = ParameterDirection.Input;
                    command.Parameters["@ContractAuthor"].Value = siteStructure.getContractAuthor();

                    command.Parameters.Add("@SSHash", SqlDbType.VarChar, 64);
                    command.Parameters["@SSHash"].Direction = ParameterDirection.Input;
                    command.Parameters["@SSHash"].Value = ssHash;

                    if (siteStructure.getGuestSupport() > 0)
                    {
                        command.Parameters.Add("@GuestSupported", SqlDbType.Bit);
                        command.Parameters["@GuestSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@GuestSupported"].Value = true;

                        if (siteStructure.getGuestSupport() == 2)
                        {
                            command.Parameters.Add("@StatelessGuestSupported", SqlDbType.Bit);
                            command.Parameters["@StatelessGuestSupported"].Direction = ParameterDirection.Input;
                            command.Parameters["@StatelessGuestSupported"].Value = true;
                        }
                    }

                    if (siteStructure.eventsSupported())
                    {
                        command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                        command.Parameters["@EventsSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@EventsSupported"].Value = true;
                    }

                    if (siteStructure.containsOwnerRole())
                    {
                        command.Parameters.Add("@OwnerDefaultRole", SqlDbType.NVarChar, 256);
                        command.Parameters["@OwnerDefaultRole"].Direction = ParameterDirection.Input;
                        command.Parameters["@OwnerDefaultRole"].Value = siteStructure.getOwnerRole();
                    }

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Site_ConstructUBSite(long siteId, SiteStructure siteStructure, string ssHash)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_UBSiteBegin";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.ExecuteNonQuery();

                    if (siteStructure.eventsSupported())
                    {
                        if (siteStructure.containsREvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddREvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.REvent evt in siteStructure.getREvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                command.ExecuteNonQuery();
                            }
                        }

                        if (siteStructure.containsQEvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddQEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@QueueSize", SqlDbType.Int);
                            command.Parameters["@QueueSize"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@GuestAccess", SqlDbType.Int);
                            command.Parameters["@GuestAccess"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@EventId", SqlDbType.BigInt);
                            command.Parameters["@EventId"].Direction = ParameterDirection.Output;

                            foreach (Softnet.Tracker.SiteModel.QEvent evt in siteStructure.getQEvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                command.Parameters["@GuestAccess"].Value = evt.guestAccess;
                                command.Parameters["@QueueSize"].Value = evt.queueSize;
                                command.ExecuteNonQuery();
                            }
                        }

                        if (siteStructure.containsPEvents())
                        {
                            command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "Softnet_SiteConstruction_AddPEvent";

                            command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                            command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                            command.Parameters["@SiteId"].Value = siteId;

                            command.Parameters.Add("@EventName", SqlDbType.NVarChar, 256);
                            command.Parameters["@EventName"].Direction = ParameterDirection.Input;

                            command.Parameters.Add("@LifeTime", SqlDbType.Int);
                            command.Parameters["@LifeTime"].Direction = ParameterDirection.Input;

                            foreach (Softnet.Tracker.SiteModel.PEvent evt in siteStructure.getPEvents())
                            {
                                command.Parameters["@EventName"].Value = evt.name;
                                command.Parameters["@LifeTime"].Value = evt.lifeTime;
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_UBSiteEnd";

                    command.Parameters.Add("@SiteId", SqlDbType.BigInt);
                    command.Parameters["@SiteId"].Direction = ParameterDirection.Input;
                    command.Parameters["@SiteId"].Value = siteId;

                    command.Parameters.Add("@ServiceType", SqlDbType.NVarChar, 256);
                    command.Parameters["@ServiceType"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceType"].Value = siteStructure.getServiceType();

                    command.Parameters.Add("@ContractAuthor", SqlDbType.NVarChar, 256);
                    command.Parameters["@ContractAuthor"].Direction = ParameterDirection.Input;
                    command.Parameters["@ContractAuthor"].Value = siteStructure.getContractAuthor();

                    command.Parameters.Add("@SSHash", SqlDbType.VarChar, 64);
                    command.Parameters["@SSHash"].Direction = ParameterDirection.Input;
                    command.Parameters["@SSHash"].Value = ssHash;

                    if (siteStructure.getGuestSupport() > 0)
                    {
                        command.Parameters.Add("@GuestSupported", SqlDbType.Bit);
                        command.Parameters["@GuestSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@GuestSupported"].Value = true;

                        if (siteStructure.getGuestSupport() == 2)
                        {
                            command.Parameters.Add("@StatelessGuestSupported", SqlDbType.Bit);
                            command.Parameters["@StatelessGuestSupported"].Direction = ParameterDirection.Input;
                            command.Parameters["@StatelessGuestSupported"].Value = true;
                        }
                    }

                    if (siteStructure.eventsSupported())
                    {
                        command.Parameters.Add("@EventsSupported", SqlDbType.Bit);
                        command.Parameters["@EventsSupported"].Direction = ParameterDirection.Input;
                        command.Parameters["@EventsSupported"].Value = true;
                    }

                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static void Site_GetSSData(long serviceId, Softnet.Tracker.SiteModel.SSRawData ssRawData)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand command = new SqlCommand();
                    command.Connection = Connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Softnet_SiteConstruction_GetSSData";

                    command.Parameters.Add("@ServiceId", SqlDbType.BigInt);
                    command.Parameters["@ServiceId"].Direction = ParameterDirection.Input;
                    command.Parameters["@ServiceId"].Value = serviceId;

                    command.Parameters.Add("@SSXml", SqlDbType.NVarChar, 4000);
                    command.Parameters["@SSXml"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@SSHash", SqlDbType.VarChar, 64);
                    command.Parameters["@SSHash"].Direction = ParameterDirection.Output;

                    command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    command.Parameters["@ReturnValue"].Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();
                    int errorCode = (int)command.Parameters["@ReturnValue"].Value;
                    if (errorCode == -1)
                        throw new SoftnetException(ErrorCodes.RESTART);

                    if (command.Parameters["@SSXml"].Value == DBNull.Value || command.Parameters["@SSHash"].Value == DBNull.Value)
                        throw new SoftnetException(ErrorCodes.DATA_INTEGRITY_ERROR);

                    ssRawData.xml = (string)command.Parameters["@SSXml"].Value;
                    ssRawData.hash = (string)command.Parameters["@SSHash"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }            
        }

        public static void Controller_CleanExpiredEvents()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand Command = new SqlCommand();
                    Command.Connection = Connection;
                    Command.CommandType = CommandType.StoredProcedure;
                    Command.CommandText = "Softnet_Controller_CleanExpiredEvents";

                    Command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static long Clock_AddTick()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand Command = new SqlCommand();
                    Command.Connection = Connection;
                    Command.CommandType = CommandType.StoredProcedure;
                    Command.CommandText = "Softnet_Clock_AddTick";

                    Command.Parameters.Add("@Ticks", SqlDbType.BigInt);
                    Command.Parameters["@Ticks"].Direction = ParameterDirection.Output;

                    Command.ExecuteNonQuery();
                    return (long)Command.Parameters["@Ticks"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        public static long Clock_GetTicks()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Softnet"].ConnectionString;
                using (SqlConnection Connection = new SqlConnection(connectionString))
                {
                    Connection.Open();

                    SqlCommand Command = new SqlCommand();
                    Command.Connection = Connection;
                    Command.CommandType = CommandType.StoredProcedure;
                    Command.CommandText = "Softnet_Clock_GetTicks";

                    Command.Parameters.Add("@Ticks", SqlDbType.BigInt);
                    Command.Parameters["@Ticks"].Direction = ParameterDirection.Output;

                    Command.ExecuteNonQuery();
                    return (long)Command.Parameters["@Ticks"].Value;
                }
            }
            catch (SqlException ex)
            {
                throw new SoftnetException(ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new SoftnetException(ex);
            }
        }

        class SiteRole : IEquatable<SiteRole>
        {
            public long id;
            public string name;

            public SiteRole(long id, string name)
            {
                this.id = id;
                this.name = name;
            }

            public bool Equals(SiteRole other)
            {
                return (this.id == other.id);
            }
        }

        class Event : IEquatable<Event>
        {
            public long eventId;
            public int eventKind;
            public string name;

            public Event(long eventId, int eventKind, string name)
            {
                this.eventId = eventId;
                this.eventKind = eventKind;
                this.name = name;
            }

            public bool Equals(Event other)
            {
                return (this.eventId == other.eventId);
            }
        }        
    }
}
