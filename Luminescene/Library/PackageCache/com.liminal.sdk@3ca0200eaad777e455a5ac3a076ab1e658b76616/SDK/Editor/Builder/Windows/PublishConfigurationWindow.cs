using Liminal.SDK.Tools;
using UnityEditor;
using UnityEngine;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// A window view for publishing configurations, mainly used for generating AppManifest information
    /// </summary>
    public class PublishConfigurationWindow : BaseWindowDrawer
    {
        private const string ManifestFolderName = "Liminal";
        private const string RootFolderName = "Assets";

        public PublishConfigurationWindow()
        {
            EnsureManifestFolderExists();

            var appManifest = AppTools.GetAppManifest;
            if (appManifest == null)
            {
                AppTools.CreateAppManifest();
            }

            _id = appManifest.Id;
            _version = appManifest.Version;
        }

        public override void Draw(BuildWindowConfig config)
        {
            var appManifest = AppTools.GetAppManifest;
            var appManifestAsset = AppTools.GetAppManifestAsset;

            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Publishing Settings");
                EditorGUILayout.LabelField(
                    "The App Manifest is how we identify your experiences from other experiences");

                _id = EditorGUILayout.IntField("ID", _id);
                _version = EditorGUILayout.IntField("Version", _version);

                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                {
                    appManifestAsset = (TextAsset)EditorGUILayout.ObjectField(appManifestAsset, typeof(TextAsset), true);

                    if (GUILayout.Button("Update Manifest"))
                    {
                        if (EnsureManifestFolderExists())
                            AppTools.CreateAppManifest(_id, _version);

                        EditorGUIUtility.PingObject(appManifestAsset);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
        }

        public static bool EnsureManifestFolderExists()
        {
            var folderExists = AssetDatabase.IsValidFolder($"{RootFolderName}/{ManifestFolderName}");
            if (!folderExists)
                AssetDatabase.CreateFolder(RootFolderName, ManifestFolderName);

            return folderExists;
        }

        private int _id = 999;
        private int _version = 1;
    }
}