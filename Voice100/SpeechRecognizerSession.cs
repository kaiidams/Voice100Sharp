using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Voice100
{
    public class SpeechRecognizerSession : IDisposable
    {
        public delegate void DebugInfoEvent(string text);
        public delegate void SpeechRecognitionEvent(short[] audio, string text);

        public const int DefaultSampleRate = 16000;
        public const int DefaultAudioBytesBufferLength = 10 * DefaultSampleRate * sizeof(short);
        const int VadWindowLength = 160;
        const int MinRepeatVoicedCount = 50;

        private readonly int _sampleRate;

        private SpeechRecognizer _recognizer;

        // Ring buffer
        private byte[] _audioBytesBuffer;
        private int _audioBytesBufferWriteOffset;

        private int _audioBufferVadOffset;
        private int _voicedRepeatCount;

        private int _audioBufferActiveOffset;

        private WebRtcVad _vad;

        private SpeechRecognizerSession()
        {
            _sampleRate = DefaultSampleRate;
            _audioBytesBuffer = new byte[DefaultAudioBytesBufferLength];
            _audioBytesBufferWriteOffset = 0;
            _audioBufferVadOffset = 0;
            _voicedRepeatCount = 0;
            _audioBufferActiveOffset = 0;
            _vad = new WebRtcVad();
            _vad.SetMode(2);
        }

        public SpeechRecognizerSession(string onnxPath) : this()
        {
            _recognizer = new SpeechRecognizer(onnxPath);
        }

        public bool IsVoiced { get; private set; }
        public bool IsActive { get; private set; }
        public DebugInfoEvent OnDebugInfo { get; set; }
        public SpeechRecognitionEvent OnSpeechRecognition { get; set; }

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

            while (_audioBufferVadOffset / VadWindowLength != audioBufferWriteOffset / VadWindowLength)
            {
                UpdateVoiced(audioBuffer);
                UpdateActive(audioBuffer);
            }
            DebugInfo();
        }

        private void DebugInfo()
        {
            string text = string.Format(
                "IsVoiced:{0} IsActive:{1} {2}",
                IsVoiced ? "X" : ".",
                IsActive ? "X" : ".",
                _voicedRepeatCount);
            if (OnDebugInfo != null)
            {
                OnDebugInfo(text);
            }
        }

        private void UpdateVoiced(Span<short> audioBuffer)
        {
            var buffer = audioBuffer.Slice(_audioBufferVadOffset, VadWindowLength).ToArray();
            IsVoiced = _vad.Process(_sampleRate, buffer);
            _audioBufferVadOffset += VadWindowLength;
            if (_audioBufferVadOffset >= audioBuffer.Length)
            {
                _audioBufferVadOffset = 0;
            }
        }

        private void UpdateActive(Span<short> audioBuffer)
        {
            if (IsActive)
            {
                _voicedRepeatCount = IsVoiced ? 0 : (_voicedRepeatCount + 1);
                if (_voicedRepeatCount >= MinRepeatVoicedCount)
                {
                    Console.WriteLine("Deactive");
                    _voicedRepeatCount = 0;
                    IsActive = false;
                    InvokeDeactivate(audioBuffer);
                }
            }
            else
            {
                _voicedRepeatCount = IsVoiced ? (_voicedRepeatCount + 1) : 0;
                if (_voicedRepeatCount >= MinRepeatVoicedCount)
                {
                    Console.WriteLine("Active");
                    _voicedRepeatCount = 0;
                    IsActive = true;
                    _audioBufferActiveOffset = _audioBufferVadOffset - 3 * MinRepeatVoicedCount * VadWindowLength;
                    while (_audioBufferActiveOffset < 0)
                    {
                        _audioBufferActiveOffset += audioBuffer.Length;
                    }
                }
            }
        }

        private void InvokeDeactivate(Span<short> audioBuffer)
        {
            var audio = GetAudioFromBuffer(audioBuffer);
            AnalyzeAudio(audio);
        }

        private short[] GetAudioFromBuffer(Span<short> audioBuffer)
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
            return audio;
        }

        private void AnalyzeAudio(short[] audio)
        {
            string text = _recognizer.Recognize(audio);
            OnSpeechRecognition(audio, text);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_recognizer != null)
                {
                    _recognizer.Dispose();
                    _recognizer = null;
                }
                if (_vad != null)
                {
                    _vad.Dispose();
                    _vad = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
