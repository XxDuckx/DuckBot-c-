using System.Collections.Generic;

namespace DuckBot.Core.Models
{
    public class SettingsModel
    {
        public GeneralSettings General { get; set; } = new();
        public AdvancedSettings Advanced { get; set; } = new();
        public SolverSettings Solvers { get; set; } = new();
        public BackupSettings Backups { get; set; } = new();
        public List<MailAccount> MailAccounts { get; set; } = new();
    }
}