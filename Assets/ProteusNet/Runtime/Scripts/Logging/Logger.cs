using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Logging
{
    [Serializable]
    public class Logger
    {
        /// <summary>
        /// Whether logged messages by the framework should also be printed to the console.
        /// </summary>
        [field: SerializeField] public bool PrintToConsole { get; set; } = true;

        /// <summary>
        /// Whether log level messages should be printed to the console.
        /// </summary>
        [field: SerializeField] public bool PrintLog { get; set; } = true;
        /// <summary>
        /// Whether warning level messages should be printed to the console.
        /// </summary>
        [field: SerializeField] public bool PrintWarning { get; set; } = true;
        /// <summary>
        /// Whether error level messages should be printed to the console.
        /// </summary>
        [field: SerializeField] public bool PrintError { get; set; } = true;
        
        public List<Log> Logs { get; } = new();

        public event Action<Log> OnLogAdded;

        public void Reset()
        {
            Logs.Clear();
        }

        public void Log(string text)
        {
            Log log = new(text, DateTime.Now, EMessageSeverity.Log);
            lock (Logs)
                Logs.Add(log);
            
            OnLogAdded?.Invoke(log);
            
            if (PrintToConsole && PrintLog)
                Debug.Log(text);
        }

        public void LogWarning(string text)
        {
            Log log = new(text, DateTime.Now, EMessageSeverity.Warning);
            lock (Logs)
                Logs.Add(log);
            
            OnLogAdded?.Invoke(log);
            
            if (PrintToConsole && PrintWarning)
                Debug.LogWarning(text);
        }
        
        public void LogError(string text)
        {
            Log log = new(text, DateTime.Now, EMessageSeverity.Error);
            lock (Logs)
                Logs.Add(log);
            
            OnLogAdded?.Invoke(log);
            
            if (PrintToConsole && PrintError)
                Debug.LogError(text);
        }
    }

    public enum EMessageSeverity
    {
        Log = 0,
        Warning = 1,
        Error = 2
    }

    public readonly struct Log
    {
        public readonly string Text;
        public readonly DateTime Time;
        public readonly EMessageSeverity Severity;

        public Log(string text, DateTime time, EMessageSeverity severity)
        {
            Text = text;
            Severity = severity;
            Time = time;
        }

        public string GetFormattedString()
        {
            var formattedTime = Time.ToString("H:mm:ss");
            return $"[{formattedTime}] {Text}";
        }
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Logger), true)]
    public class LoggerSettingsDrawer : PropertyDrawer
    {
        private static readonly GUIContent PrintToConsoleDesc = new("Print To Console", "Whether logged messages by the framework should also be printed to the console.");
        private static readonly GUIContent PrintLogDesc = new("Print Log", "Whether log level messages should be printed to the console.");
        private static readonly GUIContent PrintWarningDesc = new("Print Warning", "Whether warning level messages should be printed to the console.");
        private static readonly GUIContent PrintErrorDesc = new("Print Error", "Whether error level messages should be printed to the console.");
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var printToConsole = property.FindPropertyRelative("<PrintToConsole>k__BackingField");
            EditorGUILayout.PropertyField(printToConsole, PrintToConsoleDesc);
            if (printToConsole.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(property.FindPropertyRelative("<PrintLog>k__BackingField"), PrintLogDesc);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("<PrintWarning>k__BackingField"), PrintWarningDesc);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("<PrintError>k__BackingField"), PrintErrorDesc);
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }
    }
#endif
}
