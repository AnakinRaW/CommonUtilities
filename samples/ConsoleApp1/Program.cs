using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AnakinRaW.CommonUtilities.DownloadManager;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using AnakinRaW.CommonUtilities.Verification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsoleApp1;

internal class Program
{
    static async Task Main(string[] args)
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IVerificationManager>(sp => new VerificationManager(sp));
        sc.AddSingleton<IDownloadManagerConfigurationProvider>(_ => new ConfigProvider());
        sc.AddLogging(builder => builder.AddConsole(_ => builder.SetMinimumLevel(LogLevel.Trace)));

        var sp = sc.BuildServiceProvider();

        var d = new DownloadManager(sp);

        //Console.Write("Enter the download url: ");
        //var urlText = Console.ReadLine();
        var urlText = "https://republicatwar.com/downloads/TestTool/branches";

        if (!string.IsNullOrEmpty(urlText))
        {
            var ms = new MemoryStream();

            Console.WriteLine(ServicePointManager.SecurityProtocol);

            try
            {
                await d.DownloadAsync(new Uri(urlText), ms, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine();
                Console.WriteLine("Inner: " + e.InnerException?.Message);
            }
            
            var lines = Encoding.ASCII.GetString(ms.ToArray()).Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            ms.Dispose();

            foreach (var line in lines) 
                Console.WriteLine(line);

           
        }
    }
}

class ConfigProvider : DownloadManagerConfigurationProviderBase
{
    protected override IDownloadManagerConfiguration CreateConfiguration()
    {
        return new DownloadManagerConfiguration()
        {
            InternetClient = InternetClient.HttpClient
        };
    }
}