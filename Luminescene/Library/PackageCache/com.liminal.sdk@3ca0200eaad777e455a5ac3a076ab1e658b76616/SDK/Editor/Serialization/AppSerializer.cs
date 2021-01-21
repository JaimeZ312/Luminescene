using Liminal.SDK.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liminal.SDK.Editor.Serialization
{
    /// <summary>
    /// Serializes scene data for use in a Liminal AppPack.
    /// </summary>
    public class AppSerializer
    {
        private Scene mScene;
        private HashSet<Type> mSerializableTypes;
        private JsonSerializerSettings mJsonSettings;
        private AssetLookup mAssetLookup;

        /// <summary>
        /// Creates a new <see cref="AppSerializer"/>.
        /// </summary>
        /// <param name="assemblyDataProvider">The assembly data provider.</param>
        /// <param name="assetLookup">The asset lookup to use.</param>
        public AppSerializer(IAssemblyDataProvider assemblyDataProvider, AssetLookup assetLookup)
        {
            mAssetLookup = assetLookup;
            mSerializableTypes = SerializationUtils.BuildSerializableTypeSet("Assembly-CSharp");
            mJsonSettings = new JsonSerializerSettings()
            {
                ContractResolver = new UnityJsonContractResolver(assemblyDataProvider, mAssetLookup)
            };
        }

        /// <summary>
        /// Serializes the specified scene to a JSON file at the specified output path.
        /// </summary>
        /// <param name="scene">The scene to serialize.</param>
        /// <param name="outputPath">The path to write the serialized file to.</param>
        /// <returns>The serialized asset.</returns>
        public TextAsset Serialize(Scene scene, string outputPath)
        {
            mScene = scene;
            
            var outputData = new SerializedData()
            {
                SceneGameObjects = new List<GameObjectData>(),
                Prefabs = new List<PrefabGameObjectData>(),
                ScriptableObjects = new List<ScriptableObjectData>(),
            };

            // Scene GameObjects
            foreach (var go in mScene.GetRootGameObjects())
            {
                SerializeGameObject(go, outputData.SceneGameObjects);
            }

            var serializedPrefabs = new HashSet<UnityEngine.Object>();
            foreach (var pair in mAssetLookup)
            {
                // PRefabs
                var go = pair.Value as GameObject;
                if (go != null)
                {
                    // Is this a prefab?
                    // We only need to serialize data if the prefab root hasn't been serialized yet
                    var prefab = PrefabUtility.GetPrefabInstanceHandle(go);
                    if (prefab != null)
                    {
                        var assetPath = AssetDatabase.GetAssetPath(prefab);
                        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                        if (prefabAsset != null && !serializedPrefabs.Contains(prefabAsset))
                        {
                            SerializePrefabGameObject(pair.Key, prefabAsset, outputData.Prefabs);
                            serializedPrefabs.Add(prefabAsset);
                        }
                    }

                    // -- Done
                    continue;
                }

                // Scriptable Objects
                var so = pair.Value as ScriptableObject;
                if (so != null)
                {
                    SerializeScriptableObject(pair.Key, so, outputData.ScriptableObjects);

                    // -- Done
                    continue; 
                }
            }

            // Serialize and write to JSON file
            var json = JsonConvert.SerializeObject(outputData, Formatting.Indented);
            File.WriteAllText(outputPath, json);

            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<TextAsset>(outputPath);
        }

        private void SerializeGameObject(GameObject gameObject, List<GameObjectData> outputList)
        {
            var goData = new GameObjectData
            {
                NamePath = GetGameObjectPath(gameObject),
                IndexPath = GetGameObjectIndexPath(gameObject),
                Index = gameObject.transform.GetSiblingIndex(),
            };
            outputList.Add(goData);

            var components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; ++i)
            {
                // NOTE: Component can be null if the script cannot be found
                var component = components[i];
                if (component == null)
                    continue;

                var componentType = component.GetType();
                var fields = GetSerializeableFields(componentType);
                if (!fields.Any())
                    continue;

                var cData = new ComponentData
                {
                    Name = componentType.Name,
                    Index = i,
                };
                goData.Components.Add(cData);

                foreach (var f in fields)
                {
                    var value = f.GetValue(component);
                    if (value == null)
                        continue;

                    var jsonValue = JsonConvert.SerializeObject(value, Formatting.None, mJsonSettings);
                    cData.Fields.Add(new FieldData()
                    {
                        Name = f.Name,
                        Json = jsonValue,
                    });
                }
            }

            for (int i = 0; i < gameObject.transform.childCount; ++i)
            {
                var child = gameObject.transform.GetChild(i);
                SerializeGameObject(child.gameObject, goData.Children);
            }
        }

        private void SerializePrefabGameObject(int assetId, GameObject gameObject, List<PrefabGameObjectData> outputList)
        {
            PrefabGameObjectData prefabData = null;
            prefabData = new PrefabGameObjectData
            {
                Name = gameObject.name,
                Id = assetId
            };
            outputList.Add(prefabData);

            var components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; ++i)
            {
                // NOTE: Component can be null if the script cannot be found
                var component = components[i];
                if (component == null)
                    continue;

                var componentType = component.GetType();
                var fields = GetSerializeableFields(componentType);
                if (!fields.Any())
                    continue;

                var cData = new ComponentData
                {
                    Name = componentType.Name,
                    Index = i,
                };
                prefabData.Components.Add(cData);

                foreach (var f in fields)
                {
                    var value = f.GetValue(component);
                    var jsonValue = JsonConvert.SerializeObject(value, Formatting.None, mJsonSettings);
                    cData.Fields.Add(new FieldData()
                    {
                        Name = f.Name,
                        Json = jsonValue,
                    });
                }
            }

            for (int i = 0; i < gameObject.transform.childCount; ++i)
            {
                var child = gameObject.transform.GetChild(i);
                SerializeGameObject(child.gameObject, prefabData.Children);
            }
        }

        private void SerializeScriptableObject(int assetId, ScriptableObject scriptableObject, List<ScriptableObjectData> outputList)
        {
            var type = scriptableObject.GetType();
            var fields = GetSerializeableFields(type);
            if (!fields.Any())
                return;

            var data = new ScriptableObjectData
            {
                Name = scriptableObject.name,
                Id = assetId,
            };
            outputList.Add(data);

            foreach (var f in fields)
            {
                var value = f.GetValue(scriptableObject);
                var jsonValue = JsonConvert.SerializeObject(value, Formatting.None, mJsonSettings);
                data.Fields.Add(new FieldData()
                {
                    Name = f.Name,
                    Json = jsonValue,
                });
            }
        }

        private string GetGameObjectPath(GameObject go)
        {
            var path = "/" + go.name;
            while (go.transform.parent != null)
            {
                go = go.transform.parent.gameObject;
                path = "/" + go.name + path;
            }
            return path;
        }

        private string GetGameObjectIndexPath(GameObject go)
        {
            string path = "/" + go.transform.GetSiblingIndex();
            while (go.transform.parent != null)
            {
                go = go.transform.parent.gameObject;
                path = "/" + go.transform.GetSiblingIndex() + path;
            }
            return path;
        }

        private IEnumerable<FieldInfo> GetSerializeableFields(Type type)
        {
            if (type == null)
                return Enumerable.Empty<FieldInfo>();

            const BindingFlags bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return type
                .GetFields(bindings)
                .Where(f => ShouldSerializeField(f));
        }

        private bool ShouldSerializeField(FieldInfo field)
        {
            var fieldType = field.FieldType;

            // Field is declared NonSerializable
            if (Attribute.IsDefined(field, typeof(NonSerializedAttribute)))
                return false;

            // Non-public field, but does not decalare SerializeField attribute
            if (!field.IsPublic && !Attribute.IsDefined(field, typeof(SerializeField)))
                return false;

            if (fieldType.IsArray)
            {
                // Array type
                // Check element type against serializable type set
                var elementType = fieldType.GetElementType();
                if (mSerializableTypes.Contains(elementType) || SerializationUtils.IsUnityEventType(elementType))
                    return true;
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // List type
                // Check element type against serializable type set
                var elementType = fieldType.GetGenericArguments()[0];
                if (mSerializableTypes.Contains(elementType) || SerializationUtils.IsUnityEventType(elementType))
                    return true;
            }

            return mSerializableTypes.Contains(fieldType) || SerializationUtils.IsUnityEventType(fieldType);
        }
    }
}
