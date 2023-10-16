using Russkyc.DependencyInjection.Attributes;

namespace MediaClippex.MVVM.View.Pages;

[Service]
public partial class HomeView
{
    public HomeView()
    {
        InitializeComponent();
        UrlTextBox.Focus();
    }
}