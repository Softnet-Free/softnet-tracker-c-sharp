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

using Softnet.ServerKit;

namespace Softnet.Tracker.Core
{
    class EventCleaner
    {
        public static void Start()
        {
            ScheduledTask task = new ScheduledTask(Execute, null);
            TaskScheduler.Add(task, 10);
        }

        static void Execute(object noData)
        {
            try
            {
                SoftnetRegistry.Controller_CleanExpiredEvents();
                ScheduledTask task = new ScheduledTask(Execute, null);
                TaskScheduler.Add(task, TaskScheduler.SECONDS_600);
            }
            catch(SoftnetException e)
            {
                AppLog.WriteLine(e.Message);
                ScheduledTask task = new ScheduledTask(Execute, null);
                TaskScheduler.Add(task, 60);
            }
        }
    }
}
