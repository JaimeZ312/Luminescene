using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// A window for developers to configure and build for Limapps
    /// </summary>
    public class BuildSettingsWindow : EditorWindow
    {
        private const int _width = 550;
        private const int _height = 300;

        public static EditorWindow Window;
        public static Dictionary<BuildSettingMenus, BaseWindowDrawer> BuildSettingLookup = new Dictionary<BuildSettingMenus, BaseWindowDrawer>();

        private BuildSettingMenus _selectedMenu = BuildSettingMenus.Setup;
        private BuildWindowConfig _windowConfig = new BuildWindowConfig();

        public int SelectedMenuIndex { get { return (int)_selectedMenu; } }

        [MenuItem("Liminal/Build Window")]
        public static void OpenBuildWindow()
        {
            Window = GetWindow(typeof(BuildSettingsWindow), false, "Build Settings");
            Window.minSize = new Vector2(_width, _height);
            Window.Show();
        }

        [MenuItem("Liminal/Update Package")]
        public static void RefreshPackage()
        {
            File.WriteAllText(UnityPackageManagerUtils.ManifestPath, UnityPackageManagerUtils.ManifestWithoutLock);
            AssetDatabase.Refresh();
        }

        [MenuItem("Liminal/Use Legacy SDK")]
        public static void UseLegacy()
        {
            File.WriteAllText(UnityPackageManagerUtils.ManifestPath, UnityPackageManagerUtils.ManifestWithoutXR);
            AssetDatabase.Refresh();

            PlayerSettings.virtualRealitySupported = true;
            PlayerSettings.SetVirtualRealitySDKs(BuildTargetGroup.Android, new string[] { "Oculus" });

            var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            currentSymbols = currentSymbols.Replace("UNITY_XR", "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, $"{currentSymbols}");
        }

        [MenuItem("Liminal/Use Unity XR")]
        public static void UseUnityXR()
        {
            PlayerSettings.virtualRealitySupported = false;
            
            File.WriteAllText(UnityPackageManagerUtils.ManifestPath, UnityPackageManagerUtils.ManifestWithXR);
            AssetDatabase.Refresh();
            var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, $"{currentSymbols};UNITY_XR");
        }

        private void OnEnable()
        {
            var fileExists = Directory.Exists(BuildWindowConsts.ConfigFolderPath) || File.Exists(BuildWindowConsts.BuildWindowConfigPath);

            if (fileExists)
            {
                if (File.Exists(BuildWindowConsts.BuildWindowConfigPath))
                {
                    var json = File.ReadAllText(BuildWindowConsts.BuildWindowConfigPath);
                    _windowConfig = JsonUtility.FromJson<BuildWindowConfig>(json);
                    AssetDatabase.Refresh();
                }
            }

            SetupFolderPaths();
            SetupPreviewScene();

            SetupMenuWindows();
            
            var activeWindow = BuildSettingLookup[_selectedMenu];
            activeWindow.OnEnabled();
        }

        private void OnGUI()
        {
            var tabs = Enum.GetNames(typeof(BuildSettingMenus));
            EditorGUI.BeginChangeCheck();
            _selectedMenu = (BuildSettingMenus) GUILayout.Toolbar(SelectedMenuIndex, tabs);
            var activeWindow = BuildSettingLookup[_selectedMenu];
            if(EditorGUI.EndChangeCheck())
                activeWindow.OnEnabled();

            activeWindow.Draw(_windowConfig);

            if (!Directory.Exists(BuildWindowConsts.ConfigFolderPath))
            {
                Directory.CreateDirectory(BuildWindowConsts.ConfigFolderPath);
            }

            // boolean true is used to format the resulting string for maximum readability. False would format it for minimum size.
            var configJson = JsonUtility.ToJson(_windowConfig, true);
            File.WriteAllText(BuildWindowConsts.BuildWindowConfigPath, configJson);


            foreach (var entry in BuildSettingLookup)
            {
                entry.Value.Size = position.size;
            }
        }

        private void SetupMenuWindows()
        {
            BuildSettingLookup.AddSafe(BuildSettingMenus.Build, new BuildWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Issues, new IssueWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Publishing, new PublishConfigurationWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Setup, new SetupWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Preview, new AppPreviewWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Settings, new SettingsWindow());
        }

        private void SetupFolderPaths()
        {
            if (!Directory.Exists(BuildWindowConsts.PlatformSceneFolderPath))
            {
                Directory.CreateDirectory(BuildWindowConsts.PlatformSceneFolderPath);
            }

            if (!Directory.Exists(BuildWindowConsts.BuildPath))
            {
                Directory.CreateDirectory(BuildWindowConsts.BuildPath);
            }

            AssetDatabase.Refresh();
        }

        private void SetupPreviewScene()
        {
            var sceneExists = File.Exists(BuildWindowConsts.PreviewAppScenePath);
            if (!sceneExists)
            {
                var scenePath = $"{UnityPackageManagerUtils.FullPackageLocation}/{BuildWindowConsts.PackagePreviewAppScenePath}";
                File.Copy(scenePath, BuildWindowConsts.PreviewAppScenePath);
                AssetDatabase.Refresh();
            }
        }
    }
}
