using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using SysBot.Fraudious;
using NLog.Targets.Wrappers;
using PKHeX.Core;
using System;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public static class QueueHelper<T> where T : PKM, new()
    {
        static readonly HttpClient client = new();

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
                const string helper = "교환 대기열 추가에 성공했습니당! 거래가 시작되면 여기에 메시지를 보내겠습니다잉!";
                IUserMessage test = await trader.SendMessageAsync(helper).ConfigureAwait(false);

                // Try adding
                var result = AddToTradeQueue(context, trade, code, trainer, sig, routine, type, trader, out var msg);

                // Notify in channel
                // await context.Channel.SendMessageAsync(msg).ConfigureAwait(false); 
                
                var user = trader;
                SocketGuildUser guser = (SocketGuildUser)user;
                var UNick = guser.Nickname;

                if (UNick is null) UNick = user.Username;
                
                var userID = user.Id;
                var hub = SysCord<T>.Runner.Hub;
                var Info = hub.Queues.Info;
                var position = Info.CheckPosition(userID, routine);
                string embedMsg, embedTitle = "", embedAuthor, embedThumbUrl = "";
                bool CanGMax = false;
                uint FormArgument = 0;

                Color embedMsgColor = new();
                EmbedAuthorBuilder embedAuthorBuild = new();
                EmbedFooterBuilder embedFtr = new();

                string HeldItem = Fraudiouscl.FixHeldItemName(((HeldItem)trade.HeldItem).ToString());

                switch (trade.Version)
                {
                    case (int)GameVersion.X or (int)GameVersion.Y:
                        PK6 mon6 = (PK6)trade.Clone();
                        FormArgument = mon6.FormArgument;
                        break;
                    case (int)GameVersion.SN or (int)GameVersion.MN or (int)GameVersion.US or (int)GameVersion.UM:
                        PK7 mon7 = (PK7)trade.Clone();
                        FormArgument = mon7.FormArgument;
                        break;
                    case (int)GameVersion.GP or (int)GameVersion.GE:
                        PB7 monLGPE = (PB7)trade.Clone();
                        FormArgument = monLGPE.FormArgument;
                        break;
                    case (int)GameVersion.SW or (int)GameVersion.SH:
                        PK8 mon8 = (PK8)trade.Clone();
                        CanGMax = mon8.CanGigantamax;
                        FormArgument = mon8.FormArgument;
                        break;
                    case (int)GameVersion.BD or (int)GameVersion.SP:
                        PB8 monBDSP = (PB8)trade.Clone();
                        CanGMax = monBDSP.CanGigantamax;
                        FormArgument = monBDSP.FormArgument;
                        break;
                    case (int)GameVersion.PLA:
                        PA8 monLA = (PA8)trade.Clone();
                        CanGMax = monLA.CanGigantamax;
                        FormArgument = monLA.FormArgument;
                        break;
                    case (int)GameVersion.SL or (int)GameVersion.VL:
                        PK9 mon9 = (PK9)trade.Clone();
                        FormArgument = mon9.FormArgument;
                        break;
                }
                if (routine == PokeRoutineType.Clone || routine == PokeRoutineType.Dump || routine == PokeRoutineType.DirectTrade)
                {
                    var me = SysCord<T>.Runner;
                    var cd = SysCordSettings.HubConfig.TradeAbuse.TradeCooldown;
                    string botversion = "";
                    if (me is not null)
                        botversion = me.ToString()!.Substring(46, 3);
                    var gamever = botversion switch
                    {
                        "PK9" => "SV",
                        "PK8" => "SWSH",
                        "PA8" => "PLA",
                        "PB8" => "BDSP",
                        _ => "LGPE",
                    };

                    embedTitle = $"__한번 채팅을 보내면 거래를 초기화 할 수 있습니다.__\n";
                    embedAuthor = $"{UNick}'님 ";
                    embedMsg = $"";

                    if (routine == PokeRoutineType.Clone)
                    {
                        embedMsgColor = 0xF9F815;
                        embedAuthor += "포켓몬 복제 요청";
                        embedMsg += $"**STEPS:**\n";
                        embedMsg += $"**1.** 통신교환 창에서 복제할 포켓몬을 보여주세요\n";
                        embedMsg += $"**2.** 교환 직전, 통신 교환을 나가주세요\n";
                        embedMsg += $"**3.** 포켓몬을 복제하고 파일을 보내겠습니다.\n";
                        embedMsg += $"**4.** 해당 메시지를 기억하시길 바랍니다..\n\n";
                        embedMsg += $"당신은 **{SysCordSettings.HubConfig.TradeAbuse.TradeCooldown}** 분의 쿨다운이 지나면 교환을 다시 시작할 수 있습니다.\n\n";
                        embedMsg += $"감사합니다.";
                    }
                    else if (routine == PokeRoutineType.Dump)
                    {
                        embedMsgColor = 0x6015F9;
                        embedAuthor += "PK9 덤프 요청";
                        embedMsg += $"포켓몬 파일을 통해 교환합니다.\n";
                        embedMsg += $"당신의 포켓몬을 보여줄 수 있는 시간은 **{SysCordSettings.HubConfig.Trade.MaxDumpTradeTime}** 입니다.\n";
                        embedMsg += $"포켓몬은 **{SysCordSettings.HubConfig.Trade.MaxDumpsPerTrade}** 여기 있습니다.\n\n";
                        embedMsg += $"당신은 **{SysCordSettings.HubConfig.TradeAbuse.TradeCooldown}** 분의 쿨다운이 지나면 교환을 다시 시작할 수 있습니다.\n\n";
                        embedMsg += $"감사합니다, 다시 이용해 주세요!";
                    }
                    else if (routine == PokeRoutineType.DirectTrade)
                    {
                        embedMsgColor = 0x6FFEEC;
                        embedAuthor += "통신 교환 요청";
                        embedMsg += $"시트의 별명을 사용하여 거래하거나 **특수 기능**을 사용합니다\n";
                        embedMsg += $"현재 진행 중인 게임은 **{gamever}** 입니다.\n\n";
                        embedMsg += $"명령어:\n**$help**, **$rsv**, **$t**, **$dump**, **$clone**, **$checkcd**\n";
                        embedMsg += $"당신은 **{cd}** 분의 쿨다운이 지나면 교환을 다시 시작할 수 있습니다.\n";
                        embedMsg += $"즐거운 시간 되시고 다시 와주세요!";
                    }

                    embedAuthorBuild.IconUrl = "https://archives.bulbagarden.net/media/upload/e/e1/PCP363.png";
                    embedAuthorBuild.Name = embedAuthor;

                    embedFtr.Text = $"현재 대기열 순번은 " + SysCord<T>.Runner.Hub.Queues.Info.Count.ToString() + "입니다.";

                    var botct = Info.Hub.Bots.Count;
                    if (position.Position > botct)
                    {
                        var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                        embedFtr.Text += $"\n예상 대기 시간은 {eta:F1} 분 입니다.";
                    }
                    
                }
                else
                {
                    if (trade.IsShiny)
                    {
                        if (trade.ShinyXor == 0 && !(trade.Version == (int)GameVersion.SL || trade.Version == (int)GameVersion.VL))
                            embedTitle = "■ shiny ";
                        else
                            embedTitle = "★ shiny ";
                    }

                    embedTitle += $" {(Species)trade.Species} ";
                    if (trade.Gender == 0)
                        embedTitle += "(M)";
                    else if (trade.Gender == 1)
                        embedTitle += "(F)";
                    if (trade.HeldItem > 0)
                        embedTitle += $" ➜ {HeldItem}";

                    embedAuthor = $"{UNick}님의 ";
                    embedAuthor += trade.IsShiny ? "**이로치** " : "";
                    embedAuthor += "포켓몬:";

                    embedMsg = $"**특성:** {(Ability)trade.Ability}";
                    embedMsg += $"\n**레벨:** {trade.CurrentLevel}";
                    embedMsg += $"\n**성격:** {(Nature)trade.Nature}";
                    embedMsg += $"\n**개체값:** {trade.IV_HP}/{trade.IV_ATK}/{trade.IV_DEF}/{trade.IV_SPA}/{trade.IV_SPD}/{trade.IV_SPE}";
                    embedMsg += $"\n**노력치:** {trade.EV_HP}/{trade.EV_ATK}/{trade.EV_DEF}/{trade.EV_SPA}/{trade.EV_SPD}/{trade.EV_SPE}";

                    embedMsg += $"\n**기술:**";
                    if (trade.Move1 != 0)
                        embedMsg += $"\n- {(Move)trade.Move1}";
                    if (trade.Move2 != 0)
                        embedMsg += $"\n- {(Move)trade.Move2}";
                    if (trade.Move3 != 0)
                        embedMsg += $"\n- {(Move)trade.Move3}";
                    if (trade.Move4 != 0)
                        embedMsg += $"\n- {(Move)trade.Move4}";
                    
                    embedMsg += $"\n\n{trader.Mention} - **통신교환 대기열에 성공적으로 추가되었습니다.**\n\n";
                    embedMsg += $"당신은 **{SysCordSettings.HubConfig.TradeAbuse.TradeCooldown}**분의 쿨다운이 지나면 교환을 다시 시작할 수 있습니다.\n\n";
                    embedMsg += $"또 이용해 주세요";

                    embedAuthorBuild.IconUrl = "https://github.com/BUMTJ/HomeImages/tree/1d632c26fe6d8b693247e9694d0ffb579f2cf8a3/Ballimg/50x50" + ((Ball)trade.Ball).ToString().ToLower() + "ball.png";
                    embedAuthorBuild.Name = embedAuthor;
                    
                    embedMsgColor = new Color((uint)Enum.Parse(typeof(embedColor), Enum.GetName(typeof(Ball), trade.Ball)));

                    embedFtr.Text = $"현재 대기열 순번은: " + SysCord<T>.Runner.Hub.Queues.Info.Count.ToString() + "입니다\n개인 DM을 확인해주세요!";

                    var botct = Info.Hub.Bots.Count;
                    if (position.Position > botct)
                    {
                        var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                        embedFtr.Text += $"\n예상 대기 시간은 {eta:F1}분 입니다.";
                    }
                    embedFtr.IconUrl = "https://github.com/BUMTJ/HomeImages/blob/1d632c26fe6d8b693247e9694d0ffb579f2cf8a3/approvalspheal.png";

                    string URLStart = "https://github.com/BUMTJ/HomeImages/tree/1d632c26fe6d8b693247e9694d0ffb579f2cf8a3/Sprites/200x200/poke_capture";
                    string URLGender;
                    string URLGMax = CanGMax ? "g" : "n";
                    string URLShiny = trade.IsShiny ? "r.png" : "n.png";

                    if (trade.Gender < 2)
                        URLGender = "mf";
                    else
                        URLGender = "uk";

                    embedThumbUrl = URLStart + "_" + trade.Species.ToString("0000") + "_" + trade.Form.ToString("000") + "_" + URLGender + "_" + URLGMax + "_" + FormArgument.ToString("00000000") + "_f_" + URLShiny;

                    try
                    {
                        using HttpResponseMessage response = await client.GetAsync(embedThumbUrl);
                        response.EnsureSuccessStatusCode();
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        if (trade.Gender == 0)
                            URLGender = "md";
                        else
                            URLGender = "fd";

                        embedThumbUrl = URLStart + "_" + trade.Species.ToString("0000") + "_" + trade.Form.ToString("000") + "_" + URLGender + "_" + URLGMax + "_" + FormArgument.ToString("00000000") + "_f_" + URLShiny;

                        try
                        {
                            using HttpResponseMessage response = await client.GetAsync(embedThumbUrl);
                            response.EnsureSuccessStatusCode();
                        }
                        catch (HttpRequestException ex1) when (ex1.StatusCode == HttpStatusCode.NotFound)
                        {
                            if (trade.Gender == 0)
                                URLGender = "mo";
                            else
                                URLGender = "fo";

                            embedThumbUrl = URLStart + "_" + trade.Species.ToString("0000") + "_" + trade.Form.ToString("000") + "_" + URLGender + "_" + URLGMax + "_" + FormArgument.ToString("00000000") + "_f_" + URLShiny;
                        }
                    }
                }

                EmbedBuilder builder = new()
                {
                    //Optional color
                    Color = embedMsgColor,
                    Author = embedAuthorBuild,
                    Title = embedTitle,
                    Description = embedMsg,
                    ThumbnailUrl = embedThumbUrl,
                    Footer = embedFtr
                };

                await context.Channel.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);

                // Notify in PM to mirror what is said in the channel.
                await trader.SendMessageAsync($"{msg}\n통신 교환 코드는 **{code:0000 0000}** 입니다.").ConfigureAwait(false);

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

        private static bool AddToTradeQueue(SocketCommandContext context, T pk, int code, string trainerName, RequestSignificance sig, PokeRoutineType type, PokeTradeType t, SocketUser trader, out string msg)
        {
            var user = trader;
            var userID = user.Id;
            var name = user.Username;

            var trainer = new PokeTradeTrainerInfo(trainerName, userID);
            var notifier = new DiscordTradeNotifier<T>(pk, trainer, code, user);
            var detail = new PokeTradeDetail<T>(pk, trainer, notifier, t, code, sig == RequestSignificance.Favored);
            var trade = new TradeEntry<T>(detail, userID, type, name);

            var hub = SysCord<T>.Runner.Hub;
            var Info = hub.Queues.Info;
            var added = Info.AddToTradeQueue(trade, userID, sig == RequestSignificance.Owner);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = "죄송합니다, 당신은 이미 대기열에 있습니다.";
                return false;
            }

            var position = Info.CheckPosition(userID, type);

            var ticketID = "";
            if (TradeStartModule<T>.IsStartChannel(context.Channel.Id))
                ticketID = $", unique ID: {detail.ID}";

            var pokeName = "";
            if (t == PokeTradeType.Specific && pk.Species != 0)
                pokeName = $" 받으실 포켓몬은: {GameInfo.GetStrings(1).Species[pk.Species]}입니다.";
            msg = $"{user.Mention} - Added to the {type} queue{ticketID}. 현재 대기열 순번은: {position.Position}입니다.{pokeName}";

            var botct = Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                msg += $" 예상 대기 시간은 {eta:F1} 분 입니다.";
            }
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
                        message = context.User == trader ? "포켓몬 교환을 이용하시려면 개인 메시지를 활성화 해야합니다!" : "해당 사용자가 개인 메시지를 활성화해야 대기열에 추가할 수 있습니다.";
                    }
                    break;
                default:
                    {
                        // Send a generic error message.
                        message = ex.DiscordCode != null ? $"디스코드 오류 {(int)ex.DiscordCode}: {ex.Reason}" : $"HTTP 오류 {(int)ex.HttpCode}: {ex.Message}";
                    }
                    break;
            }
            await context.Channel.SendMessageAsync(message).ConfigureAwait(false);
        }
    }
}
