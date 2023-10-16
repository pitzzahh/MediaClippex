using System;
using System.ComponentModel;
using System.Windows;
using MediaClippex.MVVM.ViewModel;
using Russkyc.DependencyInjection.Attributes;

namespace MediaClippex.MVVM.View;

/// <summary>
///     Interaction logic for MainView.xaml
/// </summary>
[Service]
public partial class MainView
{
    public MainView(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Application.Current.Shutdown();
        Environment.Exit(0);
        base.OnClosing(e);
    }
}