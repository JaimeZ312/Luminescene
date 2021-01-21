using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Devices.GearVR.Avatar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR
{

    /// <summary>
    /// Specialisation of OVRCameraRig to support setup of the rig based on the VRAvatar setup
    /// This is pretty clunky as it depends heavily on the implementation of OVRCameraRig. Will
    /// need reviewing when updating OVR Utilities
    /// </summary>
    class GearVRCameraRig : OVRCameraRig
    {
        protected override Transform ConfigureAnchor(Transform root, string name)
        {
            var anchor = GetAnchorFromAvatar(name);

            if (anchor == null)
            {
                anchor = base.ConfigureAnchor(root, name);
            }
            else
            {
                anchor.localScale = Vector3.one;
                anchor.localPosition = Vector3.zero;
                anchor.localRotation = Quaternion.identity;
            }
            return anchor;
        }

        private Transform GetAnchorFromAvatar(string name)
        {
            try
            {
                var avatar = VRAvatar.Active;

                if (name == centerEyeAnchorName)
                {
                    return avatar.Head.CenterEyeCamera.transform;
                }
                else if (name == leftEyeAnchorName)
                {
                    return avatar.Head.LeftEyeCamera.transform;
                }
                else if (name == rightEyeAnchorName)
                {
                    return avatar.Head.RightEyeCamera.transform;
                }
                else if (name == trackerAnchorName)
                {
                    return avatar.Auxiliaries.Find(name);
                }
            }
            catch(Exception e)
            {
                // Ignored as an error here should not block the app.
            }

            return null;
        }
    }
}
