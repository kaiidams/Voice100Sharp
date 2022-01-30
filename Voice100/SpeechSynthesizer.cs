﻿using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Voice100
{
    public class SpeechSynthesizer : IDisposable
    {
        private readonly CharTokenizer _encoder;
        private readonly WORLDVocoder _vocoder;
        private InferenceSession _ttsAlignInferSess;
        private InferenceSession _ttsAudioInferSess;

        private SpeechSynthesizer()
        {
            _encoder = new CharTokenizer();
            _vocoder = new WORLDVocoder();
        }

        public SpeechSynthesizer(byte[] ttsAlignORTModel, byte[] ttsAudioORTModel) : this()
        {
            _ttsAlignInferSess = new InferenceSession(ttsAlignORTModel);
            _ttsAudioInferSess = new InferenceSession(ttsAudioORTModel);
        }

        public SpeechSynthesizer(string ttsAlignORTModel, string ttsAudioORTModel) : this()
        {
            _ttsAlignInferSess = new InferenceSession(ttsAlignORTModel);
            _ttsAudioInferSess = new InferenceSession(ttsAudioORTModel);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_ttsAlignInferSess != null)
                {
                    _ttsAlignInferSess.Dispose();
                    _ttsAlignInferSess = null;
                }
                if (_ttsAudioInferSess != null)
                {
                    _ttsAudioInferSess.Dispose();
                    _ttsAudioInferSess = null;
                }
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public byte[] Speak(string text)
        {
            short[] audio;
            long[] aligned;
            Speak(text, out audio, out aligned);
            return MemoryMarshal.Cast<short, byte>(audio).ToArray();
        }

        public void Speak(string text, out short[] audio, out string alignedText)
        {
            long[] aligned;
            Speak(text, out audio, out aligned);
            alignedText = _encoder.Decode(aligned);
        }

        public void Speak(string text, out byte[] audio, out string alignedText)
        {
            short[] shortAudio;
            long[] aligned;
            Speak(text, out shortAudio, out aligned);
            audio = MemoryMarshal.Cast<short, byte>(shortAudio).ToArray();
            alignedText = _encoder.Decode(aligned);
        }

        private void Speak(string text, out short[] audio, out long[] aligned)
        {
            long[] encoded = _encoder.Encode(text);
            if (encoded.Length == 0)
            {
                audio = new short[0];
                aligned = new long[0];
            }
            else
            {
                aligned = Align(encoded);
                audio = Predict(aligned);
            }
        }

        private long[] Align(long[] encoded)
        {
            var container = new List<NamedOnnxValue>();
            var encodedData = new DenseTensor<long>(encoded, new int[2] { 1, encoded.Length });
            container.Add(NamedOnnxValue.CreateFromTensor("text", encodedData));
            using (var res = _ttsAlignInferSess.Run(container, new string[] { "align" }))
            {
                var logAlign = res.First().AsTensor<float>();
                var align = new double[logAlign.Dimensions[1], 2];
                for (int i = 0; i < align.GetLength(0); i++)
                {
                    align[i, 0] = Math.Exp(Math.Max(0, logAlign[0, i, 0])) - 1;
                    align[i, 1] = Math.Exp(Math.Max(0, logAlign[0, i, 1])) - 1;
                }
                return MakeAlignText(encoded, align);
            }
        }

        private long[] MakeAlignText(long[] encoded, double[,] align, int head = 5, int tail = 5)
        {
            double t = head + tail;
            for (int i = 0; i < align.GetLength(0); i++)
            {
                t += align[i, 0] + align[i, 1];
            }
            int alignedLength = (int)t;
            long[] aligned = new long[alignedLength];
            t = head;
            for (int i = 0; i < align.GetLength(0); i++)
            {
                t += align[i, 0];
                int s = (int)Math.Round(t);
                t += align[i, 1];
                int e = (int)Math.Round(t);
                if (s == e) s = Math.Max(0, s - 1);
                for (int j = s; j < e; j++)
                {
                    aligned[j] = encoded[i];
                }
            }
            return aligned;
        }

        private short[] Predict(long[] aligned)
        {
            var container = new List<NamedOnnxValue>();
            var alignedData = new DenseTensor<long>(aligned, new int[2] { 1, aligned.Length });
            container.Add(NamedOnnxValue.CreateFromTensor("aligntext", alignedData));
            using (var output = _ttsAudioInferSess.Run(container, new string[] { "f0", "logspc", "codeap" }))
            {
                var outputArray = output.ToArray();
                float[] f0 = outputArray[0].AsTensor<float>().ToArray();
                float[] logspc = outputArray[1].AsTensor<float>().ToArray();
                float[] codeap = outputArray[2].AsTensor<float>().ToArray();
                return _vocoder.Decode(f0, logspc, codeap);
            }
        }
    }
}