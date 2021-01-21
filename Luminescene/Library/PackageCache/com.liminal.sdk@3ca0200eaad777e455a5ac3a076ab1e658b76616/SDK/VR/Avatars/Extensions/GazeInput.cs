using Liminal.SDK.VR.Pointers;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars.Extensions
{
    /// <summary>
    /// Policy values for determine when gaze input is enabled.
    /// </summary>
    public enum GazeInputActivationPolicy
    {
        /// <summary>
        /// Never activate gaze input.
        /// </summary>
        Never,

        /// <summary>
        /// Only activate gaze input if no controllers are connected to the device.
        /// </summary>
        NoControllers,

        /// <summary>
        /// Always allow gaze input.
        /// </summary>
        Always,
    }

    /// <summary>
    /// An avatar extension that automatically manages gaze input.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR/Avatar/Gaze Input")]
    public class GazeInput : MonoBehaviour, IVRAvatarExtension
    {
        private IVRAvatar mAvatar;
        private IVRDevice mDevice;
        private IVRPointer mPointer; // This is the GazePointer
        private IVRPointerVisual mPointerVisual;
        private bool mExternalActivated;
        private bool mActive;

        [Header("Components")]
        [Tooltip("The pointer visual for gaze input.")]
        [SerializeField] private BasePointerVisual m_PointerVisual = null;
        [Header("Settings")]
        [Tooltip("Determines when Gaze input is activated."
            + "\n\nNever - Gaze input is never activated."
            + "\n\nNo Controllers - If a controller is connected, gaze input will be disabled. If all controllers are disconnected, gaze input will be enabled."
            + "\n\nAlways - Gaze input is always active, even when controllers are connected.")]
        [SerializeField] private GazeInputActivationPolicy m_ActivationPolicy = GazeInputActivationPolicy.NoControllers;
        [Tooltip("The duration the pointer must be hovered over an object before the interaction timer begins. Applies only to timed gaze pointers.")]
        [SerializeField] private float m_HoverDelay = 0.5f;
        [Tooltip("The duration the pointer must be hovered over an object before interaction is triggered. Applies only to timed gaze pointers.")]
        [SerializeField] private float m_HoverDuration = 2f;

        #region Properties
        
        /// <summary>
        /// Gets the <see cref="BasePointerVisual"/> assigned to the component.
        /// </summary>
        public BasePointerVisual PointerVisual
        {
            get { return m_PointerVisual; }
        }

        /// <summary>
        /// Gets or sets the activation policy for gaze controls.
        /// </summary>
        public GazeInputActivationPolicy ActivationPolicy
        {
            get { return m_ActivationPolicy; }
            set
            {
                m_ActivationPolicy = value;
                DetectAndUpdateActiveState();
            }
        }

        /// <summary>
        /// Gets or set the duration the pointer must be hovered over an object before the interaction timer begins. Applies only to timed gaze pointers.
        /// </summary>
        public float HoverDelay
        {
            get { return m_HoverDelay; }
            set { m_HoverDelay = Mathf.Max(0, value); }
        }

        /// <summary>
        /// Gets or sets the duration the pointer must be hovered over an object before interaction is triggered. Applies only to timed gaze pointers.
        /// </summary>
        public float HoverDuration
        {
            get { return m_HoverDuration; }
            set { m_HoverDuration = Mathf.Max(0, value); }
        }

        #endregion

        #region MonoBehaviour

        private void OnDestroy()
        {
            mDevice = null;
        }

        private void OnValidate()
        {
            m_HoverDelay = Mathf.Max(m_HoverDelay, 0);
            m_HoverDuration = Mathf.Max(m_HoverDuration, 0);

            DetectAndUpdateActiveState();
            ApplyTimedPointerProperties();
        }

        private void Update()
        {
            DetectAndUpdateActiveState();
        }

        #endregion

        /// <summary>
        /// Set and apply a new delay and duration for the gaze input. 
        /// </summary>
        /// <param name="delay">The total time in seconds it takes to begin the gaze</param>
        /// <param name="duration">The total time in seconds it takes to trigger a click</param>
        public void UpdateHoverSettings(float delay, float duration)
        {
            m_HoverDelay = Mathf.Max(delay, 0);
            m_HoverDuration = Mathf.Max(duration, 0);
            ApplyTimedPointerProperties();
        }

        /// <summary>
        /// Initializes the avatar extension.
        /// </summary>
        /// <param name="avatar">The <see cref="IVRAvatar"/> the extension is bound to.</param>
        public void Initialize(IVRAvatar avatar)
        {
            mAvatar = avatar;
            mDevice = VRDevice.Device;
            mPointer = mDevice.Headset.Pointer;
            mPointerVisual = m_PointerVisual;

            if (mPointerVisual == null)
            {
                if (mPointer.Transform == transform)
                    mPointer.Transform = null;
            }
            else
            {
                mPointerVisual.Bind(mPointer);
                mPointer.Transform = mPointerVisual.transform;
            }

            ApplyTimedPointerProperties();

            Debug.Log("Initialized");
        }

        /// <summary>
        /// Activates gaze control for the avatar.
        /// </summary>
        public void Activate()
        {
            mExternalActivated = true;
            InternalSetActive(true);
        }

        /// <summary>
        /// Deactivates gaze control for the avatar.
        /// </summary>
        public void Deactivate()
        {
            mExternalActivated = false;
            InternalSetActive(false);
        }

        private void DetectAndUpdateActiveState()
        {
            // TODO: Find out why DetectAndUpdateActiveState is called when we build the application.
            // Prevents build errors from assembly definition not being usable yet.
            if (!Application.isPlaying)
                return;

            if (!enabled || !gameObject.activeInHierarchy)
                return;

            switch (m_ActivationPolicy)
            {
                // Always enable gaze controls
                case GazeInputActivationPolicy.Always:
                    InternalSetActive(true);
                    return;

                // Only activate gaze controllers when no controllers are connected
                case GazeInputActivationPolicy.NoControllers:
                    if (!mExternalActivated)
                    {
                        var hasController = VRAvatar.Active != null && VRAvatar.Active.PrimaryHand.IsActive;
                        InternalSetActive(!hasController);
                    }
                    break;
            
                // Never use gaze controls unless manually activated
                case GazeInputActivationPolicy.Never:
                    if (!mExternalActivated)
                    {
                        InternalSetActive(false);
                    }
                    break;
            }
        }

        //The emulator detects the active state of the hand which needs a frame
        private void EmulatorDetectAndUpdate()
        {
            if (mAvatar != null)
            {
                StopAllCoroutines();
                StartCoroutine(DoLateDetectAndApply());
            }
        }

        private IEnumerator DoLateDetectAndApply()
        {
            yield return null;
            InternalSetActive(!HandsActive);
        }

        /// <summary>
        /// Return if hands are active for the avatar
        /// </summary>
        public bool HandsActive
        {
            get
            {
                foreach (var limb in mAvatar.Hands)
                {
                    if (limb.IsActive)
                        return true;
                }

                return false;
            }
        }

        private void InternalSetActive(bool active)
        {
            if (mPointer == null)
                return;

            if (active)
            {
                mPointer.Activate();

                if (mPointerVisual != null)
                {
                    mPointer.Transform = mPointerVisual.transform;
                }
            }
            else
            {
                mPointer.Deactivate();
                mPointer.Transform = null;
            }
        }

        private bool IsNonHeadsetControllerConnected()
        {
            // This is really a special-case for the GearVR, where the HMD is also an input device
            // We only want the NoControllers activation policy to become activated when there is no _controller_,
            // but without checking if it is also a headset means it would never be activated
            return (mDevice != null) && mDevice.InputDevices.Any(x => !(x is IVRHeadset));
        }
        
        private void ApplyTimedPointerProperties()
        {
            if (!Application.isPlaying)
                return;

            var timedPointer = mPointer as TimedGazePointer;
            if (timedPointer == null)
                return;

            timedPointer.HoverDelay = m_HoverDelay;
            timedPointer.HoverActivationDuration = m_HoverDuration;
        }
    }
}
