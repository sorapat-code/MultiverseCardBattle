using System;
using System.Collections.Generic;
using System.Text;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Conditions;
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

            string keywords = KeywordText(
                data.Keywords, data.EffectiveImmuneAmount, data.EffectiveOverloadAmount);
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

                string prefix = ActionPrefix(action, data);

                if (prefix.Length > 0)
                    text.Append(prefix).Append(' ');

                string conditions = ConditionsText(action);

                if (conditions.Length > 0)
                    text.Append(conditions).Append(", ");

                text.Append(string.Join(", ", parts.ToArray())).Append('.');
            }

            // Aura: ให้ยูนิตพวกเดียวกันตัวอื่นขณะอยู่บนสนาม
            if (data.AuraAttack != 0 || data.AuraHealth != 0)
            {
                if (text.Length > 0)
                    text.Append('\n');

                text.Append("Aura: other allies get ")
                    .Append(Signed(data.AuraAttack)).Append('/')
                    .Append(Signed(data.AuraHealth)).Append('.');
            }

            // Evolve: เปลี่ยนร่างเมื่ออยู่บนสนามครบเทิร์น
            if (data.EvolvesInto != null)
            {
                if (text.Length > 0)
                    text.Append('\n');

                text.Append("Evolve: becomes ")
                    .Append(data.EvolvesInto.CardName)
                    .Append(" after ").Append(data.EvolveAfterTurns)
                    .Append(data.EvolveAfterTurns == 1 ? " turn." : " turns.");
            }

            return text.ToString();
        }

        public static string KeywordText(
            Keyword keywords,
            int immuneAmount = CardData.DefaultImmuneAmount,
            int overloadAmount = CardData.DefaultOverloadAmount)
        {
            if (keywords == Keyword.None)
                return "";

            List<string> names = new List<string>();

            foreach (Keyword keyword in Enum.GetValues(typeof(Keyword)))
            {
                if (keyword == Keyword.None)
                    continue;

                if ((keywords & keyword) != keyword)
                    continue;

                // Immune แสดงจำนวนดาเมจที่ลดได้ เช่น "Immune 3"
                names.Add(
                    keyword == Keyword.Immune
                        ? KeywordName(keyword) + " " + immuneAmount
                        : (keyword == Keyword.Overload
                            ? KeywordName(keyword) + " " + overloadAmount
                            : KeywordName(keyword)));
            }

            return string.Join(", ", names.ToArray());
        }

        /// <summary>ชื่อ Keyword สำหรับแสดงผล เช่น QuickAttack -> Quick Attack</summary>
        public static string KeywordName(Keyword keyword)
        {
            string raw = keyword.ToString();
            StringBuilder name = new StringBuilder();

            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && char.IsUpper(raw[i]) && char.IsLower(raw[i - 1]))
                    name.Append(' ');

                name.Append(raw[i]);
            }

            return name.ToString();
        }

        // Battlecry / Deathrattle / Combo / Spellburst ได้จาก Trigger และเงื่อนไขของ Action
        private static string ActionPrefix(CardActionData action, CardData data)
        {
            if (action.Conditions != null)
            {
                foreach (ConditionData condition in action.Conditions)
                {
                    if (condition != null && condition.Condition == ConditionType.Combo)
                        return "Combo:";
                }
            }

            if (action.Trigger == TriggerType.Play && data.Type == CardType.Unit)
                return "Battlecry:";

            if (action.Trigger == TriggerType.Death && data.Type == CardType.Unit)
                return "Deathrattle:";

            if (action.Trigger == TriggerType.Spellburst)
                return "Spellburst:";

            if (action.Trigger == TriggerType.OnTarget)
                return TargetedPrefix(action);

            return TriggerPrefix(action.Trigger);
        }

        // "When targeted by an enemy attack:" ฯลฯ ตามตัวกรองของ Action
        private static string TargetedPrefix(CardActionData action)
        {
            bool enemy = action.TargetedFrom == TargetedSide.Enemy;
            bool ally = action.TargetedFrom == TargetedSide.Ally;

            if (action.TargetedBy == TargetedSource.Attack)
            {
                return "When targeted by " +
                    (enemy ? "an enemy attack:" : (ally ? "an allied attack:" : "an attack:"));
            }

            if (action.TargetedBy == TargetedSource.SpellOrAbility)
            {
                return "When targeted by " +
                    (enemy ? "an enemy spell or ability:"
                        : (ally ? "an allied spell or ability:" : "a spell or ability:"));
            }

            if (enemy)
                return "When targeted by an enemy:";

            if (ally)
                return "When targeted by an ally:";

            return "When targeted:";
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
                case TriggerType.Graveyard: return "When sent to the Graveyard:";
                case TriggerType.AfterAttack: return "After attacking:";
                case TriggerType.Spellburst: return "Spellburst:";
                default: return "";
            }
        }

        // เงื่อนไขของ Action เป็นข้อความ เช่น "if the target has full health and March 7th is on the field"
        // (เงื่อนไขเก่าอย่าง Combo แสดงผ่าน prefix อยู่แล้ว จึงไม่ซ้ำที่นี่)
        private static string ConditionsText(CardActionData action)
        {
            if (action.Conditions == null)
                return "";

            List<string> phrases = new List<string>();

            foreach (ConditionData condition in action.Conditions)
            {
                if (condition == null)
                    continue;

                switch (condition.Condition)
                {
                    case ConditionType.TargetFullHealth:
                        phrases.Add("the target has full health");
                        break;

                    case ConditionType.AllyCardOnField:
                        CardData ally = CardRegistry.Get(condition.CardID);

                        phrases.Add(
                            (ally != null ? ally.CardName : condition.CardID) +
                            " is on the field");
                        break;

                    case ConditionType.FirstAttackThisTurn:
                        phrases.Add("this is its first attack this turn");
                        break;
                }
            }

            return phrases.Count == 0 ? "" : "if " + string.Join(" and ", phrases.ToArray());
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

                case TargetRule.EventSource:
                    // Attack / AfterAttack: ต้นเหตุของเหตุการณ์คือเป้าหมายที่ถูกโจมตี
                    return action.Trigger == TriggerType.Attack ||
                           action.Trigger == TriggerType.AfterAttack
                        ? "the target"
                        : "the source";

                default:
                    return "the target";
            }
        }

        private static string PickText(EffectData effect)
        {
            string type =
                effect.PickType == PickFilter.Unit ? "unit"
                : (effect.PickType == PickFilter.Spell ? "spell"
                : (effect.PickType == PickFilter.Equipment ? "equipment" : ""));

            if (type == "" && effect.PickMaxCost <= 0)
                return "";

            if (effect.PickMaxCost <= 0)
                return " (a " + type + ")";

            if (type == "")
                return " (costing " + effect.PickMaxCost + " or less)";

            return " (a " + type + " costing " + effect.PickMaxCost + " or less)";
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
                    if (effect.BonusValue > 0)
                    {
                        return "deal " + value + " damage to " + target +
                               " (+" + effect.BonusValue + " if its health is " +
                               effect.BonusIfHealthAtMost + " or less)";
                    }

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
                    if (effect.DrawnSpellDiscount > 0)
                    {
                        return "draw " + value + (value == 1 ? " card" : " cards") +
                               "; a Spell drawn this way costs " +
                               effect.DrawnSpellDiscount + " less this turn";
                    }

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

                case EffectType.LookTop:
                    return "look at the top " + value + (value == 1 ? " card" : " cards") +
                        " of your deck and take one" + PickText(effect);

                case EffectType.Transform:
                    return "transform " + target + " into " + CardName(effect.Card) +
                        (effect.Duration > 0
                            ? " for " + effect.Duration + " turns"
                            : "");

                case EffectType.ApplyStatus:
                    return "give " + target + " " +
                        StatusInfo.DisplayName(effect.Status) +
                        (value > 0 ? " " + value : "");

                case EffectType.RemoveStatus:
                    return "remove " + StatusInfo.DisplayName(effect.Status) +
                        " from " + target;

                case EffectType.GrantExtraAttack:
                    return "attack " + System.Math.Max(1, value) + " additional time(s) this turn";

                case EffectType.Adapt:
                    return "Adapt";

                case EffectType.Equip:
                    return "equip " + target;

                case EffectType.EquipLeader:
                    return "equip to your Leader";

                case EffectType.Negate:
                    return "negate the action this responds to";

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
