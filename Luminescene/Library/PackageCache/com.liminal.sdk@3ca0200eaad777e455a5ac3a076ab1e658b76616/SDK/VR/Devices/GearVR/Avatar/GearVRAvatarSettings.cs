using Liminal.SDK.VR.Avatars;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR.Avatar
{
    /// <summary>
    /// Values for the recentering policy for the GearVR HMD.
    /// </summary>
    public enum HmdRecenterPolicy
    {
        /// <summary>
        /// Never recenter the HMD.
        /// </summary>
        Never = 0,

        /// <summary>
        /// Recenter the HMD whenever the user recenters the controller.
        /// </summary>
        OnControllerRecenter = 1,
    }

    /// <summary>
    /// A component containing per-application settings for a <see cref="GearVRAvatar"/>. Attach this component to your <see cref="VRAvatar"/> to have the settings
    /// applied when running on a GearVR device.
    /// </summary>
    [DisallowMultipleComponent]
    public class GearVRAvatarSettings : MonoBehaviour, IVRAvatarSettings
    {
        [Tooltip("Determines if the HMD camera should be recentered when the user recenters the controller.")]
        [SerializeField] private HmdRecenterPolicy m_HmdRecenterPolicy = HmdRecenterPolicy.OnControllerRecenter;

        /// <summary>
        /// Determines if the HMD tracked camera should be recentered when the user recenters the controller.
        /// </summary>
        public HmdRecenterPolicy HmdRecenterPolicy
        {
            get { return m_HmdRecenterPolicy; }
        }
    }
}
