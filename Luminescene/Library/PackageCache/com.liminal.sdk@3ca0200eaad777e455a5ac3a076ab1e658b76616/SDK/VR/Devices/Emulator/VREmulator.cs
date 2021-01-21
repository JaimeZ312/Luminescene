using Liminal.SDK.Core;
using System;
using UnityEngine;
using App;

namespace Liminal.SDK.VR
{
    /// <summary>
    /// The entry component for initializing the <see cref="VRDevice"/> system using emulation for the editor.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR/Emulator Setup")]
    public class VREmulator : MonoBehaviour, IVRDeviceInitializer
    {
        public ESDKType BuildType;
        public ESDKType EditorType;

        /// <summary>
        /// Creates a new <see cref="IVRDevice"/> and returns it.
        /// </summary>
        /// <returns>The <see cref="IVRDevice"/> that was created.</returns>
        public IVRDevice CreateDevice()
        {
#if UNITY_EDITOR
            return DeviceUtils.CreateDevice(EditorType);
#else
            return DeviceUtils.CreateDevice(BuildType);
#endif
        }
    }
}
