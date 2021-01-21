using System.Collections;
using System.IO;
using System.Reflection;
using App;
using UnityEngine;
using UnityEngine.Assertions;
using Liminal.Platform.Experimental.App.Experiences;
using Liminal.Platform.Experimental.Utils;
using Liminal.Platform.Experimental.VR;
using Liminal.SDK.Core;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.Core.Fader;

// TODO Rename the namespace and class name. The world Platform shouldn't be in either.
namespace Liminal.Platform.Experimental.App
{
    /// <summary>
    /// A component to view a limapp within the SDK based on the AppPreviewConfig.
    /// </summary>
    public class PlatformAppViewer : MonoBehaviour
    {
        public VRAvatar Avatar;
        public ExperienceAppPlayer ExperienceAppPlayer;
        public AppPreviewConfig PreviewConfig;
        public BaseLoadingBar LoadingBar;
        public GameObject SceneContainer;

        private byte[] _limappData;

        private void OnValidate()
        {
            Assert.IsNotNull(LoadingBar, "LoadingBar must have a value or else the progress towards loading an experience will not be displayed.");
        }

        private void Start()
        {
            var deviceInitializer = GetComponentInChildren<IVRDeviceInitializer>();
            var device = deviceInitializer.CreateDevice();
            VRDevice.Initialize(device);
            VRDevice.Device.SetupAvatar(Avatar);
            BetterStreamingAssets.Initialize();
        }

        public void Play()
        {
            if(!ExperienceAppPlayer.IsRunning)
                StartCoroutine(PlayRoutine());
        }

        public void Stop()
        {
            StartCoroutine((StopRoutine()));
        }

        private IEnumerator PlayRoutine()
        {
            SceneContainer.SetActive(false);

            ResolvePlatformLimapp(out _limappData, out string fileName);

            var experience = new Experience
            {
                Id = ExperienceAppUtils.AppIdFromName(fileName),
                Bytes = _limappData,
                CompressionType = GetCompressionType(fileName),
            };

            var loadOp = ExperienceAppPlayer.Load(experience);
            LoadingBar.Load(loadOp);
            EnsureEmulatorFlagIsFalse();
            yield return loadOp.LoadScene();
            EnsureEmulatorFlagIsFalse();

            LoadingBar.SetActiveState(false);

            ExperienceAppPlayer.Begin();

            ExperienceApp.OnComplete += OnExperienceComplete;
            ExperienceApp.Initializing += SetScreenfaderActive;
        }

        private ECompressionType GetCompressionType(string fileName)
        {
            var compression = ECompressionType.LMZA;

            if (string.IsNullOrEmpty(fileName) || string.IsNullOrWhiteSpace(fileName))
                return compression;
            
            if (Path.GetExtension(fileName).Equals(".ulimapp")) 
                compression = ECompressionType.Uncompressed;

            return compression;
        }

        private void SetScreenfaderActive()
        {
            var avatar = (VRAvatar)FindObjectOfType(typeof(VRAvatar));
            avatar.GetComponentInChildren<CompoundScreenFader>().enabled = true;
        }

        private void OnExperienceComplete(bool completed)
        {
            Stop();   
        }

        private IEnumerator StopRoutine()
        {
            yield return ExperienceAppPlayer.Unload();
            Avatar.SetActive(true);
            SceneContainer.SetActive(true);
        }

        private void ResolvePlatformLimapp(out byte[] data, out string fileName)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                data = BetterStreamingAssets.ReadAllBytes(PreviewConfig.AndroidAppFullName);
                fileName = PreviewConfig.AndroidAppFullName;
            }
            else
            {
                var limappPath = PreviewConfig.EmulatorPath;
                fileName = Path.GetFileName(limappPath);
                data = File.ReadAllBytes(limappPath);
            }
        }

        private void EnsureEmulatorFlagIsFalse()
        {
            var isEmulator = typeof(ExperienceApp).GetField("_isEmulator", BindingFlags.Static | BindingFlags.NonPublic);
            isEmulator.SetValue(null, false);
        }
    }
}
