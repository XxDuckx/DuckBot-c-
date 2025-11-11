using System.Collections.Generic;

namespace DuckBot.Core.Emu
{
    /// <summary>
    /// Prevents selecting the same emulator instance on multiple bots.
    /// </summary>
    public sealed class InstanceRegistry
    {
        private readonly Dictionary<string, string> _heldByBotId = new(); // instance -> botId
        private static readonly InstanceRegistry _inst = new();
        public static InstanceRegistry Current => _inst;

        public bool TryReserve(string instance, string botId)
        {
            if (string.IsNullOrWhiteSpace(instance)) return true; // treat empty as not reserving
            lock (_heldByBotId)
            {
                if (_heldByBotId.TryGetValue(instance, out var holder))
                {
                    if (holder == botId) return true; // already owned by same bot
                    return false; // owned by other bot
                }
                _heldByBotId[instance] = botId;
                return true;
            }
        }

        public void ReleaseByBot(string botId)
        {
            lock (_heldByBotId)
            {
                var toFree = new List<string>();
                foreach (var kv in _heldByBotId)
                    if (kv.Value == botId) toFree.Add(kv.Key);
                foreach (var k in toFree) _heldByBotId.Remove(k);
            }
        }

        public void RebuildFromBots(IEnumerable<(string Instance, string BotId)> pairs)
        {
            lock (_heldByBotId)
            {
                _heldByBotId.Clear();
                foreach (var p in pairs)
                {
                    if (!string.IsNullOrWhiteSpace(p.Instance) && !_heldByBotId.ContainsKey(p.Instance))
                        _heldByBotId[p.Instance] = p.BotId;
                }
            }
        }
    }
}
