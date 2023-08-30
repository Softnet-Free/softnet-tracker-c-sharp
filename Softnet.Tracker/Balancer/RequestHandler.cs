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
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

using Softnet.Tracker.Core;
using Softnet.ServerKit;
using Softnet.Asn;

namespace Softnet.Tracker.Balancer
{
    class RequestHandler
    {
        public static string s_ServerName = null;
        public static void SetServerName(string name)
        {
            s_ServerName = name.ToLower();
        }

        public RequestHandler(Socket socket, SocketAsyncEventArgs saea, SaeaPool saeaPool)
        {            
            m_Socket = socket;
            m_Saea = saea;
            m_SaeaPool = saeaPool;
            m_Buffer = new byte[128];
        }

        Socket m_Socket;
        SaeaPool m_SaeaPool;
        SocketAsyncEventArgs m_Saea;
        object saea_mutex = new object();
        byte[] m_Buffer;
        int m_BytesTransferredTotal = 0;
        IPEndPoint m_LocalIPEndPoint;
        ScheduledTask m_TimeoutControlTask;

        public void Execute()
        {
            m_TimeoutControlTask = new ScheduledTask(TimeoutExpiredCallback, null);
            m_Saea.Completed += SaeaInputCompleted;

            try
            {
                if (m_Socket.ReceiveAsync(m_Saea) == false)
                {
                    SaeaInputCompleted(null, m_Saea);
                }
                
                TaskScheduler.Add(m_TimeoutControlTask, 30);
            }
            catch (SocketException)
            {
                CloseInput();
            }
        }        

        void TimeoutExpiredCallback(object noData)
        {
            CloseInput();
        }

        void SaeaInputCompleted(object source, SocketAsyncEventArgs saea)
        {
            lock (saea_mutex)
            {
                if (m_Saea == null)
                    return;

                if (m_Saea.SocketError != SocketError.Success)
                {
                    CloseInput();
                }
                else if (m_Saea.BytesTransferred > 0)
                {
                    if (m_BytesTransferredTotal + m_Saea.BytesTransferred <= m_Buffer.Length)
                    {
                        Buffer.BlockCopy(m_Saea.Buffer, m_Saea.Offset, m_Buffer, m_BytesTransferredTotal, m_Saea.BytesTransferred);
                        m_BytesTransferredTotal += m_Saea.BytesTransferred;
                    }
                    else if (m_Buffer.Length <= 512)
                    {
                        byte[] newBuffer = new byte[m_Buffer.Length * 2];
                        Buffer.BlockCopy(m_Buffer, 0, newBuffer, 0, m_BytesTransferredTotal);
                        m_Buffer = newBuffer;
                        Buffer.BlockCopy(m_Saea.Buffer, m_Saea.Offset, m_Buffer, m_BytesTransferredTotal, m_Saea.BytesTransferred);
                        m_BytesTransferredTotal += m_Saea.BytesTransferred;
                    }
                    else
                    {
                        CloseInput();
                        return;
                    }

                    try
                    {
                        m_Saea.SetBuffer(m_Saea.Offset, m_SaeaPool.BufferSize);
                        if (m_Socket.ReceiveAsync(m_Saea) == false)
                        {
                            SaeaInputCompleted(null, m_Saea);
                        }
                    }
                    catch (SocketException)
                    {
                        CloseInput();
                    }
                }
                else if (m_TimeoutControlTask.Cancel())
                {
                    m_Saea.Completed -= SaeaInputCompleted;
                    m_LocalIPEndPoint = (IPEndPoint)m_Socket.LocalEndPoint;
                    ThreadPool.QueueUserWorkItem(delegate
                    { 
                        RecognizeRequest(); 
                    });
                }
            }
        }

        public long SiteId;

