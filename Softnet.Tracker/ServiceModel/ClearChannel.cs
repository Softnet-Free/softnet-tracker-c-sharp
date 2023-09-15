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
using System.Net;
using System.Net.Sockets;

using Softnet.Tracker.Core;
using Softnet.ServerKit;
using Softnet.Asn;

namespace Softnet.Tracker.ServiceModel
{
    public class ClearChannel : IChannel
    {
        public ClearChannel(Socket socket, SocketAsyncEventArgs saea, SaeaPool saeaPool)
        {
            m_MsgSocket = new MsgSocket(socket, saea, saeaPool);
            m_Completed = false;
        }

        MsgSocket m_MsgSocket;
        Action<byte[]>[] m_Modules;
        bool m_Completed;

        Action ChannelEstablishedCallback;
        Action ChannelCompletedCallback;

        public AddressFamily GetAddressFamily()
        {
            return m_MsgSocket.GetAddressFamily();
        }

        public void Init(ServiceInstaller serviceInstaller, Action establishedCallback)
        {
            m_ServiceInstaller = serviceInstaller;
            ChannelEstablishedCallback = establishedCallback;
            m_AuthData = new AuthData();
            m_Modules = new Action<byte[]>[16];
            m_Modules[Constants.Service.ChannelMonitor.ModuleId] = OnMonitorMessageReceived;
        }

        public void SetCompletedCallback(Action callback)
        {
            ChannelCompletedCallback = callback;
        }

        public void RegisterModule(int moduleId, Action<byte[]> messageReceivedCallback)
        {
            m_Modules[moduleId] = messageReceivedCallback;
        }

        public void RemoveModule(int moduleId)
        {
            m_Modules[moduleId] = null;
        }

        public void Send(SoftnetMessage message)
        {
            m_MsgSocket.Send(message);
        }

        public void Start()
        {
            m_MsgSocket.MessageReceivedHandler = OnHandshakeMessageReceived;
            m_MsgSocket.InputCompletedHandler = OnSocketInputShutdown;
            m_MsgSocket.NetworkErrorHandler = OnSocketNetworkError;
            m_MsgSocket.FormatErrorHandler = OnSocketFormatError;
            m_MsgSocket.MinLength = 2;
            m_MsgSocket.MaxLength = 256;

            m_MsgSocket.Start();
        }

        void OnMessageReceived(byte[] message)
        {
            try
            {
                m_ExpirationTime = SystemClock.Seconds + 395L;
                int moduleId = message[0];
                if (moduleId <= 15 && m_Modules[moduleId] != null)
                {
                    m_Modules[moduleId](message);
                }
                else 
                {
                    OnModuleError(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR);
                }
            }
            catch (AsnException)
            {
                OnModuleError(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR);
            }
            catch (FormatException)
            {
                OnModuleError(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR);
            }
            catch (SoftnetException ex)
            {
                OnModuleError(ex.ErrorCode);
            }
        }

        void OnMonitorMessageReceived(byte[] message)
        {
            byte messageTag = message[1];
            if (messageTag == Constants.Service.ChannelMonitor.PING)
            {
                m_MsgSocket.Send(MsgBuilder.Create(Constants.Service.ChannelMonitor.ModuleId, Constants.Service.ChannelMonitor.PONG));
            }
            else if (messageTag == Constants.Client.ChannelMonitor.KEEP_ALIVE) { }
            else
            {
                OnFormatError();
            }
        }

        #region Termination

        long m_ExpirationTime;

        public bool IsAlive(long currentTime)
        {
            if (currentTime < m_ExpirationTime)
                return true;

            if (m_Completed == false)
            {
                m_MsgSocket.Close();
                ChannelCompletedCallback();
            }
            return false;
        }

        public void Shutdown(int errorCode)
        {
            m_Completed = true;
            m_ExpirationTime = 0;

            m_MsgSocket.Send(MsgBuilder.CreateErrorMessage(Constants.Service.Channel.ModuleId, Constants.Service.Channel.ERROR, errorCode));
            
            m_CompletionTimeoutControlTask = new ScheduledTask(OnCompletionTimeoutExpired, null);
            TaskScheduler.Add(m_CompletionTimeoutControlTask, 20);
        }

        public void Close()
        {
            m_Completed = true;
            m_ExpirationTime = 0;

            m_MsgSocket.Close();
        }

        void OnSocketInputShutdown()
        {
            m_Completed = true;
            m_ExpirationTime = 0;

            if (m_CompletionTimeoutControlTask != null)
                m_CompletionTimeoutControlTask.Cancel();
            else
                ChannelCompletedCallback();
        }

        void OnSocketNetworkError()
        {
            m_Completed = true;
            m_ExpirationTime = 0;

            ChannelCompletedCallback();
        }

        void OnSocketFormatError()
        {
            m_Completed = true;
            m_ExpirationTime = 0;

            ChannelCompletedCallback();
        }

        void OnFormatError()
        {
            m_Completed = true;
            m_ExpirationTime = 0;

            m_MsgSocket.Close();
            ChannelCompletedCallback();
        }

        void OnModuleError(int errorCode)
        {
            Shutdown(errorCode);
            ChannelCompletedCallback();
        }

        ScheduledTask m_CompletionTimeoutControlTask = null;
        void OnCompletionTimeoutExpired(object noData)
        {
            m_MsgSocket.Close();
        }

        #endregion Termination

        // ----- handshake -----------------------------------------        
        enum HandshakePhase
        {
            PHASE_1, PHASE_2, COMPLETED
        }
        HandshakePhase m_HandshakePhase = HandshakePhase.PHASE_1;

        ServiceInstaller m_ServiceInstaller;
        AuthData m_AuthData;

