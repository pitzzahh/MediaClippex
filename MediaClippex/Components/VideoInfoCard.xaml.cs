using MediaClippex.MVVM.ViewModel;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex.Components;

public partial class VideoInfoCard
{

    public VideoInfoCard()
    {
        InitializeComponent();
        DataContext = BuilderServices.Resolve<VideoInfoCardViewModel>();
    }

}