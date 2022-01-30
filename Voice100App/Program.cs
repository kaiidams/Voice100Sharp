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
    class Program
    {
        static SpeechRecognizerSession _speechRecognizer;
        static SpeechSynthesizer _speechSynthesizer;
        static BufferedWaveProvider bufferedWaveProvider;
        static string _cacheDirectoryPath;
        static string _dataDirectoryPath;
        static byte[] waveData;
        static int waveIndex;
        static WaveOut waveOut;

        static async Task Main(string[] args)
        {
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            _cacheDirectoryPath = Path.Combine(appDirPath, "Cache");
            _dataDirectoryPath = Path.Combine(appDirPath, "Data");
            Directory.CreateDirectory(_dataDirectoryPath);
            await TestSpeechRecognitionAsync();
            await BuildSpeechRecognizerAsync();
            await BuildSpeechSynthesizerAsync();

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var cap = WaveOut.GetCapabilities(i);
                Console.WriteLine(cap.ProductName);
            }

            string alignedText;
            _speechSynthesizer.Speak("Hello, I am a rocket.", out waveData, out alignedText);
            waveOut = new WaveOut();
            bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));
            waveOut.Init(bufferedWaveProvider);
            waveOut.PlaybackStopped += OnPlaybackStopped;
            waveOut.Play();
            waveIndex = 0;
            AddSample();
            var waveIn = CreateWaveIn();
            waveIn.StartRecording();
            Console.ReadLine();
            waveIn.StopRecording();
        }

        static void AddSample()
        {
            //Console.WriteLine("{0}/{1}", bufferedWaveProvider.BufferedBytes, bufferedWaveProvider.BufferLength);
            int len = Math.Min(waveData.Length - waveIndex, bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes);
            if (len > 0)
            {
                bufferedWaveProvider.AddSamples(waveData, waveIndex, len);
                waveIndex += len;
            }
        }

        private static void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (waveIndex >= waveData.Length)
            {
                waveOut.Stop();
            }
            else
            {
                AddSample();
            }
        }

        private static async Task BuildSpeechRecognizerAsync()
        {
            string modelPath;
            using (var httpClient = new HttpClient())
            {
                var downloader = new ModelDownloader(httpClient, _cacheDirectoryPath);
                modelPath = await downloader.MayDownloadAsync(
                    "asr_en_conv_base_ctc-20220126.onnx",
                    "https://github.com/kaiidams/voice100-runtime/releases/download/v1.1.1/asr_en_conv_base_ctc-20220126.onnx",
                    "92801E1E4927F345522706A553E86EEBD1E347651620FC6D69BFA30AB4104B86");
            }
            _speechRecognizer = new SpeechRecognizerSession(modelPath);
            _speechRecognizer.OnSpeechRecognition += OnSpeechRecognition;
        }

        private static async Task BuildSpeechSynthesizerAsync()
        {
            string alignModelPath;
            string audioModelPath;
            using (var httpClient = new HttpClient())
            {
                var downloader = new ModelDownloader(httpClient, _cacheDirectoryPath);
                alignModelPath = await downloader.MayDownloadAsync(
                    "ttsalign_en_conv_base-20210808.onnx",
                    "https://github.com/kaiidams/voice100-runtime/releases/download/v0.1/ttsalign_en_conv_base-20210808.onnx",
                    "D87B80B2C9CC96AC7A4C89C979C62FA3C18BACB381C3C1A3F624A33496DD1FC8");
                audioModelPath = await downloader.MayDownloadAsync(
                    "ttsaudio_en_conv_base-20220107.onnx",
                    "https://github.com/kaiidams/voice100-runtime/releases/download/v1.0.1/ttsaudio_en_conv_base-20220107.onnx",
                    "A20FEC366D1A4856006BBF7CFAC7D989EF02B0C1AF676C0B5E6F318751325A2F");
            }
            _speechSynthesizer = new SpeechSynthesizer(alignModelPath, audioModelPath);
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

        private static void OnSpeechRecognition(short[] audio, float[] melspec, string text)
        {
            string dateString = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string outputFilePath = Path.Combine(_dataDirectoryPath, $"{dateString}.wav");
            WaveFile.WriteWav(outputFilePath, 16000, true, audio);

            string melFilePath = Path.Combine(_dataDirectoryPath, $"{dateString}.bin");
            using (var o = File.OpenWrite(melFilePath))
            {
                var m = MemoryMarshal.Cast<float, byte>(melspec).ToArray();
                o.Write(m, 0, m.Length);
            }

            Console.WriteLine("Recognized: {0}", text);
        }

        private static void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Console.WriteLine("stop");
        }

        private static void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            _speechRecognizer.AddAudioBytes(e.Buffer, e.BytesRecorded);
        }

        private static async Task TestSpeechRecognitionAsync()
        {
            string modelPath;
            using (var httpClient = new HttpClient())
            {
                var downloader = new ModelDownloader(httpClient, _cacheDirectoryPath);
                modelPath = await downloader.MayDownloadAsync(
                    "asr_en_conv_base_ctc-20220126.onnx",
                    "https://github.com/kaiidams/voice100-runtime/releases/download/v1.1.1/asr_en_conv_base_ctc-20220126.onnx",
                    "92801E1E4927F345522706A553E86EEBD1E347651620FC6D69BFA30AB4104B86");
            }
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            string inputDirPath = Path.Combine(appDirPath, "..", "..", "..", "..", "test_data");
            string inputPath = Path.Combine(inputDirPath, "transcript.txt");

            using (var recognizer = new SpeechRecognizer(modelPath))
            using (var reader = File.OpenText(inputPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split('|');
                    string name = parts[0];
                    string targetText = parts[1];
                    string waveFile = Path.Combine(inputDirPath, name);
                    var waveform = WaveFile.ReadWav(waveFile, 16000, true);
                    string predictText = recognizer.Recognize(waveform);
                    Console.WriteLine("{0}|{1}|{2}", name, targetText, predictText);
                }
            }
        }
    }
}
