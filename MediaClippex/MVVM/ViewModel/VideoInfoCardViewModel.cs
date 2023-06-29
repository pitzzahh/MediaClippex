using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaClippex.MVVM.ViewModel;

public partial class VideoInfoCardViewModel : BaseViewModel
{
    [ObservableProperty]
    private string? _imageUrl;

    [ObservableProperty]
    private string? _title;
    
    [ObservableProperty]
    private string? _duration;
    
    [ObservableProperty]
    private string? _description;
}