using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Voice100;

namespace Voice100App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string run = args.Length > 0 ? args[0] : "test";
            switch (run)
            {
                case "test":
                    using (var test = new Voice100Test())
                    {
                        await test.TestAsync();
                    }
                    break;
                case "voice100":
                    using (var test = new Voice100Test())
                    {
                        await test.RunAsync(true, true);
                    }
                    break;
                case "yamnet":
                    using (var test = new YAMNetTest())
                    {
                        await test.RunAsync();
                    }
                    break;
                default:
                    throw new InvalidDataException();
            }
        }
    }
}
