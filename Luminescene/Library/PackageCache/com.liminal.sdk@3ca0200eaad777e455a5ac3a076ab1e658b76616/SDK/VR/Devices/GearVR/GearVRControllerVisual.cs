using Liminal.SDK.VR.Avatars.Controllers;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR
{
    public class GearVRControllerVisual : VRControllerVisual
    {
        [SerializeField] private OVRControllerHelper trackedRemote = null;
        private bool isOculusGo;
        public Vector3 QuestPosition = new Vector3(0, -0.0095f, 0);
        protected override void Awake()
        {
            isOculusGo = GearVRDevice.IsOculusGo;



            base.Awake();
        }
    }
}
