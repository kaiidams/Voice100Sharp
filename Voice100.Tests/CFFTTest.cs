using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Voice100.Tests
{
    [TestClass]
    public class CFFTTest
    {
        private static void CFFTRef(double[] xr, double[] xi, int N)
        {
            double[] yr = new double[N];
            double[] yi = new double[N];
            for (int i = 0; i < N; i++)
            {
                double vr = 0.0;
                double vi = 0.0;
                for (int k = 0; k < N; k++)
                {
                    vr += Math.Cos(-2 * Math.PI * k * i / N) * xr[k];
                    vi += Math.Sin(-2 * Math.PI * k * i / N) * xr[k];
                }
                yr[i] = vr;
                yi[i] = vi;
            }
            for (int i = 0; i < N; i++)
            {
                xr[i] = yr[i];
                xi[i] = yi[i];
            }
        }

        [TestMethod]
        public void TestSpectrogram()
        {
            var rng = new Random();
            for (int N = 256; N <= 512; N *= 2)
            {
                var xr0 = new double[N];
                var xi0 = new double[N];
                var xr1 = new double[N];
                var xi1 = new double[N];
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < N; j++)
                    {
                        xr0[j] = rng.NextDouble();
                        xi0[j] = rng.NextDouble();
                        xr1[j] = xr0[j];
                        xi1[j] = rng.NextDouble();
                    }
                    CFFTRef(xr0, xi0, N);
                    FFT.CFFT(xr1, xi1, N);
                    for (int j = 0; j < N; j++)
                    {
                        Assert.IsTrue(Math.Abs(xr0[j] - xi1[j]) < 1e-10);
                        Assert.IsTrue(Math.Abs(xi0[j] - xr1[j]) < 1e-10);
                    }
                }
            }
        }
    }
}