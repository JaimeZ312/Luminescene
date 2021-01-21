using Liminal.SDK.Build;
using Liminal.SDK.Core;
using Liminal.SDK.Editor.Build;
using Liminal.SDK.Extensions;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using Assembly = UnityEditor.Compilation.Assembly;

namespace Liminal.SDK.Tools
{
    /// <summary>
    /// A set of tools for setting up and working with Liminal Experience App projects.
    /// </summary>
    public static class AppTools
    {
        /// <summary>
        /// Creates or updates the Experience App manifest file. This file is required for the app to build correctly.
        /// </summary>
        public static void CreateAppManifest(int id = 999, int version = 1)
        {
            var manifest = new AppManifest(id, version);

            var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            File.WriteAllText(AppManifest.Path, json);
            AssetDatabase.Refresh();
        }

        public static AppManifest GetAppManifest
        {
            get
            {
                var asset = GetAppManifestAsset;
                var manifest = (asset != null)
                    ? JsonConvert.DeserializeObject<AppManifest>(asset.text)
                    : new AppManifest();

                return manifest;
            }
        }

        public static TextAsset GetAppManifestAsset
        {
            get
            {
                return AssetDatabase.LoadAssetAtPath<TextAsset>(AppManifest.Path);
            }
        }

        /// <summary>
        /// Sets up the current scene to be a compatible Liminal Experience App.
        /// </summary>
        public static void SetupAppScene()
        {
            var scene = SceneManager.GetActiveScene();
            var accept = EditorUtility.DisplayDialog("Setup Experience App Scene",
                $"Setup the current scene ({scene.name}) as your Experience App scene? This will modify the structure of the scene.",
                "OK", "Cancel");

            if (!accept)
                return;

            EditorSceneManager.EnsureUntitledSceneHasBeenSaved("You must save the current scene before continuing.");
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            // Clear the 'appscene' asset bundle from ALL other assets
            // Only the experience app scene can have this bundle name!
            foreach (var path in AssetDatabase.GetAssetPathsFromAssetBundle("appscene"))
            {
                AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(null, null);
            }

            foreach (var obj in scene.GetRootGameObjects())
            {
                Undo.RegisterFullObjectHierarchyUndo(obj, $"Undo setup {obj}");
            }
            // Make sure this is the only open scene
            scene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            
            var baseType = typeof(ExperienceApp);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assembly = assemblies.First(x => x.FullName.Contains("Assembly-CSharp,"));

            var types = assembly.GetTypes();
            var appType = types.First(t => t != baseType && baseType.IsAssignableFrom(t));

            var appObj = UnityEngine.Object.FindObjectOfType(appType) as GameObject;

            if (appObj == null)
            {
                appObj = new GameObject("[ExperienceApp]");
                appObj.AddComponent(appType);
            }
            else
            {
                // Move to the root
                appObj.gameObject.name = "[ExperienceApp]";
                appObj.transform.SetParentAndIdentity(null);
            }

            var app = appObj.GetComponent<ExperienceApp>();

            //attach the experience settings profile to the config if it exists 
            app.LimappConfig.ProfileToApply = Resources.Load<ExperienceProfile>("LimappConfig");

            // Ensure the VREmulator is available on the app
            app.gameObject.GetOrAddComponent<VREmulator>();

            // Move all other objects under the ExperienceApp object
            foreach (var obj in scene.GetRootGameObjects())
            {
                if (obj == app.gameObject)
                    continue;

                obj.transform.parent = app.transform;
            }

            // Ensure there is a VRAvatar
            var vrAvatar = UnityEngine.Object.FindObjectOfType<VRAvatar>();
            if (vrAvatar == null)
            {
                var prefabAsset = Resources.Load<VRAvatar>("VRAvatar");
                vrAvatar = (VRAvatar)PrefabUtility.InstantiatePrefab(prefabAsset);
            }

            // Move avatar to the app
            vrAvatar.transform.SetParentAndIdentity(app.transform);
            vrAvatar.transform.SetAsFirstSibling();

            // Disable the cameras tagged with MainCamera if it isn't part of the VRAvatar
            foreach (var camera in UnityEngine.Object.FindObjectsOfType<Camera>())
            {
                if (!camera.transform.IsDescendentOf(vrAvatar.transform))
                {
                    if (camera.CompareTag("MainCamera"))
                    {
                        camera.gameObject.SetActive(false);
                        Debug.LogWarning("MainCamera was disabled. Use VRAvatar/Head/CenterEye as your main camera", camera);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Setup complete! Please ensure that you use the cameras nested under VRAvatar/Head as your VR cameras.");
        }

        public static void GenerateScripts()
        {
            var targetPath = $"{Application.dataPath}/Liminal/MyExperienceApp.cs";
            var templatePath = $"{UnityPackageManagerUtils.FullPackageLocation}SDK/Templates/MyExperienceApp.cs.txt";

            if (!File.Exists(targetPath))
            {
                File.Copy(templatePath, targetPath);
                AssetDatabase.Refresh();
            }
        }
    }
}
