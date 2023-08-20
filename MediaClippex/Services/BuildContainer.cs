using MediaClippex.MVVM.View;
using MediaClippex.MVVM.ViewModel;
using Russkyc.DependencyInjection.Implementations;
using Russkyc.DependencyInjection.Interfaces;

namespace MediaClippex.Services;

// Container builder
public static class BuildContainer
{
    // Returns container with registered dependencies
    public static IServicesContainer ConfigureServices()
    {
        return new ServicesContainer()
            .AddSingleton<StorageService>()
            .AddSingleton<VideoInfoCardViewModel>()
            .AddSingleton<MediaClippexViewModel>()
            .AddTransient<CheckUpdateViewModel>()
            .AddSingleton<CheckUpdateView>()
            .Build();
    }
}