using Liminal.SDK.VR.Pointers;
using UnityEngine;
using Liminal.SDK.VR.Input;

namespace Liminal.SDK.VR.Devices.GearVR
{
    /// <summary>
    /// The GearVR headset is also a controller and as such implments both <see cref="IVRInputDevice"/> and <see cref="IVRHeadset"/>.
    /// <see cref="GearVRDevice"/> will use the same <see cref="GvrHeadset"/> instance for both <see cref="IVRDevice.Headset"/> and a single <see cref="IVRInputDevice"/>.
    /// <seealso href="https://developer.oculus.com/documentation/unity/latest/concepts/unity-ovrinput/"/>
    /// </summary>
    internal class GearVRHeadset : GearVRInputDevice, IVRHeadset
    {
        private const int _buttonCount = 2;
        private static VRHeadsetCapability _headsetCapabilties =
            VRHeadsetCapability.ExternalCamera | 
            VRHeadsetCapability.HeadsetDPad;

        private static VRInputDeviceCapability _inputCapabilities =
            VRInputDeviceCapability.DirectionalInput |
            VRInputDeviceCapability.DPad;

        #region Properties

        /// <summary>
        /// Gets the name of the headset device.
        /// </summary>
        public override string Name { get { return "GearVRHeadset"; } }

        /// <summary>
        /// Gets the number of binary buttons available on the device.
        /// This is all buttons that have a 'press' state and does not include axis-triggers or non-clickable touchpads/joysticks.
        /// </summary>
        public override int ButtonCount { get { return 2; } }

        /// <summary>
        /// Returns <see cref="VRInputDeviceHand.None"/>, as the GearVR Headset does not have a handedness.
        /// </summary>
        public override VRInputDeviceHand Hand { get { return VRInputDeviceHand.None; } }

        #endregion

        public GearVRHeadset() : base(OVRInput.Controller.Touchpad)
        {
        }

        protected override IVRPointer CreatePointer()
        {
            return new GearVRGazePointer(this);
        }

        public bool HasCapabilities(VRHeadsetCapability capabilities)
        {
            return ((_headsetCapabilties & capabilities) == capabilities);
        }

        public override bool HasCapabilities(VRInputDeviceCapability capabilities)
        {
            return ((_inputCapabilities & capabilities) == capabilities);
        }

        public override float GetAxis1D(string axis)
        {
            // No 1D axes on the GearVR headset
            return 0;
        }

        public override Vector2 GetAxis2D(string axis)
        {
            switch (axis)
            {
                case VRAxis.OneRaw:
                    return GetDPadAxisRaw();

                case VRAxis.One:
                    return GetDPadAxis();

                default:
                    return Vector2.zero;
            }
        }

        public override bool HasAxis1D(string axis)
        {
            return false;
        }

        public override bool HasAxis2D(string axis)
        {
            switch (axis)
            {
                case VRAxis.OneRaw:
                case VRAxis.One:
                    return true;

                default:
                    return false;
            }
        }

        public override bool HasButton(string button)
        {
            switch (button)
            {
                case VRButton.One:
                case VRButton.Touch:
                case VRButton.Back:
                case VRButton.DPadUp:
                case VRButton.DPadDown:
                case VRButton.DPadLeft:
                case VRButton.DPadRight:
                    return true;

                default:
                    return false;
            }
        }

        public override bool GetButton(string button)
        {
            switch (button)
            {
                // Touchpad tap
                case VRButton.One:
                case VRButton.Touch:
                    return OVRInput.Get(OVRInput.Button.One, ControllerMask);

                // Secondary/back
                case VRButton.Back:
                    return
                        OVRInput.Get(OVRInput.Button.Two, ControllerMask) || 
                        OVRInput.Get(OVRInput.RawButton.Back, ControllerMask);

                // D-Pad buttons
                case VRButton.DPadUp:
                    return OVRInput.Get(OVRInput.Button.DpadUp, ControllerMask);

                case VRButton.DPadDown:
                    return OVRInput.Get(OVRInput.Button.DpadDown, ControllerMask);

                case VRButton.DPadLeft:
                    return OVRInput.Get(OVRInput.Button.DpadLeft, ControllerMask);

                case VRButton.DPadRight:
                    return OVRInput.Get(OVRInput.Button.DpadRight, ControllerMask);

                default:
                    return false;
            }
        }

        public override bool GetButtonDown(string button)
        {
            switch (button)
            {
                // Touchpad tap
                case VRButton.One:
                case VRButton.Touch:
                    return OVRInput.GetDown(OVRInput.Button.One, ControllerMask);

                // Secondary/back
                case VRButton.Back:
                    return
                        OVRInput.GetDown(OVRInput.Button.Two, ControllerMask) ||
                        OVRInput.GetDown(OVRInput.RawButton.Back, ControllerMask);

                // D-Pad buttons
                case VRButton.DPadUp:
                    return OVRInput.GetDown(OVRInput.Button.DpadUp, ControllerMask);

                case VRButton.DPadDown:
                    return OVRInput.GetDown(OVRInput.Button.DpadDown, ControllerMask);

                case VRButton.DPadLeft:
                    return OVRInput.GetDown(OVRInput.Button.DpadLeft, ControllerMask);

                case VRButton.DPadRight:
                    return OVRInput.GetDown(OVRInput.Button.DpadRight, ControllerMask);

                default:
                    return false;
            }
        }

        public override bool GetButtonUp(string button)
        {
            switch (button)
            {
                // Touchpad tap
                case VRButton.One:
                case VRButton.Touch:
                    return OVRInput.GetUp(OVRInput.Button.One, ControllerMask);

                // Secondary/back
                case VRButton.Back:
                    return
                        OVRInput.GetUp(OVRInput.Button.Two, ControllerMask) ||
                        OVRInput.GetUp(OVRInput.RawButton.Back, ControllerMask);

                // D-Pad buttons
                case VRButton.DPadUp:
                    return OVRInput.GetUp(OVRInput.Button.DpadUp, ControllerMask);

                case VRButton.DPadDown:
                    return OVRInput.GetUp(OVRInput.Button.DpadDown, ControllerMask);

                case VRButton.DPadLeft:
                    return OVRInput.GetUp(OVRInput.Button.DpadLeft, ControllerMask);

                case VRButton.DPadRight:
                    return OVRInput.GetUp(OVRInput.Button.DpadRight, ControllerMask);

                default:
                    return false;
            }
        }
        
        private Vector2 GetDPadAxis()
        {
            var input = Vector2.zero;
            if (OVRInput.Get(OVRInput.Button.DpadUp, ControllerMask))
                input.y = 1;
            else if (OVRInput.Get(OVRInput.Button.DpadDown, ControllerMask))
                input.y = -1;

            if (OVRInput.Get(OVRInput.Button.DpadLeft, ControllerMask))
                input.x = -1;
            else if (OVRInput.Get(OVRInput.Button.DpadRight, ControllerMask))
                input.x = 1;

            return input;
        }

        private Vector2 GetDPadAxisRaw()
        {
            return (GetDPadAxis() + Vector2.one) / 2;
        }
    }
}
