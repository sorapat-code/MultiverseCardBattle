using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Effects
{
    public static class DiscoverExecutor
    {
        public static bool Execute(
            DiscoverResult result,
            BattlePlayer player)
        {
            if (result == null)
                return false;

            if (player == null)
                return false;

            if (!result.HasSelection)
                return false;

            CardInstance card =
                result.CreateSelectedInstance();

            if (card == null)
                return false;

            if (!player.Hand.Add(card))
                return false;

            return true;
        }
    }
}