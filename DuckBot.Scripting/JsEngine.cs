using Jint;
using Jint.Native;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DuckBot.Scripting
{
    public sealed class JsEngine
    {
        private readonly Engine _eng;

        public event Action<string>? OnPrint;

        public JsEngine(params object[] bridges)
        {
            _eng = new Engine(cfg => cfg.TimeoutInterval(TimeSpan.FromSeconds(30)));
            _eng.SetValue("print", new Action<object>(x => OnPrint?.Invoke(x?.ToString() ?? "")));
            foreach (var b in bridges)
            {
                string name = b switch
                {
                    IScriptBridge bridge => bridge.Name,
                    _ => b.GetType().Name
                };

                if (string.IsNullOrWhiteSpace(name))
                    name = b.GetType().Name;

                if (name.Length == 1)
                    name = name.ToLowerInvariant();
                else if (!char.IsLower(name[0]))
                    name = char.ToLowerInvariant(name[0]) + name[1..];

                _eng.SetValue(name, b);
            }
        }

        public Task RunAsync(string code, CancellationToken ct)
            => Task.Run(() =>
            {
                using var reg = ct.Register(() => throw new OperationCanceledException());
                _eng.Execute(code);
            }, ct);

        public JsValue Get(string id) => _eng.GetValue(id);
        public void Set(string id, object val) => _eng.SetValue(id, val);
    }
}
