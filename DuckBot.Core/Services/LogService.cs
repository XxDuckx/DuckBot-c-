using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DuckBot.Core.Services
{
    public static class LogService
    {
        public enum Level { Info, Warn, Error }

        public static event Action<string, Level>? OnLog;

        private static readonly ConcurrentQueue<(string, Level)> _buffer = new();

        public static void Info(string msg) => Write(msg, Level.Info);
        public static void Warn(string msg) => Write(msg, Level.Warn);
        public static void Error(string msg) => Write(msg, Level.Error);

        private static void Write(string msg, Level level)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            _buffer.Enqueue((line, level));
            OnLog?.Invoke(line, level);
        }

        public static List<(string, Level)> GetBuffered() => new(_buffer);
    }
}
