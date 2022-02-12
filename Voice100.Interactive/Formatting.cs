using System;

namespace Voice100.Interactive
{
    public static class Formatting
    {
        public static Audio Audio(short[] waveform, int rate)
        {
            return new Audio(waveform, rate);
        }
    }
}
