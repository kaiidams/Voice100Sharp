using System;

namespace Voice100
{
    public class AudioProcessor
    {
        private enum WindowType
        {
            None,
            Preemph,
            Center,
            CenterPreemph
        }

        private enum MelType
        {
            None,
            Slaney
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

        private static MelType GetMelType(string melNormalize)
        {
            if (string.IsNullOrWhiteSpace(melNormalize))
            {
                return MelType.None;
            }
            if (melNormalize == "slaney")
            {
                return MelType.Slaney;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        protected readonly double[] _window;
        private readonly WindowType _windowType;
        protected readonly int _hopLength;
        private readonly double _preNormalize;
        private readonly bool _postNormalize;
        private readonly double _postNormalizeOffset;
        protected readonly double _preemph;
        protected readonly double[] _melBands;
        protected readonly double[] _temp1;
        protected readonly double[] _temp2;
        protected readonly int _fftLength;
        protected readonly int _nMelBands;
        private readonly MelType _melType;
        protected readonly double _sampleRate;
        private readonly double _logOffset;

        public AudioProcessor(
            int sampleRate = 16000,
            string window = "hann",
            int windowLength = 0,
            int hopLength = 512,
            int fftLength = 2048,
            double preNormalize = 0.0,
            double preemph = 0.0,
            bool center = true,
            int nMelBands = 128,
            double melMinHz = 0.0,
            double melMaxHz = 0.0,
            bool htk = false,
            string melNormalize = "slaney",
            double logOffset = 1e-6,
            bool postNormalize = false,
            double postNormalizeOffset = 1e-5)
        {
            if (melMaxHz == 0.0)
            {
                melMaxHz = sampleRate / 2;
            }
            _sampleRate = sampleRate;
            _preNormalize = preNormalize;
            _preemph = preemph;
            // int winLength = (int)(sampleRate * windowSize); // 320
            if (windowLength == 0) windowLength = fftLength;
            _window = Window.MakeWindow(window, windowLength);
            _windowType = GetWindowType(center, preemph);
            _hopLength = hopLength;
            // _hopLength = (int)(sampleRate * windowStride); // 160
            _melBands = MelBands.MakeMelBands(melMinHz, melMaxHz, nMelBands, htk);
            _melType = GetMelType(melNormalize);
            _temp1 = new double[fftLength];
            _temp2 = new double[fftLength];
            _fftLength = fftLength;
            _nMelBands = nMelBands;
            _logOffset = logOffset;
            _postNormalize = postNormalize;
            _postNormalizeOffset = postNormalizeOffset;
        }

        public virtual float[] Process(short[] waveform)
        {
            return MelSpectrogram(waveform);
        }

        public float[] MelSpectrogram(short[] waveform)
        {
            double scale = GetScaleFactor(waveform);
            int outputStep = _nMelBands;
            int outputLength = GetOutputLength(waveform);
            float[] output = new float[outputStep * outputLength];
            int waveformOffset = 0;
            for (int outputOffset = 0; outputOffset < output.Length; outputOffset += outputStep)
            {
                MelSpectrogramStep(waveform, waveformOffset, scale, output, outputOffset);
                waveformOffset += _hopLength;
            }
            if (_postNormalize)
            {
                PostNormalize(output, outputStep);
            }
            return output;
        }

        public float[] Spectrogram(short[] waveform)
        {
            double scale = GetScaleFactor(waveform);
            int outputStep = _fftLength / 2 + 1;
            int outputLength = GetOutputLength(waveform);
            float[] output = new float[outputStep * outputLength];
            int waveformOffset = 0;
            for (int outputOffset = 0; outputOffset < output.Length; outputOffset += outputStep)
            {
                SpectrogramStep(waveform, waveformOffset, scale, output, outputOffset, outputStep);
                waveformOffset += _hopLength;
            }
            if (_postNormalize)
            {
                PostNormalize(output, outputStep);
            }
            return output;
        }

        private int GetOutputLength(short[] waveform)
        {
            return (waveform.Length - _window.Length) / _hopLength + 1;
        }

        private double GetScaleFactor(short[] waveform)
        {
            double scale;
            if (_preNormalize > 0)
            {
                scale = _preNormalize / MaxAbsValue(waveform);
            }
            else
            {
                scale = 1.0 / short.MaxValue;
            }

            return scale;
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

        public void SpectrogramStep(short[] waveform, int waveformOffset, double scale, float[] output, int outputOffset, int outputSize)
        {
            ReadFrame(waveform, waveformOffset, scale, _temp1);
            FFT.CFFT(_temp1, _temp2, _fftLength);
            ToSquareMagnitude(_temp2, _temp1, _fftLength);
            ToSpectrogram(_temp2, output, outputOffset, outputSize);
        }

        public void MelSpectrogramStep(short[] waveform, int waveformOffset, double scale, float[] output, int outputOffset)
        {
            ReadFrame(waveform, waveformOffset, scale, _temp1);
            FFT.CFFT(_temp1, _temp2, _fftLength);
            ToSquareMagnitude(_temp2, _temp1, _fftLength);
            ToMelSpectrogram(_temp2, output, outputOffset);
        }

        private void ToSpectrogram(double[] input, float[] output, int outputOffset, int outputSize)
        {
            for (int i = 0; i < outputSize; i++)
            {
                double value = Math.Log(input[i] + _logOffset);
                output[outputOffset + i] = (float)value;
            }
        }

        private void ToMelSpectrogram(double[] spec, float[] melspec, int melspecOffset)
        {
            switch (_melType)
            {
                case MelType.None:
                    ToMelSpectrogramNone(spec, melspec, melspecOffset);
                    break;
                case MelType.Slaney:
                    ToMelSpectrogramSlaney(spec, melspec, melspecOffset);
                    break;
            }
        }

        private void ToMelSpectrogramNone(double[] spec, float[] melspec, int melspecOffset)
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

        private void ToMelSpectrogramSlaney(double[] spec, float[] melspec, int melspecOffset)
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

        private static void ToMagnitude(double[] xr, double[] xi, int length)
        {
            for (int i = 0; i < length; i++)
            {
                xr[i] = Math.Sqrt(xr[i] * xr[i] + xi[i] * xi[i]);
            }
        }

        private static void ToSquareMagnitude(double[] xr, double[] xi, int length)
        {
            for (int i = 0; i < length; i++)
            {
                xr[i] = xr[i] * xr[i] + xi[i] * xi[i];
            }
        }

        private void PostNormalize(float[] output, int outputStep)
        {
            int melspecLength = output.Length / outputStep;
            for (int i = 0; i < outputStep; i++)
            {
                double sum = 0;
                for (int j = 0; j < melspecLength; j++)
                {
                    double v = output[i + outputStep * j];
                    sum += v;
                }
                float mean = (float)(sum / melspecLength);
                sum = 0;
                for (int j = 0; j < melspecLength; j++)
                {
                    double v = output[i + outputStep * j] - mean;
                    sum += v * v;
                }
                double std = Math.Sqrt(sum / melspecLength);
                float invStd = (float)(1.0 / (_postNormalizeOffset + std));

                for (int j = 0; j < melspecLength; j++)
                {
                    float v = output[i + outputStep * j];
                    output[i + outputStep * j] = (v - mean) * invStd;
                }
            }
        }
    }
}
