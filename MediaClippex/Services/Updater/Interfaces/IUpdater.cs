using System;
using System.Threading.Tasks;

namespace MediaClippex.Services.Updater.Interfaces;

public interface IUpdater
{
    string GetLatestVersion();
    Task<bool> CheckForUpdates();
    Task PerformUpdate(IProgress<double> progress);
}