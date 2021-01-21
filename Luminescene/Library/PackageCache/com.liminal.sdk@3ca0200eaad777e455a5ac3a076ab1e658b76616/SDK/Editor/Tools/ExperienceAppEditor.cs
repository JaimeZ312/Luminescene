using Liminal.SDK.Core;
using UnityEditor;
using UnityEngine;

namespace Liminal.SDK.Tools
{
    [CustomEditor(typeof(ExperienceApp), true)]
    public class ExperienceAppEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var app = target as ExperienceApp;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Pause Experience"))
                    app.Pause();

                if (GUILayout.Button("Resume Experience"))
                    app.Resume();

                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            base.OnInspectorGUI();
        }
    }
}