using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Voice100.Tests
{
    [TestClass]
    public class Voice100Test
    {
        public const string SampleWAVSpeechFile = "61-70968-0000.wav";

        private static float[] ReadData(string file)
        {
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(appDirPath, "Data", file);
            var bytes = File.ReadAllBytes(path);
            return MemoryMarshal.Cast<byte, float>(bytes).ToArray();
        }

        private static double MSE(float[] a, float[] b)
        {
            if (a.Length != b.Length) throw new ArgumentException();
            int len = Math.Min(a.Length, b.Length);
            double err = 0.0;
            for (int i = 0; i < len; i++)
            {
                double diff = a[i] - b[i];
                err += diff * diff;
            }
            return err / len;
        }

        short[] waveform;
        AudioProcessor processor;

        public Voice100Test()
        {
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            string waveFile = Path.Combine(appDirPath, "Data", SampleWAVSpeechFile);
            waveform = WaveFile.ReadWav(waveFile, 16000, true);
            processor = new AudioProcessor(
                sampleRate: 16000,
                window: "hann",
                windowLength: 400,
                hopLength: 160,
                fftLength: 512,
                //preNormalize: 0.8,
                preemph: 0.0,
                center: false,
                nMelBands: 64,
                melMinHz: 0.0,
                melMaxHz: 0.0,
                htk: true,
                melNormalize: null,
                logOffset: 1e-6,
                postNormalize: false);
        }

        [TestMethod]
        public void TestSpectrogram()
        {
            var x = processor.Spectrogram(waveform);
            AssertMSE("spectrogram.bin", x);
        }

        [TestMethod]
        public void TestMelSpectrogram()
        {
            var x = processor.MelSpectrogram(waveform);
            AssertMSE("melspectrogram.bin", x);
        }

        private void AssertMSE(string path, float[] x)
        {
            var truth = ReadData(path);
            double mse = MSE(truth, x);
            Console.WriteLine("MSE: {0}", mse);
            Assert.IsTrue(mse < 1e-3);
        }
    }
}