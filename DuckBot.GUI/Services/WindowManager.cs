using System;
using DuckBot.Core.Services;
using DuckBot.GUI.Views;
using DuckBot.GUI.ViewModels;
using System.Windows;

namespace DuckBot.GUI.Services
{
    public class WindowManager : IWindowManager
    {
        public void OpenEditor(BotEntry bot)
        {
            if (bot == null) return;
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var w = new BotEditorWindow();
                w.LoadBot(bot);
            });
        }
    }
}