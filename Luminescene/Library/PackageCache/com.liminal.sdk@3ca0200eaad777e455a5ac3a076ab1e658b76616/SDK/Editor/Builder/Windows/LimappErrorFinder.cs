using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class LimappErrorFinder
{
    private static readonly List<Assembly> _assemblies = new List<Assembly>();
    private static readonly List<LimappIssue> _issues = new List<LimappIssue>();
    private const BindingFlags _methodsBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    public static void Draw()
    {
        if (_assemblies.Any())
        {
            DrawLoadedAssemblies();
            DrawIssues();
        }
    }

    public static void LocateIssues()
    {
        GetAssemblies();
        GetParameterIssues();
    }

    private static void DrawLoadedAssemblies()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent($"{_assemblies.Count} assemblies found", EditorGUIUtility.FindTexture( "d_UnityEditor.InspectorWindow" )), "IconButton"))
        {
            string msg = string.Empty;
            for (var i = 0; i < _assemblies.Count; i++)
            {
                var assembly = _assemblies[i];
                msg += $"{i+1}: {assembly.GetName()}\n\n";
            }

            EditorUtility.DisplayDialog($"{_assemblies.Count} assemblies found", msg, "Close");
        }

        GUILayout.EndHorizontal();
    }

    public struct LimappIssue
    {
        public string Issue;
        public string Location;
    }

    private static void DrawIssues()
    {
        GUILayout.Label("The following default parameters must be removed before building a Limapp", EditorStyles.boldLabel);
        if (!_issues.Any())
        {
            GUILayout.Label("<color=#59db42><b>No issues detected</b></color>", new GUIStyle(EditorStyles.label) { richText = true });
            return;
        }

        GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f);
        foreach (var issue in _issues)
        {
            if (GUILayout.Button(new GUIContent(issue.Issue, EditorGUIUtility.FindTexture("d_console.erroricon")),
                new GUIStyle(EditorStyles.helpBox) {richText = true}))
            {
                Application.OpenURL(issue.Location);
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private static void GetParameterIssues()
    {
        _issues.Clear();
        foreach (var assembly in _assemblies)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(_methodsBindingFlags);
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    var str = $"<color=#FC0><b>{assembly.GetName().Name}</b></color> type={type.Name}, method={method.Name}\n" +
                              $"<color=#ff7631>{method.Name}</color>(";

                    if (!parameters.Any(x => x.HasDefaultValue && x.DefaultValue != null && x.ParameterType.Assembly.FullName.Contains("UnityEngine.CoreModule")))
                        continue;

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        if (parameter.HasDefaultValue)
                        {
                            if (parameter.DefaultValue != null && parameter.ParameterType.Assembly.FullName.Contains("UnityEngine.CoreModule"))
                                str += $"<color=#f02424><b>{parameter.Name} = {parameter.ParameterType.Assembly.GetName().Name}.{parameter.DefaultValue ?? "null"}</b></color>";
                            else
                                str += $"{parameter.Name} = {parameter.DefaultValue ?? "null"}";
                        }
                        else
                            str += $"{parameter.Name}";

                        if (i < parameters.Length - 1)
                            str += ", ";
                    }

                    str += ")";
                    
                    var guid = AssetDatabase.FindAssets($"t:Script {type.Name}").FirstOrDefault();
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    _issues.Add(new LimappIssue
                    {
                        Issue = str,
                        Location = $"{Application.dataPath}/../{path}"
                    });
                }
            }
        }
    }

    private static void GetAssemblies()
    {
        var list = new List<PluginImporter>();
        var importers = PluginImporter.GetImporters(BuildTargetGroup.Android, BuildTarget.Android);
        foreach (var plugin in importers)
        {
            // Skip Unity extensions
            if (plugin.assetPath.IndexOf("Editor/Data/UnityExtensions", StringComparison.OrdinalIgnoreCase) > -1)
                continue;

            // Skip native plugins, and anything that won't normally be included in a build
            if (plugin.isNativePlugin || !plugin.ShouldIncludeInBuild())
                continue;

            list.Add(plugin);
        }

        _assemblies.Clear();
        var assemblycs = GetAssemblyByName("Assembly-CSharp");
        _assemblies.Add(assemblycs);

        foreach (var plugin in list)
        {
            var assembly = Assembly.Load(AssemblyName.GetAssemblyName(plugin.assetPath));
            if (!assembly.GetName().FullName.Contains("Liminal.SDK"))
                _assemblies.Add(assembly);
        }
    }

    static Assembly GetAssemblyByName(string assemblyName)
        => AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);
}
