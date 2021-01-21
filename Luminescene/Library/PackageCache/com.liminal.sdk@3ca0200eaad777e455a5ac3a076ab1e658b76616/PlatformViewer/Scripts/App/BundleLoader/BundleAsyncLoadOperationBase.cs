using System;
using System.Collections;
using Liminal.Platform.Experimental.App.Experiences;
using Liminal.SDK.Core;
using UnityEngine;

namespace Liminal.Platform.Experimental.App.BundleLoader
{
    public abstract class BundleAsyncLoadOperationBase
    {
        public delegate void BundleAsyncLoadOperationEventHandler(BundleAsyncLoadOperationBase operationBase);

        private Exception mException;

        #region Properties

        /// <summary>
        /// Indicates if the download operation has completed.
        /// </summary>
        public bool IsDownloaded { get; protected set; }

        /// <summary>
        /// Indicates the current progress of the download.
        /// </summary>
        public float CurrentProgress { get; protected set; }

        /// <summary>
        /// Indicates the load operation has completed and that the scene
        /// </summary>
        public bool IsLoaded { get; protected set; }

        /// <summary>
        /// Indicates if the load operation has completed.
        /// </summary>
        public bool IsDone { get; protected set; }

        /// <summary>
        /// Indicates if the load operation has been cancelled.
        /// </summary>
        public bool IsCancelled { get; protected set; }

        /// <summary>
        /// Indicates if the load operation has faulted.
        /// </summary>
        public bool IsFaulted { get; protected set; }

        /// <summary>
        /// If the operation has faulted, returns the <see cref="Exception"/> that was thrown.
        /// </summary>
        public Exception Exception { get { return mException; } }

        public abstract bool DownloadState { get; }
        public abstract float DownloadProgress { get; }

        /// <summary>
        /// Gets the progress of the load operation.
        /// </summary>
        public abstract float Progress { get; protected set; }

        /// <summary>
        /// Gets the <see cref="Data.Models.Experience"/> data model.
        /// </summary>
        public Experience Experience { get; private set; }

        /// <summary>
        /// Contains the <see cref="SDK.Core.ExperienceApp"/> for the loaded app, once the load operation has completed.
        /// </summary>
        public ExperienceApp ExperienceApp { get; protected set; }

        #endregion

        #region Events

        public abstract event BundleAsyncLoadOperationEventHandler Completed;

        #endregion

        /// <summary>
        /// Start loading the scene.
        /// The scene will not be activated and the operation will not complete
        /// until after ActivateScene() is called.
        /// </summary>
        public BundleAsyncLoadOperationBase(Experience experience)
        {
            Experience = experience;
        }

        /// <summary>
        /// Initialise the scene
        /// </summary>
        public abstract IEnumerator LoadScene();

        public void Cancel()
        {
            if (!IsCancelled)
            {
                IsCancelled = true;
                OnCancelled();
            }
        }

        protected abstract void OnCancelled();

        protected void FaultWithException(Exception exception)
        {
            Debug.LogException(exception);
            IsFaulted = true;
            mException = exception;
        }
    }
}
