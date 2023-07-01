using Russkyc.DependencyInjection.Implementations;
using Russkyc.DependencyInjection.Interfaces;

namespace MediaClippex.Updater;

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