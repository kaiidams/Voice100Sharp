using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voice100
{
    public static class MelBands
    {
        public static double[] MakeMelBands(double melMinHz, double melMaxHz, int nMelBanks, bool htk)
        {
            if (htk)
            {
                return HTKMelBands.MakeMelBands(melMinHz, melMaxHz, nMelBanks);
            }
            else
            {
                return SlaneyMelBands.MakeMelBands(melMinHz, melMaxHz, nMelBanks);
            }
        }
    }
}
