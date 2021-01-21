using System.Reflection;
using Liminal.SDK.Core;

namespace Liminal.Platform.Experimental.App.Experiences
{
    public static class ExperienceAppReflectionCache
    {
        private static MethodInfo _initializeMethod;
        private static MethodInfo _shutdownMethod;
        private static FieldInfo _isEndingField;
        private static FieldInfo _assetBundleField;

        public static MethodInfo InitializeMethod
        {
            get
            {
                if (_initializeMethod == null)
                {
                    const BindingFlags binding = BindingFlags.Instance | BindingFlags.NonPublic;
                    _initializeMethod = typeof(ExperienceApp).GetMethod("Initialize", binding);
                }

                return _initializeMethod;
            }
        }

        public static MethodInfo ShutdownMethod
        {
            get
            {
                if (_shutdownMethod == null)
                {
                    const BindingFlags binding = BindingFlags.Instance | BindingFlags.NonPublic;
                    _shutdownMethod = typeof(ExperienceApp).GetMethod("Shutdown", binding);
                }

                return _shutdownMethod;
            }
        }

        public static FieldInfo IsEndingField
        {
            get
            {
                if (_isEndingField == null)
                    _isEndingField = typeof(ExperienceApp).GetField("_isEnding", BindingFlags.Static | BindingFlags.NonPublic);

                return _isEndingField;
            }
        }

        public static FieldInfo AssetBundleField
        {
            get
            {
                if (_assetBundleField == null)
                    _assetBundleField = typeof(ExperienceApp).GetField("_assetBundle", BindingFlags.Static | BindingFlags.NonPublic);

                return _assetBundleField;
            }
        }
    }
}