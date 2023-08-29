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
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Softnet.ServerKit;

namespace Softnet.Tracker.Core
{
    class ClearChannelAcceptor
    {
        public ClearChannelAcceptor(Socket socket, SocketAsyncEventArgs saea, SaeaPool saeaPool)
        {
            m_Socket = socket;
            m_Saea = saea;
            m_SaeaPool = saeaPool;
        }

        Socket m_Socket;
        SocketAsyncEventArgs m_Saea;
        SaeaPool m_SaeaPool;
        object saea_mutex = new object();
        ScheduledTask m_TimeoutControlTask;
        bool m_HeaderReceived = false;

        public void Exec()
        {
            m_Saea.SetBuffer(m_Saea.Offset, 2);
            m_Saea.Completed += SaeaInputCompleted;
            m_TimeoutControlTask = new ScheduledTask(TimeoutExpiredCallback, null);

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
                Dispose();
            }
        }

        void TimeoutExpiredCallback(object noData)
        {
            Dispose();
        }

        void SaeaInputCompleted(object caller, SocketAsyncEventArgs saea)
        {
            lock (saea_mutex)
            {
                if (m_Saea == null)
                    return;

                if (m_Saea.SocketError != SocketError.Success)
                {
                    Dispose();
                }
                else if (m_Saea.BytesTransferred > 0)
                {
                    if (m_HeaderReceived == false)
                    {
                        if (m_Saea.BytesTransferred == 2)
                        {
                            if (m_TimeoutControlTask.Cancel())
                            {
                                m_HeaderReceived = true;
                                ThreadPool.QueueUserWorkItem(delegate
                                {
                                    ProcessHeader();
                                });
                            }
                        }
                        else
                        {
                            Dispose();
                        }
                    }
                    else
                    {
                        m_Saea.SetBuffer(m_Saea.Offset, m_SaeaPool.BufferSize);
                        if (m_Socket.ReceiveAsync(m_Saea) == false)
                            SaeaInputCompleted(null, m_Saea);
                    }
                }
                else
                {
                    Dispose();
                }
            }
        }

        void ProcessHeader()
        {
            try
            {
                byte[] buffer = m_Saea.Buffer;
                int offset = m_Saea.Offset;

                int protocolVersion = buffer[offset];
                if (protocolVersion == Constants.ProtocolVersion)
                {
                    int moduleId = buffer[offset + 1];
                    if (moduleId == Constants.Client.EndpointType)
                    {
                        m_Saea.Completed -= SaeaInputCompleted;
                        m_Saea.SetBuffer(m_Saea.Offset, m_SaeaPool.BufferSize);

                        var channel = new Softnet.Tracker.ClientModel.ClearChannel(m_Socket, m_Saea, m_SaeaPool);
                        var clientInstaller = new Softnet.Tracker.ClientModel.ClientInstaller(channel);
                        clientInstaller.Start();
                    }
                    else if (moduleId == Constants.Service.EndpointType)
                    {
                        m_Saea.Completed -= SaeaInputCompleted;
                        m_Saea.SetBuffer(m_Saea.Offset, m_SaeaPool.BufferSize);

                        var channel = new Softnet.Tracker.ServiceModel.ClearChannel(m_Socket, m_Saea, m_SaeaPool);
                        var serviceInstaller = new Softnet.Tracker.ServiceModel.ServiceInstaller(channel);
                        serviceInstaller.Start();
                    }
                    else
                    {
                        throw new FormatException();
                    }
                }
                else
                {
                    var message = MsgBuilder.CreateErrorMessage(0, 0, ErrorCodes.INCOMPATIBLE_PROTOCOL_VERSION);
                    m_Socket.Send(message.buffer);

                    m_Saea.SetBuffer(m_Saea.Offset, m_SaeaPool.BufferSize);
                    if (m_Socket.ReceiveAsync(m_Saea) == false)
                        SaeaInputCompleted(null, m_Saea);
                }
            }
            catch (SocketException)
            {
                Dispose();
            }
            catch (FormatException)
            {
                Dispose();
            }
            catch (ObjectDisposedException) { }
        }

        void Dispose()
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
    }
}
