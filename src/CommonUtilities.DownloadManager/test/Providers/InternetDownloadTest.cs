using System;

namespace AnakinRaW.CommonUtilities.DownloadManager.Test.Providers;

public abstract class InternetDownloadTest : DownloadProviderTestBase
{
   protected override Uri CreateSource(bool exists)
    {
        if (!exists)
            return new Uri("https://example.com/notFound.txt");
        return new Uri(
            "https://raw.githubusercontent.com/AnakinRaW/CommonUtilities/2ab2e6a26872974422459b0605b26222c9e126ca/README.md");
    }
}