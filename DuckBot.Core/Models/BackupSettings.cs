namespace DuckBot.Core.Models
{
    public class BackupSettings
    {
        public bool AutoBackupOnExit { get; set; } = false;
        public string BackupDir { get; set; } = "data/backups";
    }
}
