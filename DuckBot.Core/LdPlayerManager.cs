using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
namespace DuckBot.Core.ADB {
  public record LdInstance(string Name, int AdbPort);
  public static class LdPlayerManager {
    public static List<LdInstance> ListInstances() {
      var list = new List<LdInstance>();
      var p = Process.Start(new ProcessStartInfo {
        FileName = "adb", Arguments = "devices", RedirectStandardOutput = true, UseShellExecute = false
      });
      p.WaitForExit();
      var re = new Regex(@"127\.0\.0\.1:(\d+)\s+device");
      foreach (Match m in re.Matches(p.StandardOutput.ReadToEnd())) {
        list.Add(new LdInstance($"LD-{m.Groups[1].Value}", int.Parse(m.Groups[1].Value)));
      }
      return list;
    }
  }
}