        void RecognizeRequest()
        {
            if (m_BytesTransferredTotal <= 2)
            {
                CloseInput();
                return;
            }

            if (m_Buffer[0] != Constants.ProtocolVersion)
            {
                SendResponse(BuildErrorMessage(ErrorCodes.INCOMPATIBLE_PROTOCOL_VERSION));
                return;
            }

            try
            {
                int messageTag = m_Buffer[1];
                if (messageTag == Constants.Balancer.SERVICE_UID)
                {
                    string receivedServerName = null;
                    SequenceDecoder sequence = ASNDecoder.Sequence(m_Buffer, 2);
                    if (sequence.Exists(1))
                        receivedServerName = sequence.UTF8String();
                    Guid serviceUid = ByteConverter.ToGuid(sequence.OctetString(16));
                    sequence.End();

                    if (string.IsNullOrWhiteSpace(receivedServerName))
                        receivedServerName = null;
                    else
                        receivedServerName = receivedServerName.ToLower();

                    if (s_ServerName != null)
                    {
                        if (receivedServerName != null)
                        {
                            if (receivedServerName.Equals(s_ServerName))
                                ResolveServiceUid(serviceUid);
                            else
                                SendResponse(BuildErrorMessage(ErrorCodes.INVALID_SERVER_ENDPOINT));
                        }
                        else
                            ResolveServiceUid(serviceUid);
                    }
                    else
                    {
                        if (receivedServerName != null)
                            SendResponse(BuildErrorMessage(ErrorCodes.CONFIG_ERROR));
                        else
                            ResolveServiceUid(serviceUid);
                    }
                }
                else if (messageTag == Constants.Balancer.CLIENT_S_KEY ||
                    messageTag == Constants.Balancer.CLIENT_SS_KEY ||
                    messageTag == Constants.Balancer.CLIENT_M_KEY ||
                    messageTag == Constants.Balancer.CLIENT_MS_KEY)
                {
                    string receivedServerName = null;
                    SequenceDecoder sequence = ASNDecoder.Sequence(m_Buffer, 2);
                    if (sequence.Exists(1))
                        receivedServerName = sequence.UTF8String();
                    string clientKey = sequence.PrintableString();
                    sequence.End();

                    if (string.IsNullOrWhiteSpace(receivedServerName))
                        receivedServerName = null;
                    else
                        receivedServerName = receivedServerName.ToLower();

                    if (s_ServerName != null)
                    {
                        if (receivedServerName != null)
                        {
                            if (receivedServerName.Equals(s_ServerName))
                            {
                                ResolveClientKey(messageTag, clientKey);
                            }
                            else
                            {
                                SendResponse(BuildErrorMessage(ErrorCodes.INVALID_SERVER_ENDPOINT));
                            }
                        }
                        else
                        {
                            ResolveClientKey(messageTag, clientKey);
                        }
                    }
                    else
                    {
                        if (receivedServerName != null)
                        {
                            SendResponse(BuildErrorMessage(ErrorCodes.CONFIG_ERROR));
                        }
                        else
                        {
                            ResolveClientKey(messageTag, clientKey);
                        }
                    }
                }
                else
                {
                    SendResponse(BuildErrorMessage(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR));
                }
            }
            catch (AsnException)
            {
                SendResponse(BuildErrorMessage(ErrorCodes.ENDPOINT_DATA_FORMAT_ERROR));
            }
        }

        void ResolveServiceUid(Guid serviceUid)
        {
            int errorCode = SoftnetRegistry.Balancer_ResolveServiceUid(serviceUid, this);
            if (errorCode != 0)
            {
                SendResponse(BuildErrorMessage(errorCode));
                return;
            }

            if (m_LocalIPEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                byte[] response = new byte[22];
                byte[] hash = ByteConverter.GetBytes(Fnv1a.Get32BitHash(m_Buffer, 0, m_BytesTransferredTotal));
                response[0] = hash[0];
                response[1] = hash[1];
                response[2] = hash[2];
                response[3] = hash[3];
                response[4] = Constants.Balancer.SUCCESS;
                response[5] = Constants.Balancer.IP_V6;
                byte[] ipBytes = m_LocalIPEndPoint.Address.GetAddressBytes();
                Buffer.BlockCopy(ipBytes, 0, response, 6, 16);

                byte[] hash2 = ByteConverter.GetBytes(Fnv1a.Get32BitHash(response));
                response[0] = hash2[0];
                response[1] = hash2[1];
                response[2] = hash2[2];
                response[3] = hash2[3];

                SendResponse(response);
            }
            else
            {
                byte[] response = new byte[10];
                byte[] hash = ByteConverter.GetBytes(Fnv1a.Get32BitHash(m_Buffer, 0, m_BytesTransferredTotal));
                
                response[0] = hash[0];
                response[1] = hash[1];
                response[2] = hash[2];
                response[3] = hash[3];
                response[4] = Constants.Balancer.SUCCESS;
                response[5] = Constants.Balancer.IP_V4;
                byte[] ipBytes = m_LocalIPEndPoint.Address.GetAddressBytes();
                Buffer.BlockCopy(ipBytes, 0, response, 6, 4);

                byte[] hash2 = ByteConverter.GetBytes(Fnv1a.Get32BitHash(response));
                response[0] = hash2[0];
                response[1] = hash2[1];
                response[2] = hash2[2];
                response[3] = hash2[3];

                SendResponse(response);
            }
        }

