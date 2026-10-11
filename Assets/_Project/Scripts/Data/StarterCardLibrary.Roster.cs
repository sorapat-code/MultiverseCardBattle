using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// การ์ดทั่วไปชุดปัจจุบัน: ตัวประกอบจากจักรวาลต่างๆ (ไม่เกิน Epic) และเวทมนตร์
    /// ตัวละครสำคัญ (Legendary / Secret) เจ้าของโปรเจกต์ออกแบบและเพิ่มเอง
    /// ID: SUP_xxx = ยูนิตตัวประกอบ, MAG_xxx = เวทมนตร์
    /// รูปการ์ดอยู่ที่ Resources/CardArt/{CardID}.png
    /// </summary>
    public static partial class StarterCardLibrary
    {
        // เวทมนตร์ธาตุ: ใส่ Tag ธาตุเพื่อให้เสียงและเอฟเฟกต์เลือกตามธาตุได้
        private static CardData ElemSpell(
            string id, string name, CardRarity rarity, int cost, CardTag element,
            params CardActionData[] actions)
        {
            return Add(Create(id, name, CardType.Spell,
                Universe.Original, Faction.Neutral, rarity,
                cost, 0, 0, Keyword.None, CardTag.Magic | element,
                CardSource.Starter, actions));
        }

        private static void BuildRoster()
        {
            CardData clone = Get("TKN_002");

            // ================= ยูนิต: Common =================

            Add(Unit("SUP_001", "Konohamaru Sarutobi",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Common,
                2, 2, 2, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxCard(EffectType.Summon, clone))));

            Add(Unit("SUP_002", "Rock Lee",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Common,
                2, 3, 1, Keyword.Rush, CardTag.Human | CardTag.Melee));

            Add(Unit("SUP_003", "Usopp",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Common,
                2, 2, 2, Keyword.None, CardTag.Human | CardTag.Ranged,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxDamageBonus(1, 1, 2))));

            Add(Unit("SUP_004", "Tony Tony Chopper",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Common,
                2, 1, 3, Keyword.None, CardTag.Beast | CardTag.Support,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 3))));

            Add(Unit("SUP_005", "Krillin",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Common,
                3, 3, 3, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2))));

            Add(Unit("SUP_006", "Connie Springer",
                Universe.AttackOnTitan, Faction.SurveyCorps, CardRarity.Common,
                1, 1, 2, Keyword.Rush, CardTag.Human | CardTag.Melee));

            Add(Unit("SUP_007", "Sasha Blouse",
                Universe.AttackOnTitan, Faction.SurveyCorps, CardRarity.Common,
                2, 3, 1, Keyword.Stealth, CardTag.Human | CardTag.Ranged));

            Add(Unit("SUP_008", "March 7th",
                Universe.HonkaiStarRail, Faction.AstralExpress, CardRarity.Common,
                2, 1, 4, Keyword.Taunt, CardTag.Human | CardTag.Tank | CardTag.Ice,
                Act(TriggerType.Play, TargetRule.Self, TargetSelection.All,
                    FxApply(StatusType.Barrier, 1, 0))));

            Add(Unit("SUP_009", "Chixia",
                Universe.WutheringWaves, Faction.Jinzhou, CardRarity.Common,
                2, 3, 1, Keyword.QuickAttack, CardTag.Human | CardTag.Ranged | CardTag.Fire));

            Add(Unit("SUP_010", "Kobeni Higashiyama",
                Universe.ChainsawMan, Faction.PublicSafety, CardRarity.Common,
                1, 1, 3, Keyword.Elusive, CardTag.Human));

            Add(Unit("SUP_011", "Crestfallen Warrior",
                Universe.DarkSouls, Faction.Lordran, CardRarity.Common,
                1, 1, 3, Keyword.Taunt, CardTag.Undead | CardTag.Tank));

            // ================= ยูนิต: Rare =================

            Add(Unit("SUP_012", "Nami",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Rare,
                3, 2, 3, Keyword.None, CardTag.Human | CardTag.Support | CardTag.Lightning,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxDrawSpellDiscount(1, 1))));

            Add(Unit("SUP_013", "Uryu Ishida",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Rare,
                3, 3, 3, Keyword.Pierce, CardTag.Human | CardTag.Ranged | CardTag.Light));

            Add(Unit("SUP_014", "Nobara Kugisaki",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Rare,
                3, 3, 2, Keyword.None, CardTag.Human | CardTag.Ranged | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2),
                    FxApply(StatusType.Marked, 0, 2))));

            Add(Unit("SUP_015", "Mash Kyrielight",
                Universe.Fate, Faction.Chaldea, CardRarity.Rare,
                3, 2, 5, Keyword.Taunt, CardTag.Human | CardTag.Tank | CardTag.Light,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    FxApply(StatusType.DefenseUp, 1, 2))));

            Add(Unit("SUP_016", "Zenitsu Agatsuma",
                Universe.DemonSlayer, Faction.DemonSlayerCorps, CardRarity.Rare,
                3, 4, 2, Keyword.QuickAttack, CardTag.Human | CardTag.Melee | CardTag.Lightning));

            Add(Unit("SUP_017", "Power",
                Universe.ChainsawMan, Faction.PublicSafety, CardRarity.Rare,
                3, 4, 3, Keyword.Frenzy, CardTag.Demon | CardTag.Melee));

            Add(Unit("SUP_018", "Lady",
                Universe.DevilMayCry, Faction.None, CardRarity.Rare,
                4, 3, 4, Keyword.None, CardTag.Human | CardTag.Ranged,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 3))));

            Add(Unit("SUP_019", "Solaire of Astora",
                Universe.DarkSouls, Faction.Lordran, CardRarity.Rare,
                3, 3, 4, Keyword.None, CardTag.Human | CardTag.Support | CardTag.Light,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 4))));

            Add(Unit("SUP_020", "Cha Hae-In",
                Universe.SoloLeveling, Faction.None, CardRarity.Rare,
                4, 5, 3, Keyword.QuickAttack, CardTag.Human | CardTag.Melee));

            Add(Unit("SUP_021", "Jean Pierre Polnareff",
                Universe.JoJo, Faction.StardustCrusaders, CardRarity.Rare,
                4, 3, 4, Keyword.Retaliate, CardTag.Human | CardTag.Melee));

            // ================= ยูนิต: Epic (ตัวประกอบสูงสุดคือ Epic) =================

            Add(Unit("SUP_022", "Igris, Blood-Red Commander",
                Universe.SoloLeveling, Faction.ShadowArmy, CardRarity.Epic,
                5, 5, 4, Keyword.Reborn,
                CardTag.Undead | CardTag.Elite | CardTag.Melee | CardTag.Dark));

            Add(Unit("SUP_023", "Albedo",
                Universe.Overlord, Faction.Nazarick, CardRarity.Epic,
                6, 5, 7, Keyword.Taunt,
                CardTag.Demon | CardTag.Elite | CardTag.Tank | CardTag.Dark,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 2))));

            Add(Unit("SUP_024", "Shalltear Bloodfallen",
                Universe.Overlord, Faction.Nazarick, CardRarity.Epic,
                5, 5, 5, Keyword.Lifesteal,
                CardTag.Undead | CardTag.Elite | CardTag.Melee | CardTag.Dark));

            Add(Unit("SUP_025", "Kyojuro Rengoku",
                Universe.DemonSlayer, Faction.DemonSlayerCorps, CardRarity.Epic,
                5, 5, 5, Keyword.None,
                CardTag.Human | CardTag.Elite | CardTag.Melee | CardTag.Fire,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    FxStatus(EffectType.Burn, 2, 3))));

            Add(Unit("SUP_026", "Gaara",
                Universe.Naruto, Faction.SandVillage, CardRarity.Epic,
                5, 4, 7, Keyword.Taunt,
                CardTag.Human | CardTag.Elite | CardTag.Tank | CardTag.Earth,
                Act(TriggerType.Play, TargetRule.Self, TargetSelection.All,
                    FxKeyword(EffectType.AddKeyword, Keyword.Shield))));

            // ================= เวทมนตร์ =================

            ElemSpell("MAG_001", "Spark", CardRarity.Common, 1, CardTag.Lightning,
                Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3)));

            ElemSpell("MAG_002", "Frost Shard", CardRarity.Common, 2, CardTag.Ice,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2),
                    FxApply(StatusType.Freeze, 0, 1)));

            ElemSpell("MAG_003", "Healing Rain", CardRarity.Common, 2, CardTag.Water,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 5)));

            ElemSpell("MAG_004", "Gale Slash", CardRarity.Common, 3, CardTag.Wind,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 2)));

            ElemSpell("MAG_005", "Stone Wall", CardRarity.Common, 2, CardTag.Earth,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    Fx(EffectType.GainHealth, 3),
                    FxKeyword(EffectType.AddKeyword, Keyword.Shield)));

            Add(Spell("MAG_006", "Scholar's Notes", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 2))));

            ElemSpell("MAG_007", "Chain Lightning", CardRarity.Rare, 3, CardTag.Lightning,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 4)),
                Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 2)));

            ElemSpell("MAG_008", "Shadow Bind", CardRarity.Rare, 2, CardTag.Dark,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Weakness, 0, 3),
                    FxApply(StatusType.Root, 0, 2)));

            ElemSpell("MAG_009", "Blessing of Dawn", CardRarity.Rare, 3, CardTag.Light,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    FxApply(StatusType.AttackUp, 1, 3),
                    FxApply(StatusType.Barrier, 2, 0)));

            FastSpell("MAG_010", "Spellbreaker", CardRarity.Rare, 3, Keyword.None,
                Act(TriggerType.Play, TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.Negate)));

            FastSpell("MAG_011", "Emergency Ward", CardRarity.Common, 1, Keyword.None,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Barrier, 4, 0)));

            ElemSpell("MAG_012", "Inferno", CardRarity.Epic, 6, CardTag.Fire,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 4),
                    FxStatus(EffectType.Burn, 2, 2)));

            Add(Spell("MAG_013", "Rewind Fate", CardRarity.Epic, 3,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    Fx(EffectType.ReturnToHand),
                    Fx(EffectType.ReduceCost, 2)),
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1))));

            ElemSpell("MAG_015", "Flame Dart", CardRarity.Common, 1, CardTag.Fire,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 3)));

            ElemSpell("MAG_014", "World's End", CardRarity.Legendary, 8, CardTag.Dark,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DestroyCard)),
                Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 4)));

            BuildEquipment();
        }

        // ---------- การ์ดสวมใส่ (Equipment): ยูนิต 5 ใบ + Leader 3 ใบ (ไม่เกิน Epic) ----------

        private static void BuildEquipment()
        {
            UnitGear("EQP_001", "Iron Katana", CardRarity.Common, 1,
                2, 0, Keyword.None, "Equip: Choose a friendly unit. It gets +2 ATK.");

            UnitGear("EQP_002", "Guardian Plate", CardRarity.Common, 2,
                1, 3, Keyword.Taunt, "Equip: Choose a friendly unit. It gets +1/+3 and Taunt.");

            UnitGear("EQP_003", "Windrunner Boots", CardRarity.Rare, 2,
                1, 1, Keyword.QuickAttack, "Equip: Choose a friendly unit. It gets +1/+1 and Quick Attack.");

            UnitGear("EQP_004", "Crimson Fang", CardRarity.Rare, 3,
                3, 1, Keyword.Lifesteal, "Equip: Choose a friendly unit. It gets +3/+1 and Lifesteal.");

            UnitGear("EQP_005", "Moonlit Blade", CardRarity.Epic, 4,
                3, 3, Keyword.Pierce | Keyword.Shield, "Equip: Choose a friendly unit. It gets +3/+3, Pierce and Shield.");

            LeaderGear("EQP_006", "Healer's Charm", CardRarity.Common, 2, 3,
                "Equip to your Leader for 3 turns.\nAt the start of your turn, restore 2 HP to your Leader.",
                Fx(EffectType.Heal, 2));

            LeaderGear("EQP_007", "Scholar's Monocle", CardRarity.Rare, 3, 3,
                "Equip to your Leader for 3 turns.\nAt the start of your turn, draw a card.",
                Fx(EffectType.DrawCard, 1));

            LeaderGear("EQP_008", "Arcane Brazier", CardRarity.Epic, 4, 3,
                "Equip to your Leader for 3 turns.\nAt the start of your turn, gain 1 Energy and restore 1 HP.",
                Fx(EffectType.GainEnergy, 1),
                Fx(EffectType.Heal, 1));
        }
    }
}
