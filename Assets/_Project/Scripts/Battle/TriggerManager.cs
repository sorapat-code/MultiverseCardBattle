using System.Collections.Generic;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Effects;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// ฟัง EventBus แล้วสั่งให้ Action ของการ์ดที่มี Trigger ตรงกันทำงาน
    ///
    ///  - Trigger เกี่ยวกับตัวการ์ดเอง (Draw, Play, Summon, Discard, Death,
    ///    Attack, Hit, DamageTaken, Heal, LeaderAttack, OnTarget)
    ///    ทำงานเฉพาะการ์ดที่เป็นเจ้าของเหตุการณ์ (event.Card)
    ///  - TurnStart / TurnEnd ทำงานกับยูนิตบนสนามของผู้เล่นที่เป็นเจ้าของเทิร์น
    ///  - BattleStart / BattleEnd ทำงานกับการ์ดในมือและบนสนามของทั้งสองฝั่ง
    /// </summary>
    public class TriggerManager
    {
        private readonly BattleManager battle;

        public TriggerManager(BattleManager battle, EventBus bus)
        {
            this.battle = battle;

            bus.Raised += Handle;
        }

        private void Handle(GameEvent e)
        {
            switch (e.Trigger)
            {
                case TriggerType.Summon:
                    Fire(e.Card, e);
                    FireAllySummoned(e);
                    break;

                case TriggerType.Draw:
                case TriggerType.Play:
                case TriggerType.Discard:
                case TriggerType.Death:
                case TriggerType.Attack:
                case TriggerType.Hit:
                case TriggerType.DamageTaken:
                case TriggerType.Heal:
                case TriggerType.LeaderAttack:
                case TriggerType.OnTarget:
                case TriggerType.Graveyard:
                case TriggerType.AfterAttack:
                    Fire(e.Card, e);
                    break;

                case TriggerType.Spellburst:
                    FireSpellburst(e);
                    break;

                case TriggerType.TurnStart:
                case TriggerType.TurnEnd:
                    if (e.Player != null)
                    {
                        foreach (CardInstance card in
                            new List<CardInstance>(e.Player.Field))
                        {
                            Fire(card, e);
                        }

                        // อุปกรณ์ Leader ทำงานต้น/จบเทิร์นของเจ้าของ
                        if (e.Player.LeaderGear != null)
                            Fire(e.Player.LeaderGear, e);
                    }
                    break;

                case TriggerType.BattleStart:
                case TriggerType.BattleEnd:
                    FireForPlayer(battle.Player1, e);
                    FireForPlayer(battle.Player2, e);
                    break;
            }
        }

        // ยูนิตอื่นฝั่งเดียวกันที่อยู่บนสนามอยู่แล้ว ได้ยินว่ามีเพื่อนลงสนาม
        private void FireAllySummoned(GameEvent e)
        {
            if (e.Card == null || e.Player == null)
                return;

            foreach (CardInstance listener in
                new List<CardInstance>(e.Player.Field))
            {
                if (listener == e.Card)
                    continue;

                GameEvent heard = new GameEvent();

                heard.Trigger = TriggerType.AllySummoned;
                heard.Card = listener;
                heard.Player = e.Player;
                heard.Target = e.Card;

                Fire(listener, heard);
            }
        }

        // Spellburst: ยูนิตที่มี Keyword Spellburst ของผู้ใช้ Spell ทำงานครั้งเดียวแล้ว Keyword หาย
        private void FireSpellburst(GameEvent e)
        {
            if (e.Player == null)
                return;

            foreach (CardInstance card in
                new List<CardInstance>(e.Player.Field))
            {
                if (!card.HasKeyword(Keyword.Spellburst))
                    continue;

                Fire(card, e);

                card.RemoveKeyword(Keyword.Spellburst);
            }
        }

        // Stun ปิดความสามารถแบบ Active (ต้นเทิร์น / จบเทิร์น / ตอนโจมตี)
        private static bool IsActiveTrigger(TriggerType trigger)
        {
            return trigger == TriggerType.TurnStart ||
                   trigger == TriggerType.TurnEnd ||
                   trigger == TriggerType.Attack ||
                   trigger == TriggerType.AfterAttack ||
                   trigger == TriggerType.Hit ||
                   trigger == TriggerType.LeaderAttack;
        }

        private void FireForPlayer(BattlePlayer player, GameEvent e)
        {
            if (player == null)
                return;

            foreach (CardInstance card in
                new List<CardInstance>(player.Hand.Cards))
            {
                Fire(card, e);
            }

            foreach (CardInstance card in
                new List<CardInstance>(player.Field))
            {
                Fire(card, e);
            }
        }

        // OnTarget: ตรวจว่าถูกเล็งด้วยอะไร (โจมตี / Spell-ความสามารถ) และโดยฝ่ายไหน
        private static bool MatchesTargeted(
            CardActionData action,
            CardInstance card,
            GameEvent e)
        {
            if (action.TargetedBy == TargetedSource.Attack && !e.ByAttack)
                return false;

            if (action.TargetedBy == TargetedSource.SpellOrAbility && e.ByAttack)
                return false;

            if (action.TargetedFrom == TargetedSide.Any)
                return true;

            int sourceOwner =
                e.TargetPlayer != null ? e.TargetPlayer.PlayerID
                : (e.Target != null ? e.Target.OwnerID : card.OwnerID);

            bool enemy = sourceOwner != card.OwnerID;

            return action.TargetedFrom == TargetedSide.Enemy ? enemy : !enemy;
        }

        private void Fire(CardInstance card, GameEvent e)
        {
            if (card == null || card.Data == null)
                return;

            List<CardActionData> actions = card.Data.Actions;

            if (actions == null || actions.Count == 0)
                return;

            // การ์ดที่ถูก Silence จะไม่ทำ Action ใดๆ
            if (card.IsSilenced)
                return;

            if (card.IsStunned && IsActiveTrigger(e.Trigger))
                return;

            BattlePlayer owner = battle.GetPlayer(card.OwnerID);

            if (owner == null)
                return;

            for (int i = 0; i < actions.Count; i++)
            {
                CardActionData action = actions[i];

                if (action == null || action.Trigger != e.Trigger)
                    continue;

                if (e.Trigger == TriggerType.OnTarget && !MatchesTargeted(action, card, e))
                    continue;

                EffectContext context = new EffectContext();

                context.SourceCard = card;
                context.SourcePlayer = owner;
                context.TargetCard = e.Target;
                context.TargetPlayer = e.TargetPlayer;
                context.Battle = battle;
                context.CardPool = battle.DiscoverPool;
                context.ChosenTarget = e.ChosenTarget;

                CardActionResolver.ExecuteAction(
                    action,
                    context,
                    BattleRules.BuildCandidates(battle));
            }
        }
    }
}
