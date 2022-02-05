using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Voice100;

namespace Voice100App
{
    internal class Program2
    {
        static YAMNetSession _yamNetSession;
        static BufferedWaveProvider _bufferedWaveProvider;
        static string _cacheDirectoryPath;
        static WaveOut waveOut;

        public static async Task InteractiveAsync()
        {
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            _cacheDirectoryPath = Path.Combine(appDirPath, "Cache");
            _yamNetSession = await BuildYAMNetAsync("yamnet");

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var cap = WaveOut.GetCapabilities(i);
                Console.WriteLine(cap.ProductName);
            }

            waveOut = new WaveOut();
            _bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));
            waveOut.Init(_bufferedWaveProvider);
            var waveIn = CreateWaveIn();
            waveIn.StartRecording();
            Console.ReadLine();
            waveIn.StopRecording();
        }

        private static async Task<YAMNetSession> BuildYAMNetAsync(string model)
        {
            string modelPath;
            string classMapPath;
            using (var httpClient = new HttpClient())
            {
                var downloader = new ModelDownloader(httpClient, _cacheDirectoryPath);
                modelPath = await downloader.MayDownloadAsync(
                    "yamnet.onnx",
                    "https://github.com/kaiidams/YamNetUnityDemo/raw/main/Assets/YamNetUnity/NNModels/yamnet.onnx",
                    "0AFAEA5521E30D766386DF2AA6AB89E1FFA9E0C0E76E71779A2EB4F95E09941D");
                classMapPath = await downloader.MayDownloadAsync(
                    "yamnet_class_map.csv",
                    "https://github.com/kaiidams/YamNetUnityDemo/raw/main/Assets/YamNetUnity/Resources/yamnet_class_map.csv",
                    "B03D48F9EBE23F69EA825193DE7E736934086A12410F0AF47BABE897B78BC0D3");
            }
            return new YAMNetSession(modelPath, classMapPath);
        }

        private static IWaveIn CreateWaveIn()
        {
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var cap = WaveIn.GetCapabilities(i);
                Console.WriteLine(cap.ProductName);
            }

            IWaveIn waveIn = new WaveInEvent() { DeviceNumber = 0 };

            waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;

            return waveIn;
        }

        private static void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Console.WriteLine("stop");
        }

        private static void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            _yamNetSession.AddAudioBytes(e.Buffer, e.BytesRecorded);
        }
    }
}
