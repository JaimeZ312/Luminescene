using System;

namespace Liminal.Platform.Experimental.Exceptions
{
    public class SceneNotFoundException : Exception
    {
        public string SceneName { get; private set; }

        public SceneNotFoundException(string name) : base(string.Format("Scene {0} does not exist", name))
        {
            SceneName = name;
        }
    }
}