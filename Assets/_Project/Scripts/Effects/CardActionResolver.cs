using System.Collections.Generic;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Conditions;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Effects
{
    public static class CardActionResolver
    {
        public static List<TargetCandidate> Resolve(
            CardActionData action,
            EffectContext context,
            List<TargetCandidate> candidates)
        {
            List<TargetCandidate> results =
                new List<TargetCandidate>();

            if (action == null)
                return results;

            if (context == null)
                return results;

            if (context.SourcePlayer == null)
                return results;


            // หา Owner Player ID
            int ownerPlayerIndex =
                context.SourcePlayer.PlayerID;

            // Resolve Target
            results = TargetResolver.Resolve(
                action.Target,
                ownerPlayerIndex,
                candidates);

            return results;
        }

        public static void ExecuteAction(
            CardActionData action,
            EffectContext sourceContext,
            List<TargetCandidate> candidates)
        {
            if (action == null)
                return;

            if (sourceContext == null)
                return;

            if (sourceContext.SourcePlayer == null)
                return;

            // ตรวจ Conditions ก่อน
            foreach (ConditionData condition in action.Conditions)
            {
                if (!ConditionEvaluator.Evaluate(
                    condition,
                    sourceContext))
                {
                    return;
                }
            }

            // หา Target
            List<TargetCandidate> targets =
                Resolve(
                    action,
                    sourceContext,
                    candidates);

            // Execute ทุก Effect กับทุก Target
            foreach (TargetCandidate target in targets)
            {
                EffectContext effectContext =
                    CreateTargetContext(
                        sourceContext,
                        target);

                if (effectContext == null)
                    continue;

                foreach (EffectData effect in action.Effects)
                {
                    if (effect == null)
                        continue;

                    EffectExecutor.Execute(
                        effect,
                        effectContext);
                }
            }
        }

        private static EffectContext CreateTargetContext(
            EffectContext sourceContext,
            TargetCandidate target)
        {
            if (target == null)
                return null;

            EffectContext context =
                new EffectContext();

            context.SourceCard =
                sourceContext.SourceCard;

            context.SourcePlayer =
                sourceContext.SourcePlayer;

            context.Battle =
                sourceContext.Battle;

            // Target เป็น CardInstance
            if (target.TargetObject is CardInstance card)
            {
                context.TargetCard = card;
                context.TargetPlayer =
                    FindPlayer(
                        sourceContext.Battle,
                        target.PlayerIndex);

                return context;
            }

            // Target เป็น BattlePlayer
            if (target.TargetObject is BattlePlayer player)
            {
                context.TargetPlayer = player;

                return context;
            }

            return null;
        }

        private static BattlePlayer FindPlayer(
            BattleManager battle,
            int playerIndex)
        {
            if (battle == null)
                return null;

            if (playerIndex == 1)
                return battle.Player1;

            if (playerIndex == 2)
                return battle.Player2;

            return null;
        }
    }
}