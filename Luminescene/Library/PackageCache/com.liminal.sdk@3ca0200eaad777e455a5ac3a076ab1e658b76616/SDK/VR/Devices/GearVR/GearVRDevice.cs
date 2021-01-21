using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Devices.GearVR.Avatar;
using Liminal.SDK.VR.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Liminal.SDK.VR.Devices.GearVR
{
    /// <summary>
    /// A concrete implementation of <see cref="IVRDevice"/> for Samsung's GearVR hardware.
    /// </summary>
    public class GearVRDevice : IVRDevice
    {
        private static readonly VRDeviceCapability _capabilities =
            VRDeviceCapability.Controller | VRDeviceCapability.UserPrescenceDetection;

        private OVRInput.Controller mConnectedControllerMask;
        private GearVRController mPrimaryController;
        private GearVRController mSecondaryController;
        private bool mHeadsetInputConnected;
        private OVRInput.Controller mCachedActiveController;
        private IVRInputDevice[] mInputDevices = new IVRInputDevice[0];
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int SmoothnessTextureChannel = Shader.PropertyToID("_SmoothnessTextureChannel");
        private static readonly int Glossiness = Shader.PropertyToID("_Glossiness");

        #region Properties

        string IVRDevice.Name { get { return "GearVR"; } }

        int IVRDevice.InputDeviceCount { get { return mInputDevices.Length; } }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public IVRHeadset Headset { get; private set; }
        public IVRInputDevice PrimaryInputDevice { get; private set; }
        public IVRInputDevice SecondaryInputDevice { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        IEnumerable<IVRInputDevice> IVRDevice.InputDevices { get { return mInputDevices; } }
        int IVRDevice.CpuLevel {  get { return OVRManager.cpuLevel; } set { OVRManager.cpuLevel = value; } }
        int IVRDevice.GpuLevel { get { return OVRManager.gpuLevel; } set { OVRManager.gpuLevel = value; } }

        /// <summary>
        /// Returns true if the device is an Oculus Go, rather than a GearVR device
        /// </summary>
        public static bool IsOculusGo { get { return SystemInfo.deviceModel == "Oculus Pacific"; } }

        #endregion
        
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public event VRInputDeviceEventHandler InputDeviceConnected;
        public event VRInputDeviceEventHandler InputDeviceDisconnected;
        public event VRDeviceEventHandler PrimaryInputDeviceChanged;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Create a GearVR device
        /// </summary>
        public GearVRDevice()
        {
            Headset = OVRUtils.IsGearVRHeadset() ? new GearVRHeadset() : GenericHeadset();
            OVRInput.Update();
            UpdateConnectedControllers();
        }

        private static IVRHeadset GenericHeadset()
        {
            return new SimpleHeadset("GenericHeadset", VRHeadsetCapability.None);
        }

        //Updates once per Tick from VRDeviceMonitor (const 0.5 seconds)
        void IVRDevice.Update()
        {
            if (mConnectedControllerMask != OVRInput.GetConnectedControllers())
            {
                // Connected controller mask has changed
                UpdateConnectedControllers();
            }

            if (mCachedActiveController != OVRInput.GetActiveController())
            {
                // Active controller has changed
                UpdateInputDevices();
            }

            CheckUsedRenderPipeline();
        }
        
        bool IVRDevice.HasCapabilities(VRDeviceCapability capabilities)
        {
            return ((_capabilities & capabilities) == capabilities);
        }

        void IVRDevice.SetupAvatar(IVRAvatar avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException("avatar");

            // Attach the GearVR avatar component
            // The component will take care of the rest of the setup
            var deviceAv = avatar.Transform.gameObject.AddComponent<GearVRAvatar>();
            deviceAv.hideFlags = HideFlags.NotEditable;

            UpdateConnectedControllers();
            SetDefaultPointerActivation();
        }

        /// <summary>
        /// By default the Primary Controller will have a pointer and Secondary will be deactivated.
        /// </summary>
        private void SetDefaultPointerActivation()
        {
            // Usually null-coalescing is a dangerous way to hide an issue but in this case,
            // Not all headsets have two controllers and some users may even open the app without a controller.
            // The app should not stop because of a lack of controllers as there is always gaze input to fall back on.

            mPrimaryController?.Pointer?.Activate();
            mSecondaryController?.Pointer?.Deactivate();
        }

        private void UpdateConnectedControllers()
        {
            var allControllers = new List<IVRInputDevice>();
            var disconnectedList = new List<IVRInputDevice>();
            var connectedList = new List<IVRInputDevice>();

            var ctrlMask = OVRInput.GetConnectedControllers();
            // NOTE: Controller tests here are in order of priority. Active hand controllers take priority over headset

            #region Controller
            var leftHandConnected = OVRUtils.IsLimbConnected(VRAvatarLimbType.LeftHand);
            var rightHandConnected = OVRUtils.IsLimbConnected(VRAvatarLimbType.RightHand);

            Debug.Log($"Left Hand Connected: {leftHandConnected}");
            Debug.Log($"Right Hand Connected: {rightHandConnected}");

            // The order the controllers are added currently determines the PrimaryInput however, 
            // It does not seem to determine the primary pointer.
            if (rightHandConnected)
            {
                mPrimaryController = mPrimaryController ?? new GearVRController(VRInputDeviceHand.Right);

                if (!mInputDevices.Contains(mPrimaryController))
                    connectedList.Add(mPrimaryController);

                allControllers.Add(mPrimaryController);
            }
            else
            {
                disconnectedList.Add(mPrimaryController);
            }

            if (leftHandConnected)
            {
                mSecondaryController = mSecondaryController ?? new GearVRController(VRInputDeviceHand.Left);

                if (!mInputDevices.Contains(mSecondaryController))
                    connectedList.Add(mSecondaryController);

                allControllers.Add(mSecondaryController);
            }
            else
            {
                disconnectedList.Add(mSecondaryController);
            }
            #endregion            

            #region Headset (Swipe-pad)

            if (Headset is GearVRHeadset)
            {
                var gearVRHeadset = Headset as GearVRHeadset;

                if ((ctrlMask & OVRInput.Controller.Touchpad) != 0)
                {
                    if (!mHeadsetInputConnected)
                    {
                        connectedList.Add(gearVRHeadset);
                        mHeadsetInputConnected = true;
                    }

                    allControllers.Add(gearVRHeadset);
                }
                else if (Headset != null)
                {
                    disconnectedList.Add(gearVRHeadset);
                    mHeadsetInputConnected = false;
                }
            }
            #endregion

            // Update internal state
            mInputDevices = allControllers.ToArray();
            mConnectedControllerMask = ctrlMask;

            foreach (var device in disconnectedList)
            {
                InputDeviceDisconnected?.Invoke(this, device);
            }

            foreach (var device in connectedList)
            {
                InputDeviceConnected?.Invoke(this, device);
            }

            // Force an update of input devices
            UpdateInputDevices();
        }

        private void CheckUsedRenderPipeline()
        {
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                UpdateToStandardMaterial();
            }
            else
            {
                UpdateToLWRPMaterial();
            }
        }

        private void UpdateToLWRPMaterial()
        {
            if (VRAvatar.Active == null)
                return;

            var hands = VRAvatar.Active.Hands;

            foreach (var hand in hands)
            {
                if (hand.GetControllerVisual() == null)
                    continue;
                
                var mesh = hand.GetControllerVisual().GetComponentInChildren<MeshRenderer>();

                if (mesh.material.shader.name.Equals("Lightweight Render Pipeline/Lit"))
                {
                    continue;
                }

                var oldMat = mesh.material;
                var newMat = new Material(Shader.Find("Lightweight Render Pipeline/Lit"));

                newMat.SetTexture(BaseMap, oldMat.mainTexture);
                newMat.SetColor(BaseColor, oldMat.color);
                newMat.SetFloat(SmoothnessTextureChannel, oldMat.GetFloat(Glossiness));
                mesh.material = newMat;
            }
        }

        private void UpdateToStandardMaterial()
        {
            if (VRAvatar.Active == null)
                return;

            var hands = VRAvatar.Active.Hands;

            foreach (var hand in hands)
            {
                if (hand.GetControllerVisual() == null)
                    continue;

                var mesh = hand.GetControllerVisual().GetComponentInChildren<MeshRenderer>();

                if (mesh.material.shader.name.Equals(("Standard")))
                {
                    continue;
                }

                var oldMat = mesh.material;
                var newMat = new Material(Shader.Find("Standard"))
                {
                    mainTexture = oldMat.GetTexture(BaseMap), color = oldMat.GetColor(BaseColor)
                };

                newMat.SetFloat(Glossiness, oldMat.GetFloat(SmoothnessTextureChannel));
                mesh.material = newMat;
            }
        }

        private void UpdateInputDevices()
        {
            mCachedActiveController = OVRInput.GetActiveController();

            var hasController = OVRUtils.IsOculusQuest ?
                OVRUtils.IsQuestControllerConnected :
                (mCachedActiveController & GearVRController.AllHandControllersMask) != 0;

            // TODO Introduce ActiveInputDevice, presently PrimaryInputDevice is treated as Active and it should be Left or Right, not Primary or Secondary.
            if (hasController)
            {
                if (OVRUtils.IsOculusQuest)
                {
                    PrimaryInputDevice = mPrimaryController;
                    SecondaryInputDevice = mSecondaryController;
                }
                else
                {
                    PrimaryInputDevice = OVRInput.GetActiveController() == OVRInput.Controller.RTrackedRemote ? mPrimaryController : mSecondaryController;
                    SecondaryInputDevice = Headset as GearVRInputDevice;
                }
            }
            else
            {
                PrimaryInputDevice = Headset as GearVRInputDevice;
                SecondaryInputDevice = null;
            }

            PrimaryInputDeviceChanged?.Invoke(this);
        }
    }
}
