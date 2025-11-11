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
                var name = b.GetType().Name;
                // expose e.g. Util -> global.util
                _eng.SetValue(char.ToLowerInvariant(name[0]) + name[1..], b);
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
