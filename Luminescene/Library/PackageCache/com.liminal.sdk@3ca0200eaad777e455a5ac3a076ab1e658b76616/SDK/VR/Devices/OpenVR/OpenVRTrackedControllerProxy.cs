using Liminal.SDK.VR.Avatars;
using UnityEngine;
using Valve.VR;

namespace Liminal.SDK.OpenVR
{
    public class OpenVRTrackedControllerProxy : IVRTrackedObjectProxy
    {
        private Transform _avatar;
        private Transform _head;

        public SteamVR_Behaviour_Pose Controller;

        public bool IsActive => Controller.gameObject.activeInHierarchy;
        public Vector3 Position => _head.TransformPoint(Controller.transform.localPosition);
        public Quaternion Rotation => _head.rotation * Controller.transform.localRotation;

        public OpenVRTrackedControllerProxy(SteamVR_Behaviour_Pose controller, Transform head, Transform avatar)
        {
            Controller = controller;
            _avatar = avatar;
            _head = head;
        }
    }
}