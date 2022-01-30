using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Voice100
{
    public static class WaveFile
    {
        /// <summary>
        /// Load a WAV file as a short array.
        /// </summary>
        /// <param name="path">File to read.</param>
        /// <param name="rate"></param>
        /// <param name="mono"></param>
        /// <returns>Waveform data.</returns>
        /// <exception cref="InvalidDataException"></exception>
        public static short[] ReadWav(string path, int rate, bool mono)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.ASCII))
            {
                return ReadWav(reader, rate, mono);
            }
        }

        /// <summary>
        /// Save a short array as a WAV file.
        /// </summary>
        /// <param name="path">File to write.</param>
        /// <param name="rate"></param>
        /// <param name="mono"></param>
        /// <param name="waveform">Waveform data.</param>
        /// <returns></returns>
        public static void WriteWav(string path, int rate, bool mono, short[] waveform)
        {
            using (var stream = File.OpenWrite(path))
            using (var writer = new BinaryWriter(stream, Encoding.ASCII))
            {
                WriteWav(writer, rate, mono, waveform);
            }
        }

        private static short[] ReadWav(BinaryReader reader, int rate, bool mono)
        {
            string fourCC = new string(reader.ReadChars(4));
            if (fourCC != "RIFF")
                throw new InvalidDataException();
            int chunkLen = reader.ReadInt32();
            fourCC = new string(reader.ReadChars(4));
            if (fourCC != "WAVE")
                throw new InvalidDataException();
            while (true)
            {
                fourCC = new string(reader.ReadChars(4));
                chunkLen = reader.ReadInt32();
                if (fourCC == "fmt ")
                {
                    if (chunkLen < 16) throw new InvalidDataException();
                    short formatTag = reader.ReadInt16();
                    if (formatTag != 1) throw new InvalidDataException("Only PCM format is supported");
                    short numChannels = reader.ReadInt16();
                    if (numChannels != (mono ? 1 : 2)) throw new NotSupportedException();
                    int originalRate = reader.ReadInt32();
                    if (originalRate != rate) throw new NotSupportedException();
                    int avgBytesPerSec = reader.ReadInt32();
                    short blockAlign = reader.ReadInt16();
                    short bitsPerSample = reader.ReadInt16();
                    if (avgBytesPerSec * 8 != originalRate * bitsPerSample * numChannels || blockAlign * 8 != bitsPerSample)
                    {
                        throw new InvalidDataException();
                    }
                    if (chunkLen > 16)
                    {
                        byte[] byteData = reader.ReadBytes(chunkLen - 16);
                    }
                }
                else
                {
                    byte[] byteData = reader.ReadBytes(chunkLen);
                    if (fourCC == "data")
                    {
                        return MemoryMarshal.Cast<byte, short>(byteData).ToArray();
                    }
                }
            }
        }

        private static void WriteWav(BinaryWriter writer, int rate, bool mono, short[] waveform)
        {
            short formatTag = 1; // PCM
            short numChannels = (short)(mono ? 1 : 2);
            short bitsPerSample = 16;
            int avgBytesPerSec = rate * bitsPerSample * numChannels / 8;
            short blockAlign = (short)(bitsPerSample / 8);

            string fourCC = "RIFF";
            writer.Write(fourCC.ToCharArray());
            int chunkLen = 36 + waveform.Length * (bitsPerSample / 8);
            writer.Write(chunkLen);

            fourCC = "WAVE";
            writer.Write(fourCC.ToCharArray());

            fourCC = "fmt ";
            chunkLen = 16;

            writer.Write(fourCC.ToCharArray());
            writer.Write(chunkLen);
            writer.Write(formatTag);
            writer.Write(numChannels);
            writer.Write(rate);
            writer.Write(avgBytesPerSec);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);

            fourCC = "data";
            chunkLen = waveform.Length * (bitsPerSample / 8);

            writer.Write(fourCC.ToCharArray());
            writer.Write(chunkLen);
            var waveformBytes = MemoryMarshal.Cast<short, byte>(waveform);
            writer.Write(waveformBytes.ToArray());
        }
    }
}
