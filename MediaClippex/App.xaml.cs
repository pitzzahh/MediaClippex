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
        var mediaClippexView = BuilderServices.Resolve<MediaClippexView>();
        MainWindow = mediaClippexView;
        mediaClippexView.Show();
        base.OnStartup(e);
    }

}