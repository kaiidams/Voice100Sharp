﻿using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Voice100
{
    public class YAMNetSession
    {
        int _sampleIndex;
        float[] _sampleBuffer;
        AudioFeatureBuffer _featureBuffer;
        InferenceSession _sess;
        string[] _classMap;
        private const int NumClasses = 521;

        public YAMNetSession(string modelPath)
        {
            _sess = new InferenceSession(modelPath);
            _sampleBuffer = new float[400 + 95 * 160];
            _featureBuffer = new AudioFeatureBuffer();

            _classMap = new string[NumClasses];
#if false
            string fileName = "yamnet_class_map.txt";
            using (var reader = File.OpenText(fileName))
            {
                string line = reader.ReadLine(); // Discard the first line.
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string[] parts = line.Split(',');
                        int classId = int.Parse(parts[0]);
                        _classMap[classId] = parts[2];
                    }
                }
            }
#endif
        }

        public void AddAudioBytes(byte[] audioBytes, int audioBytesLength)
        {
            var temp = MemoryMarshal.Cast<byte, short>(audioBytes);
            var waveform = new float[audioBytesLength / 2];
            for (int i = 0; i < waveform.Length; i++)
            {
                waveform[i] = temp[i] / (float)short.MaxValue;
            }

            int waveformOffset = 0;
            while (waveformOffset < waveform.Length)
            {
                int written = _featureBuffer.Write(waveform, waveformOffset, waveform.Length - waveformOffset);
                waveformOffset += written;

                while (_featureBuffer.OutputCount >= 96 * 64)
                {
                    try
                    {
                        var features = new float[96 * 64];
                        Array.Copy(_featureBuffer.OutputBuffer, 0, features, 0, 96 * 64);
                        Analyze(features);
                    }
                    finally
                    {
                        _featureBuffer.ConsumeOutput(48 * 64);
                    }
                }
            }
        }

        public void Analyze(float[] features)
        {
            var container = new List<NamedOnnxValue>();
            var input = new DenseTensor<float>(features, new int[4] { 1, 1, 96, 64 });
            container.Add(NamedOnnxValue.CreateFromTensor<float>("mfcc:0", input));
            var res = _sess.Run(container, new string[] { "activation_10" });
            foreach (var score in res)
            {
                var s = score.AsTensor<float>();
                float m = -10000.0f;
                int k = -1;
                for (int l = 0; l < s.Dimensions[0]; l++)
                {
                    for (int j = 0; j < s.Dimensions[1]; j++)
                    {
                        if (m < s[l, j])
                        {
                            k = j;
                            m = s[l, j];
                        }
                    }
                    Console.WriteLine(k);
                }
            }
        }
    }
}
