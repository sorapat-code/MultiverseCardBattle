using UnityEngine;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Effects;

namespace MultiverseCardBattle.Conditions
{
    public static class ConditionEvaluator
    {
        public static bool Evaluate(
            ConditionData condition,
            EffectContext context)
        {
            if (condition == null)
                return false;

            if (context == null)
                return false;

            BattlePlayer player = context.SourcePlayer;

            if (player == null)
                return false;

            switch (condition.Condition)
            {
                case ConditionType.None:
                    return true;

                // Turn
                case ConditionType.TurnNumber:
                    if (context.Battle == null ||
                        context.Battle.TurnManager == null)
                        return false;

                    return context.Battle.TurnManager.TurnNumber
                           >= condition.Value;

                case ConditionType.IsFirstTurn:
                    if (context.Battle == null ||
                        context.Battle.TurnManager == null)
                        return false;

                    return context.Battle.TurnManager.TurnNumber == 1;

                // HP
                case ConditionType.SelfHealthLessThan:
                    return player.LeaderHealth < condition.Value;

                case ConditionType.SelfHealthGreaterThan:
                    return player.LeaderHealth > condition.Value;

                // Energy
                case ConditionType.EnergyGreaterThan:
                    return player.CurrentEnergy > condition.Value;

                case ConditionType.EnergyLessThan:
                    return player.CurrentEnergy < condition.Value;

                // Cards
                case ConditionType.HandCount:
                    return player.Hand.Count >= condition.Value;

                case ConditionType.DeckCount:
                    return player.Deck.Count >= condition.Value;

                case ConditionType.GraveyardCount:
                    return player.Graveyard.Count >= condition.Value;

                // Board
                case ConditionType.AllyUnitCount:
                    return player.Field.Count >= condition.Value;

                case ConditionType.EnemyUnitCount:
                    BattlePlayer enemyPlayer =
                        GetEnemyPlayer(context);

                    if (enemyPlayer == null)
                        return false;

                    return enemyPlayer.Field.Count >= condition.Value;

                //Chance
                case ConditionType.Chance:
                     return Random.Range(0, 100) < condition.Value;

                case ConditionType.HasTag:
                    if (context.TargetCard == null ||
                        context.TargetCard.Data == null)
                        return false;

                    CardTag tag =
                        (CardTag)condition.Value;

                    return (context.TargetCard.Data.Tags & tag) == tag;

                case ConditionType.HasFaction:
                    if (context.TargetCard == null ||
                        context.TargetCard.Data == null)
                        return false;

                    return (int)context.TargetCard.Data.Faction ==
                        condition.Value;

                case ConditionType.HasUniverse:
                    if (context.TargetCard == null ||
                        context.TargetCard.Data == null)
                        return false;

                    return (int)context.TargetCard.Data.Universe ==
                        condition.Value;

                default:
                    return false;
            }
        }

        private static BattlePlayer GetEnemyPlayer(
            EffectContext context)
        {
            if (context.Battle == null ||
                context.SourcePlayer == null)
                return null;

            if (context.SourcePlayer ==
                context.Battle.Player1)
            {
                return context.Battle.Player2;
            }

            return context.Battle.Player1;
        }
    }
}