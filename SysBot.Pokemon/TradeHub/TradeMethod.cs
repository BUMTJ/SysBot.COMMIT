namespace SysBot.Pokemon
{
    /// <summary>
    /// Differentiates the different types of player initiated in-game trades.
    /// </summary>
    public enum TradeMethod
    {
        /// <summary>
        /// Trades between specific players
        /// </summary>
        통신교환,

        /// <summary>
        /// Trades between randomly matched players
        /// </summary>
        SurpriseTrade,
    }
}
