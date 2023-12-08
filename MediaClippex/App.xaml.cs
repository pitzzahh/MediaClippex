using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using MediaClippex.MVVM.View;
using MediaClippex.MVVM.ViewModel;
using MediaClippex.Services.Config.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Russkyc.DependencyInjection.Helpers;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex;

public partial class App
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DownloadFFmpeg.ps1")}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            var process = new Process { StartInfo = processStartInfo };
            process.Start();
            await process.WaitForExitAsync();

            var servicesContainer = new ServicesCollection()
                .AddServices()
                .AddServicesFromReferenceAssemblies()
                .Build();

            var backUpFilePath = Path.Combine(AppContext.BaseDirectory, "config.json.bak");

            if (File.Exists(backUpFilePath))
            {
                var backupConfigJson = await File.ReadAllTextAsync(backUpFilePath);
                var backupConfig = JsonConvert.DeserializeObject<JObject>(backupConfigJson)!;
                var config = servicesContainer.Resolve<IConfig>();
                config.ColorTheme = backupConfig["colorTheme"]!.ToString();
                config.DownloadPath = backupConfig["downloadPath"]!.ToString();
                File.Delete(backUpFilePath);
            }

            servicesContainer.Resolve<MainView>().Show();

            var homeViewModel = servicesContainer.Resolve<HomeViewModel>();
            await Task.Run(() => homeViewModel.GetQueuingVideos());
            await Task.Run(() => homeViewModel.GetDownloadedVideos());
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.IsNullOrEmpty(ex.Message) ? ex.ToString() : ex.Message,
                "Something went wrong during startup", MessageBoxButton.OK,
                MessageBoxImage.Error);
            Current.Shutdown(ex.HResult);
            Environment.Exit(ex.HResult);
        }
    }
}