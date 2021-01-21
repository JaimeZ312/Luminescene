using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR
{
    /// <summary>
    /// An abstract base implementation of <see cref="IVRInputDevice"/>.
    /// </summary>
    internal abstract class GearVRInputDevice : IVRInputDevice
    {
        public abstract string Name { get; }
        public int Index { get; set; }
        public IVRPointer Pointer { get; private set; }
        public abstract int ButtonCount { get; }
        public abstract VRInputDeviceHand Hand { get; }

        /// <summary>
        /// Gets the OVR controller type assigned to this device.
        /// Be careful when using OVRInput.Controller values. OVR APIs sometimes treat them as
        /// bitfield masks, sometimes as simple enum values.
        /// </summary>
        public OVRInput.Controller ControllerMask { get; private set; }

        /// <summary>
        /// For GearVR and OculusGo, this means if TouchPad is Touched (whether it is pressed or not.
        /// For Oculus Quest, this means if a user has placed their finder on the thumbstick.
        /// This should not be renamed in case it has been used by other limapps.
        /// </summary>
        public bool IsTouching
        {
            get
            {
                if (OVRUtils.IsOculusQuest)
                {
                    return OVRInput.Get(OVRInput.Touch.PrimaryThumbstick, ControllerMask);
                }
                else
                {
                    return OVRInput.Get(OVRInput.Touch.One);
                }
            }
        }

        protected GearVRInputDevice(OVRInput.Controller controllerMask)
        {
            ControllerMask = controllerMask;

            Pointer = CreatePointer();
            Pointer?.Activate();
        }

        protected abstract IVRPointer CreatePointer();
        public abstract float GetAxis1D(string axis);
        public abstract Vector2 GetAxis2D(string axis);
        public abstract bool GetButton(string button);
        public abstract bool GetButtonDown(string button);
        public abstract bool GetButtonUp(string button);
        public abstract bool HasAxis1D(string axis);
        public abstract bool HasAxis2D(string axis);
        public abstract bool HasButton(string button);
        public abstract bool HasCapabilities(VRInputDeviceCapability capability);
    }
}
