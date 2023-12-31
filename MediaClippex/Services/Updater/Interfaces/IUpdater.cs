﻿using System;
using System.Threading.Tasks;

namespace MediaClippex.Services.Updater.Interfaces;

public interface IUpdater
{
    string GetLatestVersion();
    Task<UpdateStatus> CheckForUpdates();
    Task PerformUpdate(IProgress<double> progress);
}