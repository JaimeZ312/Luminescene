using System;
using System.Collections;
using System.Reflection;
using Liminal.Core.Fader;
using Liminal.Platform.Experimental.App.Experiences;
using Liminal.Platform.Experimental.Exceptions;
using Liminal.Platform.Experimental.Extensions;
using Liminal.Platform.Experimental.Services;
using Liminal.Platform.Experimental.VR;
using Liminal.SDK.Core;
using Liminal.SDK.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liminal.Platform.Experimental.App.BundleLoader.Impl
{
    public class BundleAsyncLoadOperation : BundleAsyncLoadOperationBase
    {
        private Experience mExperience;
        private bool mSceneLoadCompletedHandlerDone;
        private AppPack mAppPack;
        private bool faulted = false;

        private const string SceneName = "AppScene";
        private const string CoroutineProxyName = "$_ExperienceLoadCoroutineProxy";

        private enum State
        {
            NotInProgress,
            Unpacking,
            LoadingAssemblies,
            LoadingAssetBundle,
            LoadingScene,
            Loaded,
            ActivatingScene,
            Completed,
        }

        #region Properties

        private float m_Progress;

        public override float Progress
        {
            get
            {
                return IsDone ? 1f : m_Progress;
            }

            protected set
            {
                m_Progress = value;
            }
        }

        public override bool DownloadState { get { return IsDownloaded; } }
        public override float DownloadProgress { get { return CurrentProgress; } }

        #endregion

        #region Events

        public override event BundleAsyncLoadOperationEventHandler Completed;

        #endregion

        public bool Faulted() { return faulted; }

        public BundleAsyncLoadOperation(Experience experience) : base(experience)
        {
            mExperience = experience ?? throw new ArgumentNullException("experience");
            CoroutineService.Instance.StartCoroutine(DoLoad(experience.Bytes, experience.CompressionType));
        }

        private IEnumerator DoLoad(byte[] data, ECompressionType compression = ECompressionType.LMZA)
        {
            SerializationUtils.ClearGlobalSerializableTypes();
            yield return UnpackFile(data, compression);
        }

        private IEnumerator UnpackFile(byte[] data, ECompressionType compression = ECompressionType.LMZA)
        {
            Debug.Log("[BundleLoader] Commencing unpack...");

            using (new HighCpuLevelSection())
            {
                SetState(State.Unpacking);

                var unpacker = new AppUnpacker();

                try
                {
                    unpacker.UnpackAsync(data, compression);
                }
                catch (Exception e)
                {
                    Debug.Log("[BundleLoader]" + e);
                }

                if (IsFaulted)
                {
                    Debug.Log("[BundleLoader] Bundle is faulted.");
                    yield break;
                }

                yield return new WaitUntil(() => unpacker.IsDone);

                if (unpacker.IsFaulted)
                {
                    Debug.LogError("[BundleLoader] Unpack failed");
                    Debug.LogException(unpacker.Exception);
                    Cancel();
                    yield break;
                }
                else
                {
                    Debug.Log("[BundleLoader] Unpack complete");
                    mAppPack = unpacker.Data;

                    /*
                    if (mAppPack.ApplicationId != mExperience.Id)
                    {
                        var message = string.Format("[BundleLoader] Bundle Application Id {0} did not match Experience Id {1}", mAppPack.ApplicationId, mExperience.Id);
                        FaultWithException(new BundleFileException(mExperience, message));
                        Cancel();
                        yield break;
                    }
                    */
                }
            }
        }

        /// <summary>
        /// This routine includes the operations that are likely to impact framerate and is
        /// therefore intended to be run under the cover of a 2D loading UI.
        /// </summary>
        public override IEnumerator LoadScene()
        {
            if (IsCancelled || IsFaulted)
            {
                FaultWithException(new BundleFileException(mExperience, "LoadScene() called on faulted or cancelled operation"));
            }

            yield return new WaitUntil(() => mAppPack != null);

            using (new HighCpuLevelSection())
            {
                yield return LoadSceneCoroutine();

                SetState(State.ActivatingScene);

                yield return new WaitUntil(() => mSceneLoadCompletedHandlerDone);
                SetState(State.Completed);
                IsDone = true;

                if (!IsFaulted && (ExperienceApp == null))
                {
                    FaultWithException(new BundleFileException(mExperience));
                }
                else if (Completed != null)
                {
                    Completed(this);
                }
            }
        }

        private IEnumerator LoadSceneCoroutine()
        {
            SetState(State.LoadingAssemblies);

            // Load assemblies
            if (mAppPack.Assemblies != null)
            {
                foreach (var asmBytes in mAppPack.Assemblies)
                {
                    yield return LoadAppAssembly(asmBytes);
                    yield return null;
                }
            }

            yield return LoadAppScene(mAppPack.SceneBundle);
        }

        private IEnumerator LoadAppAssembly(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                Debug.Log("[BundleLoader] No assembly included in app.");
                yield break;
            }

            // Load the assembly into the current application domain
            var asm = Assembly.Load(bytes);

            // Add the serializable types from the assembly to the global list
            // This is required by the app to be able to correctly deserialize some types after being imported
            SerializationUtils.AddGlobalSerializableTypes(asm);

            Debug.LogFormat("[BundleLoader] Assembly Loaded: {0}", asm);
            yield return null;
        }

        private IEnumerator LoadAppScene(byte[] bytes)
        {
            SetState(State.LoadingAssetBundle);

            var request = AssetBundle.LoadFromMemoryAsync(bytes);
            yield return request;

            var assetBundle = request.assetBundle;
            ExperienceAppReflectionCache.AssetBundleField.SetValue(null, assetBundle);

            SetState(State.LoadingScene);

            // Fetch scene name from the bundle and begin loading it
            var scenePath = assetBundle.GetAllScenePaths()[0];

            var loadOp = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            SceneManager.sceneLoaded += OnSceneLoadCompleted;

            loadOp.allowSceneActivation = false;

            while (loadOp.progress < 0.9f)
            {
                yield return new WaitForEndOfFrame();
            }

            SetState(State.Loaded);
            yield return new WaitForSeconds(1f);

            SetState(State.Completed);

            if (ScreenFader.Instance == null)
            {
                Debug.LogError("Screen fader singleton may not be setup. Make sure the Singleton checkbox is checked on the CompoundScreenFader.");
            }

            ScreenFader.Instance?.FadeToBlack(2f);

            yield return new WaitForSeconds(3f);

            loadOp.allowSceneActivation = true;

            IsLoaded = true;
        }

        private void OnSceneLoadCompleted(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("[BundleLoader] Scene load completed");

            // Clean up...
            SceneManager.sceneLoaded -= OnSceneLoadCompleted;

            if (IsCancelled)
            {
                // If cancelled, bail out here and unload the scene immediately
                SceneManager.UnloadSceneAsync(SceneName);
                return;
            }

            //For some reason, experience app is inactive when loading
            //maybe need to wait a second or something
            //Quick hack for now.
            if (ExperienceApp == null)
            {
                var apps = Resources.FindObjectsOfTypeAll<ExperienceApp>();

                if (apps.Length > 0)
                {
                    ExperienceApp = apps[0];
                    ExperienceApp.gameObject.SetActive(true);
                }
            }

            SceneManager.SetActiveScene(scene);
            mSceneLoadCompletedHandlerDone = true;
        }

        protected override void OnCancelled()
        {
            SceneManager.sceneLoaded -= OnSceneLoadCompleted;
        }

        private void SetState(State state)
        {
            Debug.LogFormat("[BundleLoader] State: {0}", state);
            //Progress is relative to the number of declared states (Deducting 1 for NotInProgress)
            var stateCount = Enum.GetValues(typeof(State)).Length - 1;
            Progress = (float)state / stateCount;
        }
    }
}