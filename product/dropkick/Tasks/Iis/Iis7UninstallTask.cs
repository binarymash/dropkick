// Copyright 2007-2008 The Apache Software Foundation.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

namespace dropkick.Tasks.Iis
{
    using System.Linq;
    using DeploymentModel;
    using Microsoft.Web.Administration;

    //http://blogs.msdn.com/carlosag/archive/2006/04/17/MicrosoftWebAdministration.aspx
    public class Iis7UninstallTask : Iis7Task
    {

        public bool PreserveWebSite { get; set; }
        public bool PreserveApplicationPool { get; set; }

        public override DeploymentResult VerifyCanRun()
        {
            var result = new DeploymentResult();

            IisUtility.CheckForIis7(result);

            var iisManager = ServerManager.OpenRemote(ServerName);
            if (DoesSiteExist(result))
            {
                if (DoesVirtualDirectoryExist(GetSite(iisManager, WebsiteName)))
                {
                    result.AddGood("Found VirtualDirectory '{0}'", VirtualDirectoryPath);
                }
                else
                {
                    result.AddAlert("Couldn't find VirtualDirectory '{0}'", VirtualDirectoryPath);
                }
            }

            return result;
        }

        public override DeploymentResult Execute()
        {
            bool siteDeleted = false;
            bool virtualDirectoryDeleted = false;
            bool applicationPoolDeleted = false;
            string applicationPoolName = string.Empty;

            var result = new DeploymentResult();
            var iisManager = ServerManager.OpenRemote(ServerName);
            var site = iisManager.Sites[WebsiteName];
            if (site != null)
            {
                var appPath = "/" + VirtualDirectoryPath;
                var application = site.Applications.FirstOrDefault(x => x.Path == appPath);
                if (application != null)
                {
                    site.Applications.Remove(application);
                    virtualDirectoryDeleted = true;

                    if (!PreserveApplicationPool)
                    {
                        if (ApplicationPoolIsOrphaned(iisManager, application.ApplicationPoolName))
                        {
                            var appPool = iisManager.ApplicationPools.FirstOrDefault(x => x.Name == application.ApplicationPoolName);
                            if (appPool != null)
                            {
                                iisManager.ApplicationPools.Remove(appPool);
                                applicationPoolDeleted = true;
                            }
                        }
                    }
                }
                if (!PreserveWebSite)
                {
                    if (site.Applications.Count == 0)
                    {
                        iisManager.Sites.Remove(site);
                        siteDeleted = true;
                    }
                }

            }

            iisManager.CommitChanges();

            result.AddGood(virtualDirectoryDeleted
                    ? "Virtual Directory '{0}' was deleted successfully."
                    : "Virtual Directory '{0}' was not deleted.", VirtualDirectoryPath);

            result.AddGood(applicationPoolDeleted
                ? "Application Pool '{0}' was deleted successfully."
                : "Application Pool '{0}' was not deleted.", applicationPoolName);

            result.AddGood(siteDeleted 
                ? "Site '{0}' was deleted successfully."
                : "Site '{0}' was not deleted.", WebsiteName);
            
            LogCoarseGrain("[iis7] {0}", Name);
            
            return result;
        }

        static bool ApplicationPoolIsOrphaned(ServerManager iisManager, string applicationPoolName)
        {
            return !iisManager.Sites.Any(site => site.Applications.Any(app => app.ApplicationPoolName == applicationPoolName));
        }
    }
}