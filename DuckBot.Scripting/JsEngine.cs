using Jint;
using System;
namespace DuckBot.Scripting {
  public sealed class JsEngine {
    private readonly Engine _eng;
    public JsEngine() {
      _eng = new Engine(cfg => cfg.TimeoutInterval(TimeSpan.FromSeconds(10)));
      _eng.SetValue("print", new Action<object>(x => Console.WriteLine(x?.ToString())));
    }
    public void Run(string code) => _eng.Execute(code);
  }
}
