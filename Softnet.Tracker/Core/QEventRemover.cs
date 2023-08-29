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

using Softnet.Tracker.SiteModel;

namespace Softnet.Tracker.Core
{
    class QEventRemover
    {
        static QEventRemover()
        {
            mutex = new object();
            s_eventList = new List<QEventInstance>();
            s_inProgress = false;
        }

        static object mutex; 
        static List<QEventInstance> s_eventList;
        static bool s_inProgress;

        public static void Add(QEventInstance eventInstance)
        {
            lock (mutex)
            {
                if (s_inProgress)
                {
                    s_eventList.Add(eventInstance);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(removeEvents), eventInstance);
                    s_inProgress = true;
                }
            }
        }

        static void removeEvents(object state)
        {
            QEventInstance eventInstance = (QEventInstance)state;
            try
            {                
                SoftnetRegistry.EventController_DeleteQEventInstance(eventInstance.InstanceId);

                while (true)
                {
                    lock (mutex)
                    {
                        if (s_eventList.Count == 0)
                        {
                            s_inProgress = false;
                            return;
                        }

                        eventInstance = s_eventList[s_eventList.Count - 1];
                        s_eventList.RemoveAt(s_eventList.Count - 1);
                    }

                    SoftnetRegistry.EventController_DeleteQEventInstance(eventInstance.InstanceId);
                }
            }
            catch (SoftnetException)
            {
                lock (mutex)
                {
                    s_inProgress = false;
                    s_eventList.Add(eventInstance);
                }
            }
        }
    }
}
