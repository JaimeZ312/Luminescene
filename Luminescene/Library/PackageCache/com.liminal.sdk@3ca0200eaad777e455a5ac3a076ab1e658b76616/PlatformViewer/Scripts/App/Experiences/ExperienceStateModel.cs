using System;

namespace Liminal.Platform.Experimental.App.Experiences
{
    /// <summary>
    /// A state model for an ExperienceApp
    /// </summary>
    public class ExperienceStateModel
    {
        public float StartTime { get; private set; }
        public AppState State { get; private set; }

        public ExperienceStateModel()
        {
            StartTime = 0;
        }

        public void SetState(AppState State)
        {
            this.State = State;
        }

        public void SetStartTime(float realtimeSinceStartup)
        {
            StartTime = realtimeSinceStartup;
        }
    }

    public enum AppState
    {
        NotLoaded,
        Loading,
        Running,
        Ending,
    }
}