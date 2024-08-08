using System;
using System.Collections.Generic;
using UnityEngine;

namespace jKnepel.ProteusNet.Logging
{
    public class Logger
    {
        public LoggerSettings Settings { get; }
        
        public List<Log> Logs { get; } = new();

        public event Action<Log> OnLogAdded;

        public Logger(LoggerSettings settings)
        {
            Settings = settings;
        }

        public void Log(string text)
        {
            Log log = new(text, DateTime.Now, EMessageSeverity.Log);
            lock (Logs)
                Logs.Add(log);
            
            OnLogAdded?.Invoke(log);
            
            if (Settings.PrintToConsole && Settings.PrintLog)
                Debug.Log(text);
        }

        public void LogWarning(string text)
        {
            Log log = new(text, DateTime.Now, EMessageSeverity.Warning);
            lock (Logs)
                Logs.Add(log);
            
            OnLogAdded?.Invoke(log);
            
            if (Settings.PrintToConsole && Settings.PrintWarning)
                Debug.LogWarning(text);
        }
        
        public void LogError(string text)
        {
            Log log = new(text, DateTime.Now, EMessageSeverity.Error);
            lock (Logs)
                Logs.Add(log);
            
            OnLogAdded?.Invoke(log);
            
            if (Settings.PrintToConsole && Settings.PrintError)
                Debug.LogError(text);
        }
        
        // TODO : export log and packets to file
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
}
