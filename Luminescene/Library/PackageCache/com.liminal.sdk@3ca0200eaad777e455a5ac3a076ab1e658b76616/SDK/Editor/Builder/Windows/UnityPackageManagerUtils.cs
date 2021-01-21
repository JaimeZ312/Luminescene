using System;
using System.IO;
using System.Text;
using Liminal.SDK.Extensions;
using UnityEngine;

public static class UnityPackageManagerUtils
{
    public const string sdkName = "Liminal.SDK";
    public const string sdkSeperator = "SDK\\Assemblies";

    /// <summary>
    /// Return the full package location to the package folder
    /// After the SDK is imported into a Third Party project, Application.Data path will return ThirdPartyProjectPath
    /// </summary>
    public static string FullPackageLocation
    {
        get
        {
            var sdkAssembly = AppDomain.CurrentDomain.GetLoadedAssembly(sdkName);
            var sdkLocation = sdkAssembly.Location;
            var liminalLocation = sdkLocation.Split(new string[] { sdkSeperator }, StringSplitOptions.None)[0];
            liminalLocation = DirectoryUtils.ReplaceBackWithForwardSlashes(liminalLocation);

            return liminalLocation;
        }
    }

    /// <summary>
    /// Return the path to the manifest at /Packages/manifest.json
    /// </summary>
    public static string ManifestPath
    {
        get
        {
            var rootProjectFolder = Directory.GetParent(Application.dataPath);
            var manifestPath = $"{rootProjectFolder}/Packages/manifest.json";
            return manifestPath;
        }
    }

    /// <summary>
    /// Return the manifest packages 
    /// </summary>
    public static string ManifestWithoutLock
    {
        get
        {
            var Quote = '"';
            var LockString = $"{Quote}lock";

            var manifestJson = File.ReadAllText(ManifestPath);
            var manifestWithoutLock = manifestJson.Split(new string[] { "}," }, StringSplitOptions.RemoveEmptyEntries)[0];
            manifestWithoutLock += "}\n}";

            return manifestWithoutLock;
        }
    }

    public static string ManifestWithoutXR
    {
        get
        {
            var manifestJson = File.ReadAllText(ManifestPath);
            var toolKit = "\"com.unity.xr.interaction.toolkit\": \"0.9.4-preview\",";
            var interaction = "\"com.unity.xr.interactionsubsystems\": \"1.0.1\",";
            var management = "\"com.unity.xr.management\": \"3.2.7\",";
            var oculus = "\"com.unity.xr.oculus\": \"1.3.3\",";
            manifestJson = manifestJson.Replace(toolKit, "");
            manifestJson = manifestJson.Replace(interaction, "");
            manifestJson = manifestJson.Replace(management, "");
            manifestJson = manifestJson.Replace(oculus, "");

            return manifestJson;
        }
    }
    public static string ManifestWithXR
    {
        get
        {
            var manifestJson = File.ReadAllText(ManifestPath);

            var oldOculus = "\"com.unity.xr.oculus.android\": \"1.36.0\",";
            manifestJson.Replace(oldOculus, "");

            //manifestJson = manifestJson.Replace( "\"com.unity.xr.oculus.android\": \"1.36.0\",", oculus);
            var oculus = "\r\n\"com.unity.xr.oculus\": \"1.3.3\",";
            var toolKit = "\r\n\"com.unity.xr.interaction.toolkit\": \"0.9.4-preview\",";
            var subsystems = "\r\n\"com.unity.xr.interactionsubsystems\": \"1.0.1\",";
            var management = "\r\n\"com.unity.xr.management\": \"3.2.7\",";

            var builder = new StringBuilder();

            if (!manifestJson.Contains("com.unity.xr.oculus\": \"1.3.3\","))
                builder.Append(oculus);
            if (!manifestJson.Contains("com.unity.xr.interaction.toolkit"))
                builder.Append(toolKit);
            if (!manifestJson.Contains("com.unity.xr.interactionsubsystems"))
                builder.Append(subsystems);
            if (!manifestJson.Contains("com.unity.xr.management"))
                builder.Append(management);

            var targetText = "\"dependencies\": {";
            manifestJson = manifestJson.Replace(targetText,
                $"{targetText}{builder}");

            return manifestJson;
        }
    }
}