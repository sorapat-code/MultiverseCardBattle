using System;
using System.Collections.Generic;
using System.Text;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// สร้างข้อความอธิบายการ์ดจากข้อมูล (Keyword + Action) อัตโนมัติ
    /// ใช้เมื่อ CardData.Description ว่าง ทำให้การ์ดใหม่ที่ประกอบจาก
    /// Trigger/Target/Effect มีคำอธิบายทันทีโดยไม่ต้องเขียนเอง
    /// </summary>
    public static class CardTextGenerator
    {
        /// <summary>
        /// ข้อความเต็มของการ์ด: Keyword + คำอธิบาย Action
        /// ถ้ามี CardData.Description ที่เขียนเอง จะใช้ข้อความนั้นแทน
        /// </summary>
        public static string Describe(CardData data)
        {
            if (data == null)
                return "";

            if (!string.IsNullOrEmpty(data.Description))
                return data.Description;

            string keywords = KeywordText(data.Keywords);
            string actions = ActionsText(data);

            if (keywords.Length > 0 && actions.Length > 0)
                return keywords + "\n" + actions;

            return keywords.Length > 0 ? keywords : actions;
        }

        /// <summary>
        /// เฉพาะส่วนอธิบาย Action (ไม่รวม Keyword) ใช้ Description ที่เขียนเองก่อนถ้ามี
        /// </summary>
        public static string ActionsText(CardData data)
        {
            if (data == null)
                return "";

            if (!string.IsNullOrEmpty(data.Description))
                return data.Description;

            StringBuilder text = new StringBuilder();

            if (data.Actions == null)
                return "";

            foreach (CardActionData action in data.Actions)
            {
                if (action == null || action.Effects == null)
                    continue;

                List<string> parts = new List<string>();

                foreach (EffectData effect in action.Effects)
                {
                    string phrase = EffectText(effect, action);

                    if (phrase.Length > 0)
                        parts.Add(phrase);
                }

                if (parts.Count == 0)
                    continue;

                if (text.Length > 0)
                    text.Append('\n');

                string prefix = TriggerPrefix(action.Trigger);

                if (prefix.Length > 0)
                    text.Append(prefix).Append(' ');

                text.Append(string.Join(", ", parts.ToArray())).Append('.');
            }

            return text.ToString();
        }

        public static string KeywordText(Keyword keywords)
        {
            if (keywords == Keyword.None)
                return "";

            List<string> names = new List<string>();

            foreach (Keyword keyword in Enum.GetValues(typeof(Keyword)))
            {
                if (keyword == Keyword.None)
                    continue;

                if ((keywords & keyword) == keyword)
                    names.Add(keyword.ToString());
            }

            return string.Join(", ", names.ToArray());
        }

        private static string TriggerPrefix(TriggerType trigger)
        {
            switch (trigger)
            {
                case TriggerType.Play: return "Play:";
                case TriggerType.Summon: return "On summon:";
                case TriggerType.Death: return "Death:";
                case TriggerType.Draw: return "On draw:";
                case TriggerType.Discard: return "On discard:";
                case TriggerType.TurnStart: return "Turn start:";
                case TriggerType.TurnEnd: return "Turn end:";
                case TriggerType.BattleStart: return "Battle start:";
                case TriggerType.BattleEnd: return "Battle end:";
                case TriggerType.Attack: return "On attack:";
                case TriggerType.Hit: return "On hit:";
                case TriggerType.DamageTaken: return "When damaged:";
                case TriggerType.Heal: return "When healed:";
                case TriggerType.LeaderAttack: return "On leader attack:";
                case TriggerType.OnTarget: return "When targeted:";
                default: return "";
            }
        }

        private static string TargetText(CardActionData action)
        {
            bool chosen = action.Selection == TargetSelection.Chosen;
            bool random = action.Selection == TargetSelection.Random;

            switch (action.Target)
            {
                case TargetRule.Self:
                    return "itself";

                case TargetRule.AllyLeader:
                    return "your leader";

                case TargetRule.EnemyLeader:
                    return "the enemy leader";

                case TargetRule.AnyLeader:
                    return "a leader";

                case TargetRule.AllyUnit:
                    if (chosen) return "an ally unit";
                    if (random) return "a random ally unit";
                    return "all your units";

                case TargetRule.EnemyUnit:
                    if (chosen) return "an enemy unit";
                    if (random) return "a random enemy unit";
                    return "all enemy units";

                case TargetRule.AnyUnit:
                    if (chosen) return "a unit";
                    if (random) return "a random unit";
                    return "all units";

                case TargetRule.AllAllies:
                    return "all your units";

                case TargetRule.AllEnemies:
                    return "all enemy units";

                case TargetRule.AllUnits:
                    return "all units";

                default:
                    return "the target";
            }
        }

        private static string EffectText(EffectData effect, CardActionData action)
        {
            if (effect == null)
                return "";

            string target = TargetText(action);
            int value = effect.Value;

            switch (effect.Type)
            {
                case EffectType.DealDamage:
                    return "deal " + value + " damage to " + target;

                case EffectType.DealTrueDamage:
                    return "deal " + value + " true damage to " + target;

                case EffectType.Heal:
                    return "heal " + target + " for " + value;

                case EffectType.GainAttack:
                    return "give " + target + " " + Signed(value) + " attack";

                case EffectType.GainHealth:
                    return "give " + target + " " + Signed(value) + " health";

                case EffectType.SetAttack:
                    return "set " + target + " attack to " + value;

                case EffectType.SetHealth:
                    return "set " + target + " health to " + value;

                case EffectType.ReduceCost:
                    return "reduce " + target + " cost by " + value;

                case EffectType.IncreaseCost:
                    return "increase " + target + " cost by " + value;

                case EffectType.SetCost:
                    return "set " + target + " cost to " + value;

                case EffectType.DrawCard:
                    return "draw " + value + (value == 1 ? " card" : " cards");

                case EffectType.DiscardCard:
                    return "discard a card";

                case EffectType.CreateCard:
                    return "add " + CardName(effect.Card) + " to your hand";

                case EffectType.DestroyCard:
                    return "destroy " + target;

                case EffectType.Summon:
                    if (effect.Card != null)
                        return "summon " + CardName(effect.Card);
                    return "summon " + target;

                case EffectType.ReturnToHand:
                    return "return " + target + " to hand";

                case EffectType.MoveToDeck:
                    return "shuffle " + target + " into the deck";

                case EffectType.Banish:
                    return "banish " + target;

                case EffectType.AddKeyword:
                    return "give " + target + " " + KeywordText(effect.Keyword);

                case EffectType.RemoveKeyword:
                    return "remove " + KeywordText(effect.Keyword) + " from " + target;

                case EffectType.Silence:
                    return "silence " + target;

                case EffectType.Stun:
                    return "stun " + target;

                case EffectType.Burn:
                    return "burn " + target + " for " + value;

                case EffectType.Regen:
                    return "give " + target + " regen " + value;

                case EffectType.GainEnergy:
                    return "gain " + value + " energy";

                case EffectType.SpendEnergy:
                    return "lose " + value + " energy";

                case EffectType.Discover:
                    return "discover a card";

                case EffectType.Transform:
                    return "transform " + target + " into " + CardName(effect.Card);

                default:
                    return "";
            }
        }

        private static string CardName(CardData card)
        {
            return card == null ? "a card" : card.CardName;
        }

        private static string Signed(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }
    }
}
