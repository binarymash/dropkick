// Copyright 2007-2010 The Apache Software Foundation.
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
namespace dropkick.Configuration.Dsl.Iis
{
    using System;
    using System.Collections.Generic;
    using DeploymentModel;
    using FileSystem;
    using Tasks;
    using Tasks.Iis;

    public class IisProtoTask :
        BaseProtoTask,
        IisVirtualDirectoryInstallOptions,
        IisVirtualDirectoryUninstallOptions,
        AppPoolOptions,
        IisSiteInstallOptions,
        IisSiteUninstallOptions

    {
        Path _path;

        public IisProtoTask(string websiteName, Path path)
        {
            _path = path;
            WebsiteName = websiteName;
            ManagedRuntimeVersion = Tasks.Iis.ManagedRuntimeVersion.V2;
            AuthenticationToSet = new Dictionary<IISAuthenticationMode, bool>();
        }

        public string WebsiteName { get; set; }
        public string VdirPath { get; set; }
        public IisVersion Version { get; set; }
        public string PathOnServer { get; set; }
        protected string AppPoolName { get; set; }
        public bool ClassicPipelineRequested { get; set; }
        public string ManagedRuntimeVersion { get; set; }
        public string PathForWebsite { get; set; }
        public int PortForWebsite { get; set; }
        public bool Bit32Requested { get; set; }
        public bool ProcessModelIdentityTypeSpecified { get; private set; }
        public ProcessModelIdentity ProcessModelIdentityType { get; private set; }
        public string ProcessModelUsername { get; private set; }
        public string ProcessModelPassword { get; private set; }
        public Dictionary<IISAuthenticationMode, bool> AuthenticationToSet { get; private set; }
        public Action Action { get; set; }
        protected bool PreserveWebSite { get; set; }
        protected bool PreserveApplicationPool { get; set; }

        public IisSiteUninstallOptions PreserveTheWebSite()
        {
            PreserveWebSite = true;
            return this;
        }

        IisVirtualDirectoryUninstallOptions IisSiteUninstallOptions.VirtualDirectory(string name)
        {
            VdirPath = ReplaceTokens(name);
            return this;
        }

        IisVirtualDirectoryInstallOptions IisSiteInstallOptions.VirtualDirectory(string name)
        {
            VdirPath = ReplaceTokens(name);
            return this;
        }

        [Obsolete("This has been replaced by IisVirtualDirectoryInstallOptions IisSiteInstallOptions.VirtualDirectory(string name)", true)]
        IisVirtualDirectoryOptions IisSiteOptions.VirtualDirectory(string name)
        {
            throw new NotImplementedException();
        }

        IisVirtualDirectoryInstallOptions IisVirtualDirectoryInstallOptions.SetPathTo(string path)
        {
            PathOnServer = ReplaceTokens(path);
            return this;
        }

        public IisVirtualDirectoryUninstallOptions PreserveTheApplicationPool()
        {
            PreserveApplicationPool = true;
            return this;
        }

        public void UseClassicPipeline()
        {
            ClassicPipelineRequested = true;
        }

        public void Enable32BitAppOnWin64()
        {
            Bit32Requested = true;
        }

        public IisVirtualDirectoryInstallOptions SetAppPoolTo(string appPoolName)
        {
            AppPoolName = ReplaceTokens(appPoolName);
            return this;
        }

        public IisVirtualDirectoryInstallOptions SetAppPoolTo(string appPoolName, Action<AppPoolOptions> options)
        {
            SetAppPoolTo(appPoolName);
            options(this);
            return this;
        }


        public void SetRuntimeToV4()
        {
            ManagedRuntimeVersion = Tasks.Iis.ManagedRuntimeVersion.V4;
        }

        public void SetProcessModelIdentity(string username, string password)
        {
            ProcessModelIdentityTypeSpecified = true;
            ProcessModelIdentityType = ProcessModelIdentity.SpecificUser;
            ProcessModelUsername = username;
            ProcessModelPassword = password;
        }

        public void SetProcessModelIdentity(ProcessModelIdentity identity)
        {
            ProcessModelIdentityTypeSpecified = true;
            ProcessModelIdentityType = identity;
        }

        public override void RegisterRealTasks(PhysicalServer s)
        {
            switch(Action)
            {
                case Action.Install:
                    RegisterInstallTasks(s);
                    break;
                case Action.Uninstall:
                    RegisterUninstallTasks(s);
                    break;
                default:
                    throw new NotImplementedException(String.Format("Action [{0}] has not been implemented.", Enum.GetName(typeof(Action), Action)));
            }
        }

        void RegisterInstallTasks(PhysicalServer s)
        {
            var scrubbedPath = _path.GetPhysicalPath(s, PathOnServer, true);

            if (Version == IisVersion.Six)
            {
                s.AddTask(new Iis6Task
                {
                    PathOnServer = scrubbedPath,
                    ServerName = s.Name,
                    VirtualDirectoryPath = VdirPath,
                    WebsiteName = WebsiteName,
                    AppPoolName = AppPoolName
                });
                return;
            }
            s.AddTask(new Iis7InstallTask
            {
                PathOnServer = scrubbedPath,
                ServerName = s.Name,
                VirtualDirectoryPath = VdirPath,
                WebsiteName = WebsiteName,
                AppPoolName = AppPoolName,
                UseClassicPipeline = ClassicPipelineRequested,
                ManagedRuntimeVersion = this.ManagedRuntimeVersion,
                PathForWebsite = this.PathForWebsite,
                PortForWebsite = this.PortForWebsite,
                Enable32BitAppOnWin64 = Bit32Requested,
                SetProcessModelIdentity = this.ProcessModelIdentityTypeSpecified,
                ProcessModelIdentityType = ProcessModelIdentityType.ToProcessModelIdentityType(),
                ProcessModelUsername = this.ProcessModelUsername,
                ProcessModelPassword = this.ProcessModelPassword,
                AuthenticationToSet = this.AuthenticationToSet
            });
        }

        void RegisterUninstallTasks(PhysicalServer s)
        {
            if (Version == IisVersion.Six)
            {
                throw new NotImplementedException(String.Format("Site uninstall has not been implemented for IIS 6."));
            }
            s.AddTask(new Iis7UninstallTask
            {
                ServerName = s.Name,
                WebsiteName = WebsiteName,
                VirtualDirectoryPath = VdirPath,
                PreserveWebSite = PreserveWebSite,
                PreserveApplicationPool = PreserveApplicationPool
            });
        }

        public IisVirtualDirectoryInstallOptions DisableAllAuthentication() {
           foreach(IISAuthenticationMode mode in Enum.GetValues(typeof(IISAuthenticationMode))) {
              AuthenticationToSet.Add(mode, false);
           }
           return this;
        }

        public IisVirtualDirectoryInstallOptions SetAuthentication(IISAuthenticationMode authenticationType, bool enabled) {
           if(AuthenticationToSet.ContainsKey(authenticationType)) { AuthenticationToSet[authenticationType] = enabled; } else {
              AuthenticationToSet.Add(authenticationType, enabled);
           }
           return this;
        }
       
        public IisVirtualDirectoryInstallOptions DisableAllAuthenticationBut(IISAuthenticationMode enabledAuthenticationType) {
           foreach(IISAuthenticationMode mode in Enum.GetValues(typeof(IISAuthenticationMode))) {
              AuthenticationToSet.Add(mode, mode == enabledAuthenticationType);
           }
           return this;
        }

        public IisSiteInstallOptions Install
        {
            get
            {
                Action = Action.Install;
                return this;
            }
        }

        public IisSiteUninstallOptions Uninstall
        {
            get
            {
                Action = Action.Uninstall;
                return this;
            }
        }

    }
}