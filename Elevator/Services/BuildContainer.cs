using Elevator.MVVM.View;
using Russkyc.DependencyInjection.Implementations;
using Russkyc.DependencyInjection.Interfaces;
using MediaClippexUpdaterViewModel = Elevator.MVVM.ViewModel.MediaClippexUpdaterViewModel;

namespace Elevator.Services;

// Container builder
public static class BuildContainer
{
    // Returns container with registered dependencies
    public static IServicesContainer ConfigureServices()
    {
        return new ServicesCollection()
            .AddSingleton<MediaClippexUpdaterViewModel>()
            .AddTransient<MediaClippexUpdater>()
            .Build();
    }
}