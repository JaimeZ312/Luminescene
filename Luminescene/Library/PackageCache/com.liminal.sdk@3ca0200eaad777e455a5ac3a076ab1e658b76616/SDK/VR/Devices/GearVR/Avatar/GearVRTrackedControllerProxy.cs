namespace Liminal.SDK.VR.Devices.GearVR.Avatar
{
    using UnityEngine;
    using Avatars;

    /// <summary>
    /// A concrete implementation of <see cref="IVRTrackedObjectProxy"/> for wrapping around a tracked GearVR controller.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class GearVRTrackedControllerProxy : IVRTrackedObjectProxy
    {
        private VRAvatarLimbType mLimbType;

        private IVRAvatar mAvatar;
        private Transform mAvatarTransform;
        private Transform mHeadTransform;

        #region Properties

        public bool IsActive { get { return true; } }

        public Vector3 Position
        {
            get
            {
                // The controller position is relative to the head
                var localPos = mHeadTransform.localPosition + OVRInput.GetLocalControllerPosition(ActiveController());
                return mAvatarTransform.TransformPoint(localPos);
            }
        }

        public Quaternion Rotation
        {
            get
            {
                // Controller rotation is relative to the head
                return mHeadTransform.rotation * OVRInput.GetLocalControllerRotation(ActiveController());
            }
        }
        
        #endregion

        /// <summary>
        /// Creates a new <see cref="GearVRTrackedControllerProxy"/> for the specified avatar and controller type.
        /// </summary>
        /// <param name="avatar">The avatar that owns the controller.</param>
        /// <param name="controllerType">The controller type the proxy wraps.</param>
        public GearVRTrackedControllerProxy(IVRAvatar avatar, VRAvatarLimbType limbType)
        {
            mAvatar = avatar;
            mAvatarTransform = mAvatar.Transform;
            mHeadTransform = mAvatar.Head.Transform;
            mLimbType = limbType;
        }

        private OVRInput.Controller ActiveController()
        {
            OVRInput.Controller controller = OVRInput.GetActiveController();

            if (OVRUtils.IsQuestControllerConnected)
                controller = OVRUtils.GetControllerType(mLimbType);

            return controller;
        }
    }
}
