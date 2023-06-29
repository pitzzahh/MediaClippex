using MediaClippex.MVVM.ViewModel;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex.MVVM.View;

/// <summary>
/// Interaction logic for MediaClippexView.xaml
/// </summary>
public partial class MediaClippexView
{
    
    public MediaClippexView()
    {
        InitializeComponent();
        DataContext = BuilderServices.Resolve<MediaClippexViewModel>();
    }
}