using System.Collections.Generic;
using Liminal.SDK.Build;

[System.Serializable]
public class BuildWindowConfig
{
    public string PreviousScene = "";
    public string TargetScene = "";
    public List<string> AdditionalReferences = new List<string>();

    /// <summary>
    /// The default additional references we should provide.
    /// </summary>
    public List<string> DefaultAdditionalReferences =>
        new List<string>()
        {
            "UnityEngine",
            "UnityEngine.CoreModule",
            "UnityEngine.ParticleSystemModule",
            "UnityEngine.TextRenderingModule",
            "UnityEngine.AnimationModule",
            "UnityEngine.AudioModule",
            "UnityEngine.UIModule",
            "UnityEngine.PhysicsModule",
            "UnityEngine.UnityWebRequestWWWModule",
            "UnityEngine.UnityWebRequestModule",
            "UnityEngine.IMGUIModule",
            "UnityEngine.XRModule",
            "UnityEngine.VRModule",
            "UnityEngine.DirectorModule",
            "UnityEngine.WindModule",
        };

    public BuildPlatform SelectedPlatform = BuildPlatform.Current;

    public ECompressionType CompressionType { get; set; }
}
