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