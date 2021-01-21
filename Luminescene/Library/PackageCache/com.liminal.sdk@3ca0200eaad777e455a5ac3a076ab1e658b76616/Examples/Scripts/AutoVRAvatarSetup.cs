using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script ensures that there is a set up for VRAvatar, if it has a 'device' avatar already then it won't do anything.
//To use, just drop it on VRAvatar object.
public class AutoVRAvatarSetup : MonoBehaviour {

	// Use this for initialization
	void Awake () {

        var deviceAvatar = GetComponentInChildren<IVRDeviceAvatar>(includeInactive:true);

        if (deviceAvatar == null)
        {
            var mAvatar = GetComponentInChildren<IVRAvatar>(includeInactive: true);
            var device = VRDevice.Device;

            if (device != null)
            {
                device.SetupAvatar(mAvatar);
            }
        }

    }

}
