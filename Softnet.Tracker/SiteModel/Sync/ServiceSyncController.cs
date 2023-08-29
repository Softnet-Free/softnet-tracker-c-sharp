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
using Softnet.Tracker.ServiceModel;
using Softnet.Tracker.Core;

namespace Softnet.Tracker.SiteModel
{
    class ServiceSyncController
    {
        private ServiceSyncController() { }

        public static ServiceSyncController Create(Site site)
        {
            var instance = new ServiceSyncController();
            instance.m_Site = site;
            instance.site_mutex = site.mutex;
            return instance;
        }

        object site_mutex;
        Site m_Site;

        public void SyncParams(Service service)
        {
            if (service.SyncToken == null)
                service.SyncToken = new ServiceSyncToken();
            service.SyncToken.state = 1;

            ThreadPool.QueueUserWorkItem(delegate { ExecSyncParams(service); });
        }

        void ExecSyncParams(Service service)
        {
            try
            {
                ServiceSyncData syncData = new ServiceSyncData();
                int resultCode = SoftnetRegistry.ServiceParams_GetData(service.Id, syncData);
                if (resultCode != 0)
                    return;
                
                lock (site_mutex)
                {
                    if (service.SyncToken.state == 1)
                    {
                        service.SyncToken = null;
                        if (10 <= syncData.pingPeriod && syncData.pingPeriod <= 300)
                        {
                            service.Send(EncodeMessage_Params(syncData));
                        }
                    }
                    else // service.SyncToken.state == 2
                    {
                        service.SyncToken.state = 1;
                        ThreadPool.QueueUserWorkItem(delegate { ExecSyncParams(service); });
                    }
                }                
            }
            catch (SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }
        }

        public void OnPingPeriodChanged(Service service)
        { 
            if (service.SyncToken == null)
            {
                service.SyncToken = new ServiceSyncToken();
                service.SyncToken.ping_period_state = 1;
                ThreadPool.QueueUserWorkItem(delegate { SetPingPeriod(service); });
            }
            else
            {
                if (service.SyncToken.state != 0)
                {
                    service.SyncToken.state = 2;
                }
                else if (service.SyncToken.ping_period_state == 0)
                {
                    service.SyncToken.ping_period_state = 1;
                    ThreadPool.QueueUserWorkItem(delegate { SetPingPeriod(service); });
                }
                else // service.SyncToken.ping_period_state != 0
                {
                    service.SyncToken.ping_period_state = 2;
                }
            }            
        }

        void SetPingPeriod(Service service)
        {
            try
            {
                ServiceSyncData syncData = new ServiceSyncData();
                int resultCode = SoftnetRegistry.ServiceParams_GetPingPeriod(service.Id, syncData);
                if (resultCode != 0)
                    return;

                lock (site_mutex)
                {
                    if (service.SyncToken != null && service.SyncToken.state == 0)
                    {
                        if (service.SyncToken.ping_period_state == 1)
                        {
                            service.SyncToken.ping_period_state = 0;
                            service.Send(EncodeMessage_SetPingPeriod(syncData.pingPeriod));
                        }
                        else
                        {
                            service.SyncToken.ping_period_state = 1;
                            ThreadPool.QueueUserWorkItem(delegate { SetPingPeriod(service); });
                        }
                    }
                }
            }
            catch (SoftnetException ex)
            {
                m_Site.Remove(ex.ErrorCode);
            }
        }

        SoftnetMessage EncodeMessage_Params(ServiceSyncData syncData)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var sequence = asnEncoder.Sequence;
            sequence.Int32(1, syncData.pingPeriod);
            return MsgBuilder.Create(Constants.Service.SyncController.ModuleId, Constants.Service.SyncController.PARAMS, asnEncoder);
        }

        SoftnetMessage EncodeMessage_SetPingPeriod(int pingPeriod)
        {
            ASNEncoder asnEncoder = new ASNEncoder();
            var sequence = asnEncoder.Sequence;
            sequence.Int32(pingPeriod);
            return MsgBuilder.Create(Constants.Service.SyncController.ModuleId, Constants.Service.SyncController.SET_PING_PERIOD, asnEncoder);
        }
    }
}
