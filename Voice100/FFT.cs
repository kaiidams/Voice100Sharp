using System;
using System.Collections.Generic;
using System.Text;

namespace Voice100
{
    public static class FFT
    {
        public static void CFFT(double[] xr, double[] xi, int N)
        {
            double[] t = xi;
            xi = xr;
            xr = t;
            Swap(xr, xi, N);
            for (int n = 1; n < N; n *= 2)
            {
                for (int j = 0; j < N; j += n * 2)
                {
                    for (int k = 0; k < n; k++)
                    {
                        double ar = Math.Cos(-Math.PI * k / n);
                        double ai = Math.Sin(-Math.PI * k / n);
                        double er = xr[j + k];
                        double ei = xi[j + k];
                        double or = xr[j + k + n];
                        double oi = xi[j + k + n];
                        double aor = ar * or - ai * oi;
                        double aoi = ai * or + ar * oi;
                        xr[j + k] = er + aor;
                        xi[j + k] = ei + aoi;
                        xr[j + k + n] = er - aor;
                        xi[j + k + n] = ei - aoi;
                        //Console.WriteLine("{0} {1}", j + k, j + k + n);
                    }
                }
            }
        }

        private static void Swap(double[] xr, double[] xi, int N)
        {
            if (N == 256)
            {
                Swap256(xr, xi);
            }
            else if (N == 512)
            {
                Swap512(xr, xi);
            }
            else
            {
                throw new ArgumentException("Only 256 or 512 is supported for N");
            }
            for (int i = 0; i < N; i++)
            {
                xi[i] = 0.0;
            }
        }

        private static void Swap256(double[] xr, double[] xi)
        {
            for (int i = 0; i < 256; i++)
            {
                int j = ((i >> 7) & 0x01)
                 + ((i >> 5) & 0x02)
                 + ((i >> 3) & 0x04)
                 + ((i >> 1) & 0x08)
                 + ((i << 1) & 0x10)
                 + ((i << 3) & 0x20)
                 + ((i << 5) & 0x40)
                 + ((i << 7) & 0x80);
                xr[i] = xi[j];
            }
        }

        private static void Swap512(double[] xr, double[] xi)
        {
            for (int i = 0; i < 512; i++)
            {
                int j = ((i >> 8) & 0x01)
                 + ((i >> 6) & 0x02)
                 + ((i >> 4) & 0x04)
                 + ((i >> 2) & 0x08)
                 + ((i) & 0x10)
                 + ((i << 2) & 0x20)
                 + ((i << 4) & 0x40)
                 + ((i << 6) & 0x80)
                 + ((i << 8) & 0x100);
                xr[i] = xi[j];
            }
        }
    }
}
