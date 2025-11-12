namespace DuckBot.Core.Services
{
    public sealed record EmulatorInstanceInfo(string Name, int Index, string? AdbId, bool IsRunning, string InstallDisplayName)
    {
        public override string ToString() => $"{Name} ({InstallDisplayName})";
    }
}