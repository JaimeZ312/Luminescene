using UnityEngine;

[CreateAssetMenu(menuName = "Liminal/PreviewConfig")]
public class AppPreviewConfig : ScriptableObject
{
    public string EmulatorPath;
    public string AndroidPath;
    public string AndroidAppFullName;
}