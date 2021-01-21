using System;
using System.Collections;
using System.Reflection;
using App;
using Liminal.Platform.Experimental.App.BundleLoader;
using Liminal.SDK.Core;
using Liminal.SDK.Serialization;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace Liminal.Platform.Experimental.App.Experiences
{
    /// <summary>
    /// Runs an App Bundle Loader and initializes the Application
    /// </summary>
    public class ExperienceAppPlayer : MonoBehaviour
    {
        public event Action<Experience, ExperienceApp> ExperienceAppLoaded;
        public event Action<bool> ExperienceAppUnloaded;
        public event Action<bool> ExperienceAppEnded;

        public ExperienceApp CurrentApp { get; private set; }

        private FieldInfo _isEmulator;

        private BundleLoader.Impl.BundleLoader _bundleLoader = new BundleLoader.Impl.BundleLoader();
        private BundleAsyncLoadOperationBase _loadOperation;
        private ExperienceStateModel _stateModel = new ExperienceStateModel();

        public bool IsRunning
        {
            get { return _stateModel.State != AppState.NotLoaded; }
        }

        /// <summary>
        /// Loads a limapp, all GameObjects under [ExperienceApp] will be inactive until Begin() is called
        /// </summary>
        /// <param name="experience"></param>
        /// <returns></returns>
        public BundleAsyncLoadOperationBase Load(Experience experience)
        {
            Setup();

            _loadOperation = _bundleLoader.Load(experience);
            _loadOperation.Completed += OnAppLoadComplete;

            return _loadOperation;
        }

        /// <summary>
        /// Starts the application by calling ExperienceApp.Initialize on the Limapp through reflections
        /// </summary>
        /// <returns></returns>
        public bool Begin()
        {
            if (_loadOperation.IsDone)
            {
                CurrentApp = _loadOperation.ExperienceApp;

                try
                {
                    InitializeApp();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Unload();
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Unloads everything related to limapps, GC.Collect will be called
        /// </summary>
        /// <returns></returns>
        public Coroutine Unload()
        {
            return StartCoroutine(UnloadRoutine(completed: false));
        }

        private void Setup()
        {
            EnsureEmulatorFlagIsFalse();

            // Unload the current app (forced sync)
            var itr = UnloadRoutine(completed: false);
            while (itr.MoveNext())
            {
                continue;
            }
        }

        /// <summary>
        /// Invoke a shutdown method for the current app through reflections
        /// </summary>
        private void ShutdownApp()
        {
            try
            {
                ExperienceAppReflectionCache.ShutdownMethod.Invoke(CurrentApp, null);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnAppLoadComplete(BundleAsyncLoadOperationBase operationBase)
        {
            operationBase.Completed -= OnAppLoadComplete;
            ExperienceAppLoaded?.Invoke(operationBase.Experience, operationBase.ExperienceApp);
        }

        private Coroutine InitializeApp()
        {
            _stateModel.SetState(AppState.Running);
            _stateModel.SetStartTime(Time.realtimeSinceStartup);
            InputTracking.Recenter();

            // TODO Investigate if activating the pointer is necessary.
            try
            {
                if (VRAvatar.Active.PrimaryHand != null && VRAvatar.Active.PrimaryHand.IsActive)
                {
                    VRDevice.Device.PrimaryInputDevice.Pointer.Activate();
                }
                else
                {
                    Debug.Log("Could not activate pointer");
                }
            }
            catch(Exception e)
            {
                Debug.Log($"Could not activate pointer {e}");
            }

            return StartCoroutine(_InitializeApp());
        }

        private IEnumerator _InitializeApp()
        {
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();

            yield return null;

            SceneManager.SetActiveScene(CurrentApp.gameObject.scene);
            CurrentApp.gameObject.SetActive(true);

            //# SUPER IMPORTANT
            var device = DeviceUtils.CreateDevice(CurrentApp);
            VRDevice.Replace(device);

            var method = ExperienceAppReflectionCache.InitializeMethod;
            yield return (IEnumerator)method.Invoke(CurrentApp, null);
        }

        private IEnumerator UnloadRoutine(bool completed)
        {
            _stateModel.SetState(AppState.NotLoaded);
            ExperienceAppEnded?.Invoke(completed);

            var requireUnload = _loadOperation != null || CurrentApp != null;

            Cancel();
            yield return ShutDownAppRoutine();

            UnloadAssetBundle();

            if (requireUnload)
                yield return CleanUp();

            ExperienceAppUnloaded?.Invoke(completed);
        }

        private void Cancel()
        {
            if (_loadOperation != null)
            {
                _loadOperation.Completed -= OnAppLoadComplete;
                _loadOperation.Cancel();
                _loadOperation = null;
            }
        }

        private IEnumerator ShutDownAppRoutine()
        {
            if (CurrentApp != null)
            {
                ShutdownApp();

                yield return UnloadAppScene();
                yield return _bundleLoader.Unload(CurrentApp);

                CurrentApp = null;
                _stateModel.SetState(AppState.NotLoaded);
            }
        }

        private IEnumerator UnloadAppScene()
        {
            var go = CurrentApp.gameObject;
            if (go != null)
            {
                yield return SceneManager.UnloadSceneAsync(go.scene);
            }
        }

        private void UnloadAssetBundle()
        {
            if (ExperienceApp.AssetBundle != null)
            {
                ExperienceApp.AssetBundle.Unload(unloadAllLoadedObjects: true);
                ExperienceAppReflectionCache.AssetBundleField.SetValue(null, null);
            }
        }

        private IEnumerator CleanUp()
        {
            ExperienceAppReflectionCache.IsEndingField.SetValue(null, false);
            SerializationUtils.ClearGlobalSerializableTypes();
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();
            Time.timeScale = 1f;
        }

        private void EnsureEmulatorFlagIsFalse()
        {
            _isEmulator = typeof(ExperienceApp).GetField("_isEmulator", BindingFlags.Static | BindingFlags.NonPublic);
            _isEmulator.SetValue(null, false);
        }
    }
}
