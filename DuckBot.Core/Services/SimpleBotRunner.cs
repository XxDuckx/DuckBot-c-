using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DuckBot.Data.Models;
using DuckBot.Data.Scripts;

namespace DuckBot.Core.Services
{
    /// <summary>
    /// Runner enhanced for MVP: supports variable substitution and uses the emulator stub.
    /// </summary>
    public class SimpleBotRunner : IBotRunner
    {
        private readonly IEmulatorService _emulator;
        private readonly VariableEngine _vars;

        public SimpleBotRunner(IEmulatorService emulator, VariableEngine vars)
        {
            _emulator = emulator;
            _vars = vars;
        }

        public async Task StartAsync(ScriptModel script, string instance, IProgress<string> progress, CancellationToken cancellationToken)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            progress?.Report($"Starting '{script.Name}' on instance '{instance ?? "none"}'...");

            // build variable lookup (script variables -> defaults)
            var lookup = new Dictionary<string, string?>();
            if (script.Variables != null)
            {
                foreach (var v in script.Variables)
                    lookup[v.Key ?? string.Empty] = v.Default;
            }

            // iterate steps
            var steps = script.Steps ?? new List<ScriptStep>();
            int index = 0;
            foreach (var step in steps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                index++;
                var type = step.Type ?? "step";

                // build textual param preview
                string paramPreview = string.Join(", ", step.Params.Select(kv =>
                    $"{kv.Key}={_vars.Substitute(kv.Value?.ToString() ?? string.Empty, lookup)}"));

                progress?.Report($"[{index}/{steps.Count}] {type} - {paramPreview}");

                // Basic supported actions (MVP): delay / click / send
                switch (type.ToLowerInvariant())
                {
                    case "delay":
                        if (double.TryParse(step.GetValue("ms", 0).ToString(), out var ms))
                        {
                            await _emulator.DelayAsync((int)ms, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await _emulator.DelayAsync(400, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case "click":
                        if (int.TryParse(step.GetValue("x", 0).ToString(), out var x) &&
                            int.TryParse(step.GetValue("y", 0).ToString(), out var y))
                        {
                            await _emulator.ClickAsync(instance, x, y, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await _emulator.DelayAsync(50, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case "send":
                        var raw = step.GetValue("text", string.Empty)?.ToString() ?? string.Empty;
                        var text = _vars.Substitute(raw, lookup);
                        await _emulator.SendTextAsync(instance, text, cancellationToken).ConfigureAwait(false);
                        break;

                    default:
                        // unknown step -> simulate a short delay
                        await _emulator.DelayAsync(300, cancellationToken).ConfigureAwait(false);
                        break;
                }
            }

            progress?.Report($"Completed '{script.Name}'.");
        }
    }
}