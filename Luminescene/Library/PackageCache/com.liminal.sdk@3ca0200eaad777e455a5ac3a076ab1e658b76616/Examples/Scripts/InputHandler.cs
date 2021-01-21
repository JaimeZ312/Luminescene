using Liminal.SDK.VR;
using Liminal.SDK.VR.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InputHandler : MonoBehaviour
{
    [Header("Events")]
    [Tooltip("Raised when the controller button click is detected.")]
    public UnityEvent OnButtonClicked = new UnityEvent();

    #region MonoBehaviour

    private void Update()
    {
        HandleInput();
    }

    #endregion

    private void HandleInput()
    {
        // Get the currently active VR device
        var vrDevice = VRDevice.Device;
        if (vrDevice == null)
            return;

        // Get the primary input device (the controller)
        var inputDevice = vrDevice.PrimaryInputDevice;
        if (inputDevice == null)
            return;

        // Check if the main button has been pressed
        if (inputDevice.GetButtonDown(VRButton.One))
        {
            Debug.Log(string.Format("[InputHandler] Input detected: {0}", VRButton.One), this);

            // Raise the OnButtonClicked event
            OnButtonClicked.Invoke();
        }
    }
}
