﻿using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public static class QueueHelper<T> where T : PKM, new()
    {
        private const uint MaxTradeCode = 9999_9999;

        public static async Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type, SocketUser trader)
        {
            if ((uint)code > MaxTradeCode)
            {
                await context.Channel.SendMessageAsync("Trade code should be 00000000-99999999!").ConfigureAwait(false);
                return;
            }

            try
            {
                const string helper = "대기열에 추가했습니다! 거래가 시작되면 여기에 메시지를 보내겠습니다.";
                IUserMessage test = await trader.SendMessageAsync(helper).ConfigureAwait(false);

                // Try adding
                var result = AddToTradeQueue(context, trade, code, trainer, sig, routine, type, trader, out var msg);

                // Notify in channel
                await context.Channel.SendMessageAsync(msg).ConfigureAwait(false);
                // Notify in PM to mirror what is said in the channel.
                await trader.SendMessageAsync($"{msg}\n통신교환 코드는 **{code:0000 0000}**입니다.").ConfigureAwait(false);

                // Clean Up
                if (result)
                {
                    // Delete the user's join message for privacy
                    if (!context.IsPrivate)
                        await context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
                }
                else
                {
                    // Delete our "I'm adding you!", and send the same message that we sent to the general channel.
                    await test.DeleteAsync().ConfigureAwait(false);
                }
            }
            catch (HttpException ex)
            {
                await HandleDiscordExceptionAsync(context, trader, ex).ConfigureAwait(false);
            }
        }

        public static async Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type)
        {
            await AddToQueueAsync(context, code, trainer, sig, trade, routine, type, context.User).ConfigureAwait(false);
        }

        private static bool AddToTradeQueue(SocketCommandContext context, T pk, int code, string trainerName, RequestSignificance sig, PokeRoutineType type, PokeTradeType t, SocketUser trader, out Embed embed)
        {
            var user = trader;
            var userID = user.Id;
            var name = user.Username;

            var trainerInfo = new PokeTradeTrainerInfo(trainerName, userID); // 이름 변경: trainer -> trainerInfo
            var notifier = new DiscordTradeNotifier<T>(pk, trainerInfo, code, user); // 변경: trainer -> trainerInfo
            var detail = new PokeTradeDetail<T>(pk, trainerInfo, notifier, t, code, sig == RequestSignificance.Favored); // 변경: trainer -> trainerInfo
            var trade = new TradeEntry<T>(detail, userID, type, name);
            var embedBuilder = new EmbedBuilder();

            var hub = SysCord<T>.Runner.Hub;
            var Info = hub.Queues.Info;
            var added = Info.AddToTradeQueue(trade, userID, sig == RequestSignificance.Owner);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                embedBuilder
                    .WithTitle("Sorry, you are already in the queue.");

                embed = embedBuilder.Build();

                return false;
            }
                
                
            var position = Info.CheckPosition(userID, type);

            var ticketID = "";
            if (TradeStartModule<T>.IsStartChannel(context.Channel.Id))
                ticketID = $", unique ID: {detail.ID}";
            
            var pokeName = "";
            if (t == PokeTradeType.Specific && pk.Species != 0)
                pokeName = $" 받으실 포켓몬은 **{GameInfo.GetStrings(1).Species[pk.Species]}** 입니다.";
    
            embedBuilder
                .WithTitle($"{user.Username}님! 통신교환 대기열 등록을 성공하였습니다.")
                .WithDescription("상세 정보")
                .AddField(pokeName)
                .AddField("현재 대기열 순서는", position.Position.ToString()"입니다.")
                .AddField("개인 DM을 확인하시길 바랍니다.")
                .WithColor(Color.Green);

            var botct = Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                embedBuilder.AddField("예상 대기 시간은" $"{eta:F1}분 입니다");
            }

            embed = embedBuilder.Build();

            return true;
        }


        private static async Task HandleDiscordExceptionAsync(SocketCommandContext context, SocketUser trader, HttpException ex)
        {
            string message = string.Empty;
            switch (ex.DiscordCode)
            {
                case DiscordErrorCode.InsufficientPermissions or DiscordErrorCode.MissingPermissions:
                    {
                        // Check if the exception was raised due to missing "Send Messages" or "Manage Messages" permissions. Nag the bot owner if so.
                        var permissions = context.Guild.CurrentUser.GetPermissions(context.Channel as IGuildChannel);
                        if (!permissions.SendMessages)
                        {
                            // Nag the owner in logs.
                            message = "You must grant me \"Send Messages\" permissions!";
                            Base.LogUtil.LogError(message, "QueueHelper");
                            return;
                        }
                        if (!permissions.ManageMessages)
                        {
                            var app = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
                            var owner = app.Owner.Id;
                            message = $"<@{owner}> You must grant me \"Manage Messages\" permissions!";
                        }
                    }
                    break;
                case DiscordErrorCode.CannotSendMessageToUser:
                    {
                        // The user either has DMs turned off, or Discord thinks they do.
                        message = context.User == trader ? "대기열에 들어가려면 개인 DM을 사용 가능으로 설정해야 합니다!" : "언급된 사용자가 개인 메시지를 대기열에 넣으려면 개인 메시지를 활성화해야 합니다!";
                    }
                    break;
                default:
                    {
                        // Send a generic error message.
                        message = ex.DiscordCode != null ? $"디스코드 오류 {(int)ex.DiscordCode}: {ex.Reason}" : $"Http 오류 {(int)ex.HttpCode}: {ex.Message}";
                    }
                    break;
            }
            await context.Channel.SendMessageAsync(message).ConfigureAwait(false);
        }
    }
}