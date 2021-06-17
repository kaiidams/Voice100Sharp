using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Voice100Sharp
{
    class Program
    {
        static VoiceSession _voiceSess;
        static int vid = 0;

        static void Main(string[] args)
        {
            string appDirPath = AppDomain.CurrentDomain.BaseDirectory;
            //_voiceSess = new VoiceSession(Path.Combine(appDirPath, "stt_en_conv_base_ctc-20210617.onnx"));
            _voiceSess = new VoiceSession(Path.Combine(appDirPath, "stt_en_quartznet5x3_ctc-20210616.onnx"));
            _voiceSess.OnDeactivated += OnDeactivated;

            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var cap = WaveIn.GetCapabilities(i);
                Console.WriteLine(cap.ProductName);
            }

            IWaveIn waveIn = new WaveInEvent() { DeviceNumber = 0 };

            waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;

            waveIn.StartRecording();
            Console.ReadLine();
            waveIn.StopRecording();
        }

        private static void OnDeactivated(short[] audio, float[] melspec)
        {
            string outputFilePath = $"vid-{vid}.wav";
            using (var writer = new WaveFileWriter(outputFilePath, new WaveFormat(16000, 16, 1)))
            {
                writer.WriteSamples(audio, 0, audio.Length);
            }

            using (var o = new FileStream($"vid-{vid}.raw", FileMode.Create, FileAccess.Write))
            {
                var m = MemoryMarshal.Cast<short, byte>(audio).ToArray();
                o.Write(m, 0, m.Length);
            }

            using (var o = new FileStream($"vid-{vid}.bin", FileMode.Create, FileAccess.Write))
            {
                var m = MemoryMarshal.Cast<float, byte>(melspec).ToArray();
                o.Write(m, 0, m.Length);
            }

            vid++;
        }

        private static void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Console.WriteLine("stop");
        }

        private static void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            _voiceSess.AddAudioBytes(e.Buffer, e.BytesRecorded);
        }
    }
}
