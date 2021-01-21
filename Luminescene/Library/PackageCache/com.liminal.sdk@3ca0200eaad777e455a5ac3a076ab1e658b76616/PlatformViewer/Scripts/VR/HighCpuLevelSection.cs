using System;
using Liminal.SDK.VR;

namespace Liminal.Platform.Experimental.VR
{
    public class HighCpuLevelSection : IDisposable
    {
        private int originalLevel;

        public HighCpuLevelSection()
        {
            originalLevel = VRDevice.Device.CpuLevel;
        }

        public void Dispose()
        {
            VRDevice.Device.CpuLevel = originalLevel;
        }
    }
}