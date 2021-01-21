using System;
using System.IO;
using System.Linq;
using Liminal.Platform.Experimental.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Liminal.SDK.Build
{
    public class AppPreviewWindow : BaseWindowDrawer
    {
        private const string _streamingAsset = "StreamingAssets";

        private AppPreviewConfig _appPreviewConfig = null;
        private PlatformAppViewer _previewApp;

        public override void Draw(BuildWindowConfig config)
        {
            GUILayout.BeginVertical("Box");
            EditorGUIHelper.DrawTitle("App Preview");
            GUILayout.Label("The Preview Scene will load and run your limapp created from the Build Tool");
            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            if (CanDraw)
            {
                GUILayout.Label("Select Limapp");

                _appPreviewConfig = _previewApp.PreviewConfig;
                DrawLimappSelection(ref _appPreviewConfig.EmulatorPath, "Emulator");
                DrawLimappSelection(ref _appPreviewConfig.AndroidPath, "Android");
                CopyAndroidAppToStreamingAssets();

                EditorUtility.SetDirty(_previewApp.PreviewConfig);

                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                {
                    if (EditorApplication.isPlaying)
                    {
                        if (GUILayout.Button(EditorGUIUtility.IconContent("PauseButton"))) _previewApp.Stop();
                        if (GUILayout.Button(EditorGUIUtility.IconContent("PlayButton"))) _previewApp.Play();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(config.PreviousScene))
                        {
                            if (GUILayout.Button("Back"))
                                EditorSceneManager.OpenScene(config.PreviousScene, OpenSceneMode.Single);
                        }

                        if (GUILayout.Button("Play")) EditorApplication.isPlaying = true;
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open Preview Scene"))
                {
                    config.PreviousScene = EditorSceneManager.GetActiveScene().path;
                    EditorSceneManager.OpenScene(BuildWindowConsts.PreviewAppScenePath, OpenSceneMode.Single);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Returns true if PlatformAppViewer can be found
        /// </summary>
        public bool CanDraw
        {
            get
            {
                if (_previewApp == null)
                    _previewApp = Object.FindObjectOfType<PlatformAppViewer>();

                if (_previewApp == null || _previewApp.PreviewConfig == null) return false;
                return true;
            }
        }

        /// <summary>
        /// // Copy external limapp to StreamingAssets
        /// </summary>
        public void CopyAndroidAppToStreamingAssets()
        {
            var streamingAssetFolder = $"{Application.dataPath}/{_streamingAsset}";
            DirectoryUtils.EnsureFolderExists(streamingAssetFolder);

            if (File.Exists(_appPreviewConfig.AndroidPath))
            {
                var androidFile = File.ReadAllBytes(_appPreviewConfig.AndroidPath);
                var androidFileName = Path.GetFileName(_appPreviewConfig.AndroidPath);

                var androidStreamingPath = $"{streamingAssetFolder}/{androidFileName}";
                var copyDoesNotExist = !File.Exists(androidStreamingPath);

                if (copyDoesNotExist)
                {
                    File.Copy(_appPreviewConfig.AndroidPath, androidStreamingPath, overwrite: true);
                    AssetDatabase.Refresh();
                }
                else
                {
                    var streamingFile = File.ReadAllBytes(androidStreamingPath);

                    if (GUI.changed)
                    {
                        if (!streamingFile.SequenceEqual(androidFile))
                        {
                            File.Copy(_appPreviewConfig.AndroidPath, androidStreamingPath, overwrite: true);
                            AssetDatabase.Refresh();
                        }
                    }
                }

                _appPreviewConfig.AndroidAppFullName = androidFileName;
            }
        }

        public void DrawLimappSelection(ref string limappPath, string name)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(name, GUILayout.Width(Size.x * 0.15F));

                limappPath = File.Exists(limappPath) ? limappPath : Application.dataPath;
                limappPath = GUILayout.TextField(limappPath, GUILayout.Width(Size.x * 0.7F));

                if (GUILayout.Button("...", GUILayout.Width(Size.x * 0.1F)))
                {
                    limappPath = EditorUtility.OpenFilePanelWithFilters("Limapp Directory", limappPath, new string[] { "FileType", "limapp,ulimapp" });
                    limappPath = DirectoryUtils.ReplaceBackWithForwardSlashes(limappPath);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
