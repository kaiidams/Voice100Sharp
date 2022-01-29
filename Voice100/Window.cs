using System;
using System.Collections.Generic;
using System.Text;

namespace Voice100
{
    public static class Window
    {
        public static double[] MakeWindow(string window, int length)
        {
            if (window == "hann")
            {
                return MakeHannWindow(length);
            }
            else
            {
                throw new ArgumentException("Unknown windows name");
            }
        }

        private static double[] MakeHannWindow(int length)
        {
            double[] window = new double[length];
            for (int i = 0; i < length; i++)
            {
                window[i] = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (length - 1)));
            }
            return window;
        }
    }
}
