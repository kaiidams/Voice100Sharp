using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Voice100
{
    public interface ISpeechRecognizer : IDisposable
    {
        string Recognize(short[] waveform);
    }
}
