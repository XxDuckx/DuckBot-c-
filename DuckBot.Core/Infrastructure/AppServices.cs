using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DuckBot.Core.Emu;
using DuckBot.Core.Logging;
using DuckBot.Core.Services;

namespace DuckBot.Core.Infrastructure
{
    public static class AppServices
    {
        private static readonly ConcurrentDictionary<Type, object> Services = new();
        private static bool _configured;
        private static readonly object InitLock = new();

        public static void ConfigureDefaults()
        {
            if (_configured) return;
            lock (InitLock)
            {
                if (_configured) return;

                var logger = new AppLogger();
                Register<IAppLogger>(logger);

                var detector = new LdPlayerDetector();
                Register<IEmulatorDetector>(detector);

                var emulatorService = new EmulatorService(detector, logger);
                Register<IEmulatorService>(emulatorService);

                var adbService = new AdbService(emulatorService);
                Register<IAdbService>(adbService);

                var botRunner = new BotRunnerService(logger, adbService);
                Register<IBotRunnerService>(botRunner);

                _configured = true;
            }
        }

        public static void Register<TService>(TService implementation) where TService : class
        {
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            Services[typeof(TService)] = implementation;
        }

        public static TService Get<TService>() where TService : class
        {
            if (!_configured) ConfigureDefaults();
            if (Services.TryGetValue(typeof(TService), out var service) && service is TService typed)
            {
                return typed;
            }

            throw new InvalidOperationException($"Service for {typeof(TService).Name} is not registered.");
        }

        public static IAppLogger Logger => Get<IAppLogger>();
        public static IEmulatorService EmulatorService => Get<IEmulatorService>();
        public static IAdbService AdbService => Get<IAdbService>();
        public static IBotRunnerService BotRunner => Get<IBotRunnerService>();
    }
}