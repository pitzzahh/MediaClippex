using MediaClippex.Factory;

namespace MediaClippex;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public App()
    {
        InitializeComponent();
        ViewFactory.CreateMediaClippexView();
    }
}