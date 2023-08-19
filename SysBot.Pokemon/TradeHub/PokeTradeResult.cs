namespace SysBot.Pokemon
{
    public enum PokeTradeResult
    {
        Success,

        // Trade Partner Failures
        트레이너를찾지못함,
        트레이너가너무느림,
        트레이너가떠남,
        트레이너제안이취소됨,
        트레이너요청이잘못됨,
        불법거래,
        의심스러운활동,

        // Recovery -- General Bot Failures
        // Anything below here should be retried once if possible.
        RoutineCancel,
        ExceptionConnection,
        ExceptionInternal,
        RecoverStart,
        RecoverPostLinkCode,
        RecoverOpenBox,
        RecoverReturnOverworld,
        RecoverEnterUnionRoom,
    }

    public static class PokeTradeResultExtensions
    {
        public static bool ShouldAttemptRetry(this PokeTradeResult t) => t >= PokeTradeResult.RoutineCancel;
    }
}
