using Liminal.SDK.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liminal.SDK.Editor.Serialization
{
    /// <summary>
    /// A utility class for building <see cref="AssetLookup"/> objects from a <see cref="Scene"/> input.
    /// </summary>
    public class AssetLookupBuilder
    {
        #region Static
        
        /// <summary>
        /// Destroys any existing <see cref="AssetLookup"/> objects in the scene.
        /// </summary>
        /// <param name="scene">The scene to destroy any lookups from.</param>
        public static void DestroyExisting(Scene scene)
        {
            var lookups = UnityEngine.Object.FindObjectsOfType<AssetLookup>();
            foreach (var lookup in lookups)
            {
                if (lookup.gameObject.scene != scene)
                    continue;

                // Destroy existing lookup for this scene
                UnityEngine.Object.DestroyImmediate(lookup.gameObject);
            }
        }

        #endregion

        private const string _gameObjectName = "_$AssetLookup";
        private HashSet<object> _visitedObjects = new HashSet<object>();
        private HashSet<string> _visitedPrefabs = new HashSet<string>();

        /// <summary>
        /// Builds an <see cref="AssetLookup"/> from the specified scene.
        /// </summary>
        /// <param name="scene">The scene to build the look up from.</param>
        /// <returns>The <see cref="AssetLookup"/> that was created.</returns>
        public AssetLookup Build(Scene scene)
        {
            DestroyExisting(scene);

            // Create a new lookup
            var lookup = new GameObject(_gameObjectName).AddComponent<AssetLookup>();
            lookup.transform.SetSiblingIndex(0);

            // Populate with each root game object
            foreach (var rootObj in scene.GetRootGameObjects())
            {
                if (rootObj == lookup.gameObject)
                    continue;

                AddAssetsFromGameObject(lookup, rootObj);
            }

            // Clean up
            _visitedObjects.Clear();
            _visitedPrefabs.Clear();
            return lookup;
        }
        
        private void AddAssetsFromGameObject(AssetLookup lookup, GameObject gameObject)
        {
            if (_visitedObjects.Contains(gameObject))
                return;
            
            _visitedObjects.Add(gameObject);
            lookup.AddAsset(gameObject);

            var list = gameObject.GetComponents<Component>();
            foreach (var component in list)
            {
                // NOTE: Component can be null if the script is not found
                if (component == null)
                    continue;

                AddAssetsFromObject(lookup, component);
            }

            // Step into child GameObjects
            for (int i = 0; i < gameObject.transform.childCount; ++i)
            {
                var child = gameObject.transform.GetChild(i);
                AddAssetsFromGameObject(lookup, child.gameObject);
            }
        }

        private void AddAssetsFromObject(AssetLookup lookup, object target)
        {
            if (target == null || _visitedObjects.Contains(target))
                return;

            // If this is a Unity object type, ensure it's added to the lookup
            var uObj = target as UnityEngine.Object;
            if (uObj != null)
            {
                // If it's a GameObject, use the GameObject overload
                if (uObj is GameObject)
                {
                    AddAssetsFromGameObject(lookup, (GameObject)uObj);
                    return;
                }

                lookup.AddAsset(uObj);
            }

            _visitedObjects.Add(target);

            const BindingFlags bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = GetAllFields(target.GetType(), bindings)
                .Where(f => SerializationUtils.IsSerializable(f) ||  SerializationUtils.IsUnityEventType(f.FieldType))
                .ToList();

            foreach (var f in fields)
            {
                if (SerializationUtils.IsUnityObjectType(f.FieldType))
                {
                    // This field references a Unity Object type
                    // If the value is not null, add it to the lookup
                    var value = f.GetValue(target) as UnityEngine.Object;
                    if (value == null)
                        continue;

                    AddAssetsFromObject(lookup, value);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(f.FieldType))
                {
                    // Enumerable type
                    // Iterate through each element in the collection and fill the lookup
                    var list = f.GetValue(target) as IEnumerable;
                    if (list == null)
                        continue;

                    foreach (var element in list)
                    {
                        if (element == null)
                            continue;

                        AddAssetsFromObject(lookup, element);
                    }
                }
                else
                {
                    // Search for any child fields that may be asset types
                    AddAssetsFromObject(lookup, f.GetValue(target));
                }
            }
        }
        
        private static IEnumerable<FieldInfo> GetAllFields(Type type, BindingFlags bindingFlags)
        {
            if (type == null)
                return Enumerable.Empty<FieldInfo>();

            return type
                .GetFields(bindingFlags)
                .Concat(GetAllFields(type.BaseType, bindingFlags))
                .GroupBy(f => f.Name)
                .Select(g => g.First());
        }
    }
}
