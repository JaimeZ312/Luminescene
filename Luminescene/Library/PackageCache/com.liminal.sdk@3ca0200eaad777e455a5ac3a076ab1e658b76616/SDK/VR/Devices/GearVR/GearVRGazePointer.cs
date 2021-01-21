using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR
{
    /// <summary>
    /// A GearVR specific implementation of <see cref="BasePointer"/> that uses the touchpad on the device's HMD for triggering interactions.
    /// </summary>
    internal class GearVRGazePointer : BasePointer
    {
        private IVRInputDevice mInputDevice;

        /// <summary>
        /// Creates a new <see cref="GearVRGazePointer"/> for the specified input device.
        /// </summary>
        /// <param name="inputDevice">The input device for the pointer.</param>
        public GearVRGazePointer(IVRInputDevice inputDevice) : base(inputDevice)
        {
            mInputDevice = inputDevice;
        }

        public override void OnPointerEnter(GameObject target) { }
        public override void OnPointerExit(GameObject target) { }

        public override bool GetButtonDown()
        {
            return mInputDevice.GetButtonDown(VRButton.One);
        }

        public override bool GetButtonUp()
        {
            return mInputDevice.GetButtonUp(VRButton.One);
        }
    }
}
