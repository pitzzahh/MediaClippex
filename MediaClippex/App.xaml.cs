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
}