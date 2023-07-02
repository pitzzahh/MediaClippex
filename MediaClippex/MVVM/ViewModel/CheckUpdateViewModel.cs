using System;
using System.Diagnostics;
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
    [ObservableProperty] private string _progressInfo = null!;

    private static string? _latestVersion;
    

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
                    Process.Start(new ProcessStartInfo(latestRelease.Assets[0].BrowserDownloadUrl)
                    {
                        UseShellExecute = true
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
                MessageBox.Show("You have the latest version of the application.", "No Updates Available");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error occurred while checking for updates: {ex.Message} Captured Latest Version: {_latestVersion}",
                "Update Error");
            Application.Current.Dispatcher.Invoke(() =>
            {
                MediaClippexViewModel.UpdateWindow.Close();
            });
        }
        finally
        {
            IsProgressIndeterminate = false;
            ProgressInfo = "";
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