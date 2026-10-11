using System;
using System.Collections.Generic;
using System.Text;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Conditions;
using MultiverseCardBattle.Effects;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// คำอธิบาย Keyword / Status ทั้งหมดของเกม (ใช้แสดงในหน้า "Keywords & Status")
    /// </summary>
    public static class GameGlossary
    {
        public static string KeywordDescription(Keyword keyword)
        {
            switch (keyword)
            {
                case Keyword.Rush: return "Can attack enemy units on the turn it is played, but not the Leader.";
                case Keyword.Charge: return "Can attack right away, including the Leader.";
                case Keyword.Taunt: return "Enemies must attack Taunt units before anything else.";
                case Keyword.DoubleStrike: return "Can attack twice per turn.";
                case Keyword.Lifesteal: return "Damage dealt heals its owner's Leader by the same amount.";
                case Keyword.Pierce: return "Excess damage dealt to a unit carries over to the enemy Leader.";
                case Keyword.QuickAttack: return "Deals damage first. If the target dies, there is no counterattack.";
                case Keyword.Overwhelm: return "Excess damage dealt to a unit spills onto the neighboring enemy unit.";
                case Keyword.Unstoppable: return "Attacks cannot be stopped by Stun, Freeze, Disarm or Confusion.";
                case Keyword.Guard: return "While a Guard unit is on the field, enemies cannot attack its owner's Leader.";
                case Keyword.Shield: return "Blocks the next damage, then disappears.";
                case Keyword.Ward: return "Blocks the next enemy effect that targets this unit, then disappears.";
                case Keyword.Immune: return "Reduces every hit it takes by the number shown (Immune 3 takes only 2 from a 5-damage hit) and blocks harmful statuses.";
                case Keyword.Stealth: return "Cannot be targeted by enemies until it attacks.";
                case Keyword.Reborn: return "The first time it dies, it returns to the field with 1 HP.";
                case Keyword.Backstab: return "Any unit damaged by this unit is destroyed.";
                case Keyword.Shadowbound: return "While its owner controls a Token, it gains Unstoppable and enemy units cannot attack it (spells and abilities can still target it).";
                case Keyword.Unique: return "A deck can contain only 1 Unique card in total (any Unique card, not just this one).";
                case Keyword.Spellburst: return "Triggers once the first time its owner plays a spell.";
                case Keyword.Fast: return "Spell only. Can be played in response to your opponent's action, even during their turn, using your leftover Energy. Responses chain: the last card played resolves first.";
                case Keyword.Enrage: return "Gains +2 Attack while it is damaged.";
                case Keyword.Retaliate: return "Always strikes back when attacked by a unit, even if the hit kills it, the attacker has Quick Attack, or it is Stunned, Frozen or Disarmed.";
                case Keyword.Frenzy: return "The first time it survives damage, it permanently gains +2 Attack.";
                case Keyword.Elusive: return "Cannot be targeted while its owner controls another unit without Elusive.";
                case Keyword.Regenerate: return "Heals 2 HP at the start of its owner's turn.";
                case Keyword.Thorns: return "Deals 2 damage back to a unit that damages it in combat, even if it dies.";
                case Keyword.LethalStrike: return "A unit damaged by this unit is destroyed if it is left with 3 HP or less.";
                case Keyword.Overload: return "Playing this card locks some of your Energy next turn (the number shown).";
                case Keyword.Echo: return "When played, a copy of this card without Echo returns to your hand.";
                case Keyword.Poison: return "A unit damaged by this unit gains Poison 1 for 3 turns (it takes 1 true damage each turn, and stacks).";
                default: return "";
            }
        }

        // ศัพท์พื้นฐานของเกม (กติกา โซน การกระทำ)
        private static readonly string[][] GameTerms =
        {
            new[] { "Leader", "Your hero. If its HP reaches 0 you lose. Units can attack it." },
            new[] { "Energy", "Spent to play cards. Max Energy grows by 1 each turn (up to 10) and refills at turn start." },
            new[] { "Coin flip", "A coin decides who goes first. The player who goes second draws 1 extra card and gets The Coin (+1 Energy once)." },
            new[] { "Unit", "A card that stays on the field, attacks, and has Attack and Health." },
            new[] { "Spell", "A one-shot card. It acts immediately and goes to the Graveyard. If it needs a target and none exists, it cannot be played." },
            new[] { "Token", "A card created by an effect (for example a Spirit Wisp). It cannot be put in a deck." },
            new[] { "Hand", "Cards you can play. Holds up to 10; extra draws are burned." },
            new[] { "Deck", "Cards you draw from at turn start. If your deck ever reaches 0 cards, you lose the battle immediately." },
            new[] { "Field", "Where units fight. Holds up to 7 units per player." },
            new[] { "Graveyard", "Where destroyed units and used spells go. Click Graveyard in battle to look inside." },
            new[] { "Opp. Hand", "Shows how many cards the opponent holds. You cannot see which cards they are." },
            new[] { "Summon", "Puts a unit directly onto the field (if there is room)." },
            new[] { "Draw", "Take the top card of your deck into your hand." },
            new[] { "Discover", "Shows 3 random cards. Choose 1 and it is added to your hand." },
            new[] { "Counterattack", "When a unit attacks another unit, the defender hits back. Stunned, Frozen and Disarmed units cannot counterattack, and a Stealth unit's first attack is not countered." },
            new[] { "Sleeping", "A unit cannot attack the turn it is played unless it has Charge or Rush." },
            new[] { "True damage", "Damage that ignores Shield, Barrier and Defense." },
            new[] { "Heal", "Restores HP, never above the maximum." },
            new[] { "Chain", "When a card is played or a unit attacks, the opponent may respond with a Fast spell, and responses can be answered again. When nobody responds, the last card played resolves first. Negate cancels the action it responds to." },
            new[] { "Target", "Some cards need you to click a target. If there is none, a spell cannot be played; a unit can still be played but its effect does nothing." }
        };

        /// <summary>Keyword ที่เกิดจากชนิดของ Action ไม่ใช่ flag</summary>
        private static readonly string[][] AbilityKeywords =
        {
            new[] { "Battlecry", "Triggers when the card is played from hand and enters the field." },
            new[] { "Deathrattle", "Triggers when the card dies." },
            new[] { "Sent to Graveyard", "Triggers when the card is sent to the Graveyard from anywhere: dying, being discarded or destroyed, or a spell finishing." },
            new[] { "Targeted", "Triggers when this card is chosen as a target by an attack, spell or ability. Some cards only react to enemy attacks or enemy spells." },
            new[] { "Aura", "Gives other allied units a bonus while this unit is on the field." },
            new[] { "Combo", "Gains an extra effect if another card was played earlier this turn." },
            new[] { "Transformation", "Turns the card into another form (for example Goku into Super Saiyan). May be temporary." },
            new[] { "Evolve", "Becomes a stronger form after staying on the field for some turns." },
            new[] { "Adapt", "Choose one of several abilities to add to the card." }
        };

        private static readonly StatusType[][] StatusGroups =
        {
            new[] { StatusType.Stun, StatusType.Silence, StatusType.Freeze,
                    StatusType.Root, StatusType.Disarm, StatusType.Confusion },
            new[] { StatusType.Burn, StatusType.Poison, StatusType.Bleed,
                    StatusType.Curse, StatusType.Marked, StatusType.Doom },
            new[] { StatusType.AttackUp, StatusType.AttackDown, StatusType.DefenseUp,
                    StatusType.DefenseDown, StatusType.Vulnerable, StatusType.Weakness,
                    StatusType.Barrier, StatusType.Regen, StatusType.CostUp,
                    StatusType.CostDown, StatusType.Exposed, StatusType.Protected,
                    StatusType.Blessed },
            new[] { StatusType.Transformed, StatusType.TemporaryTransformation,
                    StatusType.TransformationLock, StatusType.TransformationFatigue }
        };

        private static readonly string[] StatusGroupTitles =
        {
            "Control statuses", "Damage over time", "Buffs and debuffs", "Transformation statuses"
        };

        private static readonly Keyword[][] KeywordGroups =
        {
            new[] { Keyword.Rush, Keyword.Charge, Keyword.Taunt, Keyword.DoubleStrike,
                    Keyword.Lifesteal, Keyword.Pierce, Keyword.QuickAttack,
                    Keyword.Overwhelm, Keyword.Unstoppable, Keyword.Guard, Keyword.Backstab,
                    Keyword.Enrage, Keyword.Retaliate, Keyword.Frenzy, Keyword.Thorns,
                    Keyword.LethalStrike, Keyword.Poison },
            new[] { Keyword.Shield, Keyword.Ward, Keyword.Immune,
                    Keyword.Stealth, Keyword.Reborn, Keyword.Shadowbound,
                    Keyword.Elusive, Keyword.Regenerate },
            new[] { Keyword.Spellburst, Keyword.Unique, Keyword.Fast,
                    Keyword.Overload, Keyword.Echo }
        };

        private static readonly string[] KeywordGroupTitles =
        {
            "Combat keywords", "Defense keywords", "Ability keywords"
        };

        /// <summary>ข้อความ Rich Text ของทั้งหน้า</summary>
        public static string Build()
        {
            StringBuilder text = new StringBuilder();

            Heading(text, "Game terms");

            foreach (string[] term in GameTerms)
                Entry(text, term[0], term[1]);

            for (int g = 0; g < KeywordGroups.Length; g++)
            {
                Heading(text, KeywordGroupTitles[g]);

                foreach (Keyword keyword in KeywordGroups[g])
                {
                    Entry(text, CardTextGenerator.KeywordName(keyword),
                        KeywordDescription(keyword));
                }

                // Keyword แบบ Action ต่อท้ายกลุ่มความสามารถ
                if (g == KeywordGroups.Length - 1)
                {
                    foreach (string[] pair in AbilityKeywords)
                        Entry(text, pair[0], pair[1]);
                }
            }

            for (int g = 0; g < StatusGroups.Length; g++)
            {
                Heading(text, StatusGroupTitles[g]);

                foreach (StatusType status in StatusGroups[g])
                {
                    Entry(text, StatusInfo.DisplayName(status),
                        StatusInfo.Description(status));
                }
            }

            return text.ToString();
        }

        /// <summary>
        /// คำอธิบายศัพท์ที่ปรากฏบนการ์ดใบนี้ (Keyword, สถานะที่ติดอยู่, ความสามารถพิเศษ)
        /// ใช้แสดงใต้การ์ดพรีวิวตอนเอาเมาส์ชี้ ผู้เล่นจะได้ไม่ต้องจำศัพท์เอง
        /// instance เป็น null ได้ (การ์ดที่ยังไม่ลงสนาม)
        /// </summary>
        public static string HintsFor(CardData data, CardInstance instance)
        {
            if (data == null)
                return "";

            List<string[]> terms = new List<string[]>();

            Keyword keywords = instance != null ? instance.Keywords : data.Keywords;

            foreach (Keyword keyword in Enum.GetValues(typeof(Keyword)))
            {
                if (keyword == Keyword.None || (keywords & keyword) != keyword)
                    continue;

                string description = KeywordDescription(keyword);

                if (keyword == Keyword.Immune)
                {
                    description = "Reduces every hit by " + data.EffectiveImmuneAmount +
                        " (a 5-damage hit deals " +
                        Math.Max(0, 5 - data.EffectiveImmuneAmount) +
                        "). Blocks harmful statuses.";
                }

                string termName = CardTextGenerator.KeywordName(keyword);

                if (keyword == Keyword.Overload)
                {
                    termName += " " + data.EffectiveOverloadAmount;

                    description = "Playing this card locks " + data.EffectiveOverloadAmount +
                        " Energy next turn.";
                }

                AddTerm(terms, termName, description);
            }

            if (instance != null)
            {
                foreach (StatusEffect status in instance.Statuses)
                {
                    AddTerm(terms, StatusInfo.DisplayName(status.Type),
                        StatusInfo.Description(status.Type));
                }
            }

            if (data.AuraAttack != 0 || data.AuraHealth != 0)
                AddTerm(terms, "Aura", AbilityText("Aura"));

            if (data.EvolvesInto != null)
                AddTerm(terms, "Evolve", AbilityText("Evolve"));

            if (data.Actions != null)
            {
                foreach (CardActionData action in data.Actions)
                {
                    if (action == null)
                        continue;

                    if (action.Trigger == TriggerType.Play && data.Type == CardType.Unit)
                        AddTerm(terms, "Battlecry", AbilityText("Battlecry"));
                    else if (action.Trigger == TriggerType.Death)
                        AddTerm(terms, "Deathrattle", AbilityText("Deathrattle"));
                    else if (action.Trigger == TriggerType.Spellburst)
                        AddTerm(terms, "Spellburst", KeywordDescription(Keyword.Spellburst));
                    else if (action.Trigger == TriggerType.Graveyard)
                        AddTerm(terms, "Sent to Graveyard", AbilityText("Sent to Graveyard"));
                    else if (action.Trigger == TriggerType.OnTarget)
                        AddTerm(terms, "Targeted", AbilityText("Targeted"));

                    if (action.Conditions != null)
                    {
                        foreach (ConditionData condition in action.Conditions)
                        {
                            if (condition.Condition == ConditionType.Combo)
                                AddTerm(terms, "Combo", AbilityText("Combo"));
                        }
                    }

                    if (action.Effects == null)
                        continue;

                    foreach (EffectData effect in action.Effects)
                        AddEffectTerm(terms, effect);
                }
            }

            StringBuilder text = new StringBuilder();

            // ไม่ยาวเกินพื้นที่แสดงผล
            for (int i = 0; i < terms.Count && i < 7; i++)
            {
                if (text.Length > 0)
                    text.Append('\n');

                text.Append("<b>").Append(terms[i][0]).Append("</b>: ")
                    .Append(terms[i][1]);
            }

            return text.ToString();
        }

        private static void AddEffectTerm(List<string[]> terms, EffectData effect)
        {
            switch (effect.Type)
            {
                case EffectType.Negate:
                    AddTerm(terms, "Negate", "Cancels the action this card responds to. A negated card goes to the Graveyard without effect; a negated attack does nothing.");
                    break;

                case EffectType.LookTop:
                    AddTerm(terms, "Look at the top", "Look at the top cards of your deck and take one into your hand. The rest go to the bottom of the deck.");
                    break;

                case EffectType.Discover:
                    AddTerm(terms, "Discover", "Shows 3 random cards. Choose 1 and it is added to your hand.");
                    break;

                case EffectType.Adapt:
                    AddTerm(terms, "Adapt", AbilityText("Adapt"));
                    break;

                case EffectType.Transform:
                    AddTerm(terms, "Transformation", AbilityText("Transformation"));
                    break;

                case EffectType.Summon:
                    AddTerm(terms, "Summon", "Puts a unit directly onto the field (if there is room).");
                    break;

                case EffectType.DealTrueDamage:
                    AddTerm(terms, "True damage", "Ignores Shield, Barrier and Defense.");
                    break;

                case EffectType.Stun:
                    AddTerm(terms, "Stun", StatusInfo.Description(StatusType.Stun));
                    break;

                case EffectType.Silence:
                    AddTerm(terms, "Silence", StatusInfo.Description(StatusType.Silence));
                    break;

                case EffectType.Burn:
                    AddTerm(terms, "Burn", StatusInfo.Description(StatusType.Burn));
                    break;

                case EffectType.Regen:
                    AddTerm(terms, "Regeneration", StatusInfo.Description(StatusType.Regen));
                    break;

                case EffectType.ApplyStatus:
                    AddTerm(terms, StatusInfo.DisplayName(effect.Status),
                        StatusInfo.Description(effect.Status));
                    break;

                case EffectType.AddKeyword:
                case EffectType.RemoveKeyword:
                    foreach (Keyword keyword in Enum.GetValues(typeof(Keyword)))
                    {
                        if (keyword != Keyword.None &&
                            (effect.Keyword & keyword) == keyword)
                        {
                            AddTerm(terms, CardTextGenerator.KeywordName(keyword),
                                KeywordDescription(keyword));
                        }
                    }
                    break;
            }
        }

        private static string AbilityText(string name)
        {
            foreach (string[] pair in AbilityKeywords)
            {
                if (pair[0] == name)
                    return pair[1];
            }

            return "";
        }

        private static void AddTerm(List<string[]> terms, string name, string description)
        {
            if (string.IsNullOrEmpty(description))
                return;

            foreach (string[] existing in terms)
            {
                if (existing[0] == name)
                    return;
            }

            terms.Add(new[] { name, description });
        }

        private static void Heading(StringBuilder text, string title)
        {
            if (text.Length > 0)
                text.Append("\n");

            text.Append("<color=#7FB0FF><b>").Append(title.ToUpper()).Append("</b></color>\n");
        }

        private static void Entry(StringBuilder text, string name, string description)
        {
            text.Append("<b>").Append(name).Append("</b> - ")
                .Append(description).Append('\n');
        }
    }
}
