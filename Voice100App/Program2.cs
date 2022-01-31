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

        private static Task<YAMNetSession> BuildYAMNetAsync(string model)
        {
            string modelPath = Path.Combine(_cacheDirectoryPath, "yamnet.onnx");
            var yamNetSession = new YAMNetSession(modelPath);
            return Task.FromResult(yamNetSession);
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
