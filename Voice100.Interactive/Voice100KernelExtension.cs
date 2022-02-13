using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Voice100.Interactive
{
    public class Voice100KernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            Formatter.Register<Audio>((audio, writer) =>
            {
                writer.Write(PocketViewTags._.audio[controls: "controls"](
                    PocketViewTags.source[src: audio.GetDataUrl()]));
            }, mimeType: "text/html");

            return Task.CompletedTask;
        }
    }
}
