using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Voice100;

namespace Voice100App
{
    class Program
    {
        static SpeechRecognizerSession _speechRecognizer;
        static SpeechSynthesizer _speechSynthesizer;
        static int voiceId = 0;
        static BufferedWaveProvider bufferedWaveProvider;
        static byte[] waveData;
        static int waveIndex;
        static WaveOut waveOut;

        static void Main(string[] args)
        {
            TestSpeechRecognition();
            CreateSpeechRecognizer();
            CreateSpeechSynthesizer();

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

        private static void CreateSpeechRecognizer()
        {
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            string modelPath = Path.Combine(appDirPath, "Assets", "asr_en_conv_base_ctc-20220126.all.ort");
            _speechRecognizer = new SpeechRecognizerSession(modelPath);
            _speechRecognizer.OnSpeechRecognition += OnSpeechRecognition;
        }

        private static void CreateSpeechSynthesizer()
        {
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            string alignModelPath = Path.Combine(appDirPath, "Assets", "ttsalign_en_conv_base-20210808.all.ort");
            string audioModelPath = Path.Combine(appDirPath, "Assets", "ttsaudio_en_conv_base-20220107.all.ort");
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
            string outputFilePath = $"vid-{voiceId}.wav";
            using (var writer = new WaveFileWriter(outputFilePath, new WaveFormat(16000, 16, 1)))
            {
                writer.WriteSamples(audio, 0, audio.Length);
            }

            using (var o = new FileStream($"vid-{voiceId}.raw", FileMode.Create, FileAccess.Write))
            {
                var m = MemoryMarshal.Cast<short, byte>(audio).ToArray();
                o.Write(m, 0, m.Length);
            }

            using (var o = new FileStream($"vid-{voiceId}.bin", FileMode.Create, FileAccess.Write))
            {
                var m = MemoryMarshal.Cast<float, byte>(melspec).ToArray();
                o.Write(m, 0, m.Length);
            }

            Console.WriteLine("Recognized: {0}", text);

            voiceId++;
        }

        private static void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Console.WriteLine("stop");
        }

        private static void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            _speechRecognizer.AddAudioBytes(e.Buffer, e.BytesRecorded);
        }

        private static void TestSpeechRecognition()
        {
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            string inputDirPath = Path.Combine(appDirPath, "..", "..", "..", "..", "test_data");
            string inputPath = Path.Combine(inputDirPath, "transcript.txt");
            string modelPath = Path.Combine(appDirPath, "Assets", "asr_en_conv_base_ctc-20220126.all.ort");

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
