namespace DuckBot.Core.Models
{
    public class SolverSettings
    {
        public bool GameAdCloser { get; set; } = true;
        public bool LdStoreCrashCloser { get; set; } = true;
        public bool CaptchaSolver { get; set; } = true;
        public bool GameLoadingSolver { get; set; } = false;
        public bool MessageSolver { get; set; } = true;
    }
}
