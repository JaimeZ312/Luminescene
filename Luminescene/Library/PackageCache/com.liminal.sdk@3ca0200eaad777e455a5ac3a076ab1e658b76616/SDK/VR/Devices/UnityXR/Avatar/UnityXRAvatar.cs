#if UNITY_XR
using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Input;
using UnityEngine;
using System;
using System.Linq;
using Liminal.SDK.Extensions;
using Liminal.SDK.VR.Avatars.Extensions;

namespace Liminal.SDK.XR
{
	public class UnityXRAvatar : MonoBehaviour, IVRDeviceAvatar
	{
		#region Variables
		public static EPointerActivationType PointerActivationType = EPointerActivationType.Both;

		private IVRAvatar mAvatar;
		public IVRAvatar Avatar
		{
			get
			{
				if (mAvatar == null)
				{
					mAvatar = GetComponentInParent<IVRAvatar>();
				}

				return mAvatar;
			}
		}

		private bool IsHandControllerActive
		{
			get
			{
				if (OVRUtils.IsOculusQuest)
					return OVRUtils.IsQuestControllerConnected;

				return false;// (OVRInput.GetActiveController() & GearVRController.AllHandControllersMask) != 0;
			}
		}

		private const string ControllerVisualPrefabName = "UnityXRController";
		private readonly List<UnityXRControllerVisual> mRemotes = new List<UnityXRControllerVisual>();

		private IVRDevice mDevice;

		private IVRTrackedObjectProxy mPrimaryControllerTracker;
		private IVRTrackedObjectProxy mSecondaryControllerTracker;

		private GazeInput mGazeInput = null;

		// OVR
		private OVRManager mManager;
		private OVRCameraRig mCameraRig;
		#endregion

        public void Initialize()
        {
            mAvatar = GetComponentInParent<IVRAvatar>();
            mAvatar.InitializeExtensions();

            mDevice = VRDevice.Device;
            mGazeInput = GetComponent<GazeInput>();

            // Load controller visuals for any VRAvatarController objects attached to the avatar
            var avatarControllers = GetComponentsInChildren<VRAvatarController>(includeInactive: true);
            foreach (var controller in avatarControllers)
                AttachControllerVisual(controller);

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
				{
					mAvatar.Head.ActiveCameraChanged -= OnActiveCameraChanged;
				}
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
			// update handedness?
			//if (mCachedActiveController != OVRInput.GetActiveController())
				UpdateHandedness();

			RecenterHmdIfRequired();
			DetectAndUpdateControllerStates();

			if (OVRUtils.IsOculusQuest)
			{
				DetectPointerState();
			}

			VRDevice.Device.Update();
		}

		private void DetectPointerState()
		{
			var device = VRDevice.Device;

			// This block of code has a lot of Null-Coalescing, which usually is dangerous but in this case we do not want to block the app.
			// A controller may disconnect and reconnect anytime.
			switch (PointerActivationType)
			{
				case EPointerActivationType.ActiveController:
					// TODO: NYI
					break;

				case EPointerActivationType.Both:
					device?.PrimaryInputDevice?.Pointer?.Activate();
					device?.SecondaryInputDevice?.Pointer?.Activate();
					break;
			}
		}

		#region Setup

