using System;
using System.Threading;
using System.Threading.Tasks;
using DuckBot.Data.Models;

namespace DuckBot.Core.Services
{
    /// <summary>
    /// Minimal, safe script runner for MVP. It doesn't control emulators — it simply walks steps,
    /// substitutes variables (basic) and reports textual progress so the UI can show logs.
    /// </summary>
    public class SimpleBotRunner : IBotRunner
    {
        public async Task StartAsync(ScriptModel script, string instance, IProgress<string> progress, CancellationToken cancellationToken)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            progress?.Report($"Starting '{script.Name}' on instance '{instance ?? "none"}'...");

            int stepIndex = 0;
            foreach (var step in script.Steps ?? Array.Empty<DuckBot.Data.Scripts.ScriptStep>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                stepIndex++;
                // Simple textual simulation of performing the step:
                string type = step.Type ?? "step";
                progress?.Report($"Step {stepIndex}/{script.Steps.Count}: {type} — params: {string.Join(", ", step.Params.Keys)}");

                // Simulate work and allow cancellation
                await Task.Delay(400, cancellationToken);
            }

            progress?.Report($"Completed '{script.Name}'.");
        }
    }
}