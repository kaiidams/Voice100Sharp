using Microsoft.ML.OnnxRuntime;
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
        private readonly string _modelType;
        private readonly CharTokenizer _encoder;
        private readonly CMUTokenizer _outputTokenizer;
        private readonly WORLDVocoder _vocoder;
        private InferenceSession _ttsAlignInferSess;
        private InferenceSession _ttsAudioInferSess;

        public SpeechSynthesizer(byte[] ttsAlignORTModel, byte[] ttsAudioORTModel, string modelType) : this(modelType)
        {
            _ttsAlignInferSess = new InferenceSession(ttsAlignORTModel);
            _ttsAudioInferSess = new InferenceSession(ttsAudioORTModel);
        }

        public SpeechSynthesizer(string ttsAlignORTModel, string ttsAudioORTModel, string modelType) : this(modelType)
        {
            _ttsAlignInferSess = new InferenceSession(ttsAlignORTModel);
            _ttsAudioInferSess = new InferenceSession(ttsAudioORTModel);
        }

        private SpeechSynthesizer(string modelType)
        {
            _modelType = modelType;
            _encoder = new CharTokenizer();
            _vocoder = new WORLDVocoder();
            if (modelType == "voice100_mt")
            {
                _outputTokenizer = new CMUTokenizer();
            }
            else
            {
                _outputTokenizer = null;
            }
        }

        public bool UseMultiTask => _modelType == "voice100_mt";

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
            Speak(text, out audio);
            return MemoryMarshal.Cast<short, byte>(audio).ToArray();
        }

        public void Speak(string text, out byte[] audio, out string alignedText)
        {
            short[] shortAudio;
            Speak(text, out shortAudio, out alignedText);
            audio = MemoryMarshal.Cast<short, byte>(shortAudio).ToArray();
        }

        public void Speak(string text, out byte[] audio, out string[] phonemes)
        {
            short[] shortAudio;
            Speak(text, out shortAudio, out phonemes);
            audio = MemoryMarshal.Cast<short, byte>(shortAudio).ToArray();
        }

        private void Speak(string text, out short[] audio)
        {
            long[] encoded = _encoder.Encode(text);
            if (encoded.Length == 0)
            {
                audio = Array.Empty<short>();
            }
            else
            {
                long[] aligned = Align(encoded);
                audio = Predict(aligned);
            }
        }

        public void Speak(string text, out short[] audio, out string aligneText)
        {
            long[] encoded = _encoder.Encode(text);
            if (encoded.Length == 0)
            {
                audio = Array.Empty<short>();
                aligneText = string.Empty;
            }
            else
            {
                long[] aligned = Align(encoded);
                audio = Predict(aligned);
                aligneText = _encoder.Decode(aligned);
            }
        }

        public void Speak(string text, out short[] audio, out string[] phonemes)
        {
            long[] encoded = _encoder.Encode(text);
            if (encoded.Length == 0)
            {
                audio = Array.Empty<short>();
                phonemes = Array.Empty<string>();
            }
            else
            {
                long[] aligned = Align(encoded);
                long[] output;
                audio = PredictV1MT(aligned, out output);
                phonemes = _outputTokenizer.Decode(output);
            }
        }

        private long[] Align(long[] encoded)
        {
            if (_modelType == "voice100_v2")
            {
                return AlignV2(encoded);
            }
            else
            {
                return AlignV1(encoded);
            }
        }

        private long[] AlignV1(long[] encoded)
        {
            var container = new List<NamedOnnxValue>();
            var encodedData = new DenseTensor<long>(encoded, new int[2] { 1, encoded.Length });
            container.Add(NamedOnnxValue.CreateFromTensor("text", encodedData));
            using (var res = _ttsAlignInferSess.Run(container, new string[] { "align" }))
            {
                var logAlign = res.First().AsTensor<float>();
                var align = new double[logAlign.Dimensions[1], 2];
                for (int i = 0; i < align.GetLength(1); i++)
                {
                    for (int j = 0; j < align.GetLength(0); j++)
                    {
                        align[j, i] = Math.Exp(Math.Max(0, logAlign[0, j, i])) - 1;
                    }
                }
                return MakeAlignText(encoded, align);
            }
        }

        private long[] AlignV2(long[] encoded)
        {
            var container = new List<NamedOnnxValue>();
            var encodedData = new DenseTensor<long>(encoded, new int[2] { 1, encoded.Length });
            container.Add(NamedOnnxValue.CreateFromTensor("text", encodedData));
            var encodedLengthData = new DenseTensor<long>(new long[] { encoded.Length }, new int[1] { 1 });
            container.Add(NamedOnnxValue.CreateFromTensor("text_len", encodedLengthData));
            using (var res = _ttsAlignInferSess.Run(container, new string[] { "align" }))
            {
                var flatAlign = res.First().AsTensor<float>();//.ToArray<float>();
                var align = new double[flatAlign.Length / 2, 2];
                for (int i = 0; i < align.GetLength(1); i++)
                {
                    for (int j = 0; j < align.GetLength(0); j++)
                    {
                        align[j, i] = flatAlign[0, j, i];
                    }
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
            int alignedLength = (int)(t - align[0, 0]);
            long[] aligned = new long[alignedLength];
            t = head;
            for (int i = 0; i < align.GetLength(0); i++)
            {
                if (i > 0) t += align[i, 0];
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
            if (_modelType == "voice100_v2")
            {
                return PredictV2(aligned);
            }
            else
            {
                return PredictV1(aligned);
            }
        }

        private short[] PredictV1(long[] aligned)
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

        private short[] PredictV1MT(long[] aligned, out long[] outputText)
        {
            var container = new List<NamedOnnxValue>();
            var alignedData = new DenseTensor<long>(aligned, new int[2] { 1, aligned.Length });
            container.Add(NamedOnnxValue.CreateFromTensor("aligntext", alignedData));
            using (var output = _ttsAudioInferSess.Run(container, new string[] { "f0", "logspc", "codeap", "logits" }))
            {
                var outputArray = output.ToArray();
                float[] f0 = outputArray[0].AsTensor<float>().ToArray();
                float[] logspc = outputArray[1].AsTensor<float>().ToArray();
                float[] codeap = outputArray[2].AsTensor<float>().ToArray();
                float[] logits = outputArray[3].AsTensor<float>().ToArray();
                outputText = ArgMax(logits);
                return _vocoder.Decode(f0, logspc, codeap);
            }
        }

        private short[] PredictV2(long[] aligned)
        {
            var container = new List<NamedOnnxValue>();
            var alignedData = new DenseTensor<long>(aligned, new int[2] { 1, aligned.Length });
            container.Add(NamedOnnxValue.CreateFromTensor("aligntext", alignedData));
            var alignedLengthData = new DenseTensor<long>(new long[] { aligned.Length }, new int[1] { 1 });
            container.Add(NamedOnnxValue.CreateFromTensor("aligntext_len", alignedLengthData));
            using (var output = _ttsAudioInferSess.Run(container, new string[] { "f0", "logspc", "codeap" }))
            {
                var outputArray = output.ToArray();
                float[] f0 = outputArray[0].AsTensor<float>().ToArray();
                float[] logspc = outputArray[1].AsTensor<float>().ToArray();
                float[] codeap = outputArray[2].AsTensor<float>().ToArray();
                return _vocoder.Decode(f0, logspc, codeap);
            }
        }

        private long[] ArgMax(float[] logits)
        {
            int vocabSize = _outputTokenizer.VocabSize;
            int audioLen = logits.Length / vocabSize;
            long[] encoded = new long[audioLen];
            for (int i = 0; i < audioLen; i++)
            {
                float maxValue = float.MinValue;
                long maxArg = 0;
                for (int j = 0; j < vocabSize; j++)
                {
                    float value = logits[j * audioLen + i];
                    if (maxValue < value)
                    {
                        maxArg = j;
                        maxValue = value;
                    }
                }
                encoded[i] = maxArg;
            }
            return encoded;
        }
    }
}