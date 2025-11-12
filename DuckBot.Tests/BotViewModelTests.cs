using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DuckBot.Core.Services;
using DuckBot.GUI.ViewModels;
using Xunit;

namespace DuckBot.Tests
{
    class FakeRunner : IBotRunner
    {
        public int Calls;
        public Task StartAsync(DuckBot.Data.Models.ScriptModel script, string instance, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Calls++;
            progress?.Report("fake started");
            return Task.CompletedTask;
        }
    }

    class FakeStore : IBotStore
    {
        public Task<IList<BotEntry>> LoadAsync() => Task.FromResult<IList<BotEntry>>(new List<BotEntry>());
        public Task SaveAsync(IList<BotEntry> bots) => Task.CompletedTask;
    }

    class FakeWindowManager : IWindowManager
    {
        public BotEntry? LastOpened;
        public void OpenEditor(BotEntry bot) => LastOpened = bot;
    }

    public class BotViewModelTests
    {
        [Fact]
        public async Task StartBot_InvokesRunner()
        {
            var runner = new FakeRunner();
            var store = new FakeStore();
            var wm = new FakeWindowManager();
            var vm = new BotViewModel(runner, store, wm);
            var bot = new BotEntry { Name = "T1" };
            vm.Bots.Add(bot);

            await vm.StartBotAsync(bot);

            Assert.Equal(1, runner.Calls);
        }

        [Fact]
        public void OpenEditor_OpensWindow()
        {
            var runner = new FakeRunner();
            var store = new FakeStore();
            var wm = new FakeWindowManager();
            var vm = new BotViewModel(runner, store, wm);
            var bot = new BotEntry { Name = "T2" };
            vm.Bots.Add(bot);

            vm.OpenEditorCommand.Execute(bot);

            Assert.Equal(bot, wm.LastOpened);
        }
    }
}