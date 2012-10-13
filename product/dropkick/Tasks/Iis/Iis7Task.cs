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
    using System;
    using DeploymentModel;
    using Microsoft.Web.Administration;

    //http://blogs.msdn.com/carlosag/archive/2006/04/17/MicrosoftWebAdministration.aspx
    public abstract class Iis7Task : BaseIisTask
    {

    	public override int VersionNumber
        {
            get { return 7; }
        }

        public bool DoesSiteExist(DeploymentResult result)
        {
            var iisManager = ServerManager.OpenRemote(ServerName);
            foreach (var site in iisManager.Sites)
            {
                if (site.Name.Equals(WebsiteName))
                {
                    result.AddGood("'{0}' site exists", WebsiteName);
                    return true;
                }
            }

            result.AddAlert("'{0}' site DOES NOT exist", WebsiteName);
            return false;
        }

        public bool DoesVirtualDirectoryExist(Site site)
        {
            foreach (var app in site.Applications)
            {
                if (app.Path.Equals("/" + VirtualDirectoryPath))
                {
                    return true;
                }
            }

            return false;
        }

        public Site GetSite(ServerManager iisManager, string name)
        {
            foreach (var site in iisManager.Sites)
            {
                if (site.Name.Equals(name)) return site;
            }

            throw new Exception("Unable to find site named '{0}'".FormatWith(name));
        }
    }
}