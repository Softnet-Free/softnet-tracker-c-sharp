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

using Softnet.Tracker.SiteModel;
using Softnet.ServerKit;


namespace Softnet.Tracker.Core
{
    class SoftnetTracker
    {
        static SoftnetTracker()
        {
            mutex = new object();
            s_Sites = new Dictionary<long, Site>();
        }

        static object mutex;
        static Dictionary<long, Site> s_Sites;

        public static Site GetSite(long siteId)
        {
            Site site = null;
            bool newlyCreated = false;

            lock (mutex)
            {
                if (s_Sites.TryGetValue(siteId, out site) == false)
                {
                    site = new Site(siteId);
                    s_Sites.Add(siteId, site);
                    newlyCreated = true;
                }
                site.SetLastAcquiredTime();
            }

            if (newlyCreated)
            {
                int errorCode = site.Load();
                if (errorCode != 0)
                {
                    site.Remove(errorCode);
                    throw new SoftnetException(errorCode);
                }
            }
            return site;
        }

        public static Site GetSiteToConstruct(long siteId)
        {
            Site site = null;            
            lock (mutex)
            {
                if (s_Sites.TryGetValue(siteId, out site) == false)
                {
                    site = new Site(siteId);
                    s_Sites.Add(siteId, site);                    
                }
                site.SetLastAcquiredTime();
            }
            return site;
        }

        public static Site FindSite(long siteId)
        {
            Site site = null;
            lock (mutex)
            {
                if (s_Sites.TryGetValue(siteId, out site) == false)
                    return null;
                site.SetLastAcquiredTime();
            }            
            return site;
        }

        public static void Remove(Site site)
        {
            lock (mutex)
            {
                s_Sites.Remove(site.Id);
            }
        }

        public static bool RemoveOnExpiration(Site site)
        {
            lock (mutex)
            {
                if (site.HasBeenAcquired())
                    return false;
                
                s_Sites.Remove(site.Id);
                return true;                
            }
        }

        public static void Terminate()
        {
            lock (mutex)
            {
                foreach(KeyValuePair<long, Site> keyValue in s_Sites)
                    keyValue.Value.Terminate();
                s_Sites.Clear();
            }
        }
    }
}
