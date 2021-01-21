using System.IO;
using Liminal.SDK.Core;
using UnityEditor;
using UnityEngine;

namespace Liminal.SDK.Build
{
    public class LiminalConfigPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, new GUIContent("App Settings"), true);
        }
    }

    [CustomPropertyDrawer(typeof(LiminalConfig))]
    public class ProjectSettingsPropertyDrawer : PropertyDrawer
    {
        private Rect _current;
        private Rect _first;

        private float _singleLine = EditorGUIUtility.singleLineHeight;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var overrideProp = property.FindPropertyRelative("OverrideProfile");
            var size = overrideProp.boolValue ? 3 : 2;
            if (property.isExpanded)
                return _singleLine * size;

            return _singleLine;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            position.height = height;
            _first = position;
            _current = position;
            var row1 = position;

            EditorGUI.PropertyField(row1, property, new GUIContent("Project Settings"), includeChildren: false);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var overrideProp = property.FindPropertyRelative("OverrideProfile");
                var profileProp = property.FindPropertyRelative("ProfileToApply");

                EditorGUI.PropertyField(NextRow(width: position.width * 0.9f), overrideProp);

                var useProfile = overrideProp.boolValue;

                if (GUI.changed && !useProfile)
                    profileProp.objectReferenceValue = null;

                if (useProfile)
                {

                    if (GUI.changed)
                    {
                        profileProp.objectReferenceValue = SettingsUtils.CreateProfile();
                        SettingsUtils.CopyProjectSettingsToProfile();
                    }

                    EditorGUI.PropertyField(NextRow(width: position.width * 0.9f), profileProp, includeChildren: false);

                    if (profileProp.objectReferenceValue == null && SettingsUtils.HasFile)
                    {
                        if (GUI.Button(NextColumn(expand: true), new GUIContent("Find")))
                        {
                            var profile = Resources.Load<ExperienceProfile>("LimappConfig");
                            profileProp.objectReferenceValue = profile;
                        }
                    }
                    else if (profileProp.objectReferenceValue == null)
                    {
                        if (GUI.Button(NextColumn(expand: true), new GUIContent("Add")))
                        {
                            SettingsUtils.CreateProfile();
                            var profile =
                                AssetDatabase.LoadAssetAtPath<ExperienceProfile>(
                                    $"{SDKResourcesConsts.LiminalSettingsConfigPath}");
                            profileProp.objectReferenceValue = profile;
                        }
                    }
                    else
                    {
                        if (GUI.Button(NextColumn(expand: true), new GUIContent("X")))
                            profileProp.objectReferenceValue = null;
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        public Rect NextRow(float? height = null, float? width = null)
        {
            if (height == null)
                height = _first.height;

            if (width == null)
                width = _first.width;

            var next = _current;
            next.width = width.Value;
            next.height = height.Value;
            next.y += _current.height;
            
            next.x = _first.x;

            _current = next;
            return next;
        }

        public Rect NextColumn(float? height = null, float? width = null, bool expand = false)
        {
            if (height == null)
                height = _first.height;

            if (width == null)
                width = _first.width;

            if(expand)
                width = _first.width - _current.width;

            var next = _current;
            next.width = width.Value;
            next.height = height.Value;
            next.x += _current.width;

            _current = next;
            return next;
        }
    }

    public class SettingsWindow : BaseWindowDrawer
    {
        public override void Draw(BuildWindowConfig config)
        {
            EditorGUIHelper.DrawTitle("Experience Settings");
            EditorGUILayout.LabelField("This page is used to set the various settings of the experience");
            EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);
            
            var experienceApp = GameObject.FindObjectOfType<ExperienceApp>();
            if (experienceApp == null)
            {
                EditorGUILayout.LabelField("Please open the scene you wish to edit the settings of.");
                return;
            }

            var app = UnityEditor.Editor.CreateEditor(experienceApp);
            app.DrawDefaultInspector();
        }
    }
}
