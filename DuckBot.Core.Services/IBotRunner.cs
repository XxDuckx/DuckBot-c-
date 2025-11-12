using System;
using System.Threading;
using System.Threading.Tasks;
using DuckBot.Data.Models;

namespace DuckBot.Core.Services
{
    public interface IBotRunner
    {
        /// <summary>
        /// Start running the script for the given instance. Reports progress via progress.Report(string).
        /// </summary>
        Task StartAsync(ScriptModel script, string instance, IProgress<string> progress, CancellationToken cancellationToken);
    }
}