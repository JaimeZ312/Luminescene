using Liminal.SDK.VR.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Liminal.SDK.VR.Avatars.Controllers
{
    [CustomEditor(typeof(VRControllerInputVisual), true, isFallback = true)]
    public class VRControllerInputVisualEditor : UnityEditor.Editor
    {
        private static string[] _excludeProps = new[] { "m_InputName" };
        private static GUIContent[] _inputNameLabels;
        private static string[] _inputNameValues;

        private SerializedProperty pInputName;
        private GUIContent pInputNameLabel;
        private int mSelectedIndex;

        private void OnEnable()
        {
            pInputName = serializedObject.FindProperty("m_InputName");
            pInputNameLabel = new GUIContent(pInputName.displayName, pInputName.tooltip);
            
            var constValues = new List<string>();
            AddConstantsToList(typeof(VRButton), constValues);
            AddConstantsToList(typeof(VRAxis),  constValues);

            _inputNameLabels = constValues.Select(x => new GUIContent(x)).ToArray();
            _inputNameValues = constValues.ToArray();

            mSelectedIndex = Array.IndexOf(_inputNameValues, pInputName.stringValue);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            DrawPropertiesExcluding(serializedObject, _excludeProps);

            var newIndex = EditorGUILayout.Popup(pInputNameLabel, mSelectedIndex, _inputNameLabels);
            if (newIndex != mSelectedIndex)
            {
                mSelectedIndex = newIndex;
                pInputName.stringValue = _inputNameValues[mSelectedIndex];
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddConstantsToList(Type type, List<string> values)
        {
            var constValues = type
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.FieldType == typeof(string) && fi.IsLiteral && !fi.IsInitOnly)
                .Select(x => (string)x.GetRawConstantValue())
                .OrderBy(x => x)
                ;
            
            values.AddRange(constValues);
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member