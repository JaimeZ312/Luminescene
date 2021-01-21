using Liminal.SDK.Editor.Build;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// The window to export and build the limapp
    /// </summary>
    public class BuildWindow : BaseWindowDrawer
    {
        private string _referenceInput;

        public override void Draw(BuildWindowConfig config)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Build Limapp");
                EditorGUILayout.LabelField("This process will build a limapp file that will run on the Liminal Platform");
                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
                GUILayout.Space(10);

                DrawSceneSelector(ref _scenePath, "Target Scene", config);

                config.TargetScene = _scenePath;
                EditorGUILayout.Space();

                _selectedPlatform = config.SelectedPlatform;
                _selectedPlatform = (BuildPlatform)EditorGUILayout.EnumPopup("Select Platform", _selectedPlatform);
                config.SelectedPlatform = _selectedPlatform;

                _compressionType = config.CompressionType;
                _compressionType = (ECompressionType)EditorGUILayout.EnumPopup("Compression Format", _compressionType);
                config.CompressionType = _compressionType;

                if (_compressionType == ECompressionType.Uncompressed)
                {
                    EditorGUILayout.LabelField("Uncompressed limapps are not valid for release.", EditorStyles.boldLabel);
                }

                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                EditorGUILayout.LabelField("Additional References");
                EditorGUI.indentLevel++;

                var toRemove = new List<string>();
                foreach (var reference in config.AdditionalReferences)
                {
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(reference);
                        if (GUILayout.Button("X"))
                        {
                            toRemove.Add(reference);
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                foreach (var reference in toRemove)
                {
                    config.AdditionalReferences.Remove(reference);
                }

                GUILayout.BeginHorizontal();
                {
                    _referenceInput = EditorGUILayout.TextField("Reference: ", _referenceInput);
                    if (GUILayout.Button("+"))
                    {
                        if (string.IsNullOrEmpty(_referenceInput))
                            return;

                        if (config.DefaultAdditionalReferences.Contains(_referenceInput))
                        {
                            Debug.Log($"The default references already included {_referenceInput}");
                            return;
                        }

                        var refAsm = Assembly.Load(_referenceInput);
                        if (refAsm == null)
                        {
                            Debug.LogError($"Assembly: {_referenceInput} does not exist.");
                            return;
                        }

                        if (!config.AdditionalReferences.Contains(_referenceInput))
                            config.AdditionalReferences.Add(_referenceInput);

                        _referenceInput = "";
                    }
                }
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel--;

                GUILayout.FlexibleSpace();
                var enabled = !_scenePath.Equals(string.Empty);
                if(!enabled)
                    GUILayout.Label("Scene cannot be empty", "CN StatusWarn");

                GUI.enabled = !EditorApplication.isCompiling;

                if (GUILayout.Button("Build"))
                {
                    //run checks here.

                    IssuesUtility.CheckForAllIssues();

                    var hasBuildIssues = EditorPrefs.GetBool("HasBuildIssues");

                    if (hasBuildIssues)
                    {
                        if(EditorUtility.DisplayDialog("Build Issues Detected", "Outstanding issues have been detected in your project. " +
                        "Navigate to Build Settings->Issues for help resolving them", "Build Anyway", "Cancel Build"))
                        {
                            Build();
                        }
                    }
                    else
                        Build();
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void Build()
        {
            SettingsUtils.CopyProjectSettingsToProfile();
            EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Single);

            switch (_selectedPlatform)
            {
                case BuildPlatform.Current:
                    AppBuilder.BuildCurrentPlatform();
                    break;

                case BuildPlatform.GearVR:
                    AppBuilder.BuildLimapp(BuildTarget.Android, AppBuildInfo.BuildTargetDevices.GearVR,
                        _compressionType);
                    break;

                case BuildPlatform.Standalone:
                    AppBuilder.BuildLimapp(BuildTarget.StandaloneWindows, AppBuildInfo.BuildTargetDevices.Emulator,
                        _compressionType);
                    break;
            }
        }

        public void DrawSceneSelector(ref string scenePath, string name, BuildWindowConfig config)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(name, GUILayout.Width(Size.x * 0.2F));

                if (AssetDatabase.LoadAssetAtPath(config.TargetScene, typeof(SceneAsset)) != null)
                {
                    _targetScene = (SceneAsset) AssetDatabase.LoadAssetAtPath(config.TargetScene, typeof(SceneAsset));
                }

                _targetScene = (SceneAsset)EditorGUILayout.ObjectField(_targetScene, typeof(SceneAsset), true, GUILayout.Width(Size.x * 0.75F));

                if (_targetScene != null)
                {
                    scenePath = AssetDatabase.GetAssetPath(_targetScene);
                }
                else
                {
                    _targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorSceneManager.GetActiveScene().path);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private BuildPlatform _selectedPlatform;
        private ECompressionType _compressionType;
        private SceneAsset _targetScene;
        private string _scenePath = string.Empty;
    }

    public enum BuildPlatform
    {
        Current,
        Standalone,
        GearVR
    }
}
