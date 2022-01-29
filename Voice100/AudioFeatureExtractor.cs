﻿using System;

namespace Voice100
{
    class AudioFeatureExtractor
    {
        private readonly double[] _window;
        private readonly double[] _melBands;
        private readonly double[] _temp1;
        private readonly double[] _temp2;
        private readonly int _fftLength;
        private readonly int _nMelBands;
        private readonly double _sampleRate;
        private readonly double _logOffset;

        public AudioFeatureExtractor(
            int sampleRate = 16000,
            string window = "hann",
            int stftWindowLength = 400, int stftLength = 512,
            int nMelBands = 64, double melMinHz = 0.0, double melMaxHz = 0.0,
            bool htk = true,
            double logOffset = 1e-6)
        {
            if (melMaxHz == 0.0)
            {
                melMaxHz = sampleRate / 2;
            }
            _sampleRate = sampleRate;
            _window = Window.MakeWindow(window, stftWindowLength);
            _melBands = MelBands.MakeMelBands(melMinHz, melMaxHz, nMelBands, htk);
            _temp1 = new double[stftLength];
            _temp2 = new double[stftLength];
            _fftLength = stftLength;
            _nMelBands = nMelBands;
            _logOffset = logOffset;
        }

        public void Spectrogram(float[] waveform, int waveformOffset, float[] spec, int specOffset)
        {
            ReadFrame(waveform, waveformOffset, _temp1);
            FFT.CFFT(_temp1, _temp2, _fftLength);
            ToMagnitude(_temp2, _temp1, _fftLength);
            int specLength = _fftLength / 2 + 1;
            for (int i = 0; i < specLength; i++)
            {
                float value = (float)(20.0 * Math.Log(_temp2[i] + _logOffset));
                spec[specOffset + i] = value;
            }
        }

        public void MelSpectrogram(float[] waveform, int waveformOffset, float[] melspec, int melspecOffset)
        {
            ReadFrame(waveform, waveformOffset, _temp1);
            FFT.CFFT(_temp1, _temp2, _fftLength);
            ToSquareMagnitude(_temp2, _temp1, _fftLength);
            ToMelSpec(_temp2, melspec, melspecOffset);
        }

        public void MelSpectrogram(short[] waveform, int waveformOffset, double scale, float[] melspec, int melspecOffset)
        {
            ReadFrame(waveform, waveformOffset, scale, _temp1);
            FFT.CFFT(_temp1, _temp2, _fftLength);
            ToSquareMagnitude(_temp2, _temp1, _fftLength);
            ToMelSpec(_temp2, melspec, melspecOffset);
        }

        private void ToMelSpec(double[] spec, float[] melspec, int melspecOffset)
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
                    v += spec[j] * r;
                    j++;
                }
                while (true)
                {
                    double hz = j * _sampleRate / _fftLength;
                    if (hz > endHz)
                        break;
                    double r = (endHz - hz) / (endHz - peakHz);
                    v += spec[j] * r;
                    j++;
                }
                melspec[melspecOffset + i] = (float)Math.Log(v + _logOffset);
            }
        }

        private void ReadFrame(float[] waveform, int offset, double[] frame)
        {
            for (int i = 0; i < _window.Length; i++)
            {
                frame[i] = waveform[offset + i] * _window[i];
            }
            for (int i = _window.Length; i < frame.Length; i++)
            {
                frame[i] = 0.0;
            }
        }

        private void ReadFrame(short[] waveform, int offset, double scale, double[] frame)
        {
            for (int i = 0; i < _window.Length; i++)
            {
                frame[i] = waveform[offset + i] * _window[i] * scale;
            }
            for (int i = _window.Length; i < frame.Length; i++)
            {
                frame[i] = 0.0;
            }
        }

        static void ToMagnitude(double[] xr, double[] xi, int length)
        {
            for (int i = 0; i < length; i++)
            {
                xr[i] = Math.Sqrt(xr[i] * xr[i] + xi[i] * xi[i]);
            }
        }

        protected static void ToSquareMagnitude(double[] xr, double[] xi, int length)
        {
            for (int i = 0; i < length; i++)
            {
                xr[i] = xr[i] * xr[i] + xi[i] * xi[i];
            }
        }
    }
}
