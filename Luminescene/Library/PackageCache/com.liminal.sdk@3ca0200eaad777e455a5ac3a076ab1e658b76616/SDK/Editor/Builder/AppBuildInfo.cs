using UnityEditor;
using UnityEngine.SceneManagement;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// Represents a build profile
    /// </summary>
    public class AppBuildInfo
    {
        /// <summary>
        /// The scene to build into the app package.
        /// </summary>
        public Scene Scene { get; set; }

        /// <summary>
        /// The build target for the app package.
        /// </summary>
        public BuildTarget BuildTarget { get; set; }

        /// <summary>
        /// The build target device for app package
        /// </summary>
        public BuildTargetDevices BuildTargetDevice { get; set; }

        /// <summary>
        /// The compression type for the build
        /// </summary>
        public ECompressionType CompressionType { get; set; }

        /// <summary>
        /// The Targeted device for the app package
        /// </summary>
        public enum BuildTargetDevices
        {
            None, 
            Emulator,
            GearVR,
            DayDream
        }

    }
}

