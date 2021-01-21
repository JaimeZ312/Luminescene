using System.Collections;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

using Liminal.Platform.Experimental.App.BundleLoader;

/// <summary>
/// CanvasLoadingBar is used to display the current progress towards loading an Experience via a Unity Canvas object.
/// When Load is called and a BundleAsyncLoadOperationBase variable passed through, CanvasLoadingBar activates the GameObject it's attached too
/// and starts a coroutine that updates the fill value of a target image based on the progress of the loading operation.  
/// </summary>
public class CanvasLoadingBar 
    : BaseLoadingBar
{
    public Image LoadingBar;

    protected override void OnValidate()
    {
        Assert.IsNotNull(LoadingBar, "LoadingBar must have a value or the loading progress will not be displayed!");
    }

    public override void Load(BundleAsyncLoadOperationBase loadingOperation)
    {
        gameObject.SetActive(true);

        if (_LoadingRoutine != null)
            StopCoroutine(_LoadingRoutine);

        _LoadingRoutine = StartCoroutine(RunLoadingBarCoro(loadingOperation));
    }

    protected override IEnumerator RunLoadingBarCoro(BundleAsyncLoadOperationBase loadingOperation)
    {
        if (!loadingOperation.IsDone)
        {
            while (loadingOperation.Progress < 1f)
            {
                UpdateLoadingBarProgress(loadingOperation.Progress);

                yield return new WaitForEndOfFrame();
            }

            UpdateLoadingBarProgress(1f);
        }
    }

    public override void SetActiveState(bool state)
    {
        gameObject.SetActive(state);
    }

    protected override void UpdateLoadingBarProgress(float normalisedProgress)
    {
        LoadingBar.fillAmount = normalisedProgress;
    }
}
