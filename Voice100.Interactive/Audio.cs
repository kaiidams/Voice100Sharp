using System;

namespace Voice100.Interactive
{
    public class Audio
    {
        public const string MimeType = "audio/wav";

        private static string GetDataURL(string mimeType, byte[] bytes)
        {
            return "data:" + mimeType + ";base64," + Convert.ToBase64String(bytes);
        }

        private short[] _waveform;
        private int _rate;

        public Audio(short[] waveform, int rate)
        {
            _waveform = waveform;
            _rate = rate;
        }

        public string GetDataUrl()
        {
            byte[] bytes = WaveFile.GetWAVBytes(_waveform, _rate);
            return GetDataURL(MimeType, bytes);
        }
    }
}
