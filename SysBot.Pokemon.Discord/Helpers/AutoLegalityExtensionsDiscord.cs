using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public static class AutoLegalityExtensionsDiscord
    {
        public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, ITrainerInfo sav, ShowdownSet set)
        {
            if (set.Species <= 0)
            {
<<<<<<< HEAD
                await channel.SendMessageAsync("이런! 보내주신 메시지를 해석할 수 없었습니다! 만약 무언가를 변환하려고 했다면, 붙여넣고 있는 것을 다시 한번 확인해 주세요!").ConfigureAwait(false);
=======
                await channel.SendMessageAsync("이런! 당신의 메시지를 해석할 수 없었습니다! 만약 당신이 무언가를 변환하려고 했다면, 당신이 붙여넣고 있는 것을 다시 한번 확인해 주세요!").ConfigureAwait(false);
>>>>>>> cb5d35362ce394505b43c5ae36835a46d034f2ad
                return;
            }

            try
            {
                var template = AutoLegalityWrapper.GetTemplate(set);
                var pkm = sav.GetLegal(template, out var result);
                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];
                if (!la.Valid)
                {
<<<<<<< HEAD
                    var reason = result == "시간초과" ? $"해당 {spec} 세트를 생성하는데 너무 오래걸렸습니다." : result == "버전 불일치" ? "요청이 거부되었습니다: 자동 합법성 모드 버전이 일치하지 않습니다." : $"해당 {spec} 세트를 생성할 수 없습니다.";
=======
                    var reason = result == "시간초과" ? $"보내주신 {spec} 세트를 생성하는데 너무 오래 걸립니다." : result == "버전 불일치" ? "요청 거부 : 자동 합법성 모드에서 거부되었습니다." : $"해당 {spec} 세트 포켓몬을 생성할 수 없습니다.";
>>>>>>> cb5d35362ce394505b43c5ae36835a46d034f2ad
                    var imsg = $"이런! {reason}";
                    if (result == "Failed")
                        imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                    await channel.SendMessageAsync(imsg).ConfigureAwait(false);
                    return;
                }

<<<<<<< HEAD
                var msg = $"여기 ({result}) 자동합법화된 파일입니다. {spec} ({la.EncounterOriginal.Name})!";
=======
                var msg = $"여기 있습니다. ({result}) {spec} ({la.EncounterOriginal.Name})에 대해 합법화된 PKM 파일 입니다!";
>>>>>>> cb5d35362ce394505b43c5ae36835a46d034f2ad
                await channel.SendPKMAsync(pkm, msg + $"\n{ReusableActions.GetFormattedShowdownText(pkm)}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(AutoLegalityExtensionsDiscord));
<<<<<<< HEAD
                var msg = $"이런! 해당 쇼다운 세트에서 예기치 못한 문제가 발생했습니다.:\n```{string.Join("\n", set.GetSetLines())}```";
=======
                var msg = $"이런! 이 쇼다운 세트에 예기치 않은 문제가 발생했습니다:\n```{string.Join("\n", set.GetSetLines())}```";
>>>>>>> cb5d35362ce394505b43c5ae36835a46d034f2ad
                await channel.SendMessageAsync(msg).ConfigureAwait(false);
            }
        }

        public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, string content, int gen)
        {
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var sav = AutoLegalityWrapper.GetTrainerInfo(gen);
            await channel.ReplyWithLegalizedSetAsync(sav, set).ConfigureAwait(false);
        }

        public static async Task ReplyWithLegalizedSetAsync<T>(this ISocketMessageChannel channel, string content) where T : PKM, new()
        {
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
            await channel.ReplyWithLegalizedSetAsync(sav, set).ConfigureAwait(false);
        }

        public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, IAttachment att)
        {
            var download = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
            if (!download.Success)
            {
                await channel.SendMessageAsync(download.ErrorMessage).ConfigureAwait(false);
                return;
            }

            var pkm = download.Data!;
            if (new LegalityAnalysis(pkm).Valid)
            {
                await channel.SendMessageAsync($"{download.SanitizedFileName}: 이미 합법적입니다.").ConfigureAwait(false);
                return;
            }

            var legal = pkm.LegalizePokemon();
            if (!new LegalityAnalysis(legal).Valid)
            {
                await channel.SendMessageAsync($"{download.SanitizedFileName}: 합법화할 수 없습니다..").ConfigureAwait(false);
                return;
            }

            legal.RefreshChecksum();

            var msg = $"여기 합법화된 PKM파일 입니다! {download.SanitizedFileName}!\n{ReusableActions.GetFormattedShowdownText(legal)}";
            await channel.SendPKMAsync(legal, msg).ConfigureAwait(false);
        }
    }
}