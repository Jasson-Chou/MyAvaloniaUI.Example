using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Models
{
    public interface IWaveformSimulator
    {
        float[] GenerateF(TimeSpan startTime, TimeSpan duration);
    }
}
