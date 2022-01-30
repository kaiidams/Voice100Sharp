using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voice100
{
    internal class AudioToMelSpectrogramPreprocessor : AudioProcessor
    {
        private const double InvShortMaxValue = 1.0 / short.MaxValue;

        private readonly double _logOffset;
        private readonly double _stdOffset;

        public AudioToMelSpectrogramPreprocessor(
            int sampleRate = 16000,
            string window = "hann",
            int stftWindowLength = 400,
            int stftLength = 512,
            int nMelBands = 64,
            double melMinHz = 0.0,
            double melMaxHz = 0.0,
            bool htk = false,
            double preemph = 0.97) : base(
                sampleRate: sampleRate,
                window: window,
                windowLength: stftWindowLength,
                fftLength: stftLength,
                preemph: preemph,
                center: true,
                nMelBands: nMelBands, 
                melMinHz: melMinHz, 
                melMaxHz: melMaxHz,
                htk: htk,
                logOffset: 1e-6)
        {
            if (melMaxHz == 0.0)
            {
                melMaxHz = sampleRate / 2;
            }
            _logOffset = Math.Pow(2, -24);
            _stdOffset = 1e-5;
        }

        public override float[] Process(short[] waveform)
        {
            int audioSignalLength = waveform.Length / _hopLength + 1;
            float[] audioSignal = new float[_nMelBands * audioSignalLength];
            int waveformOffset = 0;
            for (int audioSignalOffset = 0; audioSignalOffset < audioSignal.Length; audioSignalOffset += _nMelBands)
            {
                MelSpectrogram(waveform, waveformOffset, audioSignal, audioSignalOffset);
                waveformOffset += _hopLength;
            }
            Normalize(audioSignal);
            return audioSignal;
        }

        private void MelSpectrogram(
            short[] waveform, int waveformPos, 
            float[] melspec, int melspecOffset)
        {
            ReadFrame(waveform, waveformPos, InvShortMaxValue, _temp1);
            FFT.CFFT(_temp1, _temp2, _fftLength);
            ToSquareMagnitude(_temp2, _temp1, _fftLength);
            ToMelSpec(_temp2, melspec, melspecOffset);
        }

        private void ToMelSpec(
            double[] spec,
            float[] melspec, int melspecOffset)
        {
            for (int i = 0; i < _nMelBands; i++)
            {
                double startHz = _melBands[i];
                double peakHz = _melBands[i + 1];
                double endHz = _melBands[i + 2];
                double v = 0.0;
                int j = (int)(startHz * _fftLength / _sampleRate) + 1;
                while (true)
                {
                    double hz = j * _sampleRate / _fftLength;
                    if (hz > peakHz)
                        break;
                    double r = (hz - startHz) / (peakHz - startHz);
                    v += spec[j] * r * 2 / (endHz - startHz);
                    j++;
                }
                while (true)
                {
                    double hz = j * _sampleRate / _fftLength;
                    if (hz > endHz)
                        break;
                    double r = (endHz - hz) / (endHz - peakHz);
                    v += spec[j] * r * 2 / (endHz - startHz);
                    j++;
                }
                melspec[melspecOffset + i] = (float)Math.Log(v + _logOffset);
            }
        }

        private void Normalize(float[] melspec)
        {
            int melspecLength = melspec.Length / _nMelBands;
            for (int i = 0; i < _nMelBands; i++)
            {
                double sum = 0;
                for (int j = 0; j < melspecLength; j++)
                {
                    double v = melspec[i + _nMelBands * j];
                    sum += v;
                }
                float mean = (float)(sum / melspecLength);
                sum = 0;
                for (int j = 0; j < melspecLength; j++)
                {
                    double v = melspec[i + _nMelBands * j] - mean;
                    sum += v * v;
                }
                double std = Math.Sqrt(sum / melspecLength);
                float invStd = (float)(1.0 / (_stdOffset + std));

                for (int j = 0; j < melspecLength; j++)
                {
                    float v = melspec[i + _nMelBands * j];
                    melspec[i + _nMelBands * j] = (v - mean) * invStd;
                }
            }
        }
    }
}
