using System;
using System.ComponentModel;
using MediaClippex.MVVM.ViewModel;

namespace MediaClippex.MVVM.View;

/// <summary>
///     Interaction logic for MediaClippexView.xaml
/// </summary>
public partial class MediaClippexView
{
    public MediaClippexView()
    {
        InitializeComponent();
        UrlTextBox.Focus();
        DataContext = new MediaClippexViewModel();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        Environment.Exit(0);
    }
}