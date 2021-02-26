using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshCombiner))]
public class MeshEditor : Editor
{
    void OnSceneGUI()
    {
        MeshCombiner mc = target as MeshCombiner;

        if (Handles.Button (mc.transform.position+(Vector3.up * 5), Quaternion.LookRotation(Vector3.up), 1, 1, Handles.CylinderHandleCap))
        {
            mc.CombineMeshes();
        }
    }
}
