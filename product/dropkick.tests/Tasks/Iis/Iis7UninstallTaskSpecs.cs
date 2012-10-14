namespace dropkick.tests.Tasks.Iis
{
    using System;
    using System.IO;
    using dropkick.Tasks.Iis;
    using Microsoft.Web.Administration;
    using NUnit.Framework;

    public class Iis7UninstallTaskSpecs
    {

        #region Nested type: IisUninstallTaskWithMultipleVirtualDirectoriesSpecsBase

        public abstract class IisUninstallTaskWithMultipleVirtualDirectoriesSpecsBase : TinySpec
        {
            protected string WebServer = "localhost";
            protected static string WebSiteName = "DropkickUnitTestWebsite " + DateTime.UtcNow.ToString("yy-MM-dd HH-mm-ss");

            protected string RootVirtualDirectory = "";
            protected string RootAppPool = WebSiteName + " RootAppPool";
            protected string RootPath = @"c:\NewPath";

            protected string VirtualDirectory1 = "vdirtest1";
            protected string AppPool1 = WebSiteName + " AppPool 1";
            protected string Path1 = @"C:\NewPath\Path1";
            
            protected string VirtualDirectory2 = "vdirtest2";
            protected string AppPool2 = WebSiteName + " AppPool 2";
            protected string Path2 = @"C:\NewPath\Path2";

            protected static readonly string ManagedRuntimeVersion = dropkick.Tasks.Iis.ManagedRuntimeVersion.V2;

            protected VirtualDirectory vDir;
            protected Application application;
            protected ApplicationPool applicationPool;

            public override void AfterObservations()
            {
                using (var iis = ServerManager.OpenRemote(WebServer))
                {
                    var site = iis.Sites[WebSiteName];
                    if (site != null)
                    {
                        iis.Sites.Remove(site);
                    }
                    var appPool = iis.ApplicationPools[AppPool1];
                    if (appPool != null)
                    {
                        iis.ApplicationPools.Remove(appPool);
                    }

                    appPool = iis.ApplicationPools[AppPool2];
                    if (appPool != null)
                    {
                        iis.ApplicationPools.Remove(appPool);
                    }

                    appPool = iis.ApplicationPools[RootAppPool];
                    if (appPool != null)
                    {
                        iis.ApplicationPools.Remove(appPool);
                    }

                    iis.CommitChanges();
                }
            }

            public override void Context()
            {
                CreateVirtualDirectory(RootPath, WebServer, RootVirtualDirectory, RootAppPool, ManagedRuntimeVersion, WebSiteName);
                CreateVirtualDirectory(Path1, WebServer, VirtualDirectory1, AppPool1, ManagedRuntimeVersion, WebSiteName);
                CreateVirtualDirectory(Path2, WebServer, VirtualDirectory2, AppPool2, ManagedRuntimeVersion, WebSiteName);
            }

            void CreateVirtualDirectory(string path, string webServer, string virtualDirectory, string appPool, string managedRuntimeVersion, string webSiteName)
            {
                Directory.CreateDirectory(path);

                var task = new Iis7InstallTask
                {
                    PathOnServer = path,
                    ServerName = webServer,
                    VirtualDirectoryPath = virtualDirectory,
                    AppPoolName = appPool,
                    ManagedRuntimeVersion = managedRuntimeVersion,
                    WebsiteName = webSiteName,
                };
                var output = task.Execute();

                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                //lets just check we've set up the context
                using (var iis = ServerManager.OpenRemote(webServer))
                {
                    iis.Sites[webSiteName].ShouldNotBeNull();
                    iis.Sites[webSiteName].Applications["/"+virtualDirectory].ShouldNotBeNull();
                    iis.ApplicationPools[appPool].ShouldNotBeNull();
                }
            }

            public Site Site(string webServer, string webSiteName)
            {
                using (var iis = ServerManager.OpenRemote(webServer))
                {
                    return iis.Sites[webSiteName];
                }
            }
            
            public Application VirtualDirectory(string webServer, string webSiteName, string virtualDirectory)
            {
                using (var iis = ServerManager.OpenRemote(webServer))
                {
                    var site = iis.Sites[webSiteName];
                    return site.Applications["/" + virtualDirectory];
                }                
            }

            public ApplicationPool ApplicationPool(string webServer, string applicationPool)
            {
                using (var iis = ServerManager.OpenRemote(webServer))
                {
                    return iis.ApplicationPools[applicationPool];
                }
                
            }
        }

        #endregion

        #region Nested type: When_uninstalling_a_site_which_does_not_exist

        [Category("Iis7Task")]
        [Category("Integration")]
        public class When_uninstalling_a_site_which_does_not_exist : IisUninstallTaskWithMultipleVirtualDirectoriesSpecsBase
        {
            public string NonExistentWebSiteName = WebSiteName + "B";

            public override void Because()
            {
                var uninstallTask = new Iis7UninstallTask
                {
                    WebsiteName = NonExistentWebSiteName,
                    ServerName = WebServer,
                    VirtualDirectoryPath = VirtualDirectory1,
                };

                var output = uninstallTask.VerifyCanRun();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                output = uninstallTask.Execute();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }
            }

            [Fact]
            public void The_website_will_still_not_exist_after_execution()
            {
                Site(WebServer, NonExistentWebSiteName).ShouldBeEqualTo(null);
            }

        }

        #endregion

        #region Nested type: When_uninstalling_a_virtual_directory_which_does_not_exist

        [Category("Iis7Task")]
        [Category("Integration")]
        public class When_uninstalling_a_virtual_directory_which_does_not_exist : IisUninstallTaskWithMultipleVirtualDirectoriesSpecsBase
        {
            public string NonExistentVirtualDirectory;

            public When_uninstalling_a_virtual_directory_which_does_not_exist()
            {
                NonExistentVirtualDirectory = VirtualDirectory1 + "B";
            }

            public override void Because()
            {
                var uninstallTask = new Iis7UninstallTask
                {
                    WebsiteName = WebSiteName,
                    ServerName = WebServer,
                    VirtualDirectoryPath = NonExistentVirtualDirectory,
                };

                var output = uninstallTask.VerifyCanRun();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                output = uninstallTask.Execute();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }
            }

            [Fact]
            public void The_virtual_directory_will_still_not_exist_after_execution()
            {
                VirtualDirectory(WebServer, WebSiteName, NonExistentVirtualDirectory).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_not_delete_the_website()
            {
                Site(WebServer, WebSiteName).ShouldNotBeNull();
            }

        }

        #endregion

        #region Nested type: When_uninstalling_a_virtual_directory_with_a_shared_apppool

        [Category("Iis7Task")]
        [Category("Integration")]
        public class When_uninstalling_a_virtual_directory_with_a_shared_apppool : IisUninstallTaskWithMultipleVirtualDirectoriesSpecsBase
        {
            public override void Context()
            {
                AppPool2 = AppPool1;
                base.Context();
            }

            public override void Because()
            {
                var uninstallTask = new Iis7UninstallTask
                {
                    ServerName = WebServer,
                    WebsiteName = WebSiteName,
                    VirtualDirectoryPath = VirtualDirectory1
                };

                var output = uninstallTask.VerifyCanRun();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                output = uninstallTask.Execute();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }
            }

            [Fact]
            public void It_should_delete_virtual_directory_1()
            {
                VirtualDirectory(WebServer, WebSiteName, VirtualDirectory1).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_not_delete_the_virtual_directory_2()
            {
                VirtualDirectory(WebServer, WebSiteName, VirtualDirectory2).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_root_virtual_directory()
            {
                VirtualDirectory(WebServer, WebSiteName, RootVirtualDirectory).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_shared_virtual_directory_apppool()
            {
                ApplicationPool(WebServer, AppPool1).ShouldNotBeNull();
                ApplicationPool(WebServer, AppPool2).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_root_virtual_directory_apppool()
            {
                ApplicationPool(WebServer, RootAppPool).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_website()
            {
                Site(WebServer, WebSiteName).ShouldNotBeNull();
            }
        }

        #endregion

        #region When_uninstalling_a_virtual_directory_with_unique_apppool

        [Category("Iis7Task")]
        [Category("Integration")]
        public class When_uninstalling_a_virtual_directory_with_unique_apppool : IisUninstallTaskWithMultipleVirtualDirectoriesSpecsBase
        {

            public override void Because()
            {
                var uninstallTask = new Iis7UninstallTask
                {
                    ServerName = WebServer,
                    WebsiteName = WebSiteName,
                    VirtualDirectoryPath = VirtualDirectory1
                };

                var output = uninstallTask.VerifyCanRun();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                output = uninstallTask.Execute();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }
            }

            [Fact]
            public void It_should_delete_virtual_directory_1()
            {
                VirtualDirectory(WebServer, WebSiteName, VirtualDirectory1).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_delete_the_apppool_for_virtual_directory_1()
            {
                ApplicationPool(WebServer, AppPool1).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_not_delete_the_virtual_directory_2()
            {
                VirtualDirectory(WebServer, WebSiteName, VirtualDirectory2).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_apppool_for_the_other_virtual_directory()
            {
                ApplicationPool(WebServer, AppPool2).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_root_virtual_directory()
            {
                VirtualDirectory(WebServer, WebSiteName, RootVirtualDirectory).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_root_virtual_directory_apppool()
            {
                ApplicationPool(WebServer, RootAppPool).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_website()
            {
                Site(WebServer, WebSiteName).ShouldNotBeNull();
            }

        }

        #endregion

        #region When_uninstalling_a_virtual_directory_with_unique_apppool_which_is_to_be_preserved

        [Category("Iis7Task")]
        [Category("Integration")]
        public class When_uninstalling_a_virtual_directory_with_unique_apppool_which_is_to_be_preserved : IisUninstallTaskWithMultipleVirtualDirectoriesSpecsBase
        {

            public override void Because()
            {
                var uninstallTask = new Iis7UninstallTask
                {
                    ServerName = WebServer,
                    WebsiteName = WebSiteName,
                    VirtualDirectoryPath = VirtualDirectory1,
                    PreserveApplicationPool = true
                };

                var output = uninstallTask.VerifyCanRun();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                output = uninstallTask.Execute();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }
            }

            [Fact]
            public void It_should_delete_virtual_directory_1()
            {
                VirtualDirectory(WebServer, WebSiteName, VirtualDirectory1).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_not_delete_the_apppool_for_deleted_virtual_directory_1()
            {
                ApplicationPool(WebServer, AppPool1).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_virtual_directory_2()
            {
                VirtualDirectory(WebServer, WebSiteName, VirtualDirectory2).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_apppool_for_the_other_virtual_directory()
            {
                ApplicationPool(WebServer, AppPool2).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_root_virtual_directory()
            {
                VirtualDirectory(WebServer, WebSiteName, RootVirtualDirectory).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_root_virtual_directory_apppool()
            {
                ApplicationPool(WebServer, RootAppPool).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_not_delete_the_website()
            {
                Site(WebServer, WebSiteName).ShouldNotBeNull();
            }
        }

        #endregion

        #region When_uninstalling_all_virtual_directories_on_a_site_when_all_apppools_are_unique

        [Category("Iis7Task")]
        [Category("Integration")]
        public class When_uninstalling_all_virtual_directories_on_a_site_when_all_apppools_are_unique : IisUninstallTaskWithMultipleVirtualDirectoriesSpecsBase
        {

            public override void Because()
            {
                var uninstallTask = new Iis7UninstallTask
                {
                    ServerName = WebServer,
                    WebsiteName = WebSiteName,
                    VirtualDirectoryPath = RootVirtualDirectory,
                };

                var output = uninstallTask.VerifyCanRun();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                output = uninstallTask.Execute();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                uninstallTask = new Iis7UninstallTask
                {
                    ServerName = WebServer,
                    WebsiteName = WebSiteName,
                    VirtualDirectoryPath = VirtualDirectory1,
                };

                output = uninstallTask.VerifyCanRun();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }
                
                output = uninstallTask.Execute();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                uninstallTask = new Iis7UninstallTask
                {
                    ServerName = WebServer,
                    WebsiteName = WebSiteName,
                    VirtualDirectoryPath = VirtualDirectory2,
                };

                output = uninstallTask.VerifyCanRun();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                output = uninstallTask.Execute();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }
            }

            [Fact]
            public void It_should_delete_the_apppool_for_the_virtual_directory_1()
            {
                ApplicationPool(WebServer, AppPool1).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_delete_the_apppool_for_virtual_directory_2()
            {
                ApplicationPool(WebServer, AppPool2).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_delete_the_apppool_for_root_virtual_directory()
            {
                ApplicationPool(WebServer, RootAppPool).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_delete_the_site_and_therefore_all_virtual_directories()
            {
                Site(WebServer, WebSiteName).ShouldBeEqualTo(null);
            }

        }

        #endregion

        #region When_uninstalling_all_virtual_directories_on_a_site_which_is_to_be_preserved

        [Category("Iis7Task")]
        [Category("Integration")]
        public class When_uninstalling_all_virtual_directories_on_a_site_which_is_to_be_preserved : IisUninstallTaskWithMultipleVirtualDirectoriesSpecsBase
        {

            public override void Because()
            {
                var uninstallTask = new Iis7UninstallTask
                {
                    ServerName = WebServer,
                    WebsiteName = WebSiteName,
                    VirtualDirectoryPath = RootVirtualDirectory,
                };

                var output = uninstallTask.VerifyCanRun();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }
                
                output = uninstallTask.Execute();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                uninstallTask = new Iis7UninstallTask
                {
                    ServerName = WebServer,
                    WebsiteName = WebSiteName,
                    VirtualDirectoryPath = VirtualDirectory1,
                };

                output = uninstallTask.VerifyCanRun();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }
                
                output = uninstallTask.Execute();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }

                uninstallTask = new Iis7UninstallTask
                {
                    ServerName = WebServer,
                    WebsiteName = WebSiteName,
                    VirtualDirectoryPath = VirtualDirectory2,
                    PreserveWebSite = true
                };

                output = uninstallTask.VerifyCanRun();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }
                
                output = uninstallTask.Execute();
                foreach (var item in output.Results)
                {
                    Console.WriteLine(item.Message);
                }
            }

            [Fact]
            public void It_should_delete_the_apppool_for_the_virtual_directory_1()
            {
                ApplicationPool(WebServer, AppPool1).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_delete_the_apppool_for_virtual_directory_2()
            {
                ApplicationPool(WebServer, AppPool2).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_delete_the_apppool_for_root_virtual_directory()
            {
                ApplicationPool(WebServer, RootAppPool).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_not_delete_the_site()
            {
                Site(WebServer, WebSiteName).ShouldNotBeNull();
            }

            [Fact]
            public void It_should_delete_virtual_directory_1()
            {
                VirtualDirectory(WebServer, WebSiteName, VirtualDirectory1).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_delete_virtual_directory_2()
            {
                VirtualDirectory(WebServer, WebSiteName, VirtualDirectory2).ShouldBeEqualTo(null);
            }

            [Fact]
            public void It_should_delete_the_root_virtual_directory()
            {
                VirtualDirectory(WebServer, WebSiteName, RootVirtualDirectory).ShouldBeEqualTo(null);
            }

        }

        #endregion

    }
}