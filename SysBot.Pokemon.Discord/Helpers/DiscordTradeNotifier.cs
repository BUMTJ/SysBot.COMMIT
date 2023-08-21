using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using System;
using System.Linq;

namespace SysBot.Pokemon.Discord
{
    public class DiscordTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        private T Data { get; }
        private PokeTradeTrainerInfo Info { get; }
        private int Code { get; }
        private SocketUser Trader { get; }
        public Action<PokeRoutineExecutor<T>>? OnFinish { private get; set; }
        public readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;

        public DiscordTradeNotifier(T data, PokeTradeTrainerInfo info, int code, SocketUser trader)
        {
            Data = data;
            Info = info;
            Code = code;
            Trader = trader;
        }

        public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
<<<<<<< HEAD
            Trader.SendMessageAsync($"거래 초기화 중.. {receive}을 받을 준비를 하시길 바랍니다. 당신의 통신 교환 코드는 **{Code:0000 0000}** 입니다.").ConfigureAwait(false);
=======
            Trader.SendMessageAsync($"거래 초기화 중입니다.{receive}. 준비하시길 바랍니다. 통신 교환 코드는 **{Code:0000 0000}**입니다.").ConfigureAwait(false);
>>>>>>> cb5d35362ce394505b43c5ae36835a46d034f2ad
        }

        public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            var name = Info.TrainerName;
            var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", {name}";
<<<<<<< HEAD
            Trader.SendMessageAsync($"당신을 기다리고 있습니다! 당신의 통신 교환 코드는 **{Code:0000 0000}**이며, 제 이름은 **하뤼** 입니다.").ConfigureAwait(false);
=======
            Trader.SendMessageAsync($"당신을 기다리고 있습니다.{trainer}! 통신 교환 코드는 **{Code:0000 0000}**. 제 이름은 **{routine.InGameName}**입니다.").ConfigureAwait(false);
>>>>>>> cb5d35362ce394505b43c5ae36835a46d034f2ad
        }

        public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            OnFinish?.Invoke(routine);
<<<<<<< HEAD
            Trader.SendMessageAsync($"거래 취소됨: {msg}").ConfigureAwait(false);
=======
            Trader.SendMessageAsync($"교환 취소: {msg}").ConfigureAwait(false);
>>>>>>> cb5d35362ce394505b43c5ae36835a46d034f2ad
        }

        public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
        {
            OnFinish?.Invoke(routine);
            var tradedToUser = Data.Species;
<<<<<<< HEAD
            var message = tradedToUser != 0 ? $"교환 성공. 당신의 포켓몬 : {(Species)tradedToUser}을 즐겨주세요!" : "교환 성공!";
            Trader.SendMessageAsync(message).ConfigureAwait(false);
            if (result.Species != 0 && Hub.Config.Discord.ReturnPKMs)
                Trader.SendPKMAsync(result, "당신이 저에게 교환 보낸 포켓몬입니다!").ConfigureAwait(false);
=======
            var message = tradedToUser != 0 ? $"교환이 끝났습니다. {(Species)tradedToUser} 을 즐겨보세요!" : "교환 완료!";
            Trader.SendMessageAsync(message).ConfigureAwait(false);
            if (result.Species != 0 && Hub.Config.Discord.ReturnPKMs)
                Trader.SendPKMAsync(result, "당신의 포켓몬입니다!").ConfigureAwait(false);
>>>>>>> cb5d35362ce394505b43c5ae36835a46d034f2ad
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
        {
            Trader.SendMessageAsync(message).ConfigureAwait(false);
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
        {
            if (message.ExtraInfo is SeedSearchResult r)
            {
                SendNotificationZ3(r);
                return;
            }

            var msg = message.Summary;
            if (message.Details.Count > 0)
                msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
            Trader.SendMessageAsync(msg).ConfigureAwait(false);
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
        {
            if (result.Species != 0 && (Hub.Config.Discord.ReturnPKMs || info.Type == PokeTradeType.Dump))
                Trader.SendPKMAsync(result, message).ConfigureAwait(false);
        }

        private void SendNotificationZ3(SeedSearchResult r)
        {
            var lines = r.ToString();

            var embed = new EmbedBuilder { Color = Color.LighterGrey };
            embed.AddField(x =>
            {
                x.Name = $"Seed: {r.Seed:X16}";
                x.Value = lines;
                x.IsInline = false;
            });
<<<<<<< HEAD
            var msg = $"여기 데이터가 있습니다 `{r.Seed:X16}`:";
=======
            var msg = $"다음은 `{r.Seed:X16}`에 대한 세부 정보입니다:";
>>>>>>> cb5d35362ce394505b43c5ae36835a46d034f2ad
            Trader.SendMessageAsync(msg, embed: embed.Build()).ConfigureAwait(false);
        }
    }
}
