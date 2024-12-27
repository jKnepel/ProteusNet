using UnityEngine;
using UnityEditor;

namespace jKnepel.ProteusNet.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NetworkTransform))]
    public class NetworkTransformEditor : Editor
    {
        private SerializedProperty _networkChannel;
        private SerializedProperty _synchronizeValues;

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
            _synchronizeValues = serializedObject.FindProperty("synchronizeValues");

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
            
            GUILayout.Label("Values", EditorStyles.boldLabel);
            DrawToggleLine("Position", ETransformValues.PositionX, ETransformValues.PositionY, ETransformValues.PositionZ);
            DrawToggleLine("Rotation", ETransformValues.RotationX, ETransformValues.RotationY, ETransformValues.RotationZ);
            DrawToggleLine("Scale", ETransformValues.ScaleX, ETransformValues.ScaleY, ETransformValues.ScaleZ);
            EditorGUILayout.Space();
            
            GUILayout.Label("Snapping", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_snapPosition, new GUIContent("Snap Position"));
            if (_snapPosition.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_snapPositionThreshold, new GUIContent("Threshold"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(_snapRotation, new GUIContent("Snap Rotation"));
            if (_snapRotation.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_snapRotationThreshold, new GUIContent("Threshold"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(_snapScale, new GUIContent("Snap Scale"));
            if (_snapScale.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_snapScaleThreshold, new GUIContent("Threshold"));
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

        private void DrawToggleLine(string label, ETransformValues x, ETransformValues y, ETransformValues z)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(label, GUILayout.Width(70));

            var xToggled = (_synchronizeValues.intValue & (int)x) != 0;
            var yToggled = (_synchronizeValues.intValue & (int)y) != 0;
            var zToggled = (_synchronizeValues.intValue & (int)z) != 0;

            xToggled = EditorGUILayout.ToggleLeft("X", xToggled, GUILayout.Width(40));
            yToggled = EditorGUILayout.ToggleLeft("Y", yToggled, GUILayout.Width(40));
            zToggled = EditorGUILayout.ToggleLeft("Z", zToggled, GUILayout.Width(40));

            _synchronizeValues.intValue = xToggled ? _synchronizeValues.intValue | (int)x : _synchronizeValues.intValue & (int)~x;
            _synchronizeValues.intValue = yToggled ? _synchronizeValues.intValue | (int)y : _synchronizeValues.intValue & (int)~y;
            _synchronizeValues.intValue = zToggled ? _synchronizeValues.intValue | (int)z : _synchronizeValues.intValue & (int)~z;
            
            EditorGUILayout.EndHorizontal();
        }
    }
}