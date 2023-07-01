using System.Windows;
using MediaClippex.MVVM.View;
using MediaClippex.Services;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex;

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
        BuilderServices.Resolve<MediaClippexView>().Show();
        base.OnStartup(e);
    }
}