using Liminal.SDK.Editor.Build;
using Liminal.SDK.Editor.Utils;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// Describes a Liminal application for use with the build system.
    /// </summary>
    public class AppManifest
    {
        internal const string Path = "Assets/Liminal/liminalapp.json";
        internal const int MaxIdLength = 12;

        /// <summary>
        /// Gets the unique identifier for the application.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; private set; }
        /// <summary>
        /// Version number for the application. This should increment between releases and is used to ensure
        /// the assembly name is unique to avoid clashes when loading multiple versions in the same Platform session.
        /// </summary>
        [JsonProperty("version")]
        public int Version { get; private set; }

        public AppManifest(int id = 999, int version = 1)
        {
            Id = id;
            Version = version;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Validate();
        }

        private void Validate()
        {
            if (Id <= 0)
            {
                throw new InvalidAppException(string.Format("Id must be positive. Found {0}", Id));
            }
            if (NumberUtils.IntLength(Id) > MaxIdLength)
            {
                throw new InvalidAppException(string.Format("Id value is too long. Id must not exceed {0} digits in length. Found {0}", MaxIdLength, Id));
            }
            if (Version <= 0)
            {
                throw new InvalidAppException(string.Format("Version number must be positive. Found {0}", Version));
            }
        }
    }
}
