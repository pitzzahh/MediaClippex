using MediaClippex.Updater.MVVM.View;
using MediaClippex.Updater.MVVM.ViewModel;
using Russkyc.DependencyInjection.Implementations;
using Russkyc.DependencyInjection.Interfaces;

namespace MediaClippex.Updater.Services;

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