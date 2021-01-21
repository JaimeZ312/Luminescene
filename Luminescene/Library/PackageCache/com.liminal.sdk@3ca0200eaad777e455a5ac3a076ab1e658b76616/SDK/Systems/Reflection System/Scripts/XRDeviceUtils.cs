using UnityEngine;

namespace Liminal.Systems
{
    using System.Collections.Generic;
    using UnityEngine.XR;

    public static class XRDeviceUtils
    {
        public static HashSet<EDeviceModelType> PlanarReflectionSupported = new HashSet<EDeviceModelType>
        {
            EDeviceModelType.Go,
            EDeviceModelType.HtcVive,
            EDeviceModelType.Quest,
            EDeviceModelType.AcerAH101,
            EDeviceModelType.Rift,
            EDeviceModelType.RiftS,
            EDeviceModelType.HtcVivePro,
            EDeviceModelType.Quest2
        };

        public static EDeviceModelType GetDeviceModelType()
        {
            var model = XRDevice.model;
            var type = EDeviceModelType.Unknown;
            model = model.ToLower();

            if (model.Contains("rift"))
            {
                if (model.Contains("rift s"))
                    type = EDeviceModelType.RiftS;
                else
                    type = EDeviceModelType.Rift;
            }

            if (model.Contains("vive"))
            {
                if (model.Contains("pro"))
                    type = EDeviceModelType.HtcVivePro;
                else if (model.Contains("cosmos"))
                    type = EDeviceModelType.HtcViveCosmos;
                else
                    type = EDeviceModelType.HtcVive;
            }

            if (model.Contains("go"))
                type = EDeviceModelType.Go;

            if (model.Contains("quest"))
            {
                var graphicsCardName = SystemInfo.graphicsDeviceName;

                if (graphicsCardName.Contains("650"))
                    type = EDeviceModelType.Quest2;
                else
                    type = EDeviceModelType.Quest;
            }

            if (model.Contains("AcerAH101"))
                type = EDeviceModelType.AcerAH101;

            return type;
        }

        public static bool SupportsPlanarReflection()
        {
            return PlanarReflectionSupported.Contains(GetDeviceModelType());
        }
    }
}