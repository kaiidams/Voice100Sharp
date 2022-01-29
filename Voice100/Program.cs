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

            string inputDirPath = Path.Combine(appDirPath, "..", "..", "..", "..", "test_data");
            string inputPath = Path.Combine(inputDirPath, "transcript.txt");

            using var recognizer = new SpeechRecognizer(modelPath);
            using var reader = File.OpenText(inputPath);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split("|");
                string name = parts[0];
                string targetText = parts[1];
                string waveFile = Path.Combine(inputDirPath, name);
                var waveform = WaveFile.ReadWav(waveFile, 16000, true);
                string predictText = recognizer.Recognize(waveform);
                Console.WriteLine("{0}|{1}|{2}", name, targetText, predictText);
            }
#endif
        }
    }
}