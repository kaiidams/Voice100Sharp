using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voice100
{
    internal class AudioToMelSpectrogramPreprocessor : AudioFeatureExtractor
    {
        private const double InvShortMaxValue = 1.0 / short.MaxValue;

        private readonly int _winLength;
        private readonly int _hopWidth;
        private readonly double[] _window;
        private readonly double[] _melBands;
        private readonly double[] _temp1;
        private readonly double[] _temp2;
        private readonly int _fftLength;
        private readonly int _nMelBands;
        private readonly double _sampleRate;
        private readonly double _logOffset;
        private readonly double _stdOffset;
        private readonly double _preemph;

        public AudioToMelSpectrogramPreprocessor(
            int sampleRate = 16000,
            string window = "hann",
            double windowSize = 0.02,
            double windowStride = 0.01,
            int stftLength = 512,
            int nMelBands = 64, double melMinHz = 0.0, double melMaxHz = 0.0,
            bool htk = false,
            double preemph = 0.97)
        {
            if (melMaxHz == 0.0)
            {
                melMaxHz = sampleRate / 2;
            }
            _sampleRate = sampleRate;
            _winLength = (int)(sampleRate * windowSize); // 320
            _hopWidth = (int)(sampleRate * windowStride); // 160
            _window = Window.MakeWindow(window, _winLength);
            _melBands = MelBands.MakeMelBands(melMinHz, melMaxHz, nMelBands, htk);
            _temp1 = new double[stftLength];
            _temp2 = new double[stftLength];
            _fftLength = stftLength;
            _nMelBands = nMelBands;
            _preemph = preemph;
            _logOffset = Math.Pow(2, -24);
            _stdOffset = 1e-5;
        }

        public float[] Process(short[] waveform)
        {
            int audioSignalLength = waveform.Length / _hopWidth + 1;
            float[] audioSignal = new float[_nMelBands * audioSignalLength]; 
            for (int i = 0; i < audioSignalLength; i++)
            {
                MelSpectrogram(
                    waveform, _hopWidth * i, 
                    audioSignal, i, audioSignalLength);
            }
            Normalize(audioSignal, audioSignalLength);
            return audioSignal;
        }

        private void MelSpectrogram(
            short[] waveform, int waveformPos, 
            float[] melspec, int melspecOffset, int melspecStride)
        {
            ReadFrame(waveform, waveformPos, InvShortMaxValue, _temp1);
            FFT.CFFT(_temp1, _temp2, _fftLength);
            ToSquareMagnitude(_temp2, _temp1, _fftLength);
            ToMelSpec(_temp2, melspec, melspecOffset, melspecStride);
        }

        private void ToMelSpec(
            double[] spec,
            float[] melspec, int melspecOffset, int melspecStride)
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
                melspec[melspecOffset + melspecStride * i] = (float)Math.Log(v + _logOffset);
            }
        }

        private void Normalize(float[] melspec, int melspecStride)
        {
            for (int i = 0; i < _nMelBands; i++)
            {
                double sum = 0;
                for (int j = 0; j < melspecStride; j++)
                {
                    double v = melspec[melspecStride * i + j];
                    sum += v;
                }
                float mean = (float)(sum / melspecStride);
                sum = 0;
                for (int j = 0; j < melspecStride; j++)
                {
                    double v = melspec[melspecStride * i + j] - mean;
                    sum += v * v;
                }
                double std = Math.Sqrt(sum / melspecStride);
                float invStd = (float)(1.0 / (_stdOffset + std));

                for (int j = 0; j < melspecStride; j++)
                {
                    float v = melspec[melspecStride * i + j];
                    melspec[melspecStride * i + j] = (v - mean) * invStd;
                }
            }
        }

        private void ReadFrame(short[] waveform, int offset, double scale, double[] frame)
        {
            int winOffset = (_winLength - _fftLength) / 2;
            int waveformOffset = offset - _fftLength / 2;
            for (int i = 0; i < _fftLength; i++)
            {
                int j = i + winOffset;
                if (j >= 0 && j < _winLength)
                {
                    int k = i + waveformOffset;
                    double v = (k >= 0 && k < waveform.Length) ? waveform[k] : 0;
                    k--;
                    if (k >= 0 && k < waveform.Length) v -= _preemph * waveform[k];
                    frame[i] = scale * v * _window[j];
                }
                else
                {
                    frame[i] = 0;
                }
            }
        }
    }
}
