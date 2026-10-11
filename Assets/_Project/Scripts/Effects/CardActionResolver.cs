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

            // Self = ตัวการ์ดเอง ไม่ว่าจะอยู่ในสนามหรือไม่
            // (ใช้กับ Spell, ยูนิตที่ตายแล้วยิง Death ฯลฯ)
            if (action.Target == TargetRule.Self)
            {
                if (context.SourceCard != null)
                {
                    results.Add(
                        new TargetCandidate(
                            ownerPlayerIndex,
                            false,
                            true,
                            context.SourceCard));
                }

                return results;
            }

            // EventSource = การ์ดต้นเหตุของ Trigger (เช่น ผู้ที่เล็งเป้าใส่เรา) ต้องยังอยู่บนสนาม
            if (action.Target == TargetRule.EventSource)
            {
                CardInstance source = context.TargetCard;

                if (source != null && source.Zone == CardZone.Field)
                {
                    results.Add(
                        new TargetCandidate(
                            source.OwnerID,
                            false,
                            true,
                            source));
                }

                return results;
            }

            // AllyByCardID = ยูนิตฝ่ายเราที่ระบุด้วย CardID (เช่น คู่หูของ Kiana / Mei)
            if (action.Target == TargetRule.AllyByCardID)
            {
                foreach (CardInstance ally in context.SourcePlayer.Field)
                {
                    if (ally != context.SourceCard &&
                        ally.CurrentHealth > 0 &&
                        ally.Data != null &&
                        ally.Data.CardID == action.TargetCardID)
                    {
                        results.Add(
                            new TargetCandidate(
                                ownerPlayerIndex,
                                false,
                                true,
                                ally));
                    }
                }

                return results;
            }

            // Resolve Target
            results = TargetResolver.Resolve(
                action.Target,
                ownerPlayerIndex,
                candidates);

            // All = ทุกเป้าหมายที่เข้าเงื่อนไข (พฤติกรรมเดิม)
            if (action.Selection == TargetSelection.All)
                return results;

            // Chosen / Random: ต้องตัดยูนิตที่ Stealth ออก
            results = TargetResolver.RemoveHiddenTargets(
                results,
                ownerPlayerIndex);

            if (results.Count == 0)
                return results;

            // Chosen: ใช้เป้าหมายที่ผู้เล่นเลือกถ้าถูกต้อง
            if (action.Selection == TargetSelection.Chosen &&
                context.ChosenTarget != null)
            {
                foreach (TargetCandidate candidate in results)
                {
                    if (candidate.TargetObject ==
                        context.ChosenTarget.TargetObject)
                    {
                        return new List<TargetCandidate> { candidate };
                    }
                }
            }

            // Random (หรือ Chosen ที่ไม่มีเป้าหมายที่เลือกไว้): สุ่ม 1 เป้าหมาย
            int index = UnityEngine.Random.Range(0, results.Count);

            return new List<TargetCandidate> { results[index] };
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
            if (action.Conditions != null)
            {
                foreach (ConditionData condition in action.Conditions)
                {
                    if (!ConditionEvaluator.Evaluate(
                        condition,
                        sourceContext))
                    {
                        return;
                    }
                }
            }

            // หา Target
            List<TargetCandidate> targets =
                Resolve(
                    action,
                    sourceContext,
                    candidates);

            // บอก BattleManager ว่าตอนนี้ใครเป็นต้นเหตุของเอฟเฟกต์ (แยกจากการต่อสู้)
            BattleManager battle = sourceContext.Battle;

            CardInstance previousSource = null;
            BattlePlayer previousPlayer = null;

            if (battle != null)
            {
                previousSource = battle.ActiveEffectSource;
                previousPlayer = battle.ActiveEffectPlayer;

                battle.ActiveEffectSource = sourceContext.SourceCard;
                battle.ActiveEffectPlayer = sourceContext.SourcePlayer;
            }

            // Execute ทุก Effect กับทุก Target
            foreach (TargetCandidate target in targets)
            {
                // Ward กัน Effect ที่เลือกเป้าหมายจากศัตรูได้ 1 ครั้ง
                if (IsWardBlocked(action, sourceContext, target))
                    continue;

                NotifyTargeted(action, sourceContext, target);

                EffectContext effectContext =
                    CreateTargetContext(
                        sourceContext,
                        target);

                if (effectContext == null)
                    continue;

                if (action.Effects == null)
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

            if (battle != null)
            {
                battle.ActiveEffectSource = previousSource;
                battle.ActiveEffectPlayer = previousPlayer;
            }
        }

        // เอฟเฟกต์ที่ "เลือก" เป้าหมาย (Chosen / Random) แจ้งยูนิตเป้าหมายว่าถูกเล็ง (OnTarget)
        private static void NotifyTargeted(
            CardActionData action,
            EffectContext sourceContext,
            TargetCandidate target)
        {
            if (action.Selection == TargetSelection.All ||
                sourceContext.Battle == null ||
                sourceContext.SourceCard == null)
                return;

            CardInstance card = target.TargetObject as CardInstance;

            if (card == null || card == sourceContext.SourceCard ||
                card.Zone != CardZone.Field)
                return;

            // เป้าหมายที่ผู้เล่นเลือกตอนลงการ์ด PlayCardCommand แจ้งไปแล้ว
            if (action.Trigger == TriggerType.Play &&
                sourceContext.ChosenTarget != null &&
                sourceContext.ChosenTarget.TargetObject == target.TargetObject)
                return;

            sourceContext.Battle.Publish(
                TriggerType.OnTarget,
                card: card,
                player: sourceContext.Battle.GetPlayer(card.OwnerID),
                target: sourceContext.SourceCard,
                targetPlayer: sourceContext.SourcePlayer);
        }

        private static bool IsWardBlocked(
            CardActionData action,
            EffectContext sourceContext,
            TargetCandidate target)
        {
            // เฉพาะ Effect ที่ "เลือก" เป้าหมาย (Chosen / Random) ไม่ใช่ทุกเป้าหมาย
            if (action.Selection == TargetSelection.All)
                return false;

            CardInstance card = target.TargetObject as CardInstance;

            if (card == null || !card.HasKeyword(Keyword.Ward))
                return false;

            if (sourceContext.SourcePlayer == null ||
                card.OwnerID == sourceContext.SourcePlayer.PlayerID)
                return false;

            card.RemoveKeyword(Keyword.Ward);

            if (sourceContext.Battle != null)
            {
                sourceContext.Battle.Log(
                    card.Data.CardName + "'s ward blocks the effect");
            }

            return true;
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

            context.CardPool =
                sourceContext.CardPool;

            context.ChosenTarget =
                sourceContext.ChosenTarget;

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