using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using MediaClippex.MVVM.View;
using MediaClippex.MVVM.ViewModel;
using Russkyc.DependencyInjection.Helpers;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            Process.Start("powershell.exe",
                $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DownloadFFmpeg.ps1")}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error downloading FFmpeg: {ex.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            Current.Shutdown(-1);
            return;
        }

        var servicesContainer = new ServicesCollection()
            .AddServices()
            .AddServicesFromReferenceAssemblies()
            .Build();
        servicesContainer.Resolve<MainView>().Show();
        var homeViewModel = servicesContainer.Resolve<HomeViewModel>();
        Task.Run(homeViewModel.GetQueuingVideos);
        Task.Run(homeViewModel.GetDownloadedVideos);
        base.OnStartup(e);
    }
}