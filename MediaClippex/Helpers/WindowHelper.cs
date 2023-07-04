using System.Windows;

namespace MediaClippex.Helpers;

public static class WindowHelper
{
    public static void ShowOrCloseWindow(Window window)
    {
        if (!window.IsVisible) window.Show();
        else window.Hide();
    }
    
    public static void HideWindow(Window window)
    {
        if (window.IsVisible) window.Hide();
    }
}