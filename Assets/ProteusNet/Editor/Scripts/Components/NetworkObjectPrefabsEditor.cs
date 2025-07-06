using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace jKnepel.ProteusNet.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NetworkObjectPrefabs), true)]
    public class NetworkObjectPrefabsEditor : Editor
    {
        private SerializedProperty _networkObjectPrefabsProp;
        private ReorderableList _reorderableList;
        private ReorderableList _pathReorderableList;
        private SerializedProperty _searchPathsProp;

        private void OnEnable()
        {
            _networkObjectPrefabsProp = serializedObject.FindProperty("networkObjectPrefabs");
            _searchPathsProp = serializedObject.FindProperty("searchPaths");

            _reorderableList = new(serializedObject, _networkObjectPrefabsProp, true, true, true, true)
            {
                drawElementCallback = DrawElementCallback,
                drawHeaderCallback = DrawHeaderCallback,
                onRemoveCallback = OnRemoveCallback,
                onAddCallback = OnAddCallback,
                elementHeight = EditorGUIUtility.singleLineHeight + 2
            };

            _pathReorderableList = new(serializedObject, _searchPathsProp, true, true, true, true)
            {
                drawElementCallback = DrawPathElementCallback,
                drawHeaderCallback = DrawPathHeaderCallback,
                onRemoveCallback = OnRemovePathCallback,
                onAddCallback = OnAddPathCallback,
                elementHeight = EditorGUIUtility.singleLineHeight + 2
            };
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            _reorderableList.DoLayoutList();
            
            GUILayout.Space(30);
            
            _pathReorderableList.DoLayoutList();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find Prefabs"))
                    FindNetworkObjectPrefabs();

                if (GUILayout.Button("Sort Prefabs"))
                    SortNetworkObjectPrefabs();
            }

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            float rowHeight = EditorGUIUtility.singleLineHeight + 4;
            rect.height = rowHeight;

            SerializedProperty element = _networkObjectPrefabsProp.GetArrayElementAtIndex(index);
            NetworkObject associatedGameObject = element.objectReferenceValue as NetworkObject;

            string elementName = associatedGameObject != null ? associatedGameObject.name : "Empty";
            Rect nameRect = new Rect(rect.x, rect.y, rect.width - 120, rowHeight);
            EditorGUI.LabelField(nameRect, elementName);

            Rect objectRect = new Rect(rect.x + 130, rect.y, rect.width - 130, rowHeight);
            EditorGUI.PropertyField(objectRect, element, GUIContent.none);
        }

        private void DrawPathElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            float rowHeight = EditorGUIUtility.singleLineHeight + 4;
            rect.height = rowHeight;

            SerializedProperty pathElement = _searchPathsProp.GetArrayElementAtIndex(index);

            Rect pathRect = new Rect(rect.x, rect.y, rect.width - 120, rowHeight);
            EditorGUI.PropertyField(pathRect, pathElement, GUIContent.none);
        }

        private void DrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Network Object Prefabs");
        }

        private void DrawPathHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Search Paths");
        }

        private void OnRemoveCallback(ReorderableList list)
        {
            int indexToRemove = list.index;
            if (indexToRemove >= 0 && indexToRemove < _networkObjectPrefabsProp.arraySize)
            {
                _networkObjectPrefabsProp.DeleteArrayElementAtIndex(indexToRemove);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void OnAddCallback(ReorderableList list)
        {
            _networkObjectPrefabsProp.InsertArrayElementAtIndex(_networkObjectPrefabsProp.arraySize);
            serializedObject.ApplyModifiedProperties();
        }

        private void OnRemovePathCallback(ReorderableList list)
        {
            int indexToRemove = list.index;
            if (indexToRemove >= 0 && indexToRemove < _searchPathsProp.arraySize)
            {
                _searchPathsProp.DeleteArrayElementAtIndex(indexToRemove);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void OnAddPathCallback(ReorderableList list)
        {
            _searchPathsProp.InsertArrayElementAtIndex(_searchPathsProp.arraySize);
            serializedObject.ApplyModifiedProperties();
        }

        private void FindNetworkObjectPrefabs()
        {
            var foundPrefabs = new List<NetworkObject>();
            
            var paths = new string[_searchPathsProp.arraySize];
            for (int i = 0; i < _searchPathsProp.arraySize; i++)
            {
                SerializedProperty pathElement = _searchPathsProp.GetArrayElementAtIndex(i);
                paths[i] = pathElement.stringValue;
            }

            var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", paths);
            foreach (var prefabGUID in prefabGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null && prefab.TryGetComponent<NetworkObject>(out var networkObject))
                {
                    foundPrefabs.Add(networkObject);
                }
            }

            // Add each found prefab to the serialized list if not already present
            foreach (var networkObject in foundPrefabs)
            {
                bool isAlreadyInList = false;

                for (int i = 0; i < _networkObjectPrefabsProp.arraySize; i++)
                {
                    var element = _networkObjectPrefabsProp.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue == networkObject)
                    {
                        isAlreadyInList = true;
                        break;
                    }
                }

                // Add missing NetworkObjects to the list
                if (!isAlreadyInList)
                {
                    _networkObjectPrefabsProp.InsertArrayElementAtIndex(_networkObjectPrefabsProp.arraySize);
                    var newElement = _networkObjectPrefabsProp.GetArrayElementAtIndex(_networkObjectPrefabsProp.arraySize - 1);
                    newElement.objectReferenceValue = networkObject;
                }
            }

            // Fill any empty indices with null values
            for (int i = _networkObjectPrefabsProp.arraySize - 1; i >= 0; i--)
            {
                var element = _networkObjectPrefabsProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                    _networkObjectPrefabsProp.DeleteArrayElementAtIndex(i);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SortNetworkObjectPrefabs()
        {
            // Create a list of NetworkObjects and their respective names
            var networkObjects = new List<NetworkObject>();
            for (int i = 0; i < _networkObjectPrefabsProp.arraySize; i++)
            {
                var element = _networkObjectPrefabsProp.GetArrayElementAtIndex(i);
                NetworkObject associatedGameObject = element.objectReferenceValue as NetworkObject;
                networkObjects.Add(associatedGameObject);
            }

            // Sort the list by NetworkObject names
            networkObjects.Sort((x, y) => string.Compare(x?.name ?? string.Empty, y?.name ?? string.Empty, StringComparison.Ordinal));

            // Reorder the list based on the sorted order
            _networkObjectPrefabsProp.ClearArray();
            foreach (var networkObject in networkObjects)
            {
                _networkObjectPrefabsProp.InsertArrayElementAtIndex(_networkObjectPrefabsProp.arraySize);
                var newElement = _networkObjectPrefabsProp.GetArrayElementAtIndex(_networkObjectPrefabsProp.arraySize - 1);
                newElement.objectReferenceValue = networkObject;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
