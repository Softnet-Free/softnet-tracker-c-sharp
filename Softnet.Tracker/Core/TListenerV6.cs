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
    class TListenerV6
    {
        static Socket s_ServerSocket;
        static Thread s_Thread;
        static SaeaPool s_SaeaPool;
        static bool s_running = false;

        public static void Init(int saeaPoolSize)
        {
            s_SaeaPool = new SaeaPool();
            s_SaeaPool.Init(saeaPoolSize, 512);
        }

        public static void Start()
        {
            try
            {
                s_ServerSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                s_ServerSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, Constants.Tracker_TcpPort));
                s_ServerSocket.Listen(100);

                s_running = true;
                s_Thread = new Thread(new ThreadStart(ThreadProc));
                s_Thread.Start();
            }
            catch (SocketException e)
            {
                s_ServerSocket = null;
                throw new SoftnetException(ErrorCodes.CONFIG_ERROR, e.Message);
            }
        }

        public static void Stop()
        {
            s_running = false;
            if (s_ServerSocket != null)
                s_ServerSocket.Close();
        }

        static void ThreadProc()
        {
            while (s_running)
            {
                SocketAsyncEventArgs saea = s_SaeaPool.Get();
                if (saea != null)
                {
                    try
                    {
                        Socket socket = s_ServerSocket.Accept();
                        ClearChannelAcceptor connectionAcceptor = new ClearChannelAcceptor(socket, saea, s_SaeaPool);
                        connectionAcceptor.Exec();
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode != SocketError.Interrupted)
                            AppLog.WriteLine(e.Message);
                        s_SaeaPool.Add(saea);
                    }
                    catch (ObjectDisposedException)
                    {
                        s_SaeaPool.Add(saea);
                        break;
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
