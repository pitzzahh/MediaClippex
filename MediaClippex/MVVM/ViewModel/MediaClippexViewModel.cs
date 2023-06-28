using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MediaClippex.MVVM.ViewModel;

public partial class MediaClippexViewModel : ObservableValidator
{
    [ObservableProperty]
    [Required(ErrorMessage = "Please enter a URL.")]
    private string? _url;
    
    [ObservableProperty]
    private string? _status;
    
    [ObservableProperty]
    private string? _showPreview = "Hidden";
    
    [ObservableProperty]
    private string? _quality = "Quality";
    
    [ObservableProperty]
    private string? _imagePreview;

    [ObservableProperty] private bool _canDownload = false;
    
    [ObservableProperty]
    private ObservableCollection<string> _formats = new();
    
    [ObservableProperty]
    private ObservableCollection<string> _qualities = new();


    [RelayCommand]
    private void Download()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            return;
        }
    }
}