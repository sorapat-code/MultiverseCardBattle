using System;
using System.Text;
using MultiverseCardBattle.Cards;

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
                case Keyword.Poison: return "Any unit damaged by this unit is destroyed.";
                case Keyword.Spellburst: return "Triggers once the first time its owner plays a spell.";
                default: return "";
            }
        }

        /// <summary>Keyword ที่เกิดจากชนิดของ Action ไม่ใช่ flag</summary>
        private static readonly string[][] AbilityKeywords =
        {
            new[] { "Battlecry", "Triggers when the card is played from hand and enters the field." },
            new[] { "Deathrattle", "Triggers when the card dies." },
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
                    StatusType.Curse, StatusType.Marked },
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
                    Keyword.Overwhelm, Keyword.Unstoppable, Keyword.Guard, Keyword.Poison },
            new[] { Keyword.Shield, Keyword.Ward, Keyword.Immune,
                    Keyword.Stealth, Keyword.Reborn },
            new[] { Keyword.Spellburst }
        };

        private static readonly string[] KeywordGroupTitles =
        {
            "Combat keywords", "Defense keywords", "Ability keywords"
        };

        /// <summary>ข้อความ Rich Text ของทั้งหน้า</summary>
        public static string Build()
        {
            StringBuilder text = new StringBuilder();

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
