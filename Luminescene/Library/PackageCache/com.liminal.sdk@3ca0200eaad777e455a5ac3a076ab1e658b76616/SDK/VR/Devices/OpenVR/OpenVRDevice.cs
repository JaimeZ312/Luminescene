using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using Liminal.Systems;
using UnityEngine;
using Valve.VR;

namespace Liminal.SDK.OpenVR
{
    public class CoroutineService : MonoBehaviour
    {
    }

    public class OpenVRDevice : IVRDevice
    {
        private static readonly VRDeviceCapability _capabilities =
            VRDeviceCapability.Controller | VRDeviceCapability.DualController |
            VRDeviceCapability.UserPrescenceDetection;

        public string Name => "OpenVR";
        public int InputDeviceCount => 3;

        public IVRHeadset Headset => new SimpleHeadset("", VRHeadsetCapability.PositionalTracking);

        public IEnumerable<IVRInputDevice> InputDevices { get; }
        public IVRInputDevice PrimaryInputDevice { get; }
        public IVRInputDevice SecondaryInputDevice { get; }

        public event VRInputDeviceEventHandler InputDeviceConnected;
        public event VRInputDeviceEventHandler InputDeviceDisconnected;
        public event VRDeviceEventHandler PrimaryInputDeviceChanged;

        public int CpuLevel { get; set; }
        public int GpuLevel { get; set; }

        public OpenVRDevice()
        {
            PrimaryInputDevice = new OpenVRController(VRInputDeviceHand.Right);
            SecondaryInputDevice = new OpenVRController(VRInputDeviceHand.Left);

            InputDevices = new List<IVRInputDevice>
            {
                PrimaryInputDevice,
                SecondaryInputDevice,
            };
        }

        public bool HasCapabilities(VRDeviceCapability capabilities) => ((_capabilities & capabilities) == capabilities);

        public void SetupAvatar(IVRAvatar avatar)
        {
            var openVRAvatar = avatar.Transform.gameObject.AddComponent<OpenVRAvatar>();
            var rigPrefab = Resources.Load("SteamVRRig");
            var rig = GameObject.Instantiate(rigPrefab) as GameObject;
            rig.transform.SetParent(avatar.Auxiliaries);

            var leftHand = rig.GetComponentsInChildren<SteamVR_Behaviour_Pose>().FirstOrDefault(x => x.inputSource == SteamVR_Input_Sources.LeftHand);
            var rightHand = rig.GetComponentsInChildren<SteamVR_Behaviour_Pose>().FirstOrDefault(x => x.inputSource == SteamVR_Input_Sources.RightHand);

            avatar.PrimaryHand.TrackedObject = new OpenVRTrackedControllerProxy(rightHand, avatar.Head.Transform, avatar.Transform);
            avatar.SecondaryHand.TrackedObject = new OpenVRTrackedControllerProxy(leftHand, avatar.Head.Transform, avatar.Transform);

            var leftModel = leftHand.GetComponentInChildren<SteamVR_RenderModel>();
            var rightModel = rightHand.GetComponentInChildren<SteamVR_RenderModel>();

            var coroutineService = new GameObject("Coroutine Service").AddComponent<CoroutineService>();
            coroutineService.StartCoroutine(MigrateModel(leftModel, avatar.SecondaryHand));
            coroutineService.StartCoroutine(MigrateModel(rightModel, avatar.PrimaryHand));
        }

        private IEnumerator MigrateModel(SteamVR_RenderModel model, IVRAvatarHand hand)
        {
            var controllerVisual = hand.Transform.GetComponentInChildren<VRAvatarController>(includeInactive: true);
            yield return new WaitUntil(() => model.transform.childCount != 1);

            if (controllerVisual == null)
            {
                model.SetMeshRendererState(false);
                yield break;
            }

            model.transform.SetParent(controllerVisual.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            switch (XRDeviceUtils.GetDeviceModelType())
            {
                case EDeviceModelType.HtcVive:
                case EDeviceModelType.HtcViveCosmos:
                case EDeviceModelType.HtcVivePro:

                    hand.Anchor.transform.localPosition += new Vector3(0, 0, -0.1f);
                    model.transform.localPosition = new Vector3(0,0,0.1f);

                    break;
            }

            var pointerVisual = controllerVisual.GetComponentInChildren<LaserPointerVisual>(includeInactive: true);
            pointerVisual.Bind(hand.InputDevice.Pointer);
            hand.InputDevice.Pointer.Transform = pointerVisual.transform;
        }   

        public void Update()
        {
        }
    }
}