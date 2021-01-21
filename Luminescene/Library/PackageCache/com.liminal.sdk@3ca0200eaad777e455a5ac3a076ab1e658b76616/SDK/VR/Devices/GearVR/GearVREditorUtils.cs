using Liminal.SDK.VR.Avatars.Controllers;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR
{
#if UNITY_EDITOR
    public static class GearVREditorUtils
    {
        public static VRControllerVisual CreatePreviewControllerVisual()
        {
            var prefab = Resources.Load<VRControllerVisual>("GearVRController");
            if (prefab == null)
                return null;

            return UnityEngine.Object.Instantiate(prefab);
        }
    }
#endif
}