        void ProcessMessage_Restore(byte[] message)
        {
            var sequence = ASNDecoder.Sequence(message, 2);
            m_AuthData.ServiceUid = sequence.OctetString(16);
            m_ServiceInstaller.ChannelId = sequence.OctetString(16);
            sequence.End();

            int errorCode = SoftnetRegistry.Service_GetIData(m_ServiceInstaller, m_AuthData);
            if (errorCode == 0)
            {
                m_AuthData.SecurityKey1 = Randomizer.ByteString(20);
                m_ServiceInstaller.ChannelRestored = true;

                SoftnetMessage response = EncodeMessage_SaltAndKey1();
                m_MsgSocket.Send(response);

                m_HandshakePhase = HandshakePhase.PHASE_2;
            }
            else
            {
                m_MsgSocket.Send(MsgBuilder.CreateErrorMessage(Constants.Service.Channel.ModuleId, Constants.Service.Channel.ERROR, errorCode));
                m_HandshakePhase = HandshakePhase.COMPLETED;
            }
        }

        void ProcessMessage_Open(byte[] message)
        {
            var sequence = ASNDecoder.Sequence(message, 2);
            m_AuthData.ServiceUid = sequence.OctetString(16);
            sequence.End();

            int errorCode = SoftnetRegistry.Service_GetIData(m_ServiceInstaller, m_AuthData);
            if (errorCode == 0)
            {
                m_AuthData.SecurityKey1 = Randomizer.ByteString(20);
                m_ServiceInstaller.ChannelRestored = false;

                SoftnetMessage response = EncodeMessage_SaltAndKey1();
                m_MsgSocket.Send(response);                

                m_HandshakePhase = HandshakePhase.PHASE_2;
            }
            else
            {
                m_MsgSocket.Send(MsgBuilder.CreateErrorMessage(Constants.Service.Channel.ModuleId, Constants.Service.Channel.ERROR, errorCode));
                m_HandshakePhase = HandshakePhase.COMPLETED;
            }
        }

        void ProcessMessage_HashAndKey2(byte[] message)
        {
            var sequence = ASNDecoder.Sequence(message, 2);
            byte[] receivedPasswordHash = sequence.OctetString(20);
            byte[] securityKey2 = sequence.OctetString(20);
            sequence.End();

            byte[] validHash = PasswordHash.Compute(m_AuthData.SecurityKey1, securityKey2, m_AuthData.SaltedPassword);
            if (receivedPasswordHash.SequenceEqual(validHash))
            {
                if (m_ServiceInstaller.ChannelRestored)
                {
                    m_MsgSocket.Send(MsgBuilder.Create(Constants.Service.Channel.ModuleId, Constants.Service.Channel.RESTORE_OK));

                    m_MsgSocket.MessageReceivedHandler = OnMessageReceived;
                    m_MsgSocket.MaxLength = 4194304;
                    m_ServiceInstaller = null;
                    m_AuthData = null;
                    m_ExpirationTime = SystemClock.Seconds + 395L;

                    ChannelEstablishedCallback();
                }
                else
                {
                    m_ServiceInstaller.ChannelId = ByteConverter.GetBytes(Guid.NewGuid());
                    m_MsgSocket.Send(EncodeMessage_OpenOk());

                    m_MsgSocket.MessageReceivedHandler = OnMessageReceived;
                    m_MsgSocket.MaxLength = 4194304;
                    m_ServiceInstaller = null;
                    m_AuthData = null;
                    m_ExpirationTime = SystemClock.Seconds + 395L;

                    ChannelEstablishedCallback();
                }
            }
            else
            {
                m_MsgSocket.Send(MsgBuilder.CreateErrorMessage(Constants.Service.Channel.ModuleId, Constants.Service.Channel.ERROR, ErrorCodes.PASSWORD_NOT_MATCHED));
                m_HandshakePhase = HandshakePhase.COMPLETED;
            }
        }

        SoftnetMessage EncodeMessage_SaltAndKey1()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder sequence = asnEncoder.Sequence;
            sequence.OctetString(m_AuthData.Salt);
            sequence.OctetString(m_AuthData.SecurityKey1);

            return MsgBuilder.Create(Constants.Service.Channel.ModuleId, Constants.Service.Channel.SALT_AND_KEY1, asnEncoder);
        }

        SoftnetMessage EncodeMessage_OpenOk()
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            SequenceEncoder sequence = asnEncoder.Sequence;
            sequence.OctetString(m_ServiceInstaller.ChannelId);

            return MsgBuilder.Create(Constants.Service.Channel.ModuleId, Constants.Service.Channel.OPEN_OK, asnEncoder);
        }

        void OnHandshakeMessageReceived(byte[] message)
        {
            try
            {
                if (m_HandshakePhase == HandshakePhase.PHASE_1)
                {
                    int messageTag = message[1];
                    if (messageTag == Constants.Service.Channel.RESTORE)
                    {
                        ProcessMessage_Restore(message);
                    }
                    else if (messageTag == Constants.Service.Channel.OPEN)
                    {
                        ProcessMessage_Open(message);
                    }
                    else
                    {
                        OnFormatError();
                    }
                }
                else if (m_HandshakePhase == HandshakePhase.PHASE_2)
                {
                    if (message[1] == Constants.Service.Channel.HASH_AND_KEY2)
                    {
                        ProcessMessage_HashAndKey2(message);
                    }
                    else
                    {
                        OnFormatError();
                    }
                }
                else // m_HandshakePhase == HandshakePhase.COMPLETED
                {
                    OnFormatError();
                }
            }
            catch(AsnException)
            {
                OnFormatError();
            }
            catch (FormatException)
            {
                OnFormatError();
            }
        }
    }
}
