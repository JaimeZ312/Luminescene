using Liminal.SDK.VR.Avatars;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Liminal.SDK.XR
{
    /// <summary>
    /// 
    /// </summary>
    [DisallowMultipleComponent]
    public class UnityXRTrackedControllerProxy : IVRTrackedObjectProxy
    {
        #region Variables
        private VRAvatarLimbType mLimbType;

        private IVRAvatar mAvatar;
        private Transform mAvatarTransform;
        private Transform mHeadTransform;

        private InputDevice mInputDevice;

        public bool IsActive { get { return true; } }

        /// <summary>
        /// World position of the device
        /// </summary>
        public Vector3 Position
        {
            get
            {
                if (mInputDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 devicePosition))
                {
                    return devicePosition;
                }

                return Vector3.zero;
            }
        }

        public Quaternion Rotation
        {
            get
            {
                if (mInputDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion deviceRotation))
                {
                    return deviceRotation;
                }

                return Quaternion.identity;
            }
        }
        #endregion

        public UnityXRTrackedControllerProxy(IVRAvatar avatar, VRAvatarLimbType limbType)
        {
            mAvatar = avatar;
            mAvatarTransform = mAvatar.Transform;
            mHeadTransform = mAvatar.Head.Transform;
            mLimbType = limbType;

            if (TryGetXRNode(out XRNode outNode))
            {
                mInputDevice = InputDevices.GetDeviceAtXRNode(outNode);

                if (!mInputDevice.isValid)
                {
                    Debug.LogError($"No valid input device for {limbType}");
                    //throw new System.Exception($"No valid input device for {limbType}");
                }
            }
        }

        /// <summary>
        /// TODO: A better/different way to do this
        /// </summary>
        /// <param name="outNode"></param>
        /// <returns></returns>
        private bool TryGetXRNode(out XRNode outNode)
        {
            outNode = 0;

            switch (mLimbType)
            {
                case VRAvatarLimbType.Head:
                    outNode = XRNode.Head;
                    return true;
                case VRAvatarLimbType.LeftHand:
                    outNode = XRNode.LeftHand;
                    return true;
                case VRAvatarLimbType.RightHand:
                    outNode = XRNode.RightHand;
                    return true;
                case VRAvatarLimbType.None:
                case VRAvatarLimbType.Other:
                default:
                    return false;
            }
        }
    }
}