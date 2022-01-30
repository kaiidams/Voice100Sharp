using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Voice100
{
    public class SpeechRecognizer : IDisposable
    {
        private readonly AudioProcessor _processor;
        private readonly CharTokenizer _tokenizer;
        private readonly InferenceSession _inferSess;
        private readonly int _nMelBands;

        public SpeechRecognizer(string filePath)
        {
            _nMelBands = 64;
            _processor = new AudioProcessor();
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
            var audioSignal = _processor.Process(waveform);
            var container = new List<NamedOnnxValue>();
            var audioSignalData = new DenseTensor<float>(
                audioSignal,
                new int[3] { 1, audioSignal.Length / _nMelBands, _nMelBands });
            container.Add(NamedOnnxValue.CreateFromTensor("audio", audioSignalData));
            using (var res = _inferSess.Run(container, new string[] { "logits" }))
            {
                foreach (var score in res)
                {
                    var preds = ArgMax(score.AsTensor<float>());
                    text = _tokenizer.Decode(preds);
                    text = _tokenizer.MergeRepeated(text);
                }
            }
            return text;
        }

        private long[] ArgMax(Tensor<float> score)
        {
            long[] preds = new long[score.Dimensions[1]];
            for (int l = 0; l < preds.Length; l++)
            {
                int k = -1;
                float m = -10000.0f;
                for (int j = 0; j < score.Dimensions[2]; j++)
                {
                    if (m < score[0, l, j])
                    {
                        k = j;
                        m = score[0, l, j];
                    }
                }
                preds[l] = k;
            }

            return preds;
        }
    }
}
