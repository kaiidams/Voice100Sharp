//using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Voice100
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
#if false
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            var settings = config.GetRequiredSection("Settings").Get<Settings>();
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            string cacheDirectoryPath = Path.Combine(appDirPath, "Cache");
            string modelPath = null;
            using (var httpClient = new HttpClient())
            {
                ModelDownloader downloader = new ModelDownloader(httpClient, cacheDirectoryPath);
                modelPath = await downloader.MayDownloadAsync(settings.FileName, settings.URL, settings.SHA256);
            }
#endif
        }
    }
}