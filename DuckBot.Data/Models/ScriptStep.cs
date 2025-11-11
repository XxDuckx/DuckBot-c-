using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace DuckBot.Data.Models
{
    public class ScriptStep
    {
        public string Type { get; set; } = "";
        public Dictionary<string, object> Params { get; set; } = new();
        public List<ScriptStep>? ActionIfFound { get; set; }
        public List<ScriptStep>? ActionIfNotFound { get; set; }

        public object? this[string key]
        {
            get => Params.TryGetValue(key, out var value) ? value : null;
            set
            {
                if (value == null)
                    Params.Remove(key);
                else
                    Params[key] = Normalize(value);
            }
        }

        public ScriptStep Clone()
        {
            return new ScriptStep
            {
                Type = Type,
                Params = Params.ToDictionary(p => p.Key, p => CloneValue(p.Value)),
                ActionIfFound = ActionIfFound?.Select(step => step.Clone()).ToList(),
                ActionIfNotFound = ActionIfNotFound?.Select(step => step.Clone()).ToList()
            };
        }

        public IEnumerable<ScriptStep> EnumerateSelfAndChildren()
        {
            yield return this;
            if (ActionIfFound != null)
            {
                foreach (var step in ActionIfFound.SelectMany(s => s.EnumerateSelfAndChildren()))
                    yield return step;
            }
            if (ActionIfNotFound != null)
            {
                foreach (var step in ActionIfNotFound.SelectMany(s => s.EnumerateSelfAndChildren()))
                    yield return step;
            }
        }

        public void ApplyDefaults(IReadOnlyDictionary<string, object> template)
        {
            foreach (var kvp in template)
            {
                if (!Params.ContainsKey(kvp.Key))
                {
                    Params[kvp.Key] = CloneValue(kvp.Value);
                }
            }
        }

        public bool ValidateAgainst(IReadOnlyDictionary<string, object> template, out string? error)
        {
            foreach (var kvp in template)
            {
                if (!Params.TryGetValue(kvp.Key, out var value))
                {
                    error = $"Missing parameter '{kvp.Key}' for step '{Type}'.";
                    return false;
                }

                if (!IsCompatible(value, kvp.Value))
                {
                    error = $"Parameter '{kvp.Key}' has unexpected type.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        public Dictionary<string, object> ToSerializableDictionary()
            => Params.ToDictionary(kvp => kvp.Key, kvp => CloneValue(kvp.Value));

        public string Summary
        {
            get
            {
                var args = string.Join(", ", Params.Select(p => $"{p.Key}:{p.Value}"));
                return string.IsNullOrWhiteSpace(args) ? Type : $"{Type} ({args})";
            }
        }

        public T GetValue<T>(string key, T defaultValue = default!)
        {
            if (!Params.TryGetValue(key, out var value)) return defaultValue;
            value = value is JsonElement json ? Normalize(json) : value;
            if (value is T castValue) return castValue;
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        private static object CloneValue(object value) => value switch
        {
            JsonElement json => Normalize(json),
            Dictionary<string, object> dict => dict.ToDictionary(p => p.Key, p => CloneValue(p.Value)),
            List<object> list => list.Select(CloneValue).ToList(),
            ScriptStep step => step.Clone(),
            _ => value
        };

        private static object Normalize(object value) => value switch
        {
            JsonElement json => Normalize(json),
            _ => value
        };

        private static object Normalize(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(e => Normalize(e)).Cast<object>().ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => Normalize(p.Value)),
            JsonValueKind.Null => string.Empty,
            _ => element.ToString()
        };

        private static bool IsCompatible(object? value, object template)
        {
            if (value == null) return false;
            return template switch
            {
                int => value is int or long or double,
                float => value is float or double or int,
                double => value is double or float or int,
                bool => value is bool,
                string => value is string,
                Dictionary<string, object> dictTemplate when value is IDictionary<string, object> dictValue
                    => dictTemplate.All(kv => dictValue.ContainsKey(kv.Key)),
                IEnumerable<object> listTemplate when value is IEnumerable<object> listValue
                    => listValue.Count() >= listTemplate.Count(),
                _ => true
            };
        }
    }
}