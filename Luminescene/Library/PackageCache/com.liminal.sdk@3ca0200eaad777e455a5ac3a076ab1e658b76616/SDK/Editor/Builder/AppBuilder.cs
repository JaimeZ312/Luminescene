using Liminal.SDK.Build;
using Liminal.SDK.Core;
using Liminal.SDK.Editor.Serialization;
using Liminal.SDK.Serialization;
using Liminal.SDK.VR.Avatars;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liminal.SDK.Editor.Build
{
    /// <summary>
    /// Provides functionality for building a Liminal app.
    /// </summary>
    public static class AppBuilder
    {
        /// <summary>
        /// The name of the application serialized data file.
        /// </summary>
        private const string AppDataName = "AppData.json";

        /// <summary>
        /// The output name of the application assembly.
        /// </summary>
        private const string AppAssemblyFileName = "AppModule.dll";

        #region Public Interface

        /// <summary>
        /// Builds a Liminal Experience Application from the current scene and build target.
        /// </summary>
        public static void BuildCurrentPlatform()
        {
            Build(new AppBuildInfo()
            {
                Scene = SceneManager.GetActiveScene(),
                BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                BuildTargetDevice = AppBuildInfo.BuildTargetDevices.None,
            });
        }

        /// <summary>
        /// Builds a Liminal Experience Application from the current scene and standalone build target.
        /// </summary>
        public static void BuildLimapp(BuildTarget target, AppBuildInfo.BuildTargetDevices devices,
            ECompressionType compression = ECompressionType.LMZA)
        {
            Build(new AppBuildInfo()
            {
                Scene = SceneManager.GetActiveScene(),
                BuildTarget = target,
                BuildTargetDevice = devices,
                CompressionType = compression,
            });
        }

        /// <summary>
        /// Lists the assemblies that will be included in the App build.
        /// </summary>
        [MenuItem("Liminal/List Included Assemblies")]
        public static void ListAssemblies()
        {
            var plugins = GetIncludedPlugins(EditorUserBuildSettings.selectedBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget);
            var names = plugins.Select(x => x.assetPath);
            Debug.LogFormat("Assemblies included in App Build ({0}): {1}", plugins.Count(), string.Join("\n", names.ToArray()));
        }

        public static void UpdateProgressBar(string title, string description, float progress)
        {
            EditorUtility.DisplayProgressBar(title, description, progress);
        }

        /// <summary>
        /// Builds a Liminal App.
        /// </summary>
        /// <param name="buildInfo">The build information.</param>
        public static void Build(AppBuildInfo buildInfo)
        {
            if (buildInfo == null)
                throw new ArgumentNullException("buildInfo");

            var assetBundles = AssetDatabase.GetAllAssetBundleNames();
            foreach (var bundle in assetBundles)
            {
                // boolean true forces the asset bundles to be deleted even if they're in use.
                AssetDatabase.RemoveAssetBundleName(bundle, true);
            }

            AssetImporter.GetAtPath(buildInfo.Scene.path).SetAssetBundleNameAndVariant("appscene", "");

            // Get and verify the target platform is supported
            var appPlatform = MapAppTargetPlatform(buildInfo.BuildTarget);
            if (appPlatform == AppPackPlatform.Unknown)
                throw new Exception(string.Format("The supplied buildTarget is currently unsupported: {0}", buildInfo.BuildTarget));

            // Read the app manifest (this will throw if there are errors)
            var appManifest = ReadAppManifest();
            if (appManifest.Id == 0)
            {
                throw new Exception("Application Id is zero");
            }

            UpdateProgressBar("Building Limapp", "Checking Scene", 0.1F);

            // Find the ExperienceApp
            var app = UnityEngine.Object.FindObjectOfType<ExperienceApp>();
            VerifyAppSceneSetup(app);

            Debug.LogFormat("[Liminal.Build] Building app {0}, for platform {1}", appManifest.Id, appPlatform);

            // Clear out existing data fields on the experience
            ClearAppData(app);

            var asmName = "App" + appManifest.Id.ToString().PadLeft(AppManifest.MaxIdLength, '0');
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildInfo.BuildTarget);
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                // Ensure the output directory is ready
                var outputPath = GetOutputPath();
                Directory.CreateDirectory(outputPath);

                // Generate final application assembly
                // This will be packed into an AssetBundle that will be loaded into the master app
                // NOTE: .bytes extension is used so that unity won't try to load the DLL
                var asmPath = GetAppAssemblyPath() + ".bytes";
                var asmBuilder = new AppAssemblyBuilder();
                var asmBuildInfo = new AppAssemblyBuilder.AssemblyBuildInfo()
                {
                    Name = asmName,
                    BuildTarget = buildInfo.BuildTarget,
                    BuildTargetGroup = buildTargetGroup,
                    Version = appManifest.Version
                };
                asmBuilder.Build(asmBuildInfo, asmPath);
                
                // Build asset lookup for the current scene
                Debug.Log("[Liminal.Build] Building asset lookup...");
                var assetLookupBuilder = new AssetLookupBuilder();
                var assetLookup = assetLookupBuilder.Build(buildInfo.Scene);

                // Serialize scene data
                // This will write any data from classes/structs in the app marked with [Serializable], so that we
                // can deserialize them when loaded into the master app (Unity won't do this with loaded assemblies...)
                Debug.Log("[Liminal.Build] Serializing scene...");
                var serializer = new AppSerializer(new AssemblyDataProvider(asmName), assetLookup);
                var jsonPath = Path.Combine(outputPath, AppDataName);
                var jsonData = serializer.Serialize(buildInfo.Scene, jsonPath);

                UpdateProgressBar("Building Limapp", "Serializing App Data", 0.2F);
                // Assign serialized app data and lookup to the ExperienceApp object
                SetAppData(app, jsonData, assetLookup);

                // Build asset bundles
                UpdateProgressBar("Building Limapp", "Building scene AssetBundle", 0.4F);
                Debug.Log("[Liminal.Build] Building scene AssetBundle...");
                BuildPipeline.BuildAssetBundles(outputPath,
                    BuildAssetBundleOptions.UncompressedAssetBundle | BuildAssetBundleOptions.ForceRebuildAssetBundle,
                    buildInfo.BuildTarget);

                // Run post-processor on the asset bundle
                var sceneBundlePath = Path.Combine(outputPath, "appscene");
                var sceneBundleProc = new SceneBundleProcessor(BuildConsts.ProjectAssemblyName, asmName);
                sceneBundleProc.Process(sceneBundlePath);

                // Pack to AppPack (.limapp)
                // This will also compress the app (using LZMA)
                UpdateProgressBar("Building Limapp", "Packing App", 0.7F);
                Debug.Log("[Liminal.Build] Packing app...");

                var platformName = GetAppPlatformOutputName(buildInfo);
                var extension = GetFileExtension(buildInfo.CompressionType);
                var appFilename = string.Format("app_{0}_{1}_v{2}.{3}", appManifest.Id, platformName,
                    appManifest.Version, extension);
                var appPackPath = Path.Combine(outputPath, appFilename);
                var appPack = new AppPack()
                {
                    TargetPlatform = appPlatform,
                    ApplicationId = appManifest.Id,
                    Assemblies = BuildPackAssemblyRawBytesList(asmPath, buildTargetGroup, buildInfo.BuildTarget),
                    SceneBundle = File.ReadAllBytes(Path.Combine(outputPath, "appscene")),
                    CompressionType = buildInfo.CompressionType,
                };

                // Pack the AppPack into a compressed file
                UpdateProgressBar("Building Limapp", "Compressing App", 0.8F);
                
                new AppPacker()
                    .PackAsync(appPack, appPackPath)
                    .Wait();
                    
                UpdateProgressBar("Building Limapp", "Cleaning up", 0.9F);
                Debug.Log("[Liminal.Build] Cleaning up...");

                // Clean up
                // Delete temporary files
                foreach (var file in Directory.GetFiles(outputPath))
                {
                    var ext = Path.GetExtension(file).ToLower();
                    if (ext != ".limapp" && ext != ".ulimapp")
                    {
                        TryDeleteFile(file);
                    }
                }

                AssetDatabase.Refresh();

                var appPath = Path.GetFullPath(appPackPath);
                var appFile = new FileInfo(appPath);

                EditorUtility.ClearProgressBar();
                sw.Stop();
                Debug.LogFormat("[Liminal.Build] Build completed successfully in {0:0.00}s. Size: {1:0.00}mb, Output: {2}", sw.Elapsed.TotalSeconds, BytesToMb(appFile.Length), appPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Liminal.Build] Build failed.");
                Debug.LogException(ex);

                EditorUtility.ClearProgressBar();
            }
            finally
            {
                // Ensure everything is always cleaned up...
                ClearAppData(app);
                AssetLookupBuilder.DestroyExisting(buildInfo.Scene);

                GUIUtility.ExitGUI();
            }
        }

        private static string GetFileExtension(ECompressionType compressionType = ECompressionType.LMZA)
        {
            var extension = string.Empty;

            switch (compressionType)
            {
                case ECompressionType.LMZA:
                    extension = "limapp";
                    break;
                case ECompressionType.Uncompressed:
                    extension = "ulimapp";
                    break;
            }

            return extension;
        }
        #endregion

        private static string GetAppPlatformOutputName(AppBuildInfo buildInfo)
        {
            if (buildInfo.BuildTargetDevice == AppBuildInfo.BuildTargetDevices.None)
            {
                var platformName = "";
                var appPlatform = MapAppTargetPlatform(buildInfo.BuildTarget);

                switch (appPlatform)
                {
                    case AppPackPlatform.WindowsStandalone:
                        platformName = "emulator";
                        break;

                    case AppPackPlatform.Android:
                        platformName = "android";
                        break;
                }

                return platformName.ToLower();
            }
            else
            {
                return buildInfo.BuildTargetDevice.ToString().ToLower();
            }

        }

        private static float BytesToMb(long len)
        {
            return (len / 1024f / 1024f);
        }

        private static void TryDeleteFile(string file)
        {
            try { File.Delete(file); }
            catch { }
        }

        private static void VerifyAppSceneSetup(ExperienceApp app)
        {
            if (app == null)
                throw new Exception("No ExperienceApp found in scene." +
                    "Please ensure your scene is setup correctly. " +
                    " Run Liminal -> Setup App Scene to prepare your scene for build."
                    );
            
            if (app.gameObject.GetComponentInChildren<VRAvatar>() == null)
                throw new Exception(
                    "No active VRAvatar component was found inside the ExperienceApp. " +
                    " A VRAvatar is required for compatibility with the Liminal application. " +
                    " Please ensure your scene is setup correctly. Run Liminal -> Setup App Scene to prepare your scene for build."
                    );
        }
        
        private static IEnumerable<PluginImporter> GetIncludedPlugins(BuildTargetGroup targetGroup, BuildTarget target)
        {
            var list = new List<PluginImporter>();
            var importers = PluginImporter.GetImporters(targetGroup, target);
            foreach (var plugin in importers)
            {
                // Skip anything in the /Liminal folder
                if (plugin.assetPath.IndexOf("Assets/Liminal", StringComparison.OrdinalIgnoreCase) > -1)
                    continue;

                // Skip Unity extensions
                if (plugin.assetPath.IndexOf("Editor/Data/UnityExtensions", StringComparison.OrdinalIgnoreCase) > -1)
                    continue;

                // Skip anything located in the Packages/ folder of the main project
                if (plugin.assetPath.IndexOf("Packages/", StringComparison.OrdinalIgnoreCase) == 0)
                    continue;

                // Skip native plugins, and anything that won't normally be included in a build
                if (plugin.isNativePlugin || !plugin.ShouldIncludeInBuild())
                    continue;
                
                list.Add(plugin);
            }

            return list;
        }

        private static List<byte[]> BuildPackAssemblyRawBytesList(string mainAppAsmPath, BuildTargetGroup targetGroup, BuildTarget target)
        {
            var list = new List<byte[]>();

            if (File.Exists(mainAppAsmPath))
            {
                list.Add(File.ReadAllBytes(mainAppAsmPath));
            }

            foreach (var plugin in GetIncludedPlugins(targetGroup, target))
            {
                list.Add(File.ReadAllBytes(plugin.assetPath));
            }
            
            return list;
        }

        private static AppManifest ReadAppManifest()
        {
            var appManifestAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Liminal/liminalapp.json");
            if (appManifestAsset == null)
            {
                throw new Exception("Application manifest not found. Run 'Liminal > Create or Update App Manifest' and ensure your app details are correct.");
            }
            try
            {
                return JsonConvert.DeserializeObject<AppManifest>(appManifestAsset.text);
            }
            catch (Exception)
            {
                Debug.LogError("Failed to read AppManifest");
                throw;
            }
        }

        private static AppPackPlatform MapAppTargetPlatform(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return AppPackPlatform.Android;

                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return AppPackPlatform.WindowsStandalone;

                default:
                    return AppPackPlatform.Unknown;
            }
        }

        private static string GetAppAssemblyPath()
        {
            return Path.Combine(GetOutputPath(), AppAssemblyFileName).Replace("\\", "/");
        }

        private static string GetOutputPath()
        {
            return "Assets/_Builds";
        }
        
        private static void SetAppData(ExperienceApp app, TextAsset jsonData, AssetLookup assetLookup)
        {
            if (app == null)
                return;

            var so = new SerializedObject(app);
            so.FindProperty("m_AppData").objectReferenceValue = jsonData;
            so.FindProperty("m_AssetLookup").objectReferenceValue = assetLookup;

            var rootObjects = app.gameObject.scene.GetRootGameObjects();
            var pRootObjects = so.FindProperty("m_RootGameObjects");
            for (int i = 0; i < rootObjects.Length; ++i)
            {
                pRootObjects.InsertArrayElementAtIndex(i);
                pRootObjects.GetArrayElementAtIndex(i).objectReferenceValue = rootObjects[i];
            }
            
            so.ApplyModifiedProperties();
        }

        private static void ClearAppData(ExperienceApp app)
        {
            if (app == null)
                return;

            var so = new SerializedObject(app);
            so.FindProperty("m_AppData").objectReferenceValue = null;
            so.FindProperty("m_AssetLookup").objectReferenceValue = null;
            so.FindProperty("m_RootGameObjects").ClearArray();
            so.FindProperty("m_RootGameObjects").arraySize = 0;
            so.ApplyModifiedProperties();
        }
    }
}