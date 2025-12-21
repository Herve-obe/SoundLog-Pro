using System;

namespace Veriflow.Avalonia.Services
{
    // STUB: VeriflowMeteringProvider disabled during Avalonia migration
    // Requires Audio Engine replacement (CSCore -> Bass/PortAudio)
    /*
    public class VeriflowMeteringProvider : ISampleSource
    {
       // ... Original Code ...
    }
    */
    public class VeriflowMeteringProvider
    {
        // Stub
         public float[] ChannelPeaks { get; private set; } = new float[0];
    }
}
