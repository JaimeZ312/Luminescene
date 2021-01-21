using System;
using UnityEditor;
using UnityEngine;

public static class EditorGUIHelper
{
    public static void DrawTitle(string label)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(30));
        EditorGUILayout.LabelField(label, EditorStyles.whiteLargeLabel);
        EditorGUILayout.EndVertical();
    }

    public static void DrawTitleFoldout(string label, ref bool boolToSet, Action drawContent, Action onFoldout = null)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(30));
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        boolToSet = EditorGUILayout.Foldout(boolToSet, label, toggleOnLabelClick: true);
        if (EditorGUI.EndChangeCheck() && boolToSet)
            onFoldout?.Invoke();

        EditorGUILayout.EndHorizontal();
        
        if (boolToSet)
            drawContent?.Invoke();

        EditorGUILayout.EndVertical();
    }

    public static void DrawSpritedLabel(string label, Texture2D texture, params GUILayoutOption[] spriteOptions)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(texture, spriteOptions);
        EditorGUILayout.LabelField(label);
        EditorGUILayout.EndHorizontal();
    }
}