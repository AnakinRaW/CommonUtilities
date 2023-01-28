﻿using System;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;

namespace AnakinRaW.CommonUtilities.DownloadManager;

/// <summary>
/// Get's thrown if there could be no <see cref="IDownloadProvider"/> found for a download operation.
/// </summary>
public class DownloadProviderNotFoundException : InvalidOperationException
{
    /// <summary>
    /// Creates the exception
    /// </summary>
    /// <param name="message">The message of the exception</param>
    public DownloadProviderNotFoundException(string message)
        : base(message)
    {
    }
}