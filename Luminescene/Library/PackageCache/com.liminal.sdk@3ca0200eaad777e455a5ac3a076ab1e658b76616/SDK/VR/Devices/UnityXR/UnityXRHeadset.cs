#if UNITY_XR
using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using Object = UnityEngine.Object;
using System.Linq;
using Liminal.SDK.VR.Devices.GearVR.Avatar;

namespace Liminal.SDK.XR
{
    public class UnityXRHeadset : UnityXRInputDevice, IVRHeadset
    {
        private static readonly VRInputDeviceCapability _inputCapabilities =
            VRInputDeviceCapability.None;

        private static readonly VRHeadsetCapability _headsetCapabilities =
            VRHeadsetCapability.PositionalTracking;

        public override string Name => "UnityXRHeadset";
        public override int ButtonCount => 0;
        public override bool IsTouching => false;
        public override VRInputDeviceHand Hand => VRInputDeviceHand.None;

        public UnityXRHeadset()
        {
            Pointer = CreatePointer();
        }

        protected override IVRPointer CreatePointer()
        {
            return new UnityXRGazePointer(this);
        }

        public override bool HasCapabilities(VRInputDeviceCapability capabilities)
        {
            return (_inputCapabilities & capabilities) == capabilities;
        }

        public bool HasCapabilities(VRHeadsetCapability capabilities)
        {
            return (_headsetCapabilities & capabilities) == capabilities;
        }

        public override float GetAxis1D(string axis) { return 0f; }
        public override Vector2 GetAxis2D(string axis) { return Vector2.zero; }
        public override bool GetButton(string button) { return false; }
        public override bool GetButtonDown(string button) { return false; }
        public override bool GetButtonUp(string button) { return false; }
        public override bool HasAxis1D(string axis) { return false; }
        public override bool HasAxis2D(string axis) { return false; }
        public override bool HasButton(string button) { return false; }

        public override void Update() { }
    }
}
#endif