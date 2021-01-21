using Liminal.SDK.Extensions;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Avatars.Extensions;
using Liminal.SDK.VR.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR.Avatar
{
    /// <summary>
    /// A device-specific implementation of <see cref="IVRDeviceAvatar"/> to prepare an <see cref="IVRAvatar"/> for Samsung's GearVR hardware.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class GearVRAvatar : MonoBehaviour, IVRDeviceAvatar
    {
        public static EPointerActivationType PointerActivationType = EPointerActivationType.Both;

        private GazeInput mGazeInput = null;

        private const string ControllerVisualPrefabName = "GearVRController";
        private const int TargetFramerate = 72;
        private readonly List<OVRControllerHelper> mRemotes = new List<OVRControllerHelper>();

        private IVRAvatar mAvatar;
        private IVRDevice mDevice;
        private GearVRAvatarSettings mSettings;
        private GearVRTrackedControllerProxy mPrimaryControllerTracker;
        private GearVRTrackedControllerProxy mSecondaryControllerTracker;

        // OVR
        private OVRManager mManager;
        private OVRCameraRig mCameraRig;

        // Cached state values
        private OVRInput.Controller mCachedActiveController;

        // This is essentially mCachedActiveController, however that is being used a different way and 
        // Oculus Quest report active Controller as Controller.Touch instead of Controller.RTouch.
        private OVRInput.Controller mQuestActiveController = OVRInput.Controller.RTouch;

        #region Properties
        /// <summary>
        /// Gets the <see cref="IVRAvatar"/> for this device avatar.
        /// </summary>
        public IVRAvatar Avatar
        {
            get
            {
                if (mAvatar == null)
                    mAvatar = GetComponentInParent<IVRAvatar>();

                return mAvatar;
            }
        }

        private bool IsHandControllerActive
        {
            get
            {
                if (OVRUtils.IsOculusQuest)
                    return OVRUtils.IsQuestControllerConnected;

                return (OVRInput.GetActiveController() & GearVRController.AllHandControllersMask) != 0;
            }
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            mAvatar = GetComponentInParent<IVRAvatar>();
            mAvatar.InitializeExtensions();

            mPrimaryControllerTracker = new GearVRTrackedControllerProxy(mAvatar, VRAvatarLimbType.RightHand);
            mSecondaryControllerTracker = new GearVRTrackedControllerProxy(mAvatar, VRAvatarLimbType.LeftHand);

            mDevice = VRDevice.Device;
            mSettings = gameObject.GetOrAddComponent<GearVRAvatarSettings>();
            mGazeInput = GetComponent<GazeInput>();

            // Setup auxiliary systems
            SetupManager();
            SetupCameraRig();

            // Activate OVRManager once everything is setup
            mManager.gameObject.SetActive(true);
            
            // Load controller visuals for any VRAvatarController objects attached to the avatar
            {
                var avatarControllers = GetComponentsInChildren<VRAvatarController>(includeInactive: true);
                foreach (var controller in avatarControllers)
                {
                    AttachControllerVisual(controller);
                }
            }

            // Add event listeners
            mDevice.InputDeviceConnected += OnInputDeviceConnected;
            mDevice.InputDeviceDisconnected += OnInputDeviceDisconnected;
            mAvatar.Head.ActiveCameraChanged += OnActiveCameraChanged;
            SetupInitialControllerState();

            UpdateHandedness();
        }

        private void OnEnable()
        {
            TrySetLimbsActive();
        }

        private void OnDestroy()
        {
            // Clean up event handlers
            if (mAvatar != null)
            {
                if (mAvatar.Head != null)
                    mAvatar.Head.ActiveCameraChanged -= OnActiveCameraChanged;
            }

            if (mDevice != null)
            {
                mDevice.InputDeviceConnected -= OnInputDeviceConnected;
                mDevice.InputDeviceDisconnected -= OnInputDeviceDisconnected;
            }
        }

        private void OnTransformParentChanged()
        {
            mAvatar = GetComponentInParent<IVRAvatar>();
        }

        private void Update()
        {
            if (mCachedActiveController != OVRInput.GetActiveController())
                UpdateHandedness();

            RecenterHmdIfRequired();
            DetectAndUpdateControllerStates();

            if (OVRUtils.IsOculusQuest)
                DetectPointerState();
        }

        private void DetectPointerState()
        {
            var device = VRDevice.Device;
            var activeController = mQuestActiveController;

            // This block of code has a lot of Null-Coalescing, which usually is dangerous but in this case we do not want to block the app.
            // A controller may disconnect and reconnect anytime.
            switch (PointerActivationType)
            {
                case EPointerActivationType.ActiveController:
                {
                    if (OVRInput.GetDown(OVRInput.Button.Any, OVRInput.Controller.RTouch) ||
                        OVRInput.GetUp(OVRInput.Button.Any, OVRInput.Controller.RTouch))
                    {
                        mQuestActiveController = OVRInput.Controller.RTouch;

                        if (activeController != mQuestActiveController)
                        {
                            device?.PrimaryInputDevice?.Pointer?.Activate();
                            device?.SecondaryInputDevice?.Pointer?.Deactivate();
                        }
                    }

                    if (OVRInput.GetDown(OVRInput.Button.Any, OVRInput.Controller.LTouch) ||
                        OVRInput.GetUp(OVRInput.Button.Any, OVRInput.Controller.LTouch))
                    {
                        mQuestActiveController = OVRInput.Controller.LTouch;

                        if (activeController != mQuestActiveController)
                        {
                            device?.SecondaryInputDevice?.Pointer?.Activate();
                            device?.PrimaryInputDevice?.Pointer?.Deactivate();
                        }
                    }
                    break;
                }

                case EPointerActivationType.Both:
                    device?.PrimaryInputDevice?.Pointer?.Activate();
                    device?.SecondaryInputDevice?.Pointer?.Activate();
                    break;
            }
        }

        #endregion

        #region Setup

        private void SetupManager()
        {
            if (OVRManager.instance == null)
            {
                Debug.Log("[GearVR] Adding OVRManager");
                var go = new GameObject("OVRManager");
                mManager = go.AddComponent<OVRManager>();
                DontDestroyOnLoad(go);
            }
            else
            {
                mManager = OVRManager.instance;
            }
        }

        private void SetupCameraRig()
        {
            var cameraRigPrefab = VRAvatarHelper.EnsureLoadPrefab<GearVRCameraRig>("GearVRCameraRig");
            cameraRigPrefab.gameObject.SetActive(false);
            mCameraRig = Instantiate(cameraRigPrefab);
            mCameraRig.transform.SetParentAndIdentity(mAvatar.Auxiliaries);

            OnActiveCameraChanged(mAvatar.Head);
        }
        
        private void SetupInitialControllerState()
        {
            if (mDevice.InputDevices.Any(x => x is GearVRController))
            {
                foreach (var controller in mDevice.InputDevices)
                {
                    EnableController(controller as GearVRController);
                }
            }
            else
            {
                // Disable controllers and enable gaze controls
                DisableAllControllers();
            }
        }
 
        private void AttachControllerVisual(VRAvatarController avatarController)
        {
            var limb = avatarController.GetComponentInParent<IVRAvatarLimb>();

            var prefab = VRAvatarHelper.EnsureLoadPrefab<VRControllerVisual>(ControllerVisualPrefabName);
            prefab.gameObject.SetActive(false);

            // Create controller instance
            var instance = Instantiate(prefab);
            instance.name = prefab.name;
            instance.transform.SetParentAndIdentity(avatarController.transform);

            // Make sure the OVRGearVrController component exists...
            var trackedRemote = instance.gameObject.GetComponent<OVRControllerHelper>();

            if (trackedRemote == null)
                trackedRemote = instance.gameObject.AddComponent<OVRControllerHelper>();

            avatarController.ControllerVisual = instance;
            mRemotes.Add(trackedRemote);

            // Assign the correct controller based on the limb type the controller is attached to
            OVRInput.Controller controllerType = GetControllerTypeForLimb(limb);
            trackedRemote.m_controller = controllerType;
            trackedRemote.m_modelGearVrController.SetActive(true);

            // Activate the controller
            // TODO Do we need to set active here? 
            var active = OVRUtils.IsLimbConnected(limb.LimbType);
            instance.gameObject.SetActive(active);

            Debug.Log($"Attached Controller: {limb.LimbType} and SetActive: {active} Controller Type set to: {controllerType}");
        }

        /// <summary>
        /// Replacement for OVRInput.IsControllerConnected() that handles None properly
        /// and works with masks as well as single enum values
        /// </summary>
        private bool IsControllerConnected(OVRInput.Controller controllerMask)
        {
            return (controllerMask != OVRInput.Controller.None)
                && ((controllerMask & OVRInput.GetConnectedControllers()) != 0);
        }

        #endregion

        #region Controllers

        // TODO See if this method can be removed, it appears to not be used at all and it can be misleading when debugging.
        /// <summary>
        /// Instantiates a <see cref="VRControllerVisual"/> for a limb.
        /// </summary>
        /// <param name="limb">The limb for the controller.</param>
        /// <returns>The newly instantiated controller visual for the specified limb, or null if no controller visual was able to be created.</returns>
        public VRControllerVisual InstantiateControllerVisual(IVRAvatarLimb limb)
        {
            if (limb == null)
                throw new ArgumentNullException("limb");

            if (limb.LimbType == VRAvatarLimbType.Head)
                return null;

            var prefab = VRAvatarHelper.EnsureLoadPrefab<VRControllerVisual>(ControllerVisualPrefabName);
            var instance = Instantiate(prefab);

            var ovrController = instance.GetComponent<OVRControllerHelper>();
            ovrController.m_controller = GetControllerTypeForLimb(limb);
            ovrController.m_modelGearVrController.SetActive(true);
            ovrController.enabled = false;

            instance.gameObject.SetActive(true);
            return instance;
        }

        private void EnableController(GearVRController controller)
        {
            if (controller == null)
                return;

            // Find the visual for the hand that matches the controller
            var remote = mRemotes.FirstOrDefault(x => (x.m_controller & controller.ControllerMask) != 0);
            if (remote != null)
                remote.gameObject.SetActive(true);
        }

        private void DisableAllControllers()
        {
            // Disable all controller visuals
            foreach (var remote in mRemotes)
            {
                remote.gameObject.SetActive(false);
            }
        }
        
        #endregion

        private void UpdateHandedness()
        {
            mCachedActiveController = OVRInput.GetActiveController();

            var primary = mAvatar.PrimaryHand;
            primary.TrackedObject = mPrimaryControllerTracker;
            primary.SetActive(true);

            var secondary = mAvatar.SecondaryHand;
            secondary.TrackedObject = mSecondaryControllerTracker;
            secondary.SetActive(true);
        }

        /// <summary>
        /// Detects and Updates the state of the controllers including the TouchPad on the GearVR headset
        /// </summary>
        public void DetectAndUpdateControllerStates()
        {
            TrySetLimbsActive();
            TrySetGazeInputActive(!IsHandControllerActive);
        }

        /// <summary>
        /// A temporary method to split Oculus Quest changes with the other devices. 
        /// </summary>
        private void TrySetLimbsActive()
        {
            if (OVRUtils.IsOculusQuest)
            {
                TrySetHandActive(VRAvatarLimbType.RightHand);
                TrySetHandActive(VRAvatarLimbType.LeftHand);
            }
            else
            {
                TrySetHandsActive(IsHandControllerActive);
            }
        }
        
        private void TrySetHandActive(VRAvatarLimbType limbType)
        {
            var isLimbConnected = OVRUtils.IsLimbConnected(limbType);
            var limb = mAvatar.GetLimb(limbType);

            limb.SetActive(isLimbConnected);
        }

        private void TrySetHandsActive(bool active)
        {
            if (mAvatar != null)
            {
                if (OVRUtils.IsGearVRHeadset())
                {
                    if (OVRInput.GetActiveController() == OVRInput.Controller.Touchpad)
                        active = false;
                }

                mAvatar.SetHandsActive(active);
            }
        }

        private void TrySetGazeInputActive(bool active)
        {
            // Ignore Always & Never Policy
            if (mGazeInput != null && mGazeInput.ActivationPolicy == GazeInputActivationPolicy.NoControllers)
            {
                if (active)
                    mGazeInput.Activate();
                else
                    mGazeInput.Deactivate();
            }
        }

        private void RecenterHmdIfRequired()
        {
            if (mSettings != null && mSettings.HmdRecenterPolicy != HmdRecenterPolicy.OnControllerRecenter)
                return;

            if (OVRInput.GetControllerWasRecentered())
            {
                // Recenter the camera when the user recenters the controller
                UnityEngine.XR.InputTracking.Recenter();
            }
        }

        private OVRInput.Controller GetControllerTypeForLimb(IVRAvatarLimb limb)
        {
            if (limb.LimbType == VRAvatarLimbType.LeftHand)
                return OVRInput.Controller.LTouch;

            if (limb.LimbType == VRAvatarLimbType.RightHand)
                return OVRInput.Controller.RTouch;

            return OVRInput.Controller.None;
        }

        #region Event Handlers

        //Notes: Device Connecting is difference than controller being active
        private void OnInputDeviceConnected(IVRDevice vrDevice, IVRInputDevice inputDevice)
        {
            var gearController = inputDevice as GearVRController;
            if (gearController != null)
            {
                // A controller was connected
                // Disable gaze controls
                EnableController(gearController);
            }
        }

        private void OnInputDeviceDisconnected(IVRDevice vrDevice, IVRInputDevice inputDevice)
        {
            if (!vrDevice.InputDevices.Any(x => x is GearVRController))
            {
                // No controllers are connected
                // Enable gaze controls
                DisableAllControllers();
            }
        }
        
        private void OnActiveCameraChanged(IVRAvatarHead head)
        {
            if (mCameraRig != null)
            {
                mCameraRig.usePerEyeCameras = head.UsePerEyeCameras;
            }
        }
        #endregion
    }
}
