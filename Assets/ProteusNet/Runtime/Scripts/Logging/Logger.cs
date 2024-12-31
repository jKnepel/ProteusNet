using jKnepel.ProteusNet.Networking.Transporting;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace jKnepel.ProteusNet.Logging
{
    public class Logger
    {
        public LoggerSettings Settings { get; }
        
        public List<Log> Logs { get; } = new();

        public NetworkMetrics TotalMetrics { get; private set; } = new();
        public List<NetworkMetrics> MetricsList { get; } = new();

        public event Action<Log> OnLogAdded;
        public event Action<NetworkMetrics> OnMetricsAdded;

        public Logger(LoggerSettings settings)
        {
            Settings = settings;
        }

        public void Reset()
        {
            Logs.Clear();
            TotalMetrics = new();
            MetricsList.Clear();
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

        public void LogNetworkMetrics(NetworkMetrics metrics)
        {
            if (metrics == null) return;
            
            lock (MetricsList)
                MetricsList.Add(metrics);
            lock (TotalMetrics)
                TotalMetrics.AddNetworkMetrics(metrics);

            OnMetricsAdded?.Invoke(metrics);
        }
        
        /*
        // TODO : export log and packets to file
        public void ExportClientTrafficStats(string filepath, string filename, bool overwrite)
        {
            var file = Path.Combine(filepath, filename);
            var fileIndex = 1;
            while (!overwrite && File.Exists(file))
            {
                file = GenerateFileNameWithIndex(filepath, filename, fileIndex);
                fileIndex++;
            }
            
            using var outputFile = new StreamWriter(file, false);
            outputFile.WriteLine("Client Traffic Statistics");
            outputFile.WriteLine("Tick,Incoming Bytes,Outgoing Bytes");
            foreach (var stat in ClientTrafficStats)
                outputFile.WriteLine($"{stat.Tick},{stat.IncomingBytes},{stat.OutgoingBytes}");
        }
        
        public void ExportServerTrafficStats(string filepath, string filename, bool overwrite)
        {
            var file = Path.Combine(filepath, filename);
            var fileIndex = 1;
            while (!overwrite && File.Exists(file))
            {
                file = GenerateFileNameWithIndex(filepath, filename, fileIndex);
                fileIndex++;
            }
            
            using var outputFile = new StreamWriter(file, false);
            outputFile.WriteLine("Server Traffic Statistics");
            outputFile.WriteLine("Tick,Incoming Bytes,Outgoing Bytes");
            foreach (var stat in ServerTrafficStats)
                outputFile.WriteLine($"{stat.Tick},{stat.IncomingBytes},{stat.OutgoingBytes}");
        }
        
        private static string GenerateFileNameWithIndex(string directory, string baseFileName, int index)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);
            var extension = Path.GetExtension(baseFileName);
            var newFileName = $"{fileNameWithoutExtension}_{index}{extension}";
            return Path.Combine(directory, newFileName);
        }
        */
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