		private void SetupInitialControllerState()
		{
			if (mDevice.InputDevices.Any(x => x is UnityXRController))
			{
				foreach (var controller in mDevice.InputDevices)
				{
					EnableControllerVisual(controller as UnityXRController);
				}
			}
			else
			{
				// Disable controllers and enable gaze controls
				DisableAllControllerVisuals();
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
			var trackedRemote = instance.gameObject.GetComponent<UnityXRControllerVisual>();

			if (trackedRemote == null)
				trackedRemote = instance.gameObject.AddComponent<UnityXRControllerVisual>();

			avatarController.ControllerVisual = instance;
			mRemotes.Add(trackedRemote);

			// Assign the correct controller based on the limb type the controller is attached to
			OVRInput.Controller controllerType = GetControllerTypeForLimb(limb);
			trackedRemote.m_controller = controllerType;
			trackedRemote.m_modelGearVrController.SetActive(true);

			// Activate the controller
			var active = OVRUtils.IsLimbConnected(limb.LimbType);
			instance.gameObject.SetActive(true);

			Debug.Log($"Attached Controller: {limb.LimbType} and SetActive: {active} Controller Type set to: {controllerType}");
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
			{
				throw new ArgumentNullException("limb");
			}

			if (limb.LimbType == VRAvatarLimbType.Head)
			{
				return null;
			}

			var prefab = VRAvatarHelper.EnsureLoadPrefab<VRControllerVisual>(ControllerVisualPrefabName);
			var instance = Instantiate(prefab);

			var ovrController = instance.GetComponent<UnityXRControllerVisual>();
			ovrController.m_controller = GetControllerTypeForLimb(limb);
			ovrController.m_modelGearVrController.SetActive(true);
			ovrController.enabled = false;

			instance.gameObject.SetActive(true);

			return instance;
		}

		private void EnableControllerVisual(UnityXRController controller)
		{
			if (controller == null)
				return;

			// Find the visual for the hand that matches the controller
			UnityXRControllerVisual remote = mRemotes.FirstOrDefault(x => (x.m_controller & controller.ControllerMask) != 0);
			if (remote != null)
			{
				remote.gameObject.SetActive(true);
			}
		}

		private void DisableAllControllerVisuals()
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
			//throw new NotImplementedException();
		}

		/// <summary>
		/// Detects and Updates the state of the controllers including the TouchPad on the GearVR headset
		/// </summary>
		public void DetectAndUpdateControllerStates()
		{
			TrySetLimbsActive();
			TrySetGazeInputActive(false);
			//TrySetGazeInputActive(!IsHandControllerActive);
		}

		/// <summary>
		/// A temporary method to split Oculus Quest changes with the other devices. 
		/// </summary>
		private void TrySetLimbsActive()
		{
			TrySetHandsActive(true);

			//if (OVRUtils.IsOculusQuest)
			//{
			//    TrySetHandActive(VRAvatarLimbType.RightHand);
			//    TrySetHandActive(VRAvatarLimbType.LeftHand);
			//}
			//else
			//{
			//    TrySetHandsActive(IsHandControllerActive);
			//}
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
				//if (OVRUtils.IsGearVRHeadset())
				//{
				//	if (OVRInput.GetActiveController() == OVRInput.Controller.Touchpad)
				//		active = false;
				//}

				mAvatar.SetHandsActive(active);
			}
		}

		private void TrySetGazeInputActive(bool active)
		{
			// Ignore Always & Never Policy
			if (mGazeInput != null && mGazeInput.ActivationPolicy == GazeInputActivationPolicy.NoControllers)
			{
				if (active)
				{
					mGazeInput.Activate();
				}
				else
				{
					mGazeInput.Deactivate();
				}
			}
		}

		private void RecenterHmdIfRequired()
		{
			//throw new NotImplementedException();

			//if (mSettings != null && mSettings.HmdRecenterPolicy != HmdRecenterPolicy.OnControllerRecenter)
			//    return;

			//if (OVRInput.GetControllerWasRecentered())
			//{
			//    // Recenter the camera when the user recenters the controller
			//    UnityEngine.XR.InputTracking.Recenter();
			//}
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
			var unityController = inputDevice as UnityXRController;
			if (unityController != null)
			{
				// A controller was connected
				// Disable gaze controls
				EnableControllerVisual(unityController);
			}
		}

		private void OnInputDeviceDisconnected(IVRDevice vrDevice, IVRInputDevice inputDevice)
		{
			if (!vrDevice.InputDevices.Any(x => x is UnityXRController))
			{
				// No controllers are connected
				// Enable gaze controls
				DisableAllControllerVisuals();
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
#endif