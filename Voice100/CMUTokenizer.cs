using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Voice100
{
    internal class CMUTokenizer
    {
        private static readonly string[] DefaultVocab;
        private readonly IDictionary<string, long> _v2i;
        private readonly string[] _i2v;

        static CMUTokenizer()
        {
            DefaultVocab = new[] {
                "_",
                "AA0", "AA1", "AA2", "AE0", "AE1", "AE2", "AH0", "AH1", "AH2", "AO0",
                "AO1", "AO2", "AW0", "AW1", "AW2", "AY0", "AY1", "AY2", "B", "CH", "D", "DH",
                "EH0", "EH1", "EH2", "ER0", "ER1", "ER2", "EY0", "EY1",
                "EY2", "F", "G", "HH",
                "IH0", "IH1", "IH2", "IY0", "IY1", "IY2", "JH", "K", "L",
                "M", "N", "NG", "OW0", "OW1",
                "OW2", "OY0", "OY1", "OY2", "P", "R", "S", "SH", "T", "TH",
                "UH0", "UH1", "UH2", "UW",
                "UW0", "UW1", "UW2", "V", "W", "Y", "Z", "ZH"
            };
        }

        public CMUTokenizer() : this(DefaultVocab)
        {
        }

        public CMUTokenizer(string[] vocab)
        {
            _i2v = vocab;
            _v2i = new Dictionary<string, long>();
            for (int i = 0; i < _i2v.Length; i++) _v2i[_i2v[i]] = i;
        }

        public int VocabSize => _i2v.Length;

        public long[] Encode(string[] tokens)
        {
            long[] encoded = new long[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                _v2i.TryGetValue(tokens[i], out encoded[i]);
            }
            return encoded;
        }

        public string[] Decode(long[] encoded)
        {
            var tokens = new string[encoded.Length];
            for (int i = 0; i < encoded.Length; i++)
            {
                long index = encoded[i];
                if (index < 0 || index >= _i2v.Length) index = 0;
                tokens[i] = _i2v[(int)index];
            }
            return tokens;
        }

        public string MergeRepeated(string text)
        {
            throw new NotImplementedException();
        }
    }
}