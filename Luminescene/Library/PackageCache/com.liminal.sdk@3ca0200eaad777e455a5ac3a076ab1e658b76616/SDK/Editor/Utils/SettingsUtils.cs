using UnityEditor;
using UnityEngine;
using System.IO;

public class SettingsUtils : EditorWindow
{
    public static bool HasFile => Resources.Load("LimappConfig") != null;
    public static ExperienceProfile GetProfile() => Resources.Load<ExperienceProfile>("LimappConfig");

    public static void CopyProjectSettingsToProfile()
    {
        var profile = Resources.Load<ExperienceProfile>("LimappConfig");
        if (profile == null)
            return;

        // This actually means, copy.
        profile.SaveProjectSettings();
    }

    public static ExperienceProfile CreateProfile()
    {
        if (File.Exists($"{SDKResourcesConsts.LiminalSettingsConfigPath}"))
            return GetProfile();

        var defaultSettings = CreateInstance<ExperienceProfile>();
        defaultSettings.Init();

        AssetDatabase.CreateAsset(defaultSettings,$"{SDKResourcesConsts.LiminalSettingsConfigPath}");

        return GetProfile();
    }
}
