using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Voice100;

namespace Voice100App
{
    internal class Voice100Test : IDisposable
    {
        //const string AsrModel = "QuartzNet15x5Base-En";
        const string AsrModel = "voice100";

        SpeechRecognizerSession _speechRecognizerSession;
        SpeechSynthesizer _speechSynthesizer;
        BufferedWaveProvider _bufferedWaveProvider;
        string _cacheDirectoryPath;

        string _dataDirectoryPath;
        byte[] _waveData;
        int _waveIndex;
        WaveOut _waveOut;

        public Voice100Test()
        {
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            _cacheDirectoryPath = Path.Combine(appDirPath, "Cache");
            _dataDirectoryPath = Path.Combine(appDirPath, "Data");
            Directory.CreateDirectory(_dataDirectoryPath);
        }

        public async Task RunAsync()
        {
            var recognizer = await BuildSpeechRecognizerAsync(AsrModel);
            _speechRecognizerSession = new SpeechRecognizerSession(recognizer);
            _speechRecognizerSession.OnSpeechRecognition += OnSpeechRecognition;
            _speechSynthesizer = await BuildSpeechSynthesizerAsync();

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var cap = WaveOut.GetCapabilities(i);
                Console.WriteLine(cap.ProductName);
            }

            string alignedText;
            _speechSynthesizer.Speak("Hello, I am a rocket.", out _waveData, out alignedText);
            _waveOut = new WaveOut();
            _bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));
            _waveOut.Init(_bufferedWaveProvider);
            _waveOut.PlaybackStopped += OnPlaybackStopped;
            _waveOut.Play();
            _waveIndex = 0;
            AddSample();
            var waveIn = CreateWaveIn();
            waveIn.StartRecording();
            Console.ReadLine();
            waveIn.StopRecording();
        }

        void AddSample()
        {
            //Console.WriteLine("{0}/{1}", bufferedWaveProvider.BufferedBytes, bufferedWaveProvider.BufferLength);
            int len = Math.Min(_waveData.Length - _waveIndex, _bufferedWaveProvider.BufferLength - _bufferedWaveProvider.BufferedBytes);
            if (len > 0)
            {
                _bufferedWaveProvider.AddSamples(_waveData, _waveIndex, len);
                _waveIndex += len;
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (_waveIndex >= _waveData.Length)
            {
                _waveOut.Stop();
            }
            else
            {
                AddSample();
            }
        }

        private async Task<ISpeechRecognizer> BuildSpeechRecognizerAsync(string model)
        {
            ISpeechRecognizer recognizer;

            if (model == "voice100")
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
                recognizer = new Voice100SpeechRecognizer(modelPath);
            }
            else if (model == "QuartzNet15x5Base-En")
            {
                string modelPath;
                using (var httpClient = new HttpClient())
                {
                    var downloader = new ModelDownloader(httpClient, _cacheDirectoryPath);
                    modelPath = await downloader.MayDownloadAsync(
                        "QuartzNet15x5Base-En.onnx",
                        "https://github.com/kaiidams/NeMoOnnxSharp/releases/download/v1.1.0.pre1/QuartzNet15x5Base-En.onnx",
                        "EE1B72102FD0C5422D088E80F929DBDEE7E889D256A4CE1E412CD49916823695");
                }
                recognizer = new NeMoSpeechRecognizer(modelPath);
            }
            else
            {
                throw new ArgumentException();
            }

            return recognizer;
        }

        private async Task<SpeechSynthesizer> BuildSpeechSynthesizerAsync()
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
            return new SpeechSynthesizer(alignModelPath, audioModelPath);
        }

        private IWaveIn CreateWaveIn()
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

        private void OnSpeechRecognition(short[] audio, string text)
        {
            string dateString = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string outputFilePath = Path.Combine(_dataDirectoryPath, $"{dateString}.wav");
            WaveFile.WriteWAV(outputFilePath, audio, 16000);
            string outputTextPath = Path.Combine(_dataDirectoryPath, $"{dateString}.txt");
            WriteTextFile(outputTextPath, text);
            Console.WriteLine("Recognized: {0}", text);
        }

        private void WriteTextFile(string path, string text)
        {
            using (var stream = File.OpenWrite(path))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(text);
            }
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Console.WriteLine("stop");
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            _speechRecognizerSession.AddAudioBytes(e.Buffer, e.BytesRecorded);
        }

        public async Task TestAsync()
        {
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            string inputDirPath = Path.Combine(appDirPath, "..", "..", "..", "..", "test_data");
            string inputPath = Path.Combine(inputDirPath, "transcript.txt");

            using (var recognizer = await BuildSpeechRecognizerAsync(AsrModel))
            using (var reader = File.OpenText(inputPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split('|');
                    string name = parts[0];
                    string targetText = parts[1];
                    string waveFile = Path.Combine(inputDirPath, name);
                    var waveform = WaveFile.ReadWAV(waveFile, 16000);
                    string predictText = recognizer.Recognize(waveform);
                    Console.WriteLine("{0}|{1}|{2}", name, targetText, predictText);
                }
            }
        }

        public void Dispose()
        {
            if (_speechRecognizerSession != null)
            {
                _speechRecognizerSession.Dispose();
                _speechRecognizerSession = null;
            }
        }
    }
}
