using System;
using System.Collections.Generic;

namespace DuckBot.Core.Logging
{
    public interface IAppLogger
    {
        event EventHandler<LogEntry>? EntryWritten;

        IReadOnlyCollection<LogEntry> RecentEntries { get; }

        void Info(string message);
        void Warn(string message);
        void Error(string message);
    }
}