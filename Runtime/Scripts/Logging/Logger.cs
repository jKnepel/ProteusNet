using System;
using System.Collections.Generic;
using UnityEngine;

namespace jKnepel.ProteusNet.Logging
{
    public class Logger
    {
        public LoggerSettings Settings => _settings;
        private LoggerSettings _settings;

        public List<Message> Messages => _messages;
        private readonly List<Message> _messages = new();

        public event Action<Message> OnMessageAdded;

        public Logger(LoggerSettings settings)
        {
            _settings = settings;
        }

        public void Log(string text, EMessageSeverity sev = EMessageSeverity.Error)
        {
            Message msg = new(text, DateTime.Now, sev);
            lock (_messages)
                _messages.Add(msg);
            
            OnMessageAdded?.Invoke(msg);
            
            if (!_settings.PrintToConsole) return;
            
            switch (sev)
            {
                case EMessageSeverity.Log:
                    if (Settings.PrintLog)
                        Debug.Log(text);
                    break;
                case EMessageSeverity.Warning:
                    if (Settings.PrintWarning)
                        Debug.LogWarning(text);
                    break;
                case EMessageSeverity.Error:
                    if (Settings.PrintError)
                        Debug.LogError(text);
                    break;
            }
        }
    }

    public struct Message
    {
        public string Text;
        public DateTime Time;
        public EMessageSeverity Severity;

        public Message(string text, DateTime time, EMessageSeverity severity)
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

    public enum EMessageSeverity
    {
        Log = 0,
        Warning = 1,
        Error = 2
    }
}
