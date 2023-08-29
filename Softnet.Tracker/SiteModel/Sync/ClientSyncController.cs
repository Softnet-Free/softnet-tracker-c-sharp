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
using Softnet.Tracker.ClientModel;
using Softnet.Tracker.Core;

namespace Softnet.Tracker.SiteModel
{
    class ClientSyncController
    {
        private ClientSyncController() { }

        public static ClientSyncController Create(Site site)
        {
            var instance = new ClientSyncController();
            instance.m_Site = site;
            instance.site_mutex = site.mutex;
            return instance;
        }

        object site_mutex;
        Site m_Site;

        public void SyncParams(Client client) // synchronized by site mutex
        {
            if (client.SyncToken == null)
                client.SyncToken = new ClientSyncToken();
            client.SyncToken.state = 1;

            ThreadPool.QueueUserWorkItem(delegate { ExecSyncParams(client); });
        }

        void ExecSyncParams(Client client)
        {
            try
            {
                ClientSyncData syncData = new ClientSyncData();
                int resultCode = SoftnetRegistry.ClientParams_GetData(client.Id, syncData);
                if (resultCode != 0)
                    return;
                
                lock (site_mutex)
                {
                    if (client.SyncToken.state == 1)
                    {
                        client.SyncToken = null;
                        if (10 <= syncData.pingPeriod && syncData.pingPeriod <= 300)
                        {
                            client.Send(EncodeMessage_Params(syncData));
                        }
                    }
                    else // service.SyncToken.state == 2
                    {
                        client.SyncToken.state = 1;
                        ThreadPool.QueueUserWorkItem(delegate { ExecSyncParams(client); });
                    }
                }
            }
            catch (SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }
        }

        public void OnPingPeriodChanged(Client client) // synchronized by site mutex
        {
            if (client.SyncToken == null)
            {
                client.SyncToken = new ClientSyncToken();
                client.SyncToken.ping_period_state = 1;
                ThreadPool.QueueUserWorkItem(delegate { ResetPingPeriod(client); });
            }
            else
            {
                if (client.SyncToken.state != 0)
                {
                    client.SyncToken.state = 2;
                }
                else if (client.SyncToken.ping_period_state == 0)
                {
                    client.SyncToken.ping_period_state = 1;
                    ThreadPool.QueueUserWorkItem(delegate { ResetPingPeriod(client); });
                }
                else // client.SyncToken.ping_period_state != 0
                {
                    client.SyncToken.ping_period_state = 2;
                }
            }
        }

        void ResetPingPeriod(Client client)
        {
            try
            {
                ClientSyncData syncData = new ClientSyncData();
                int resultCode = SoftnetRegistry.ClientParams_GetPingPeriod(client.Id, syncData);
                if (resultCode != 0)
                    return;
                
                lock (site_mutex)
                {
                    if (client.SyncToken != null && client.SyncToken.state == 0)
                    {
                        if (client.SyncToken.ping_period_state == 1)
                        {
                            client.SyncToken.ping_period_state = 0;
                            client.Send(EncodeMessage_SetPingPeriod(syncData.pingPeriod));
                        }
                        else
                        {
                            client.SyncToken.ping_period_state = 1;
                            ThreadPool.QueueUserWorkItem(delegate { ResetPingPeriod(client); });
                        }
                    }
                }                
            }
            catch (SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }
        }

        SoftnetMessage EncodeMessage_Params(ClientSyncData syncData)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var sequence = asnEncoder.Sequence;
            sequence.Int32(1, syncData.pingPeriod);
            return MsgBuilder.Create(Constants.Client.StateController.ModuleId, Constants.Client.StateController.PARAMS, asnEncoder);
        }

        SoftnetMessage EncodeMessage_SetPingPeriod(int pingPeriod)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var sequence = asnEncoder.Sequence;
            sequence.Int32(pingPeriod);
            return MsgBuilder.Create(Constants.Client.StateController.ModuleId, Constants.Client.StateController.SET_PING_PERIOD, asnEncoder);
        }
    }
}
