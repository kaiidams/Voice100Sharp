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
    public class SpeechRecognizer : IDisposable
    {
        private readonly AudioPreprocessor _preprocessor;
        private readonly CharTokenizer _tokenizer;
        private readonly InferenceSession _inferSess;
        private readonly int _nMelBands;

        public SpeechRecognizer(string filePath)
        {
            _nMelBands = 64;
            _preprocessor = new AudioPreprocessor();
            _tokenizer = new CharTokenizer();
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
            var container = new List<NamedOnnxValue>();
            var audioSignalData = new DenseTensor<float>(
                audioSignal,
                new int[3] { 1, audioSignal.Length / _nMelBands, _nMelBands });
            container.Add(NamedOnnxValue.CreateFromTensor("audio", audioSignalData));
            using (var res = _inferSess.Run(container, new string[] { "logits" }))
            {
                foreach (var score in res)
                {
                    var s = score.AsTensor<float>();
                    long[] preds = new long[s.Dimensions[1]];
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

                    text = _tokenizer.Decode(preds);
                    text = _tokenizer.MergeRepeated(text);
                }
            }
            return text;
        }
    }
}
