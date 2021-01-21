using Liminal.SDK.Core;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Input;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ControllerInputExample : MonoBehaviour
{
    public Text InputText;

    private void Update()
    {
        var device = VRDevice.Device;
        if (device != null)
        {
            StringBuilder inputStringBuilder = new StringBuilder("");

            AppendDeviceInput(inputStringBuilder, device.PrimaryInputDevice, "Primary");
            inputStringBuilder.AppendLine();
            AppendDeviceInput(inputStringBuilder, device.SecondaryInputDevice, "Secondary");

            InputText.text = inputStringBuilder.ToString();

        }
    }

    public void AppendDeviceInput(StringBuilder builder, IVRInputDevice inputDevice, string deviceName)
    {
        if (inputDevice == null)
            return;

        builder.AppendLine($"{deviceName} Back: {inputDevice.GetButton(VRButton.Back)}");
        builder.AppendLine($"{deviceName} Touch Pad Touching: {inputDevice.IsTouching}");
        builder.AppendLine($"{deviceName} Trigger: {inputDevice.GetButton(VRButton.Trigger)}");
        builder.AppendLine($"{deviceName} Primary: {inputDevice.GetButton(VRButton.Primary)}");
        builder.AppendLine($"{deviceName} Seconday: {inputDevice.GetButton(VRButton.Seconday)}");
        builder.AppendLine($"{deviceName} Three: {inputDevice.GetButton(VRButton.Three)}");
        builder.AppendLine($"{deviceName} Four: {inputDevice.GetButton(VRButton.Four)}");

        builder.AppendLine($"{deviceName} Axis One: {inputDevice.GetAxis2D(VRAxis.One)}");
        builder.AppendLine($"{deviceName} Axis One Raw: {inputDevice.GetAxis2D(VRAxis.OneRaw)}");

        builder.AppendLine($"{deviceName} Axis Two: {inputDevice.GetAxis1D(VRAxis.Two)}");
        builder.AppendLine($"{deviceName} Axis Two Raw: {inputDevice.GetAxis1D(VRAxis.TwoRaw):0.00}");

        builder.AppendLine($"{deviceName} Axis Three: {inputDevice.GetAxis1D(VRAxis.Three)}");
        builder.AppendLine($"{deviceName} Axis Three Raw: {inputDevice.GetAxis1D(VRAxis.ThreeRaw):0.00}");

        if (inputDevice.GetButtonUp(VRButton.Trigger))
        {
            Debug.Log("Button up");
        }

        //builder.AppendLine($"{deviceName} Axis2D-One: {inputDevice.GetAxis2D(VRAxis.One)}");
        //builder.AppendLine($"{deviceName} Axis2D-OneRaw: {inputDevice.GetAxis2D(VRAxis.OneRaw)}");
    }

    public void End() 
    {
        ExperienceApp.End();
    }
}
