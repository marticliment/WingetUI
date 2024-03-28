using Microsoft.UI.Xaml.Data;
using UniGetUI.Core.Data;
using UniGetUI.Core;
using Nancy;
using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Graphics.DirectX.Direct3D11;

namespace UniGetUI.Interface
{
    public class BackgroundApiRunner
    {

        private bool __running = false;

        /// <summary>
        /// Run the background api and wait for it for being stopped with the Stop() method
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            try
            {
                if(AppTools.Instance.GetSettings("DisableWidgetsApi"))
                {
                    AppTools.Log("Widgets API is disabled");
                    return;
                }

                __running = true;
                NancyHost host;
                try
                {
                    host = new NancyHost(new HostConfiguration() { RewriteLocalhost = false, }, new Uri("http://localhost:7058/"));
                    host.Start();
                }
                catch
                {

                    host = new NancyHost(new Uri("http://localhost:7058/"));
                    host.Start();
                }

                AppTools.Log("Api running on http://localhost:7058");
                
                while(__running)
                {
                    await Task.Delay(100);
                }
                host.Stop();
                AppTools.Log("Api was shut down");
            }
            catch (Exception e)
            {
                AppTools.Log(e);
            }
        }

        /// <summary>
        /// Stop the background api
        /// </summary>
        public void Stop()
        {
            __running = false;
        }
    }
    
    /// <summary>
    /// The background api builder
    /// </summary>
    public class BackgroundApi: NancyModule
    {
        public BackgroundApi()
        {
            // Enable CORS
            After.AddItemToEndOfPipeline((ctx) =>
            {
                ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
                    .WithHeader("Access-Control-Allow-Methods", "POST,GET,OPTIONS")
                    .WithHeader("Access-Control-Allow-Headers", "Authorization");
            });
            BuildShareApi();
            BuildV1WidgetsApi();
        }

        /// <summary>
        /// Build the endpoints required for the Share Interface
        /// </summary>
        public void BuildShareApi()
        { 
            // Show package from https://marticliment.com/wingetui/share
            Get("/v2/show-package", async (parameters) =>
            {
                try
                {
                    if (Request.Query.@pid == "" || Request.Query.@psource == "")
                        return 400;

                    AppTools.Log(Request.Query.@pid);

                    while (AppTools.Instance.App.MainWindow is null) await Task.Delay(100);
                    while (AppTools.Instance.App.MainWindow.NavigationPage is null) await Task.Delay(100);
                    while (AppTools.Instance.App.MainWindow.NavigationPage.DiscoverPage is null) await Task.Delay(100);

                    AppTools.Instance.App.MainWindow.NavigationPage.DiscoverPage.ShowSharedPackage_ThreadSafe(Request.Query.@pid.ToString(), Request.Query.@psource.ToString());

                    return "{\"status\": \"success\"}";
                }
                catch (Exception e)
                {
                    AppTools.Log(e);
                    return 500;
                }
            });


            // Basic entrypoint to know if WingetUI is running
            Get("/is-running", (parameters) =>
            {
                return "{\"status\": \"success\"}";
            });
        }

        /// <summary>
        /// Build the endpoints required for the /widgets/v1 endpoint. All of these 
        /// endpoints are authenticated with AppTools.Instance.AuthenticateToken
        /// </summary>
        public void BuildV1WidgetsApi()
        {
            // Basic version check
            Get("/widgets/v1/get_wingetui_version", (parameters) =>
            {
                if(!AppTools.Instance.AuthenticateToken(Request.Query.@token))
                    return 401;

                return CoreData.VersionNumber.ToString();
            });

            // Return found updates
            Get("/widgets/v1/get_updates", (parameters) =>
            {
                if (!AppTools.Instance.AuthenticateToken(Request.Query.@token))
                    return 401;

                string packages = "";
                foreach(var package in AppTools.Instance.App.MainWindow.NavigationPage.UpdatesPage.Packages)
                {
                    if(package.Tag == PackageEngine.Classes.PackageTag.OnQueue || package.Tag == PackageEngine.Classes.PackageTag.BeingProcessed)
                        continue; // Do not show already processed packages on queue 

                    var icon = package.GetIconUrl().ToString();
                    if(icon == "ms-appx:///Assets/Images/package_color.png")
                        icon = "https://marticliment.com/resources/widgets/package_color.png";
                    packages += $"{package.Name.Replace('|', '-')}|{package.Id}|{package.Version}|{package.NewVersion}|{package.Source}|{package.Manager.Name}|{icon}&&";
                }
                
                if(packages.Length > 2)
                    packages = packages[..(packages.Length - 2)];
                AppTools.Log(packages);

                return packages;
            });

            // Open WingetUI (as it was)
            Get("/widgets/v1/open_wingetui", (parameters) =>
            {
                if (!AppTools.Instance.AuthenticateToken(Request.Query.@token))
                    return 401;

                AppTools.Instance.App.MainWindow.DispatcherQueue.TryEnqueue(() => { AppTools.Instance.App.MainWindow.Activate(); });
                return 200;
            });

            // Open WingetUI with the Updates page shown
            Get("/widgets/v1/view_on_wingetui", (parameters) =>
            {
                if (!AppTools.Instance.AuthenticateToken(Request.Query.@token))
                    return 401;

                AppTools.Instance.App.MainWindow.DispatcherQueue.TryEnqueue(() => {
                    AppTools.Instance.App.MainWindow.NavigationPage.UpdatesNavButton.ForceClick();
                    AppTools.Instance.App.MainWindow.Activate();
                });
                return 200;
            });

            // Update a specific package given its Id
            Get("/widgets/v1/update_package", (parameters) =>
            {
                if (!AppTools.Instance.AuthenticateToken(Request.Query.@token))
                    return 401;

                if(Request.Query.@id == "")
                    return 400;

                AppTools.Instance.App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    AppTools.Instance.App.MainWindow.NavigationPage.UpdatesPage.UpdatePackageForId(Request.Query.@id);
                });
                return 200;
            });

            // Update all packages
            Get("/widgets/v1/update_all_packages", (parameters) =>
            {
                if (!AppTools.Instance.AuthenticateToken(Request.Query.@token))
                    return 401;

                AppTools.Instance.App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    AppTools.Instance.App.MainWindow.NavigationPage.UpdatesPage.UpdateAllPackages();
                });
                return 200;
            });

            // Update all packages for a specific manager
            Get("/widgets/v1/update_all_packages_for_source", (parameters) =>
            {
                if (!AppTools.Instance.AuthenticateToken(Request.Query.@token))
                    return 401;

                if (Request.Query.@source == "")
                    return 400;

                AppTools.Instance.App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    AppTools.Instance.App.MainWindow.NavigationPage.UpdatesPage.UpdateAllPackagesForManager(Request.Query.@source);
                });
                return 200;
            });
        }
    }
}
