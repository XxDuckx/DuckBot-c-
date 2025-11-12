using OpenCvSharp;
using System;

namespace DuckBot.Core.Logging
{
    public sealed record LogEntry(DateTime Timestamp, string Message, LogLevel Level)
    {
        public override string ToString() => $"[{Timestamp:HH:mm:ss}] {Message}";
    }
}