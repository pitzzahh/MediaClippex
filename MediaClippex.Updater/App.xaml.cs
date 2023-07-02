using System.Windows;
using MediaClippex.Updater.MVVM.View;
using MediaClippex.Updater.Services;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex.Updater;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public App()
    {
        BuilderServices.BuildWithContainer(BuildContainer.ConfigureServices());
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        BuilderServices.Resolve<MediaClippexUpdater>().Show();
        base.OnStartup(e);
    }

}