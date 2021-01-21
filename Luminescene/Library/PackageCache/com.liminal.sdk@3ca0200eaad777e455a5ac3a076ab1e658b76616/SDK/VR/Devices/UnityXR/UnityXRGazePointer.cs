using Liminal.SDK.VR.Pointers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Devices;
using Liminal.SDK.VR.Input;

namespace Liminal.SDK.XR
{
    public class UnityXRGazePointer : BasePointer
    {
        private IVRInputDevice mInputDevice;

        public UnityXRGazePointer(IVRInputDevice inputDevice) : base(inputDevice)
        {
            mInputDevice = inputDevice;
        }

        public override void OnPointerEnter(GameObject target) { }
        public override void OnPointerExit(GameObject target) { }

        public override bool GetButtonDown()
        {
            // TODO: Is this needed?
            return false;
        }

        public override bool GetButtonUp()
        {
            // TODO: Is this needed?
            return false;
        }
    }
}


