using UnityEngine;
using UnityEditor;

namespace jKnepel.ProteusNet.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NetworkTransform))]
    public class NetworkTransformEditor : Editor
    {
        private SerializedProperty _networkChannel;
        private SerializedProperty _synchronizePosition;
        private SerializedProperty _synchronizeRotation;
        private SerializedProperty _synchronizeScale;

        private SerializedProperty _snapPosition;
        private SerializedProperty _snapPositionThreshold;
        private SerializedProperty _snapRotation;
        private SerializedProperty _snapRotationThreshold;
        private SerializedProperty _snapScale;
        private SerializedProperty _snapScaleThreshold;
        
        private SerializedProperty _useInterpolation;
        private SerializedProperty _interpolationInterval;
        
        private SerializedProperty _useExtrapolation;
        private SerializedProperty _extrapolationInterval;

        public void OnEnable() 
        {
            _networkChannel = serializedObject.FindProperty("networkChannel");
            _synchronizePosition = serializedObject.FindProperty("synchronizePosition");
            _synchronizeRotation = serializedObject.FindProperty("synchronizeRotation");
            _synchronizeScale = serializedObject.FindProperty("synchronizeScale");

            _snapPosition = serializedObject.FindProperty("snapPosition");
            _snapPositionThreshold = serializedObject.FindProperty("positionSnapThreshold");
            _snapRotation = serializedObject.FindProperty("snapRotation");
            _snapRotationThreshold = serializedObject.FindProperty("rotationSnapThreshold");
            _snapScale = serializedObject.FindProperty("snapScale");
            _snapScaleThreshold = serializedObject.FindProperty("scaleSnapThreshold");
            
            _useInterpolation = serializedObject.FindProperty("useInterpolation");
            _interpolationInterval = serializedObject.FindProperty("interpolationInterval");
            _useExtrapolation = serializedObject.FindProperty("useExtrapolation");
            _extrapolationInterval = serializedObject.FindProperty("extrapolationInterval");
        }

        public override void OnInspectorGUI()
        {
            var t = (NetworkTransform)target;
            
            EditorGUILayout.PropertyField(_networkChannel, new GUIContent("Network Channel"));
            t.Type = (ETransformType)EditorGUILayout.EnumPopup(new GUIContent("Component Type"), t.Type);
            EditorGUILayout.Space();
            
            GUILayout.Label("Synchronization", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_synchronizePosition, new GUIContent("Synchronize Position"));
            if (_synchronizePosition.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_snapPosition, new GUIContent("Snap Position"));
                if (_snapPosition.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_snapPositionThreshold, new GUIContent("Threshold"));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(_synchronizeRotation, new GUIContent("Synchronize Rotation"));
            if (_synchronizeRotation.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_snapRotation, new GUIContent("Snap Rotation"));
                if (_snapRotation.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_snapRotationThreshold, new GUIContent("Threshold"));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(_synchronizeScale, new GUIContent("Synchronize Scale"));
            if (_synchronizeScale.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_snapScale, new GUIContent("Snap Scale"));
                if (_snapScale.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_snapScaleThreshold, new GUIContent("Threshold"));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Smoothing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useInterpolation, new GUIContent("Use Interpolation"));
            if (_useInterpolation.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_interpolationInterval, new GUIContent("Interval"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(_useExtrapolation, new GUIContent("Use Extrapolation"));
            if (_useExtrapolation.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_extrapolationInterval, new GUIContent("Interval"));
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}