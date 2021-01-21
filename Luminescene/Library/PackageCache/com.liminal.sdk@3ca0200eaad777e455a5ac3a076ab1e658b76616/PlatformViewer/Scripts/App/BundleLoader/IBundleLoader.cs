using System.Collections;
using Liminal.Platform.Experimental.App.Experiences;
using Liminal.SDK.Core;

namespace Liminal.Platform.Experimental.App.BundleLoader
{
    public interface IBundleLoader
    {
        /// <summary>
        /// Loads an ExperienceApp asynchronously and returns a loading operation.
        /// </summary>
        /// <param name="experience">The <see cref="Experience"/> to load.</param>
        /// <returns>An <see cref="BundleAsyncLoadOperationBase"/>.</returns>
        BundleAsyncLoadOperationBase Load(Experience experience);

        /// <summary>
        /// Unloads an <see cref="ExperienceApp"/>.
        /// </summary>
        /// <param name="app">The <see cref="ExperienceApp"/> to unload.</param>
        /// <returns>A yieldable <see cref="IEnumerator"/>.</returns>
        IEnumerator Unload(ExperienceApp app);
    }
}