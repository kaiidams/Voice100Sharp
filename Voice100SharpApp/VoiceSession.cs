using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Voice100Sharp
{
    class VoiceSession
    {
        public delegate void DeactivatedEvent(short[] audio, float[] melspec);

        const int SampleRate = 16000;
        const int AudioBytesBufferLength = 10 * SampleRate * sizeof(short);
        const int VadWindowLength = 160;
        const double VoicedDecibelThreshold = -30.0;
        const double ActivateThreshold = 0.7;
        const double DeactivateThreshold = 0.2;

        InferenceSession _inferSess;
        AudioFeatureExtractor _featureExtractor;

        private byte[] _audioBytesBuffer;
        private int _audioBytesBufferWriteOffset;

        private int _audioBufferVadOffset;
        private double _audioLevelExpMovingAverage;

        private bool _isVoiced;
        private double _unvoicedAverageDecibel;
        private double _voicedAverageDecibel;
        private int _zeroCrossing;
        private double _voicedExpMovingAverage;

        private bool _isActive;
        private int _audioBufferActiveOffset;

        public VoiceSession(string onnxPath)
        {
            _inferSess = new InferenceSession(onnxPath);
            _featureExtractor = new AudioFeatureExtractor();
            _audioBytesBuffer = new byte[AudioBytesBufferLength];
            _audioBytesBufferWriteOffset = 0;
            _audioBufferVadOffset = 0;
            _audioLevelExpMovingAverage = 0.0;
            _unvoicedAverageDecibel = VoicedDecibelThreshold;
            _voicedAverageDecibel = VoicedDecibelThreshold;
            _voicedExpMovingAverage = 0.0;
            _audioBufferActiveOffset = 0;
        }

        public bool IsVoiced { get { return _isVoiced; } }
        public double AudioDecibel { get { return 10 * Math.Log10(_audioLevelExpMovingAverage); } }
        public bool IsActive { get { return _isActive; } }
        public DeactivatedEvent OnDeactivated { get; set; }

        public void AddAudioBytes(byte[] audioBytes, int audioBytesLength)
        {
            for (int sourceIndex = 0; sourceIndex < audioBytesLength;)
            {
                int copyLength = Math.Min(_audioBytesBuffer.Length - _audioBytesBufferWriteOffset, audioBytesLength - sourceIndex);
                Array.Copy(audioBytes, sourceIndex, _audioBytesBuffer, _audioBytesBufferWriteOffset, copyLength);
                sourceIndex += copyLength;
                _audioBytesBufferWriteOffset += copyLength;
                if (_audioBytesBufferWriteOffset >= _audioBytesBuffer.Length)
                {
                    _audioBytesBufferWriteOffset = 0;
                }
            }

            var audioBuffer = MemoryMarshal.Cast<byte, short>(_audioBytesBuffer);
            int audioBufferWriteOffset = _audioBytesBufferWriteOffset / sizeof(short);

            UpdateAudioLevel(audioBuffer, audioBufferWriteOffset);
#if false
            while (audioBufferWriteIndex)
            waveBuffer.
            short[] waveBuffer = new short[e.BytesRecorded / 2];
                Buffer.BlockCopy(e.Buffer, 0, waveBuffer, 0, e.BytesRecorded);
                //var waveBuffer = new WaveBuffer(e.Buffer);
                //waveBuffer.numberOfBytes = e.BytesRecorded;
                for (int i = 0; i < waveBuffer.Length; i++)
                {
                    var sampleBuffer = new float[1 * 100 * 64];
                    if (sampleIndex == sampleBuffer.Length)
                    {
                        var container = new List<NamedOnnxValue>();
                        var input = new DenseTensor<float>(sampleBuffer, new int[3] { 1, 100, 64 });
                        container.Add(NamedOnnxValue.CreateFromTensor<float>("audio", input));
                        var res = sess.Run(container, new string[] { "logits:0" });
                        foreach (var score in res)
                        {
                            var s = score.AsTensor<float>();
                            float m = -10000.0f;
                            int k = -1;
                            for (int l = 0; l < s.Dimensions[0]; l++)
                            {
                                for (int j = 0; j < s.Dimensions[1]; j++)
                                {
                                    if (m < s[l, j])
                                    {
                                        k = j;
                                        m = s[l, j];
                                    }
                                }
                                Console.WriteLine(k);

                            }
                        }

                        sampleIndex = 0;
                    }
                }
            }
#endif
        }

        private void UpdateAudioLevel(Span<short> audioBuffer, int audioBufferWriteOffset)
        {
            while (_audioBufferVadOffset / VadWindowLength != audioBufferWriteOffset / VadWindowLength)
            {
                double frameAudioLevel = FrameAudioLevel(audioBuffer, _audioBufferVadOffset, VadWindowLength);
                _audioLevelExpMovingAverage = _audioLevelExpMovingAverage * 0.9 + frameAudioLevel * 0.1;
                _audioBufferVadOffset += VadWindowLength;
                if (_audioBufferVadOffset >= audioBuffer.Length)
                {
                    _audioBufferVadOffset = 0;
                }

                _zeroCrossing = FrameZeroCrossing(audioBuffer, _audioBufferVadOffset, VadWindowLength);

                UpdateVoiced();
                _voicedExpMovingAverage = _voicedExpMovingAverage * 0.95 + (IsVoiced ? 1 : 0) * 0.05;

                UpdateActive(audioBuffer);

                DebugInfo();
            }
        }

        private void DebugInfo() 
        {
            string text = string.Format(
                "IsVoiced:{0} IsActive:{1} AudioDecibel:{2:##.#} {3:##.#} {4:##.#} {5} {6}",
                IsVoiced ? "X" : ".",
                IsActive ? "X" : ".",
                AudioDecibel,
                _voicedAverageDecibel,
                _unvoicedAverageDecibel,
                _zeroCrossing,
                (int)(100 * _voicedExpMovingAverage));
            Console.WriteLine(text);
        }

        private void UpdateVoiced()
        {
            double audioDecibel = AudioDecibel;

            _isVoiced = 2 * audioDecibel > _unvoicedAverageDecibel + _voicedAverageDecibel;
            if (_isVoiced)
            {
                _voicedAverageDecibel = Math.Max(_voicedAverageDecibel * 0.9 + audioDecibel * 0.1, -30.0);
            }
            else
            {
                _unvoicedAverageDecibel = Math.Min(-30.0, _unvoicedAverageDecibel * 0.9 + audioDecibel * 0.1);
            }
        }

        private void UpdateActive(Span<short> audioBuffer)
        {
            if (_isActive)
            {
                if (_voicedExpMovingAverage <= DeactivateThreshold)
                {
                    _isActive = false;
                    InvokeDeactivate(audioBuffer);
                }
            }
            else
            {
                if (_voicedExpMovingAverage >= ActivateThreshold)
                {
                    _isActive = true;
                    _audioBufferActiveOffset = _audioBufferVadOffset - VadWindowLength * 100;
                    while (_audioBufferActiveOffset < 0)
                    {
                        _audioBufferActiveOffset += audioBuffer.Length;
                    }
                }
            }
        }

        private void InvokeDeactivate(Span<short> audioBuffer)
        {
            int _audioBufferDeactiveOffset = _audioBufferVadOffset;
            int audioLength = _audioBufferDeactiveOffset - _audioBufferActiveOffset;
            while (audioLength < 0)
            {
                audioLength += audioBuffer.Length;
            }

            // Make a short buffer
            short[] audio = new short[audioLength];
            int audioIndex = _audioBufferActiveOffset;
            for (int i = 0; i < audioLength; i++)
            {
                audio[i] = audioBuffer[audioIndex++];
                if (audioIndex >= audioBuffer.Length) audioIndex = 0;
            }

            audioIndex = _audioBufferActiveOffset;
            short audioMaxShortValue = 0;
            for (int i = 0; i < audioLength; i++)
            {
                short value = Math.Abs(audioBuffer[audioIndex++]);
                if (audioMaxShortValue < value) audioMaxShortValue = value;
                if (audioIndex >= audioBuffer.Length) audioIndex = 0;
            }
            double audioScale = 0.8 / audioMaxShortValue;

            float[] melspec = new float[64 * ((audioLength - 400) / 160 + 1)];
            int melspecOffset = 0;
            while ((_audioBufferActiveOffset + 400) % audioBuffer.Length <= _audioBufferDeactiveOffset)
            {
                _featureExtractor.MelSpectrogram(audioBuffer, _audioBufferActiveOffset, audioScale, melspec, melspecOffset);
                melspecOffset += 64;
                _audioBufferActiveOffset += 160;
                while (_audioBufferActiveOffset >= audioBuffer.Length) _audioBufferActiveOffset -= audioBuffer.Length;
            }
            OnDeactivated(audio, melspec);
        }

        private static double FrameAudioLevel(Span<short> audio, int offset, int length)
        {
            long mag = 0;
            for (int i = offset; i < offset + length; i++)
            {
                long v = audio[i];
                mag += v * v;
            }
            return mag / (length * 32768.0 * 32768.0);
        }


        private static int FrameZeroCrossing(Span<short> audio, int offset, int length)
        {
            double average = 0.0;
            for (int i = offset; i < offset + length; i++)
            {
                average += audio[i];
            }
            average /= length;

            int zeroCrossCount = 0;
            for (int i = offset; i < offset + length - 1; i++)
            {
                if (audio[i] >= average && audio[i + 1] < average || audio[i + 1] >= average && audio[i] < average)
                {
                    zeroCrossCount++;
                }
            }

            return zeroCrossCount;
        }
    }
}
