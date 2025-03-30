using jKnepel.ProteusNet.Utilities;
using UnityEngine;
using UnityEditor;

using ETransformType = jKnepel.ProteusNet.Components.NetworkTransform.ETransformType;
using ETransformValues = jKnepel.ProteusNet.Components.NetworkTransform.ETransformValues;

namespace jKnepel.ProteusNet.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NetworkTransform))]
    public class NetworkTransformEditor : Editor
    {
        private SerializedProperty _networkChannel;
        private SerializedProperty _synchronizeValues;

        private SerializedProperty _positionUseWorld;
        private SerializedProperty _positionTolerance;
        private SerializedProperty _positionSmoothingMul;
        private SerializedProperty _positionSnap;
        private SerializedProperty _positionSnapThreshold;
        
        private SerializedProperty _rotationUseWorld;
        private SerializedProperty _rotationTolerance;
        private SerializedProperty _rotationSmoothingMul;
        private SerializedProperty _rotationSnap;
        private SerializedProperty _rotationSnapThreshold;
        
        private SerializedProperty _scaleUseWorld;
        private SerializedProperty _scaleTolerance;
        private SerializedProperty _scaleSmoothingMul;
        private SerializedProperty _scaleSnap;
        private SerializedProperty _scaleSnapThreshold;
        
        private SerializedProperty _useInterpolation;
        private SerializedProperty _interpolationInterval;
        private SerializedProperty _useExtrapolation;
        private SerializedProperty _extrapolationInterval;

        private SavedBool _showPosFoldout;
        private SavedBool _showRotFoldout;
        private SavedBool _showScaFoldout;

        private static readonly GUIContent UseWorldDesc = new("Use World", "Uses the world coordinate values instead of the local ones for all calculations and sent updates. World coordinates will introduce additional computational overhead.");
        private static readonly GUIContent ToleranceDesc = new("Tolerance", "The change between ticks necessary to prompt a network update. If no change above the tolerance was performed, no packet will be send this tick. Set to 0 to always send an update.");
        private static readonly GUIContent SmoothingMulDesc = new("Smoothing Multiplier", "The multiplier applied to the smoothing between transform snapshots. Set to 0 to disable smoothing.");
        private static readonly GUIContent SnappingEnabledDesc = new("Snapping Enabled", "If smoothing should be disabled for deltas greater than a defined threshold, causing immediate updates.");
        private static readonly GUIContent SnappingThresholdDesc = new("Snapping Threshold", "The threshold where smoothing will be disabled once deltas are greater than or equal to the defined value.");

        public void OnEnable()
        {
            _networkChannel = serializedObject.FindProperty("networkChannel");
            _synchronizeValues = serializedObject.FindProperty("synchronizeValues");

            _positionUseWorld = serializedObject.FindProperty("positionUseWorld");
            _positionTolerance = serializedObject.FindProperty("positionTolerance");
            _positionSmoothingMul = serializedObject.FindProperty("positionSmoothingMul");
            _positionSnap = serializedObject.FindProperty("positionSnap");
            _positionSnapThreshold = serializedObject.FindProperty("positionSnapThreshold");
            
            _rotationUseWorld = serializedObject.FindProperty("rotationUseWorld");
            _rotationTolerance = serializedObject.FindProperty("rotationTolerance");
            _rotationSmoothingMul = serializedObject.FindProperty("rotationSmoothingMul");
            _rotationSnap = serializedObject.FindProperty("rotationSnap");
            _rotationSnapThreshold = serializedObject.FindProperty("rotationSnapThreshold");
            
            _scaleUseWorld = serializedObject.FindProperty("scaleUseWorld");
            _scaleTolerance = serializedObject.FindProperty("scaleTolerance");
            _scaleSmoothingMul = serializedObject.FindProperty("scaleSmoothingMul");
            _scaleSnap = serializedObject.FindProperty("scaleSnap");
            _scaleSnapThreshold = serializedObject.FindProperty("scaleSnapThreshold");
            
            _useInterpolation = serializedObject.FindProperty("useInterpolation");
            _interpolationInterval = serializedObject.FindProperty("interpolationInterval");
            _useExtrapolation = serializedObject.FindProperty("useExtrapolation");
            _extrapolationInterval = serializedObject.FindProperty("extrapolationInterval");
            
            _showPosFoldout = new($"{target.GetType()}.ShowPosFoldout", false);
            _showRotFoldout = new($"{target.GetType()}.ShowRotFoldout", false);
            _showScaFoldout = new($"{target.GetType()}.ShowScaFoldout", false);
        }

        public override void OnInspectorGUI()
        {
            var t = (NetworkTransform)target;
            
            EditorGUILayout.PropertyField(_networkChannel, new GUIContent("Network Channel"));
            t.Type = (ETransformType)EditorGUILayout.EnumPopup(new GUIContent("Component Type"), t.Type);
            EditorGUILayout.Space();
            
            GUILayout.Label("Values", EditorStyles.boldLabel);
            DrawToggleLine(ref _showPosFoldout, "Position", ETransformValues.PositionX, ETransformValues.PositionY, ETransformValues.PositionZ);
            if (_showPosFoldout)
            {
                EditorGUILayout.Space();
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_positionUseWorld, UseWorldDesc);
                EditorGUILayout.PropertyField(_positionTolerance, ToleranceDesc);
                EditorGUILayout.PropertyField(_positionSmoothingMul, SmoothingMulDesc);
                EditorGUILayout.PropertyField(_positionSnap, SnappingEnabledDesc);
                if (_positionSnap.boolValue)
                    EditorGUILayout.PropertyField(_positionSnapThreshold, SnappingThresholdDesc);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            
            DrawToggleLine(ref _showRotFoldout, "Rotation", ETransformValues.RotationX, ETransformValues.RotationY, ETransformValues.RotationZ);
            if (_showRotFoldout)
            {
                EditorGUILayout.Space();
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_rotationUseWorld, UseWorldDesc);
                EditorGUILayout.PropertyField(_rotationTolerance, ToleranceDesc);
                EditorGUILayout.PropertyField(_rotationSmoothingMul, SmoothingMulDesc);
                EditorGUILayout.PropertyField(_rotationSnap, SnappingEnabledDesc);
                if (_rotationSnap.boolValue)
                    EditorGUILayout.PropertyField(_rotationSnapThreshold, SnappingThresholdDesc);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            
            DrawToggleLine(ref _showScaFoldout, "Scale", ETransformValues.ScaleX, ETransformValues.ScaleY, ETransformValues.ScaleZ);
            if (_showScaFoldout)
            {
                EditorGUILayout.Space();
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_scaleUseWorld, UseWorldDesc);
                EditorGUILayout.PropertyField(_scaleTolerance, ToleranceDesc);
                EditorGUILayout.PropertyField(_scaleSmoothingMul, SmoothingMulDesc);
                EditorGUILayout.PropertyField(_scaleSnap, SnappingEnabledDesc);
                if (_scaleSnap.boolValue)
                    EditorGUILayout.PropertyField(_scaleSnapThreshold, SnappingThresholdDesc);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
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

        private void DrawToggleLine(ref SavedBool foldout, string label, ETransformValues x, ETransformValues y, ETransformValues z)
        {
            EditorGUILayout.BeginHorizontal();

            foldout.Value = EditorGUILayout.Foldout(foldout.Value, label, true);

            var xToggled = (_synchronizeValues.intValue & (int)x) != 0;
            var yToggled = (_synchronizeValues.intValue & (int)y) != 0;
            var zToggled = (_synchronizeValues.intValue & (int)z) != 0;

            xToggled = EditorGUILayout.ToggleLeft("X", xToggled, GUILayout.MaxWidth(40));
            yToggled = EditorGUILayout.ToggleLeft("Y", yToggled, GUILayout.MaxWidth(40));
            zToggled = EditorGUILayout.ToggleLeft("Z", zToggled, GUILayout.MaxWidth(40));

            _synchronizeValues.intValue = xToggled ? _synchronizeValues.intValue | (int)x : _synchronizeValues.intValue & (int)~x;
            _synchronizeValues.intValue = yToggled ? _synchronizeValues.intValue | (int)y : _synchronizeValues.intValue & (int)~y;
            _synchronizeValues.intValue = zToggled ? _synchronizeValues.intValue | (int)z : _synchronizeValues.intValue & (int)~z;
            
            EditorGUILayout.Space();
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
