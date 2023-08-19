using PKHeX.Core;

namespace SysBot.Pokemon
{
    public static class ShowdownUtil
    {
        /// <summary>
        /// Converts a single line to a showdown set
        /// </summary>
        /// <param name="setstring">single string</param>
        /// <returns>ShowdownSet object</returns>
        public static ShowdownSet? ConvertToShowdown(string setstring)
        {
            // LiveStreams remove new lines, so we are left with a single line set
            var restorenick = string.Empty;

            var nickIndex = setstring.LastIndexOf(')');
            if (nickIndex > -1)
            {
                restorenick = setstring[..(nickIndex + 1)];
                if (restorenick.TrimStart().StartsWith("("))
                    return null;
                setstring = setstring[(nickIndex + 1)..];
            }

            foreach (string i in splittables)
            {
                if (setstring.Contains(i))
                    setstring = setstring.Replace(i, $"\r\n{i}");
            }

            var finalset = restorenick + setstring;
            return new ShowdownSet(finalset);
        }

        private static readonly string[] splittables =
        {
            "특성:", "노력치:", "개체값:", "이로치:", "기간타맥스:", "포켓볼:", "- ", "레벨:",
            "Happiness:", "국적:", "어버이:", "어버이성별:", "TID:", "SID:", "알파:", "테라타입:",
            "고집", "수줍음", "용감", "대담", "차분",
            "신중", "온순", "얌전", "노력", "성급",
            "장난꾸러기", "명랑", "촐랑", "외로운", "의젓",
            "조심", "천진난만", "개구쟁이", "냉정", "변덕",
            "덜렁", "무사태평", "건방", "성실", "겁쟁이",
        };
    }
}
