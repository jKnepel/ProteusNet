using System;
using System.Collections.Generic;
using UnityEngine;

namespace jKnepel.ProteusNet.Logging
{
    public class Logger
    {
        public LoggerSettings Settings { get; }
        
        public List<Log> Logs { get; } = new();
        public List<PacketLog> ClientSentPackets { get; } = new();
        public List<PacketLog> ServerSentPackets { get; } = new();
        public List<PacketLog> ClientReceivedPackets { get; } = new();
        public List<PacketLog> ServerReceivedPackets { get; } = new();

        public event Action<Log> OnLogAdded;
        public event Action<PacketLog> OnSentPacketAdded;
        public event Action<PacketLog> OnReceivedPacketAdded;

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
        
        public void LogClientSentPacket(uint tick, DateTime time, ulong byteLength)
        {
            PacketLog packet = new(tick, time, byteLength);
            
            lock (ClientSentPackets)
                ClientSentPackets.Add(packet);

            OnSentPacketAdded?.Invoke(packet);
        }
        
        public void LogServerSentPacket(uint tick, DateTime time, ulong byteLength)
        {
            PacketLog packet = new(tick, time, byteLength);
            
            lock (ServerSentPackets)
                ServerSentPackets.Add(packet);

            OnSentPacketAdded?.Invoke(packet);
        }

        public void LogClientReceivedPacket(uint tick, DateTime time, ulong byteLength)
        {
            PacketLog packet = new(tick, time, byteLength);
            
            lock (ClientReceivedPackets)
                ClientReceivedPackets.Add(packet);
            
            OnReceivedPacketAdded?.Invoke(packet);
        }
        
        public void LogServerReceivedPacket(uint tick, DateTime time, ulong byteLength)
        {
            PacketLog packet = new(tick, time, byteLength);
            
            lock (ServerReceivedPackets)
                ServerReceivedPackets.Add(packet);
            
            OnReceivedPacketAdded?.Invoke(packet);
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

    public readonly struct PacketLog
    {
        public readonly uint Tick;
        public readonly DateTime Time;
        public readonly ulong ByteLength;

        public PacketLog(uint tick, DateTime time, ulong byteLength)
        {
            Tick = tick;
            Time = time;
            ByteLength = byteLength;
        }
    }
}
