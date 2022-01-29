using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voice100
{
    internal class AudioPreprocessor
    {
        private AudioFeatureExtractor _extractor;

        public AudioPreprocessor()
        {
            _extractor = new AudioFeatureExtractor();
        }

        public virtual float[] Process(short[] waveform)
        {
            int specLength = (waveform.Length - 400) / 160 + 1;
            float[] spec = new float[64 * specLength];
            int specOffset = 0;
            double scale = 0.8 / MaxAbsValue(waveform);
            for (int waveOffset = 0; waveOffset + 400 < waveform.Length; waveOffset += 160)
            {
                _extractor.MelSpectrogram(waveform, waveOffset, scale, spec, specOffset);
                specOffset += 64;
            }
            return spec;
        }
        private int MaxAbsValue(short[] waveform)
        {
            int maxValue = 1;
            for (int i = 0; i < waveform.Length; i++)
            {
                int value = waveform[i];
                if (value < 0) value = -value;
                if (maxValue < value) maxValue = value;
            }
            return maxValue;
        }
    }
}
