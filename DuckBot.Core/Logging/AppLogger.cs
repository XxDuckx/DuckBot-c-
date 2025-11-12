using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DuckBot.Core.Logging
{
    public sealed class AppLogger : IAppLogger
    {
        private readonly ConcurrentQueue<LogEntry> _buffer = new();
        private readonly int _maxEntries;

        public AppLogger(int maxEntries = 500)
        {
            if (maxEntries <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxEntries));
            _maxEntries = maxEntries;
        }

        public event EventHandler<LogEntry>? EntryWritten;

        public IReadOnlyCollection<LogEntry> RecentEntries => _buffer.ToArray();

        public void Info(string message) => Write(message, LogLevel.Info);
        public void Warn(string message) => Write(message, LogLevel.Warning);
        public void Error(string message) => Write(message, LogLevel.Error);

        private void Write(string message, LogLevel level)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var entry = new LogEntry(DateTime.Now, message.Trim(), level);
            _buffer.Enqueue(entry);

            while (_buffer.Count > _maxEntries && _buffer.TryDequeue(out _))
            {
                // trim old entries
            }

            EntryWritten?.Invoke(this, entry);
        }
    }
}