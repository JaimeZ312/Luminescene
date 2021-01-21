using System;
using System.IO;
using System.Linq;
using Liminal.SDK.Core;
using Liminal.SDK.Tools;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// A helper place for setting up the scene
    /// </summary>
    public class SetupWindow : BaseWindowDrawer
    {
        private static bool _generated;

        public override void OnEnabled()
        {
            base.OnEnabled();
            CheckSetup();
        }

        [DidReloadScripts]
        private static void CheckSetup()
        {
            var baseType = typeof(ExperienceApp);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assembly = assemblies.FirstOrDefault(x => x.FullName.Contains("Assembly-CSharp,"));

            if (assembly != null)
            {
                var types = assembly.GetTypes();
                _generated = types.Any(t => t != baseType && baseType.IsAssignableFrom(t));
                EditorPrefs.SetInt("GeneratedScripts", _generated ? 1 : 0);
            }
        }

        public override void Draw(BuildWindowConfig config)
        {
            GUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Scene Setup");
                GUILayout.Label(
                    "In order to build for the Liminal Platform, you need to set up the app scene" +
                    "\nCurrently, we only support 1 Scene", EditorStyles.boldLabel);
                GUILayout.Space(4);
                GUILayout.Label("Setting Up", EditorStyles.boldLabel);
                var guiStyle = new GUIStyle(EditorStyles.label){richText = true, wordWrap = true, clipping = TextClipping.Overflow};
                GUILayout.Label("<b><size=17>1.</size></b> Click <b>[Generate Scripts]</b> which will provide you with methods to override basic implementation such as Pause, Resume and how the app ends.", guiStyle);
                GUILayout.Space(2);
                GUILayout.Label("<b><size=17>2.</size></b> Open the scene you want to create your experience in.", guiStyle);
                GUILayout.Space(2);
                GUILayout.Label("<b><size=17>3.</size></b> Click <b>[Setup Scene]</b> which will setup the scene to work with the Limapp system. When it comes time to build, only things under the [ExperienceApp] object will be included.", guiStyle);

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(_generated))
                {
                    if (GUILayout.Button("Generate Scripts"))
                    {
                        AppTools.GenerateScripts();
                    }
                }

                using (new EditorGUI.DisabledScope(!_generated))
                {
                    if (GUILayout.Button("Setup Scene"))
                    {
                        AppTools.SetupAppScene();
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }
    }
}