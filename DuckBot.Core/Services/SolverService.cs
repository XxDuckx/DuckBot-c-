using System;
using System.Collections.Concurrent;
using DuckBot.Core.Models;

namespace DuckBot.Core.Services
{
    public static class SolverService
    {
        private static readonly ConcurrentDictionary<string, bool> _states = new(StringComparer.OrdinalIgnoreCase);
        public static event Action<string, bool>? SolverToggled;

        public static void Initialize()
        {
            ApplySettings(SettingsManager.Current.Solvers);
            LogService.Info("Solver service initialised.");
        }

        public static void ApplySettings(SolverSettings settings)
        {
            ToggleSolver("GameAdCloser", settings.GameAdCloser);
            ToggleSolver("LdStoreCrashCloser", settings.LdStoreCrashCloser);
            ToggleSolver("CaptchaSolver", settings.CaptchaSolver);
            ToggleSolver("GameLoadingSolver", settings.GameLoadingSolver);
            ToggleSolver("MessageSolver", settings.MessageSolver);
        }

        public static void ToggleSolver(string name, bool enabled)
        {
            _states[name] = enabled;
            SolverToggled?.Invoke(name, enabled);
            LogService.Info($"Solver '{name}' {(enabled ? "enabled" : "disabled")}.");
        }

        public static bool IsEnabled(string name) => _states.TryGetValue(name, out var enabled) && enabled;
    }
}
