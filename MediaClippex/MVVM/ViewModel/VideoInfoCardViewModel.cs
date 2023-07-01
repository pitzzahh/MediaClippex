using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class VideoInfoCardViewModel : BaseViewModel
{
    [ObservableProperty] private string? _description;

    [ObservableProperty] private string? _duration;

    [ObservableProperty] private string? _imageUrl;

    [ObservableProperty] private string? _title;
}