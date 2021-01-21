using Liminal.SDK.Core;
using Liminal.SDK.OpenVR;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Devices.Emulator;
using Liminal.SDK.VR.Devices.GearVR;
using Liminal.SDK.XR;
using UnityEngine;
using UnityEngine.XR;

namespace App
{
    public static class DeviceUtils
    {
        public static IVRDevice CreateDevice(ExperienceApp experienceApp = null)
        {
            // TODO Add an environment SDK Configuration.
#if UNITY_XR
            return CreateDevice(ESDKType.UnityXR);
#else
            return CreateDevice(Application.platform == RuntimePlatform.Android ? ESDKType.OVR : ESDKType.OpenVR);
#endif
        }

        // This is used by the Editor
        public static IVRDevice CreateDevice(ESDKType sdkType)
        {
            switch (sdkType)
            {
#if UNITY_XR
                case ESDKType.UnityXR:
                    return new UnityXRDevice();
#endif
                case ESDKType.OVR:
                    XRSettings.enabled = true;
                    return new GearVRDevice();

                case ESDKType.OpenVR:
                    return new OpenVRDevice();

                default:
                    return new EmulatorDevice(VREmulatorDevice.Daydream);
            }
        }
    }
}