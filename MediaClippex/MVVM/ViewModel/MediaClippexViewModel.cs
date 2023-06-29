using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
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

    [ObservableProperty] private bool _isResolved;
    
    [ObservableProperty]
    private ObservableCollection<string> _formats = new();

    [ObservableProperty]
    private string? _selectedFormat;
    
    [ObservableProperty]
    private ObservableCollection<string> _qualities = new();
    
    [ObservableProperty]
    private string? _selectedQuality;

    public MediaClippexViewModel()
    {
        Formats.Add("mp4");
        Formats.Add("mp3");
        Formats.Add("wav");
        Formats.Add("ogg");
        Formats.Add("webm");
        Formats.Add("flac");
        Formats.Add("m4a");
        SelectedFormat = Formats.First();
    }

    [RelayCommand]
    private void Resolve()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            MessageBox.Show("Please enter a URL.");
            return;
        }
        IsResolved = true;
    }

    [RelayCommand]
    private void Download()
    {
        
    }
    
}