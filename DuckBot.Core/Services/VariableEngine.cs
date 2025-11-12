using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DuckBot.Core.Services
{
    /// <summary>
    /// Very small variable substitution engine.
    /// Syntax: ${varName} substitutes value from variables dictionary (falls back to empty).
    /// </summary>
    public class VariableEngine
    {
        private static readonly Regex _rx = new(@"\$\{(?<name>[A-Za-z0-9_.-]+)\}", RegexOptions.Compiled);

        public string Substitute(string input, IDictionary<string, string?> variables)
        {
            if (string.IsNullOrEmpty(input) || variables == null) return input;
            return _rx.Replace(input, m =>
            {
                var name = m.Groups["name"].Value;
                return variables.TryGetValue(name, out var val) ? (val ?? string.Empty) : string.Empty;
            });
        }

        public IDictionary<string, string?> BuildLookup(IEnumerable<KeyValuePair<string, string?>> vars)
        {
            var d = new Dictionary<string, string?>();
            foreach (var kv in vars) d[kv.Key] = kv.Value;
            return d;
        }
    }
}