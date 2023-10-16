using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using MediaClippex.MVVM.View;
using Russkyc.DependencyInjection.Helpers;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public App()
    {
        var ffmpegFile = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(ffmpegFile)) return;
        Process.Start(Path.Combine(AppContext.BaseDirectory, "Launcher.exe"));
        Current.Shutdown();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var servicesContainer = new ServicesCollection()
            .AddServices()
            .AddServicesFromReferenceAssemblies()
            .Build();
        servicesContainer.Resolve<MainView>().Show();
        base.OnStartup(e);
    }
}