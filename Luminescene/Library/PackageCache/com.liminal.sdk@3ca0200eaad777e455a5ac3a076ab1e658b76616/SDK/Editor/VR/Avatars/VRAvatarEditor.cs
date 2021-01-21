using Liminal.SDK.Extensions;
using UnityEditor;
using UnityEngine;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Liminal.SDK.VR.Avatars
{
    [CustomEditor(typeof(VRAvatar), isFallback = true)]
    public class VRAvatarEditor : UnityEditor.Editor
    {
        private static string[] _excludeProps = new[] { "m_Script" };

        #region Editor

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            DrawPropertiesExcluding(serializedObject, _excludeProps);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Controllers", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Controllers", EditorStyles.miniButtonLeft))
                {
                    AddControllers();
                }
                else if (GUILayout.Button("Remove Controllers", EditorStyles.miniButtonRight))
                {
                    RemoveControllers();
                }
            }

            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        private void AddControllers()
        {
            Undo.RegisterFullObjectHierarchyUndo(target, "VRAvatar Add Controllers");

            var avatar = (VRAvatar)target;
            AddController(avatar.PrimaryHand);
            AddController(avatar.SecondaryHand);
        }
        
        private void RemoveControllers()
        {
            Undo.RegisterFullObjectHierarchyUndo(target, "VRAvatar Remove Controllers");

            var avatar = (VRAvatar)target;
            var keepChildren = true;

            if (ControllerHasChildren(avatar.PrimaryHand) || ControllerHasChildren(avatar.SecondaryHand))
            {
                // One ore both of the existing controllers have nested child objects
                // Ask the user what they would like to do with them...

                var choice = EditorUtility.DisplayDialogComplex(
                    "Remove VRController?",
                    "One or more VRAvatarControllers have child GameObjects. Are you sure you want to remove them?",
                    "Yes",
                    "No",
                    "Keep Children");

                switch (choice)
                {
                    // Don't remove them
                    case 1:
                        return;

                    // Remove
                    case 0:
                        keepChildren = false;
                        break;

                    // Remove, but keep children
                    case 2:
                        keepChildren = true;
                        break;

                    default:
                        return;

                }
            }
            
            RemoveController(avatar.PrimaryHand, keepChildren);
            RemoveController(avatar.SecondaryHand, keepChildren);
        }

        private void AddController(IVRAvatarHand hand)
        {
            if (hand == null || hand.Transform == null)
                return;

            var controller = hand.Transform.GetComponentInChildren<VRAvatarController>();
            if (controller == null)
            {
                controller = new GameObject("Controller").AddComponent<VRAvatarController>();
                controller.transform.parent = hand.Anchor;
                controller.transform.Identity();
            }

            // Ensure the controller is nested under the anchor
            controller.transform.parent = hand.Anchor;
            controller.transform.Identity();
        }

        private bool ControllerHasChildren(IVRAvatarHand hand)
        {
            if (hand == null || hand.Transform == null)
                return false;

            var controller = hand.Transform.GetComponentInChildren<VRAvatarController>();
            if (controller == null)
                return false;

            return (controller.transform.childCount > 0);
        }

        private void RemoveController(IVRAvatarHand hand, bool keepChildren)
        {
            if (hand == null || hand.Transform == null)
                return;

            var controller = hand.Transform.GetComponentInChildren<VRAvatarController>();
            if (controller == null)
                return;

            if (keepChildren)
            {
                for (int i = 0; i < controller.transform.childCount; ++i)
                {
                    var child = controller.transform.GetChild(i);
                    child.transform.parent = controller.transform.parent;
                }
            }

            DestroyImmediate(controller.gameObject, false);
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
