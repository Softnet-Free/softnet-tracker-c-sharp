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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Configuration;

using Softnet.ServerKit;
using Softnet.Tracker.Core;
using Softnet.Tracker.Balancer;
using Softnet.Tracker.Management;

namespace Softnet.Tracker
{
    public partial class ServerRoot : ServiceBase
    {
        public ServerRoot()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            AppLog.WriteLine("Service started!");
            try
            {
                TaskScheduler.Start();
                MgtAgent.Start();
                AppClock.start();
                EventCleaner.Start();
                Monitor.Start(60);
                NetworkResources.Load();

                TrackerConfig trackerConfig = (TrackerConfig)ConfigurationManager.GetSection("tracker");
                BalancerConfig balancerConfig = (BalancerConfig)ConfigurationManager.GetSection("balancer");
                
                int trackerCapacity = trackerConfig.Capacity;
                int trackerMaxPendingConnections = trackerConfig.MaxPendingConnections;

                if (string.IsNullOrWhiteSpace(balancerConfig.ServerName) == false)
                    RequestHandler.SetServerName(balancerConfig.ServerName);
                int balancerMaxConnections = balancerConfig.MaxConnections;

                if (System.Net.Sockets.Socket.OSSupportsIPv6)
                {
                    TListenerV6.Init(trackerCapacity + trackerMaxPendingConnections);
                    TListenerV6.Start();

                    BListenerV6.Init(balancerMaxConnections);
                    BListenerV6.Start();
                }

                if (System.Net.Sockets.Socket.OSSupportsIPv4)
                {
                    TListenerV4.Init(trackerCapacity + trackerMaxPendingConnections);
                    TListenerV4.Start();

                    BListenerV4.Init(balancerMaxConnections);
                    BListenerV4.Start();
                }
            }
            catch (ConfigurationErrorsException e)
            {
                AppLog.WriteLine(e.Message);
                throw e;
            }
            catch (SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                throw e;
            }
        }

        protected override void OnStop()
        {
            if (System.Net.Sockets.Socket.OSSupportsIPv6)
            {
                TListenerV6.Stop();
                BListenerV6.Stop();
            }

            if (System.Net.Sockets.Socket.OSSupportsIPv4)
            {
                TListenerV4.Stop();
                BListenerV4.Stop();
            }

            MgtAgent.Close();
            TaskScheduler.Close();
            SoftnetTracker.Terminate();

            AppLog.WriteLine("Service stopped!");
        }
    }
}
