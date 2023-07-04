using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Octokit;
using Application = System.Windows.Application;

namespace MediaClippex.MVVM.ViewModel;

public partial class CheckUpdateViewModel : BaseViewModel
{
    private static string Owner => "pitzzahh";
    private static string Repo => "MediaClippex";

    [ObservableProperty] private bool _isProgressIndeterminate;
    [ObservableProperty] private string _progressInfo = string.Empty;

    private static string? _latestVersion = "?";

    public async Task CheckForUpdate()
    {
        try
        {
            IsProgressIndeterminate = true;
            ProgressInfo = "Checking for updates...";
            var currentVersion = ReadCurrentVersion();

            var latestRelease = await new GitHubClient(new ProductHeaderValue(Repo))
                .Repository
                .Release
                .GetLatest(Owner, Repo);

            _latestVersion = latestRelease.TagName;

            if (currentVersion == null)
            {
                MessageBox.Show("Unable to read current version.", "Update Error");
                return;
            }

            if (ShouldUpdate(currentVersion, _latestVersion))
            {
                IsProgressIndeterminate = false;
                ProgressInfo = "";
                var result = MessageBox.Show("An update is available. Do you want to install it?", "Update Available",
                    MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(AppContext.BaseDirectory, "Elevator.exe"),
                        UseShellExecute = true,
                        Verb = "runas" // Set the Verb property to "runas" for elevated permissions
                    };

                    Process.Start(startInfo);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MediaClippexViewModel.UpdateWindow.Close();
                    });
                }
            }
            else
            {
                IsProgressIndeterminate = false;
                MessageBox.Show("You have the latest version of the application.", "No Updates Available");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MediaClippexViewModel.UpdateWindow.Close();
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error occurred while checking for updates: {ex.Message} Captured Latest Version: {_latestVersion}", "Update Error");
            Application.Current.Dispatcher.Invoke(() =>
            {
                MediaClippexViewModel.UpdateWindow.Close();
            });
        }
    }

    private static bool ShouldUpdate(string currentVersion, string latestVersion)
    {
        return new Version(latestVersion) > new Version(currentVersion);
    }

    private static string? ReadCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    }
}