        void ResolveClientKey(int clientCategory, string clientKey)
        {
            if (clientKey.Length < 4 || clientKey.Length > 32)
            {
                SendResponse(BuildErrorMessage(ErrorCodes.CLIENT_NOT_REGISTERED));
                return;
            }

            int errorCode = SoftnetRegistry.Balancer_ResolveClientKey(clientCategory, clientKey, this);
            if (errorCode != 0)
            {
                SendResponse(BuildErrorMessage(errorCode));
                return;
            }

            if (m_LocalIPEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                byte[] response = new byte[22];
                byte[] hash = ByteConverter.GetBytes(Fnv1a.Get32BitHash(m_Buffer, 0, m_BytesTransferredTotal));
                response[0] = hash[0];
                response[1] = hash[1];
                response[2] = hash[2];
                response[3] = hash[3];
                response[4] = Constants.Balancer.SUCCESS;
                response[5] = Constants.Balancer.IP_V6;
                byte[] ipBytes = m_LocalIPEndPoint.Address.GetAddressBytes();
                Buffer.BlockCopy(ipBytes, 0, response, 6, 16);

                byte[] hash2 = ByteConverter.GetBytes(Fnv1a.Get32BitHash(response));
                response[0] = hash2[0];
                response[1] = hash2[1];
                response[2] = hash2[2];
                response[3] = hash2[3];

                SendResponse(response);
            }
            else
            {
                byte[] response = new byte[10];
                byte[] hash = ByteConverter.GetBytes(Fnv1a.Get32BitHash(m_Buffer, 0, m_BytesTransferredTotal));
                response[0] = hash[0];
                response[1] = hash[1];
                response[2] = hash[2];
                response[3] = hash[3];
                response[4] = Constants.Balancer.SUCCESS;
                response[5] = Constants.Balancer.IP_V4;
                byte[] ipBytes = m_LocalIPEndPoint.Address.GetAddressBytes();
                Buffer.BlockCopy(ipBytes, 0, response, 6, 4);

                byte[] hash2 = ByteConverter.GetBytes(Fnv1a.Get32BitHash(response));
                response[0] = hash2[0];
                response[1] = hash2[1];
                response[2] = hash2[2];
                response[3] = hash2[3];

                SendResponse(response);
            }
        }

        byte[] BuildErrorMessage(int errorCode)
        {
            byte[] response = new byte[7];
            byte[] hash = ByteConverter.GetBytes(Fnv1a.Get32BitHash(m_Buffer, 0, m_BytesTransferredTotal));
            response[0] = hash[0];
            response[1] = hash[1];
            response[2] = hash[2];
            response[3] = hash[3];
            response[4] = Constants.Balancer.ERROR;
            ByteConverter.WriteAsInt16(errorCode, response, 5);

            byte[] hash2 = ByteConverter.GetBytes(Fnv1a.Get32BitHash(response));
            response[0] = hash2[0];
            response[1] = hash2[1];
            response[2] = hash2[2];
            response[3] = hash2[3];

            return response;
        }

        void SendResponse(byte[] message)
        {
            m_Buffer = message;
            m_BytesTransferredTotal = 0;

            Buffer.BlockCopy(m_Buffer, 0, m_Saea.Buffer, m_Saea.Offset, m_Buffer.Length);
            m_Saea.SetBuffer(m_Saea.Offset, m_Buffer.Length);
            m_Saea.Completed += SaeaOutputCompleted;

            try
            {
                if (m_Socket.SendAsync(m_Saea) == false)
                {
                    SaeaOutputCompleted(null, m_Saea);
                }
            }
            catch (SocketException)
            {
                CloseOutput();
            }
        }

        void SaeaOutputCompleted(object source, SocketAsyncEventArgs saea)
        {
            if (m_Saea.SocketError != SocketError.Success)
            {
                CloseOutput();
            }
            else if (m_BytesTransferredTotal + m_Saea.BytesTransferred < m_Buffer.Length)
            {
                m_BytesTransferredTotal += m_Saea.BytesTransferred;
                Buffer.BlockCopy(m_Buffer, m_BytesTransferredTotal, m_Saea.Buffer, m_Saea.Offset, m_Buffer.Length - m_BytesTransferredTotal);
                m_Saea.SetBuffer(m_Saea.Offset, m_Buffer.Length - m_BytesTransferredTotal);

                try
                {
                    if (m_Socket.SendAsync(m_Saea) == false)
                    {
                        SaeaOutputCompleted(null, m_Saea);
                    }
                }
                catch (SocketException)
                {
                    CloseOutput();
                }
            }
            else
            {
                CloseOutput();
            }                        
        }

        void CloseInput()
        {
            m_TimeoutControlTask.Cancel();
            m_Socket.Close();
            lock (saea_mutex)
            {
                if (m_Saea != null)
                {
                    m_Saea.Completed -= SaeaInputCompleted;
                    m_SaeaPool.Add(m_Saea);
                    m_Saea = null;
                }
            }
        }

        void CloseOutput()
        { 
            m_Socket.Close();
            m_Saea.Completed -= SaeaOutputCompleted;
            m_SaeaPool.Add(m_Saea);
            m_Saea = null;
        }
    }
}
