using System;
using System.Threading;
using System.Threading.Tasks;

namespace DuckBot.Scripting.Bridges
{
    public sealed class Util
    {
        private readonly Action<string> _log;
        private readonly Func<CancellationToken> _getToken;

        public Util(Action<string> log, Func<CancellationToken> getToken)
        {
            _log = log;
            _getToken = getToken;
        }

        public void log(object msg) => _log(msg?.ToString() ?? "");

        public void sleep(int ms)
        {
            Task.Delay(ms, _getToken()).GetAwaiter().GetResult();
        }
    }
}
