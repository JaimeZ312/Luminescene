namespace Liminal.SDK.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// A helper to OVR Config as the SDK do not want to modify OVR Code.
    /// Presently, it only ensure that the OVRConfig file exists inside a Resources folder
    /// </summary>
    [InitializeOnLoad]
    public static class OVRConfigHelper
    {
        public static string ResourcePath => Path.Combine(Application.dataPath, BuildWindowConsts.ResourcesFolder);

        /// <summary>
        /// In Oculus Integration v1.38, there is an issue where OVRConfig.cs creates an .asset with filename  OVRConfigBuild,
        /// however Resources.Load was used to look for OVRConfig. So if this is patched and fixed in a later update,
        /// we may need to patch the ConfigName as well
        /// </summary>
        public const string ConfigName = "OVRConfig";

        static OVRConfigHelper()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            DirectoryUtils.EnsureFolderExists(ResourcePath, refreshAfterCreation: true);
            EnsureConfigExists();
        }

        private static void EnsureConfigExists()
        {
            var instance = Resources.Load<OVRConfig>(ConfigName);

            if (instance == null)
                MakeConfig();
        }

        private static OVRConfig MakeConfig()
        {
            var instance = ScriptableObject.CreateInstance<OVRConfig>();

            var relativeFolderPath = Path.Combine("Assets", BuildWindowConsts.ResourcesFolder);
            var relativeFilePath = Path.Combine(relativeFolderPath, $"{ConfigName}.asset");
            AssetDatabase.CreateAsset(instance, relativeFilePath);

            return instance;
        }
    }
}
