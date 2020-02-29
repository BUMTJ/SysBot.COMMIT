﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public abstract class BotEnvironment
    {
        protected readonly PokeTradeHub<PK8> Hub;
        private readonly CancellationTokenSource Source = new CancellationTokenSource();
        public readonly List<PokeRoutineExecutor> Bots = new List<PokeRoutineExecutor>();
        public bool CanStart => Hub.Bots.Count != 0;
        public bool CanStop => IsRunning;
        public bool IsRunning { get; private set; }

        protected BotEnvironment(PokeTradeHub<PK8> hub) => Hub = hub;
        protected BotEnvironment(PokeTradeHubConfig config) => Hub = new PokeTradeHub<PK8>(config);

        public void Start(BotList cfg)
        {
            Hub.Bots.Clear();
            Hub.Counts.ReloadCounts(); // if user modified them

            CreateBots(cfg.Bots);

            var token = Source.Token;
            var tasks = CreateBotTasks(token);
            Task.Run(() => Task.WhenAll(tasks), token);
            IsRunning = true;
        }

        private List<Task> CreateBotTasks(CancellationToken token)
        {
            var tasks = new List<Task>();
            AddIntegrations();

            tasks.AddRange(Bots.Select(b => b.RunAsync(token)));
            bool hasTradeBot = Bots.Any(z => z is PokeTradeBot);
            if (hasTradeBot)
                AddTradeBotMonitors(tasks, token);
            return tasks;
        }

        protected virtual void AddIntegrations() { }

        private void AddTradeBotMonitors(ICollection<Task> tasks, CancellationToken token)
        {
            if (Hub.Config.DistributeWhileIdle)
            {
                var path = Hub.Config.DistributeFolder;
                if (!Directory.Exists(path))
                    throw new DirectoryNotFoundException(nameof(path));
                var task = Hub.Queues.MonitorTradeQueueAddIfEmpty(path, token);
                tasks.Add(task);

                if (Hub.Ledy.Pool.Count == 0)
                    LogUtil.LogError("Nothing to distribute for Empty Trade Queues!", "Hub");
            }
            if (Hub.Config.MonitorForPriorityTrades)
            {
                var path = Hub.Config.PriorityFolder;
                if (!Directory.Exists(path))
                    throw new DirectoryNotFoundException(nameof(path));
                var task = Hub.Queues.MonitorFolderAddPriority(path, PokeTradeHub<PK8>.LogNotifier, token);
                tasks.Add(task);
            }
        }

        private void CreateBots(IEnumerable<PokeBotConfig> bots)
        {
            foreach (var cfg in bots)
            {
                var bot = GetBotFromConfig(cfg);
                Bots.Add(bot);
            }
        }

        private PokeRoutineExecutor GetBotFromConfig(PokeBotConfig cfg)
        {
            switch (cfg.NextRoutineType)
            {
                case PokeRoutineType.Idle:
                case PokeRoutineType.SurpriseTrade:
                case PokeRoutineType.FlexTrade:
                case PokeRoutineType.LinkTrade:
                case PokeRoutineType.Clone:
                case PokeRoutineType.Dump:
                case PokeRoutineType.DuduBot:
                    return new PokeTradeBot(Hub, cfg);

                case PokeRoutineType.EggFetch:
                    return new EggBot(Hub, cfg);

                case PokeRoutineType.FossilBot:
                    return new FossilBot(Hub, cfg);

                case PokeRoutineType.RaidBot:
                    return new RaidBot(Hub, cfg);

                default:
                    throw new ArgumentException(nameof(cfg.NextRoutineType));
            }
        }

        public void Stop()
        {
            Source.Cancel();
            IsRunning = false;

            // bots currently don't de-register
            Thread.Sleep(100);
            int count = Hub.BotSync.Barrier.ParticipantCount;
            if (count != 0)
                Hub.BotSync.Barrier.RemoveParticipants(count);

            DetatchBots();
        }

        private void DetatchBots()
        {
            foreach (var bot in Bots)
            {
                Task.Run(() => bot.Connection.SendAsync(SwitchCommand.DetachController(), CancellationToken.None));
            }
        }

        public void Pause()
        {
            // Tell all the bots to go to Idle after finishing.
            foreach (var b in Bots)
                b.Config.Pause();
            IsRunning = false;
        }

        public void Resume()
        {
            IsRunning = true;
            // Tell all the bots to go to Idle after finishing.
            foreach (var b in Bots)
                b.Config.Resume();
        }
    }
}