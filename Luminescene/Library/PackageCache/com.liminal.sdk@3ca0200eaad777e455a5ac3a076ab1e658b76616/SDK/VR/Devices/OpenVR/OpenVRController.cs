using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;
using Valve.VR;

namespace Liminal.SDK.OpenVR
{
    public class OpenVRController : IVRInputDevice
    {
        private static readonly VRInputDeviceCapability _capabilities = VRInputDeviceCapability.DirectionalInput | VRInputDeviceCapability.Touch | VRInputDeviceCapability.TriggerButton;

        public string Name => "OpenVR Controller";
        public int ButtonCount => 5;
        
        public VRInputDeviceHand Hand { get; }
        public IVRPointer Pointer { get; }

        public bool IsTouching => SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "JoystickNearPress")[SteamHand].state;

        public SteamVR_Input_Sources SteamHand => Hand == VRInputDeviceHand.Right ? SteamVR_Input_Sources.RightHand : SteamVR_Input_Sources.LeftHand;


        public Dictionary<string, SteamVR_Action_Boolean_Source> _buttonInputMap;
        public Dictionary<string, SteamVR_Action_Vector2_Source> _axis2DMap;
        public Dictionary<string, SteamVR_Action_Single_Source> _axis1DMap;

        public Dictionary<string, SteamVR_Action_Boolean_Source> GenerateButtonInputMap() => new Dictionary<string, SteamVR_Action_Boolean_Source>()
        {
            {VRButton.Trigger, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "InteractUI")[SteamHand]},
            {VRButton.One, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "InteractUI")[SteamHand]},
            {VRButton.Two, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "Secondary")[SteamHand]},
            {VRButton.Touch, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "JoystickPress")[SteamHand]},
            {VRButton.Back, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "Back")[SteamHand]},
        };

        public Dictionary<string, SteamVR_Action_Vector2_Source> GenerateAxis2DInputMap() => new Dictionary<string, SteamVR_Action_Vector2_Source>()
        {
            {VRAxis.One, SteamVR_Input.GetAction<SteamVR_Action_Vector2>("default", "Joystick")[SteamHand]},
            {VRAxis.OneRaw, SteamVR_Input.GetAction<SteamVR_Action_Vector2>("default", "Joystick")[SteamHand]},
        };

        public Dictionary<string, SteamVR_Action_Single_Source> GenerateAxis1DInputMap() => new Dictionary<string, SteamVR_Action_Single_Source>()
        {
            {VRAxis.Two, SteamVR_Input.GetAction<SteamVR_Action_Single>("default", "Trigger")[SteamHand]},
            {VRAxis.TwoRaw, SteamVR_Input.GetAction<SteamVR_Action_Single>("default", "Trigger")[SteamHand]},
            {VRAxis.Three, SteamVR_Input.GetAction<SteamVR_Action_Single>("default", "Squeeze")[SteamHand]},
            {VRAxis.ThreeRaw, SteamVR_Input.GetAction<SteamVR_Action_Single>("default", "Squeeze")[SteamHand]},
        };

        public OpenVRController(VRInputDeviceHand hand)
        {
            Pointer = new InputDevicePointer(this);
            Pointer.Activate();
            Hand = hand;

            _buttonInputMap = GenerateButtonInputMap();
            _axis1DMap = GenerateAxis1DInputMap();
            _axis2DMap = GenerateAxis2DInputMap();
        }

        public bool HasCapabilities(VRInputDeviceCapability capabilities) => ((_capabilities & capabilities) == capabilities);

        public bool HasAxis1D(string axis)
        {
            _axis1DMap.TryGetValue(axis, out var result);
            return result != null;
        }

        public bool HasAxis2D(string axis)
        {
            _axis2DMap.TryGetValue(axis, out var result);
            return result != null;
        }

        public bool HasButton(string button)
        {
            // TODO This is probably wrong or going to throw an error
            _buttonInputMap.TryGetValue(button, out var result);
            return result != null;
        }

        public float GetAxis1D(string axis)
        {
            _axis1DMap.TryGetValue(axis, out var result);
            return result?.axis ?? 0;
        }

        public Vector2 GetAxis2D(string axis)
        {
            _axis2DMap.TryGetValue(axis, out var result);
            return result?.axis ?? Vector2.zero;
        }

        public bool GetButton(string button)
        {
            _buttonInputMap.TryGetValue(button, out var result);
            return result != null && result.state;
        }

        public bool GetButtonDown(string button)
        {
            _buttonInputMap.TryGetValue(button, out var result);
            return result != null && result.stateDown;
        }

        public bool GetButtonUp(string button)
        {
            _buttonInputMap.TryGetValue(button, out var result);
            return result != null && result.stateUp;
        }
    }
}