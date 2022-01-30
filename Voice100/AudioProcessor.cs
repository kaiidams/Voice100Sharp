﻿using System;

namespace Voice100
{
    class AudioProcessor
    {
        private enum WindowType
        {
            None,
            Preemph,
            Center,
            CenterPreemph
        }

        private static WindowType GetWindowType(bool center, double preemph)
        {
            if (preemph == 0.0)
            {
                return center ? WindowType.Center : WindowType.None;
            }
            else
            {
                return center ? WindowType.CenterPreemph : WindowType.Preemph;
            }
        }

        protected readonly double[] _window;
        private readonly WindowType _windowType;
        protected readonly int _hopLength;
        private readonly double _preNormalize;
        protected readonly double _preemph;
        protected readonly double[] _melBands;
        protected readonly double[] _temp1;
        protected readonly double[] _temp2;
        protected readonly int _fftLength;
        protected readonly int _nMelBands;
        protected readonly double _sampleRate;
        private readonly double _logOffset;

        public AudioProcessor(
            int sampleRate = 16000,
            string window = "hann",
            int windowLength = 400,
            int hopLength = 160,
            int fftLength = 512,
            double preNormalize = 0.8,
            double preemph = 0.0,
            bool center = false,
            int nMelBands = 64, 
            double melMinHz = 0.0,
            double melMaxHz = 0.0,
            bool htk = true,
            double logOffset = 1e-6)
        {
            if (melMaxHz == 0.0)
            {
                melMaxHz = sampleRate / 2;
            }
            _sampleRate = sampleRate;
            _preNormalize = preNormalize;
            _preemph = preemph;
            // int winLength = (int)(sampleRate * windowSize); // 320
            _window = Window.MakeWindow(window, windowLength);
            _windowType = GetWindowType(center, preemph);
            _hopLength = hopLength;
            // _hopLength = (int)(sampleRate * windowStride); // 160
            _melBands = MelBands.MakeMelBands(melMinHz, melMaxHz, nMelBands, htk);
            _temp1 = new double[fftLength];
            _temp2 = new double[fftLength];
            _fftLength = fftLength;
            _nMelBands = nMelBands;
            _logOffset = logOffset;
        }

        public virtual float[] Process(short[] waveform)
        {
            int melspecLength = (waveform.Length - _window.Length) / _hopLength + 1;
            float[] melspec = new float[_nMelBands * melspecLength];
            double scale;
            if (_preNormalize > 0)
            {
                scale = _preNormalize / MaxAbsValue(waveform);
            }
            else
            {
                scale = 1.0 / short.MaxValue;
            }
            int waveformOffset = 0;
            for (int melspecOffset = 0; melspecOffset < melspec.Length; melspecOffset += _nMelBands)
            {
                MelSpectrogram(waveform, waveformOffset, scale, melspec, melspecOffset);
                waveformOffset += _hopLength;
            }
            return melspec;
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

        protected void Spectrogram(short[] waveform, int waveformOffset, double scale, float[] spec, int specOffset)
        {
            ReadFrame(waveform, waveformOffset, scale, _temp1);
            FFT.CFFT(_temp1, _temp2, _fftLength);
            ToMagnitude(_temp2, _temp1, _fftLength);
            int specLength = _fftLength / 2 + 1;
            for (int i = 0; i < specLength; i++)
            {
                float value = (float)(20.0 * Math.Log(_temp2[i] + _logOffset));
                spec[specOffset + i] = value;
            }
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

        protected void ReadFrame(short[] waveform, int offset, double scale, double[] frame)
        {
            switch (_windowType)
            {
                case WindowType.None:
                    ReadFrameNone(waveform, offset, scale, frame);
                    break;
                case WindowType.Preemph:
                    throw new NotImplementedException();
                case WindowType.Center:
                    throw new NotImplementedException();
                case WindowType.CenterPreemph:
                    ReadFrameCenterPreemphasis(waveform, offset, scale, frame);
                    break;
            }
        }

        private void ReadFrameNone(short[] waveform, int offset, double scale, double[] frame)
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

        private void ReadFrameCenterPreemphasis(short[] waveform, int offset, double scale, double[] frame)
        {
            int winOffset = (_window.Length - frame.Length) / 2;
            int waveformOffset = offset - frame.Length / 2;
            for (int i = 0; i < frame.Length; i++)
            {
                int j = i + winOffset;
                if (j >= 0 && j < _window.Length)
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
