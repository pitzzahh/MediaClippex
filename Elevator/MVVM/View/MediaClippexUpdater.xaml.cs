using System;
using System.ComponentModel;
using System.Threading;
using MediaClippexUpdaterViewModel = Elevator.MVVM.ViewModel.MediaClippexUpdaterViewModel;

namespace Elevator.MVVM.View;

public partial class MediaClippexUpdater
{
    public MediaClippexUpdater()
    {
        Thread.Sleep(2000);
        InitializeComponent();
        DataContext = new MediaClippexUpdaterViewModel();
    }
    
    
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        Environment.Exit(0);
    }
}