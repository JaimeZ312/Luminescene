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
    [CustomEditor(typeof(VRControllerNode))]
    public class VRControllerNodeEditor : UnityEditor.Editor
    {
        private static string[] _excludeProps = new[] { "m_NodeName" };
        private static GUIContent[] _nodeNameLabels;
        private static string[] _nodeNameValues;

        private SerializedProperty pNodeName;
        private GUIContent pNodeNameLabel;
        private int mSelectedIndex;

        private void OnEnable()
        {
            pNodeName = serializedObject.FindProperty("m_NodeName");
            pNodeNameLabel = new GUIContent(pNodeName.displayName, pNodeName.tooltip);
            
            var constValues = new List<string>();
            AddConstantsToList(typeof(VRControllerNode), constValues);
            AddConstantsToList(typeof(VRButton), constValues);
            AddConstantsToList(typeof(VRAxis),  constValues);

            _nodeNameLabels = constValues.Select(x => new GUIContent(x)).ToArray();
            _nodeNameValues = constValues.ToArray();

            mSelectedIndex = Array.IndexOf(_nodeNameValues, pNodeName.stringValue);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            DrawPropertiesExcluding(serializedObject, _excludeProps);

            var newIndex = EditorGUILayout.Popup(pNodeNameLabel, mSelectedIndex, _nodeNameLabels);
            if (newIndex != mSelectedIndex)
            {
                mSelectedIndex = newIndex;
                pNodeName.stringValue = _nodeNameValues[mSelectedIndex];
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