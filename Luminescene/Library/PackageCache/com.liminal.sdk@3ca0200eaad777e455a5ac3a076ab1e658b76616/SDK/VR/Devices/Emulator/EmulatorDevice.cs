using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;
using System;
using System.Collections.Generic;

namespace Liminal.SDK.VR.Devices.Emulator
{
    /// <summary>
    /// A class representing the emulator device type.
    /// </summary>
    public class EmulatorDevice : IVRDevice
    {
        /// <summary>
        /// Thed device name 
        /// </summary>
        public const string DeviceName = "Emulator";

        private const VREmulatorDevice _defaultDevice = VREmulatorDevice.Daydream;
        private static VRDeviceCapability _capabilties = VRDeviceCapability.Controller;
        
        private IVRDevice mImpl;

        #region Properties

        string IVRDevice.Name { get { return DeviceName; } }

        int IVRDevice.InputDeviceCount { get { return 1; } }

        IVRHeadset IVRDevice.Headset { get { return mImpl.Headset; } }        
        IVRInputDevice IVRDevice.PrimaryInputDevice { get { return mImpl.PrimaryInputDevice; } }
        IVRInputDevice IVRDevice.SecondaryInputDevice { get { return mImpl.SecondaryInputDevice; } }        
        IEnumerable<IVRInputDevice> IVRDevice.InputDevices { get { return mImpl.InputDevices; } }
        int IVRDevice.CpuLevel {  get { return mImpl.CpuLevel; } set { mImpl.CpuLevel = value; } }
        int IVRDevice.GpuLevel { get { return mImpl.GpuLevel; } set { mImpl.GpuLevel = value; } }

        #endregion

        #region Events
#pragma warning disable 0649
        event VRInputDeviceEventHandler IVRDevice.InputDeviceConnected
        {
            add { mImpl.InputDeviceConnected += value; }
            remove { mImpl.InputDeviceConnected -= value; }
        }

        event VRInputDeviceEventHandler IVRDevice.InputDeviceDisconnected
        {
            add { mImpl.InputDeviceDisconnected += value; }
            remove { mImpl.InputDeviceDisconnected -= value; }
        }

        event VRDeviceEventHandler IVRDevice.PrimaryInputDeviceChanged
        {
            add { mImpl.PrimaryInputDeviceChanged += value; }
            remove { mImpl.PrimaryInputDeviceChanged -= value; }
        }

#pragma warning restore 0649
        #endregion

        /// <summary>
        /// Creates a new emulator device.
        /// </summary>
        /// <param name="device">The emulator device type.</param>
        public EmulatorDevice(VREmulatorDevice device)
        {
            if (!Enum.IsDefined(typeof(VREmulatorDevice), device))
            {
                UnityEngine.Debug.LogFormat("[EmulatorDevice] No VREmulatorSetup supplied, using default device ({0}).", _defaultDevice.ToString());
                mImpl = CreateDeviceImplementation(_defaultDevice);
            }
            else
            {
                mImpl = CreateDeviceImplementation(device);
            }

            UnityEngine.Debug.LogFormat("[EmulatorDevice] Using device implementation: {0}", mImpl.GetType().Name);
        }

        /// <summary>
        /// Creates the device implementation for the specified emulation method.
        /// </summary>
        /// <param name="device">The <see cref="VREmulatorDevice"/> type.</param>
        /// <returns>The <see cref="IVRDevice"/> implementation for the specified device.</returns>
        private IVRDevice CreateDeviceImplementation(VREmulatorDevice device)
        {
            switch (device)
            {
                case VREmulatorDevice.GearVR:
                    return new GearVR.GearVRDevice();

                case VREmulatorDevice.Daydream:
                default:
                    return new DaydreamView.DaydreamViewDevice();
            }
        }
        
        /// <summary>
        /// Indicates if the device has a specific set of capabilities using a bitmask of <see cref="VRDeviceCapability"/> values.
        /// </summary>
        /// <param name="capabilities">A bitmask of capabilities to check.</param>
        /// <returns>A boolean indicating if the device has all of the specified capabilities.</returns>
        public bool HasCapabilities(VRDeviceCapability capabilities)
        {
            return ((_capabilties & capabilities) == 0);
        }

        void IVRDevice.Update()
        {
            mImpl.Update();
        }

        void IVRDevice.SetupAvatar(IVRAvatar avatar)
        {
            mImpl.SetupAvatar(avatar);
        }
    }
}
