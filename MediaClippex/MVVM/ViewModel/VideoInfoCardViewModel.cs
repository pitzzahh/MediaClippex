using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class VideoInfoCardViewModel : BaseViewModel
{
    [ObservableProperty]
    public string? _imageUrl;

    [ObservableProperty]
    public string? _title;
    
    [ObservableProperty]
    public string? _duration;
    
    [ObservableProperty]
    public string? _description;
}