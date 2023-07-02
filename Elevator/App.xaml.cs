using System.Windows;
using Elevator.MVVM.View;
using Elevator.Services;
using Russkyc.DependencyInjection.Implementations;

namespace Elevator;

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