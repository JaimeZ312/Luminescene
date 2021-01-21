using System.Collections;
using Liminal.Platform.Experimental.App.Experiences;
using Liminal.SDK.Core;
using Liminal.SDK.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liminal.Platform.Experimental.App.BundleLoader.Impl
{
    public class BundleLoader : IBundleLoader
    {
        public BundleAsyncLoadOperationBase Load(Experience experience)
        {
            return new BundleAsyncLoadOperation(experience);
        }

        public IEnumerator Unload(ExperienceApp app)
        {
            SerializationUtils.ClearGlobalSerializableTypes();

            if (app != null)
            {
                var scene = app.gameObject.scene;
                if (scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }

                yield return Resources.UnloadUnusedAssets();
            }
        }
    }
}