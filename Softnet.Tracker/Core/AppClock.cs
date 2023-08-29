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
    class AppClock
    {
        static long s_currentTicks;
        static long s_startingSystemTime;
        
        static AppClock()
        {
            s_currentTicks = 0;
            s_startingSystemTime = 0;
        }

        public static long getTicks()
        {
            return s_currentTicks;
        }

        public static void start()
        {
            s_currentTicks = SoftnetRegistry.Clock_GetTicks();
            s_startingSystemTime = SystemClock.Seconds;

            ScheduledTask task = new ScheduledTask(new WaitCallback(nextTick), null);
            TaskScheduler.Add(task, 10);
        }

        static void nextTick(object noData)
        {
            try
            {
                long currentTicks = SoftnetRegistry.Clock_AddTick();
                long lastTickSeconds = SystemClock.Seconds;

                s_currentTicks = currentTicks;

                long elapsedTime = lastTickSeconds - s_startingSystemTime;
                int waitSeconds = 10 - (int)(elapsedTime % 10L);
                if (waitSeconds < 5)
                    waitSeconds = 5;
                ScheduledTask task = new ScheduledTask(new WaitCallback(nextTick), null);
                TaskScheduler.Add(task, waitSeconds);
            }
            catch (SoftnetException ex)
            {
                AppLog.WriteLine(ex.Message);
                ScheduledTask task = new ScheduledTask(new WaitCallback(nextTick), null);
                TaskScheduler.Add(task, 5);
            }
        }
    }
}
