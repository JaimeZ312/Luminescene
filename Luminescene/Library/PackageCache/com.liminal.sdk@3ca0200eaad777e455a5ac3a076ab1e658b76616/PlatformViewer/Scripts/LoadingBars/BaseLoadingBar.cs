using System.Collections;

using UnityEngine;

using Liminal.Platform.Experimental.App.BundleLoader;

/// <summary>
/// BaseLoadingBar is an abstract class from which other loading bar classes can derive.
/// It does nothing on its own as Canvas and OVR style loading bars have different requirements despite performing identical tasks. The intention is to make it easy to write new kinds of loading bars as needed.
/// </summary>
public abstract class BaseLoadingBar
    : MonoBehaviour
{
    protected Coroutine _LoadingRoutine;

    protected abstract void OnValidate();

    public abstract void Load(BundleAsyncLoadOperationBase loadingOperation);

    protected abstract IEnumerator RunLoadingBarCoro(BundleAsyncLoadOperationBase loadingOperation);

    public abstract void SetActiveState(bool state);

    protected abstract void UpdateLoadingBarProgress(float normalisedProgress);
}
