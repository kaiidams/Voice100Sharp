using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Voice100
{
    public class NeMoSpeechRecognizer : ISpeechRecognizer
    {
        private const string Vocabulary = " abcdefghijklmnopqrstuvwxyz'_";
        private readonly Regex mergeRx = new Regex(@"(.)\1+");

        private readonly AudioToMelSpectrogramPreprocessor _preprocessor;
        private readonly InferenceSession _inferSess;
        private readonly int _nMelBands;

        public NeMoSpeechRecognizer(string filePath)
        {
            _nMelBands = 64;
            _preprocessor = new AudioToMelSpectrogramPreprocessor();
            _inferSess = new InferenceSession(filePath);
        }

        public void Dispose()
        {
            _inferSess.Dispose();
        }

        public string Recognize(short[] waveform)
        {
            string text = string.Empty;
            var audioSignal = _preprocessor.Process(waveform);
            audioSignal = Transpose(audioSignal, _nMelBands);
            var container = new List<NamedOnnxValue>();
            var audioSignalData = new DenseTensor<float>(
                audioSignal,
                new int[3] { 1, _nMelBands, audioSignal.Length / _nMelBands });
            container.Add(NamedOnnxValue.CreateFromTensor("audio_signal", audioSignalData));
            using (var res = _inferSess.Run(container, new string[] { "logprobs" }))
            {
                foreach (var score in res)
                {
                    var s = score.AsTensor<float>();
                    int[] preds = new int[s.Dimensions[1]];
                    for (int l = 0; l < preds.Length; l++)
                    {
                        int k = -1;
                        float m = -10000.0f;
                        for (int j = 0; j < s.Dimensions[2]; j++)
                        {
                            if (m < s[0, l, j])
                            {
                                k = j;
                                m = s[0, l, j];
                            }
                        }
                        preds[l] = k;
                    }

                    text = Decode(preds);
                    text = MergeRepeated(text);
                }
            }
            return text;
        }

        public float[] Transpose(float[] x, int cols)
        {
            var y = new float[x.Length];
            int rows = x.Length / cols;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    y[j * rows + i] = x[i * cols + j];  
                }
            }
            return y;
        }

        private string Decode(int[] preds)
        {
            var chars = new char[preds.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = Vocabulary[preds[i]];
            }
            return new string(chars);
        }

        private string MergeRepeated(string text)
        {
            text = mergeRx.Replace(text, "$1");
            text = text.Replace("_", "");
            return text;
        }
    }
}
