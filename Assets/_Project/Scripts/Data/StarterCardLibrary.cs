using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Conditions;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// การ์ดตั้งต้นที่สร้างจากโค้ด (ประกอบจาก Trigger / Target / Effect ที่มีอยู่)
    /// ทำให้เกมเล่นได้ทันทีโดยไม่ต้องสร้าง asset และใช้เป็นตัวอย่างการประกอบการ์ด
    /// ส่งออกเป็น asset ได้จากเมนู Multiverse Card Battle > Export Starter Cards
    /// </summary>
    public static class StarterCardLibrary
    {
        private static List<CardData> cards;
        private static Dictionary<string, CardData> byId;

        public static IReadOnlyList<CardData> All
        {
            get
            {
                Build();
                return cards;
            }
        }

        public static CardData Get(string cardId)
        {
            Build();

            CardData data;

            if (byId.TryGetValue(cardId, out data))
                return data;

            return null;
        }

        // การ์ดที่ผู้เล่นฝั่งเริ่มหลังได้ตอนโยนเหรียญ: +1 Energy ในเทิร์นนั้น
        public const string CoinId = "TKN_COIN";

        // การ์ดลับ: มีโอกาสน้อยมากที่ Summon ในการต่อสู้จะได้การ์ดนี้แทน Token
        public const string SecretId = "EVT_001";

        // การ์ดลับที่ปลดล็อกจากเงื่อนไขในแมตช์ (SecretCardService)
        public const string ArtoriasId = "EVT_002";
        public const string AinzId = "EVT_003";
        public const string VergilId = "EVT_004";
        public const string DevilTriggerId = "TKN_006";

        // การ์ดไม้ตายของ Leader (ได้จากเงื่อนไข Power ของ Leader ไม่ใช่จากแพ็ก)
        public const string SpiritBombId = "TKN_SPIRIT_BOMB";

        private static void Build()
        {
            if (cards != null)
                return;

            cards = new List<CardData>();
            byId = new Dictionary<string, CardData>();

            // ---------- Token ----------

            CardData wisp = Add(Unit(
                "TKN_001", "Spirit Wisp",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                1, 1, 1, Keyword.None,
                CardTag.Spirit | CardTag.Token,
                CardSource.Token));

            Add(Create(
                CoinId, "The Coin", CardType.Spell,
                Universe.Original, Faction.Neutral, CardRarity.Common,
                0, 0, 0, Keyword.None,
                CardTag.Magic | CardTag.Token,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                        Fx(EffectType.GainEnergy, 1))
                }));

            // ไม้ตายของ Leader Goku: ปิดเกมหรือพลิกสถานการณ์
            Add(Create(
                SpiritBombId, "Spirit Bomb", CardType.Spell,
                Universe.DragonBall, Faction.ZFighters, CardRarity.Legendary,
                4, 0, 0, Keyword.None,
                CardTag.Magic | CardTag.Token,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                        Fx(EffectType.DealDamage, 10)),
                    Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                        Fx(EffectType.DealDamage, 5)),
                    Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                        Fx(EffectType.Heal, 5))
                }));

            CardData clone = Add(Unit(
                "TKN_002", "Shadow Clone",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Common,
                1, 1, 1, Keyword.None,
                CardTag.Human | CardTag.Token,
                CardSource.Token));


            // ร่างแปลงของ Goku (Transformation ชั่วคราว)
            CardData superGoku = Add(Unit(
                "TKN_003", "Super Saiyan Goku",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Legendary,
                8, 9, 9, Keyword.DoubleStrike | Keyword.Unstoppable,
                CardTag.Human | CardTag.Elite | CardTag.Melee | CardTag.Token,
                CardSource.Token));

            // ร่างวิวัฒนาการของ Ichigo (Evolve)
            CardData bankai = Add(Unit(
                "TKN_004", "Ichigo, Bankai",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Epic,
                6, 9, 8, Keyword.QuickAttack | Keyword.Pierce,
                CardTag.Human | CardTag.Spirit | CardTag.Melee | CardTag.Token,
                CardSource.Token));

            CardData shadowSoldier = Add(Unit(
                "TKN_005", "Shadow Soldier",
                Universe.SoloLeveling, Faction.ShadowArmy, CardRarity.Rare,
                2, 2, 2, Keyword.Rush,
                CardTag.Undead | CardTag.Melee | CardTag.Token,
                CardSource.Token));

            // การ์ดลับ (ไม่อยู่ใน Collection ได้จากโอกาส 1% ตอน Summon เท่านั้น)
            Add(Create(
                SecretId, "Sung Jin-Woo, Shadow Monarch", CardType.Unit,
                Universe.SoloLeveling, Faction.ShadowArmy, CardRarity.Secret,
                9, 8, 8, Keyword.Charge | Keyword.Lifesteal | Keyword.Shadowbound | Keyword.Unique,
                CardTag.Human | CardTag.Elite | CardTag.Boss | CardTag.Dark,
                CardSource.Event,
                new[]
                {
                    Act(TriggerType.Summon, TargetRule.AllEnemies, TargetSelection.All,
                        Fx(EffectType.DealDamage, 3)),
                    Act(TriggerType.Summon, TargetRule.AllyLeader, TargetSelection.All,
                        FxCard(EffectType.Summon, shadowSoldier),
                        FxCard(EffectType.Summon, shadowSoldier)),
                    Act(TriggerType.TurnStart, TargetRule.AllyLeader, TargetSelection.All,
                        FxCard(EffectType.Summon, shadowSoldier))
                }));

            // Artorias: ปลดล็อกด้วยเงื่อนไข "A Knight's Duty" ไม่ใช่การสุ่ม
            CardData artorias = Create(
                ArtoriasId, "Artorias the Abysswalker", CardType.Unit,
                Universe.DarkSouls, Faction.Lordran, CardRarity.Secret,
                7, 6, 9, Keyword.Taunt | Keyword.Lifesteal | Keyword.Unique,
                CardTag.Human | CardTag.Elite | CardTag.Boss | CardTag.Melee | CardTag.Tank,
                CardSource.Event,
                new[]
                {
                    Act(TriggerType.Summon, TargetRule.AllEnemies, TargetSelection.All,
                        FxHeal(EffectType.DealDamage, 2, 2))
                });

            artorias.Passives = CardPassive.Abysswalker | CardPassive.LastGuardian;
            artorias.Description =
                "Taunt, Lifesteal, Unique\n" +
                "Summon: Deal 2 damage to all enemy units. Restore 2 HP to your Leader for each enemy that took damage.\n" +
                "Abysswalker: Whenever damaged by an attack, +1 ATK until end of turn.\n" +
                "Last Guardian: Once per match, instead of dying, survive with 1 HP but lose Taunt until the end of next turn.";

            Add(artorias);

            // Ainz: ปลดล็อกด้วยเงื่อนไข "The Throne of Nazarick"
            CardData ainz = Create(
                AinzId, "Ainz Ooal Gown, The Supreme Overlord", CardType.Unit,
                Universe.Overlord, Faction.Nazarick, CardRarity.Secret,
                9, 7, 12, Keyword.Unstoppable | Keyword.Unique,
                CardTag.Undead | CardTag.Elite | CardTag.Boss | CardTag.Magic | CardTag.Dark,
                CardSource.Event,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                        FxApply(StatusType.Doom, 0, StatusEffect.Permanent))
                });

            ainz.Passives =
                CardPassive.GraspHeart | CardPassive.DarkWisdom |
                CardPassive.SupremeMagicCaster | CardPassive.UndeadOverlord;

            ainz.Description =
                "Unstoppable, Unique\n" +
                "Play: Choose an enemy unit. It gains Doom: destroyed at the start of your next turn unless cleansed first.\n" +
                "Grasp Heart: At the start of your turn, choose an enemy unit and deal 4 damage. If it dies, deal 3 damage to 2 other random enemy units.\n" +
                "Dark Wisdom: When an enemy unit dies to a Spell or Ainz's abilities, gain 1 Energy (max 3 per turn).\n" +
                "Supreme Magic Caster: Your first Spell each turn costs 2 less, the second costs 1 less.\n" +
                "The Undead Overlord: Once per match, when he would die from damage, he survives with 1 HP and gains Unstoppable until the end of your next turn.";

            Add(ainz);

            // Vergil ร่าง Devil Trigger (แปลงร่างจากปุ่ม ไม่ใช่ Token ที่ Summon จึงไม่ใส่ Tag Token)
            CardData devilTrigger = Create(
                DevilTriggerId, "Vergil, Awakened Dark Slayer", CardType.Unit,
                Universe.DevilMayCry, Faction.SpardaClan, CardRarity.Legendary,
                8, 12, 10, Keyword.Charge | Keyword.Unstoppable | Keyword.Lifesteal,
                CardTag.Demon | CardTag.Elite | CardTag.Boss | CardTag.Melee | CardTag.Dark,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Attack, TargetRule.AllEnemies, TargetSelection.All,
                        Fx(EffectType.DealDamage, 3))
                });

            devilTrigger.Passives =
                CardPassive.SinDevilRegeneration | CardPassive.OverwhelmingPower |
                CardPassive.DevilTriggerForm;

            devilTrigger.Description =
                "Charge, Unstoppable, Lifesteal\n" +
                "Judgment of the End: When attacking, deal 3 damage to all enemy units.\n" +
                "Sin Devil's Regeneration: At the start of your turn, heal 3.\n" +
                "Overwhelming Power: Attack damage cannot be reduced by Immune, Defense Up or Weakness (Shield still blocks).\n" +
                "Lasts 2 of your turns, then reverts to Vergil.";

            Add(devilTrigger);

            // Vergil: ปลดล็อกด้วยเงื่อนไข "The Power of the Yamato"
            CardData vergil = Create(
                VergilId, "Vergil, The Alpha and the Omega", CardType.Unit,
                Universe.DevilMayCry, Faction.SpardaClan, CardRarity.Secret,
                8, 8, 6, Keyword.Charge | Keyword.Unstoppable | Keyword.Unique,
                CardTag.Human | CardTag.Demon | CardTag.Elite | CardTag.Boss | CardTag.Melee,
                CardSource.Event,
                new[]
                {
                    Act(TriggerType.Summon, TargetRule.AllEnemies, TargetSelection.All,
                        Fx(EffectType.DealDamage, 3)),
                    Act(TriggerType.Summon, TargetRule.Self, TargetSelection.All,
                        Fx(EffectType.GrantExtraAttack, 1))
                });

            vergil.Passives = CardPassive.Concentration | CardPassive.DevilTrigger;
            vergil.Description =
                "Charge, Unstoppable, Unique\n" +
                "Judgment Cut: On summon, deal 3 damage to all enemy units, then attack one additional time this turn.\n" +
                "Concentration: At the start of your turn, if Vergil took no damage since your last turn start, +2 ATK until end of turn.\n" +
                "Devil Trigger: Once per match during your turn (not the turn he is summoned), press the DEVIL TRIGGER button to transform for free for 2 of your turns.";

            Add(vergil);

            // ---------- Units ----------

            Add(Unit("STR_001", "Leaf Genin",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Common,
                1, 2, 2, Keyword.None, CardTag.Human | CardTag.Melee));

            Add(Unit("STR_002", "Shield Guardian",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                2, 2, 5, Keyword.Taunt, CardTag.Human | CardTag.Tank));

            Add(Unit("STR_003", "Field Medic",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                2, 2, 3, Keyword.None, CardTag.Human | CardTag.Support,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 5))));

            Add(Unit("STR_004", "Pirate Sharpshooter",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Common,
                3, 3, 3, Keyword.None, CardTag.Human | CardTag.Ranged,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 3))));

            Add(Unit("STR_005", "Iron Golem",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                4, 4, 7, Keyword.Taunt, CardTag.Machine | CardTag.Tank));

            Add(Unit("STR_006", "Mist Assassin",
                Universe.Naruto, Faction.MistVillage, CardRarity.Common,
                3, 4, 2, Keyword.Stealth, CardTag.Human | CardTag.Assassin));

            Add(Unit("STR_007", "Vampire Knight",
                Universe.Original, Faction.Neutral, CardRarity.Rare,
                3, 3, 4, Keyword.Lifesteal, CardTag.Undead | CardTag.Melee));

            Add(Unit("STR_008", "Disaster Curse",
                Universe.JujutsuKaisen, Faction.CursedSpirits, CardRarity.Rare,
                2, 2, 2, Keyword.Backstab, CardTag.Spirit | CardTag.Demon));

            Add(Unit("STR_009", "Megumi Fushiguro",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Rare,
                4, 4, 4, Keyword.Shield, CardTag.Human | CardTag.Magic | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxCard(EffectType.Summon, wisp))));

            Add(Unit("STR_010", "Soul Society Scout",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Common,
                3, 3, 2, Keyword.Charge, CardTag.Spirit | CardTag.Melee));

            Add(Unit("STR_011", "Twin Blade Duelist",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Rare,
                4, 4, 3, Keyword.DoubleStrike, CardTag.Human | CardTag.Melee));

            Add(Unit("STR_012", "Raid Skirmisher",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                3, 3, 3, Keyword.Rush, CardTag.Human | CardTag.Melee));

            Add(Unit("STR_013", "Sage Scholar",
                Universe.Fate, Faction.Chaldea, CardRarity.Common,
                2, 2, 3, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1))));

            Add(Unit("STR_014", "Nobara Kugisaki",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Common,
                3, 3, 3, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2))));

            Add(Unit("STR_015", "Death Cultist",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                3, 3, 2, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Death, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 4))));

            Add(Unit("STR_016", "Spirit Caller",
                Universe.Fate, Faction.Chaldea, CardRarity.Rare,
                4, 3, 4, Keyword.None, CardTag.Spirit | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxCard(EffectType.Summon, wisp),
                    FxCard(EffectType.Summon, wisp))));

            Add(Unit("STR_017", "Pyromancer",
                Universe.Original, Faction.Neutral, CardRarity.Rare,
                4, 4, 4, Keyword.None, CardTag.Human | CardTag.Magic | CardTag.Fire,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxStatus(EffectType.Burn, 3, 3))));

            Add(Unit("STR_018", "Toge Inumaki",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Rare,
                3, 3, 3, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxStatus(EffectType.Stun, 0, 1))));

            Add(Unit("STR_019", "Silencer Monk",
                Universe.WutheringWaves, Faction.Jinzhou, CardRarity.Rare,
                3, 3, 3, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.Silence))));

            Add(Unit("STR_020", "Jogo, Volcano Curse",
                Universe.JujutsuKaisen, Faction.CursedSpirits, CardRarity.Legendary,
                7, 7, 7, Keyword.None, CardTag.Demon | CardTag.Boss | CardTag.Fire,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3)),
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    FxStatus(EffectType.Burn, 2, 3))));

            Add(Unit("STR_021", "Titan Colossus",
                Universe.Original, Faction.Neutral, CardRarity.Epic,
                6, 7, 8, Keyword.Taunt, CardTag.Machine | CardTag.Elite));

            Add(Unit("STR_022", "Shoko Ieiri",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Common,
                3, 2, 4, Keyword.None, CardTag.Human | CardTag.Support,
                Act(TriggerType.TurnStart, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 3))));

            Add(Unit("STR_023", "Berserker",
                Universe.Fate, Faction.Chaldea, CardRarity.Common,
                2, 3, 3, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.DamageTaken, TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.GainAttack, 1))));

            Add(Unit("STR_024", "Gladiator",
                Universe.Original, Faction.Neutral, CardRarity.Epic,
                5, 6, 6, Keyword.None, CardTag.Human | CardTag.Elite | CardTag.Melee));

            // ---------- Multiverse Heroes ----------
            // ตัวละครจากหลายจักรวาล ประกอบจาก Trigger / Target / Effect เดิมทั้งหมด

            Add(Unit("NAR_001", "Naruto Uzumaki",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Epic,
                5, 4, 5, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxCard(EffectType.Summon, clone),
                    FxCard(EffectType.Summon, clone),
                    FxCard(EffectType.Summon, clone))));

            Add(Unit("NAR_002", "Sasuke Uchiha",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Rare,
                4, 5, 3, Keyword.Charge, CardTag.Human | CardTag.Assassin | CardTag.Lightning,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2))));

            Add(Unit("OPC_001", "Monkey D. Luffy",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Epic,
                5, 6, 6, Keyword.Rush, CardTag.Human | CardTag.Melee));

            Add(Unit("OPC_002", "Roronoa Zoro",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Rare,
                5, 5, 5, Keyword.DoubleStrike, CardTag.Human | CardTag.Melee));

            Add(Unit("BLE_001", "Ichigo Kurosaki",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Epic,
                6, 6, 6, Keyword.Charge | Keyword.Pierce, CardTag.Human | CardTag.Spirit | CardTag.Melee));

            Add(Unit("BLE_002", "Rukia Kuchiki",
                Universe.Bleach, Faction.Gotei13, CardRarity.Rare,
                3, 3, 3, Keyword.None, CardTag.Spirit | CardTag.Magic | CardTag.Ice,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxStatus(EffectType.Stun, 0, 1))));

            Add(Unit("DBZ_001", "Son Goku",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Legendary,
                8, 8, 8, Keyword.Charge, CardTag.Human | CardTag.Elite | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 5)),
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 3))));

            Add(Unit("DBZ_002", "Vegeta",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Epic,
                6, 6, 6, Keyword.None, CardTag.Human | CardTag.Elite | CardTag.Melee,
                Act(TriggerType.DamageTaken, TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.GainAttack, 2))));

            Add(Unit("JJB_001", "Jotaro Kujo",
                Universe.JoJo, Faction.StardustCrusaders, CardRarity.Epic,
                6, 6, 6, Keyword.DoubleStrike, CardTag.Human | CardTag.Melee));

            Add(Unit("JJB_002", "Dio Brando",
                Universe.JoJo, Faction.DioFollowers, CardRarity.Legendary,
                7, 7, 7, Keyword.Lifesteal, CardTag.Undead | CardTag.Elite,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.All,
                    FxStatus(EffectType.Stun, 0, 1))));

            Add(Unit("FAT_001", "Artoria Pendragon",
                Universe.Fate, Faction.Chaldea, CardRarity.Legendary,
                7, 7, 7, Keyword.Shield, CardTag.Human | CardTag.Melee | CardTag.Light,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 8)),
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    FxApply(StatusType.Protected, 0, 2))));

            Add(Unit("HSR_001", "March 7th",
                Universe.HonkaiStarRail, Faction.AstralExpress, CardRarity.Common,
                3, 3, 3, Keyword.Taunt, CardTag.Human | CardTag.Ranged | CardTag.Ice,
                Act(TriggerType.Play, TargetRule.Self, TargetSelection.All,
                    FxKeyword(EffectType.AddKeyword, Keyword.Shield))));

            Add(Unit("HSR_002", "Kafka",
                Universe.HonkaiStarRail, Faction.StellaronHunters, CardRarity.Epic,
                4, 4, 4, Keyword.None, CardTag.Human | CardTag.Magic | CardTag.Lightning,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.All,
                    FxStatus(EffectType.Burn, 2, 3))));

            Add(Unit("WUW_001", "Jiyan",
                Universe.WutheringWaves, Faction.Jinzhou, CardRarity.Rare,
                5, 5, 5, Keyword.Rush, CardTag.Human | CardTag.Melee | CardTag.Wind,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 3))));

            Add(Unit("MYT_001", "Satoru Gojo",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Legendary,
                8, 8, 8, Keyword.Shield, CardTag.Human | CardTag.Magic | CardTag.Boss,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 4)),
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    FxApply(StatusType.Freeze, 0, 1))));

            Add(Unit("MYT_002", "Ryomen Sukuna",
                Universe.JujutsuKaisen, Faction.CursedSpirits, CardRarity.Legendary,
                7, 7, 6, Keyword.None, CardTag.Demon | CardTag.Boss | CardTag.Dark,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DestroyCard)),
                Act(TriggerType.TurnEnd, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 2))));


            // ---------- Keyword / Status Showcase ----------
            // การ์ดที่ใช้ Keyword และสถานะใหม่ (Pierce, Quick Attack, Aura, Evolve ฯลฯ)

            Add(Unit("NAR_003", "Rock Lee",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Rare,
                3, 4, 3, Keyword.Rush | Keyword.QuickAttack,
                CardTag.Human | CardTag.Melee));

            Add(Unit("NAR_004", "Itachi Uchiha",
                Universe.Naruto, Faction.Akatsuki, CardRarity.Epic,
                5, 5, 5, Keyword.Stealth,
                CardTag.Human | CardTag.Assassin | CardTag.Dark,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Confusion, 0, 2),
                    FxStatus(EffectType.Stun, 0, 1))));

            Add(Unit("OPC_003", "Sanji",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Rare,
                4, 4, 3, Keyword.None,
                CardTag.Human | CardTag.Melee | CardTag.Fire,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 1)),
                ActIf(ConditionType.Combo, 1,
                    TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 3))));

            Add(Unit("OPC_004", "Jinbe",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Rare,
                4, 3, 7, Keyword.Guard,
                CardTag.Human | CardTag.Tank,
                Act(TriggerType.Play, TargetRule.Self, TargetSelection.All,
                    FxApply(StatusType.DefenseUp, 2, 3))));

            CardData shikai = Add(Unit("BLE_003", "Ichigo, Shikai",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Rare,
                4, 4, 4, Keyword.QuickAttack,
                CardTag.Human | CardTag.Spirit | CardTag.Melee));

            // Evolve: อยู่บนสนามครบ 2 เทิร์นแล้วปลดปล่อย Bankai
            shikai.EvolvesInto = bankai;
            shikai.EvolveAfterTurns = 2;

            Add(Unit("BLE_004", "Kenpachi Zaraki",
                Universe.Bleach, Faction.Gotei13, CardRarity.Epic,
                6, 7, 6, Keyword.Unstoppable | Keyword.Rush,
                CardTag.Human | CardTag.Elite | CardTag.Melee));

            Add(Unit("DBZ_003", "Goku, Saiyan Warrior",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Epic,
                5, 5, 6, Keyword.None,
                CardTag.Human | CardTag.Elite | CardTag.Melee,
                ActIf(ConditionType.SelfHealthLessThan, 15,
                    TriggerType.TurnStart, TargetRule.Self, TargetSelection.All,
                    FxTransform(superGoku, 3))));

            Add(Unit("DBZ_004", "Frieza",
                Universe.DragonBall, Faction.FriezaForce, CardRarity.Epic,
                7, 6, 7, Keyword.None,
                CardTag.Elite | CardTag.Boss | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    FxStatus(EffectType.Burn, 2, 3))));

            Add(Unit("DBZ_005", "Whis",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Legendary,
                7, 4, 10, Keyword.Immune | Keyword.Guard,
                CardTag.God | CardTag.Elite | CardTag.Support,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    FxApply(StatusType.Barrier, 2, 0)),
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 5))));

            Add(Unit("JJB_003", "Star Platinum",
                Universe.JoJo, Faction.StardustCrusaders, CardRarity.Epic,
                7, 7, 6, Keyword.QuickAttack | Keyword.Overwhelm,
                CardTag.Spirit | CardTag.Melee));

            CardData gilgamesh = Add(Unit("FAT_002", "Gilgamesh",
                Universe.Fate, Faction.Chaldea, CardRarity.Legendary,
                8, 8, 8, Keyword.Ward,
                CardTag.Human | CardTag.Elite | CardTag.Light));

            // Aura: ยูนิตพวกเดียวกันตัวอื่นได้ +1 Attack ขณะ Gilgamesh อยู่บนสนาม
            gilgamesh.AuraAttack = 1;

            Add(Unit("FAT_003", "Medea, Witch of Betrayal",
                Universe.Fate, Faction.Chaldea, CardRarity.Rare,
                3, 3, 3, Keyword.Spellburst,
                CardTag.Human | CardTag.Magic,
                Act(TriggerType.Spellburst, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1))));

            Add(Unit("HSR_003", "Dan Heng",
                Universe.HonkaiStarRail, Faction.AstralExpress, CardRarity.Rare,
                3, 4, 3, Keyword.Pierce,
                CardTag.Human | CardTag.Ranged | CardTag.Wind));

            Add(Unit("HSR_004", "Silver Wolf",
                Universe.HonkaiStarRail, Faction.StellaronHunters, CardRarity.Epic,
                4, 3, 4, Keyword.None,
                CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Vulnerable, 0, 3),
                    FxApply(StatusType.AttackDown, 2, 3))));

            Add(Unit("WUW_002", "Rover, Resonator",
                Universe.WutheringWaves, Faction.BlackShores, CardRarity.Epic,
                4, 3, 4, Keyword.None,
                CardTag.Human | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.Adapt, 3))));

            Add(Unit("MYT_003", "Yuji Itadori",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Epic,
                5, 5, 4, Keyword.Reborn,
                CardTag.Human | CardTag.Melee));

            Add(Unit("MYT_004", "Principal Yaga",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Rare,
                4, 3, 6, Keyword.Ward,
                CardTag.Human | CardTag.Support,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    FxApply(StatusType.Barrier, 2, 0))));

            Add(Unit("MYT_005", "Mahito",
                Universe.JujutsuKaisen, Faction.CursedSpirits, CardRarity.Rare,
                4, 4, 4, Keyword.None,
                CardTag.Spirit | CardTag.Demon | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Disarm, 0, 2),
                    FxApply(StatusType.Poison, 1, 3))));

            // ---------- Spells ----------

            Add(Spell("SPL_001", "Lightning Bolt", CardRarity.Common, 1,
                Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 4))));

            Add(Spell("SPL_002", "Fireball", CardRarity.Common, 3,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 5))));

            Add(Spell("SPL_003", "Healing Light", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 6))));

            Add(Spell("SPL_004", "Power Surge", CardRarity.Common, 1,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    Fx(EffectType.GainAttack, 3),
                    Fx(EffectType.GainHealth, 3))));

            Add(Spell("SPL_005", "Arcane Insight", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 2))));

            Add(Spell("SPL_006", "Mass Smite", CardRarity.Epic, 5,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 4))));

            Add(Spell("SPL_007", "Assassinate", CardRarity.Rare, 3,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DestroyCard))));

            Add(Spell("SPL_008", "Recall", CardRarity.Rare, 1,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    Fx(EffectType.ReturnToHand),
                    Fx(EffectType.ReduceCost, 1))));

            Add(Spell("SPL_009", "Discovery Scroll", CardRarity.Rare, 2,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Discover, 3))));

            Add(Spell("SPL_010", "Divine Blessing", CardRarity.Rare, 2,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    FxKeyword(EffectType.AddKeyword, Keyword.Shield))));

            Add(Spell("SPL_011", "Hex of Ruin", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Weakness, 0, 3),
                    FxApply(StatusType.Curse, 0, 3),
                    FxApply(StatusType.Marked, 0, 3))));

            Add(Spell("SPL_012", "Frozen Prison", CardRarity.Rare, 2,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Freeze, 0, 2),
                    FxApply(StatusType.Root, 0, 2))));

            Add(Spell("SPL_013", "Rejuvenation", CardRarity.Rare, 3,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    FxStatus(EffectType.Regen, 2, 3),
                    FxApply(StatusType.Blessed, 0, 3))));

            Add(Spell("SPL_014", "Opportunist", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 3)),
                ActIf(ConditionType.Combo, 1,
                    TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1))));

            Add(Spell("SPL_015", "Sanctuary", CardRarity.Rare, 3,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    FxApply(StatusType.Protected, 0, 2),
                    FxApply(StatusType.DefenseUp, 1, 2))));

            BuildRiftSet();
            BuildChainSet();
            BuildTriggerSet();
        }

        // ---------- Rift Pack (50 ใบ: ยูนิต 30 / เวทมนตร์ 12 / อุปกรณ์ 8) ----------

        private static CardData AddRift(CardData data)
        {
            data.Set = CardSet.Rift;

            return Add(data);
        }

        private static CardData RiftToken(
            string id, string name, Universe universe, Faction faction,
            CardRarity rarity, int cost, int atk, int hp,
            Keyword keywords, CardTag tags, params CardActionData[] actions)
        {
            return Add(Create(id, name, CardType.Unit, universe, faction, rarity,
                cost, atk, hp, keywords, tags | CardTag.Token,
                CardSource.Token, actions));
        }

        private static CardData RiftSpell(
            string id, string name, CardRarity rarity, int cost,
            params CardActionData[] actions)
        {
            return AddRift(Spell(id, name, rarity, cost, actions));
        }

        // อุปกรณ์สวมยูนิต: เลือกยูนิตพวกเรา 1 ตัวแล้วได้โบนัสถาวร
        private static CardData UnitGear(
            string id, string name, CardRarity rarity, int cost,
            int attack, int health, Keyword keywords, string text)
        {
            CardData data = Create(id, name, CardType.Equipment,
                Universe.Original, Faction.Neutral, rarity,
                cost, 0, 0, Keyword.None, CardTag.None, CardSource.Starter,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                        Fx(EffectType.Equip))
                });

            data.GearAttack = attack;
            data.GearHealth = health;
            data.GearKeywords = keywords;
            data.Description = text;

            return AddRift(data);
        }

        // อุปกรณ์ Leader: อยู่ได้ N เทิร์น ทำงานตอนเริ่ม/จบเทิร์น
        private static CardData LeaderGear(
            string id, string name, CardRarity rarity, int cost,
            int durability, string text, params EffectData[] turnStartEffects)
        {
            CardData data = Create(id, name, CardType.Equipment,
                Universe.Original, Faction.Neutral, rarity,
                cost, 0, 0, Keyword.None, CardTag.None, CardSource.Starter,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                        Fx(EffectType.EquipLeader)),
                    Act(TriggerType.TurnStart, TargetRule.AllyLeader, TargetSelection.All,
                        turnStartEffects)
                });

            data.GearDurability = durability;
            data.Description = text;

            return AddRift(data);
        }

        private static void BuildRiftSet()
        {
            // ----- Token -----

            CardData toad = RiftToken("TKN_012", "Giant Toad",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Common,
                3, 3, 3, Keyword.Taunt, CardTag.Beast);

            CardData familiar = RiftToken("TKN_013", "Spirit Familiar",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Common,
                1, 2, 1, Keyword.None, CardTag.Spirit);

            CardData minatoForm = RiftToken("TKN_007", "Minato, Nine-Tails Mode",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Epic,
                6, 9, 8, Keyword.Rush | Keyword.Unstoppable,
                CardTag.Human | CardTag.Elite | CardTag.Melee);

            CardData aceForm = RiftToken("TKN_008", "Ace, Flame Emperor",
                Universe.OnePiece, Faction.WhitebeardPirates, CardRarity.Epic,
                7, 9, 7, Keyword.Charge,
                CardTag.Human | CardTag.Elite | CardTag.Fire,
                Act(TriggerType.Attack, TargetRule.AllEnemies, TargetSelection.All,
                    FxStatus(EffectType.Burn, 1, 2)));

            CardData aizenForm = RiftToken("TKN_009", "Aizen, Hogyoku Fusion",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Epic,
                6, 10, 10, Keyword.Lifesteal,
                CardTag.Spirit | CardTag.Elite | CardTag.Boss | CardTag.Magic);

            CardData gohanForm = RiftToken("TKN_010", "Gohan, Beast",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Epic,
                7, 9, 9, Keyword.DoubleStrike,
                CardTag.Human | CardTag.Elite | CardTag.Melee);

            CardData erenForm = RiftToken("TKN_011", "Eren, Attack Titan",
                Universe.AttackOnTitan, Faction.SurveyCorps, CardRarity.Epic,
                6, 10, 10, Keyword.Taunt,
                CardTag.Beast | CardTag.Elite | CardTag.Boss | CardTag.Melee);

            // ----- Units: Common -----

            AddRift(Unit("RFT_001", "Neji Hyuga",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Common,
                2, 3, 3, Keyword.Rush, CardTag.Human | CardTag.Melee));

            AddRift(Unit("RFT_002", "Usopp",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Common,
                2, 2, 3, Keyword.Stealth, CardTag.Human | CardTag.Ranged,
                Act(TriggerType.Attack, TargetRule.EnemyUnit, TargetSelection.Random,
                    Fx(EffectType.DealDamage, 1))));

            AddRift(Unit("RFT_003", "Chopper",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Common,
                2, 2, 3, Keyword.None, CardTag.Beast | CardTag.Support,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 4))));

            AddRift(Unit("RFT_004", "Uryu Ishida",
                Universe.Bleach, Faction.Wandenreich, CardRarity.Common,
                3, 4, 2, Keyword.None, CardTag.Human | CardTag.Ranged | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 3))));

            AddRift(Unit("RFT_005", "Krillin",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Common,
                2, 2, 3, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.Death, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1))));

            AddRift(Unit("RFT_006", "Joseph Joestar",
                Universe.JoJo, Faction.StardustCrusaders, CardRarity.Common,
                3, 4, 3, Keyword.QuickAttack, CardTag.Human | CardTag.Melee));

            AddRift(Unit("RFT_007", "Lancer Cu Chulainn",
                Universe.Fate, Faction.Chaldea, CardRarity.Common,
                4, 5, 3, Keyword.Pierce, CardTag.Human | CardTag.Melee));

            AddRift(Unit("RFT_008", "Tanjiro Kamado",
                Universe.DemonSlayer, Faction.DemonSlayerCorps, CardRarity.Common,
                3, 3, 4, Keyword.Lifesteal, CardTag.Human | CardTag.Melee | CardTag.Water));

            AddRift(Unit("RFT_009", "Nezuko Kamado",
                Universe.DemonSlayer, Faction.DemonSlayerCorps, CardRarity.Common,
                2, 3, 2, Keyword.Reborn, CardTag.Demon | CardTag.Melee));

            AddRift(Unit("RFT_010", "Mikasa Ackerman",
                Universe.AttackOnTitan, Faction.SurveyCorps, CardRarity.Common,
                4, 5, 4, Keyword.QuickAttack, CardTag.Human | CardTag.Melee));

            AddRift(Unit("RFT_011", "Armin Arlert",
                Universe.AttackOnTitan, Faction.SurveyCorps, CardRarity.Common,
                2, 2, 3, Keyword.None, CardTag.Human | CardTag.Support,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1))));

            // ----- Units: Rare -----

            AddRift(Unit("RFT_012", "Jiraiya",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Rare,
                5, 5, 6, Keyword.None, CardTag.Human | CardTag.Elite | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxCard(EffectType.Summon, toad))));

            AddRift(Unit("RFT_013", "Trafalgar Law",
                Universe.OnePiece, Faction.None, CardRarity.Rare,
                4, 4, 4, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Freeze, 0, 1))));

            AddRift(Unit("RFT_014", "Nico Robin",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Rare,
                4, 4, 5, Keyword.Stealth, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.Silence))));

            AddRift(Unit("RFT_015", "Renji Abarai",
                Universe.Bleach, Faction.Gotei13, CardRarity.Rare,
                5, 6, 5, Keyword.Overwhelm, CardTag.Spirit | CardTag.Melee));

            AddRift(Unit("RFT_016", "Future Trunks",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Rare,
                5, 5, 5, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.TurnEnd, TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.GainAttack, 1))));

            AddRift(Unit("RFT_017", "Aoi Todo",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Rare,
                4, 4, 4, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxCard(EffectType.Summon, familiar),
                    FxCard(EffectType.Summon, familiar))));

            AddRift(Unit("RFT_018", "Beru",
                Universe.SoloLeveling, Faction.ShadowArmy, CardRarity.Rare,
                6, 7, 5, Keyword.Backstab | Keyword.Stealth,
                CardTag.Beast | CardTag.Elite | CardTag.Assassin));

            AddRift(Unit("RFT_019", "Rengoku",
                Universe.DemonSlayer, Faction.DemonSlayerCorps, CardRarity.Rare,
                5, 5, 5, Keyword.None, CardTag.Human | CardTag.Melee | CardTag.Fire,
                Act(TriggerType.Death, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 4))));

            AddRift(Unit("RFT_020", "Levi Ackerman",
                Universe.AttackOnTitan, Faction.SurveyCorps, CardRarity.Rare,
                5, 6, 5, Keyword.DoubleStrike, CardTag.Human | CardTag.Elite | CardTag.Melee));

            AddRift(Unit("RFT_021", "Makima",
                Universe.ChainsawMan, Faction.PublicSafety, CardRarity.Rare,
                5, 4, 6, Keyword.None, CardTag.Demon | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.Stun, 2),
                    FxApply(StatusType.Weakness, 0, 3),
                    FxApply(StatusType.Vulnerable, 0, 3))));

            // ----- Units: Epic (แปลงร่างด้วย Transform) -----

            AddRift(Unit("RFT_022", "Minato Namikaze",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Epic,
                6, 6, 5, Keyword.QuickAttack, CardTag.Human | CardTag.Elite | CardTag.Melee,
                ActIf(ConditionType.SelfHealthLessThan, 15,
                    TriggerType.TurnStart, TargetRule.Self, TargetSelection.All,
                    FxTransform(minatoForm, 3))));

            AddRift(Unit("RFT_023", "Portgas D. Ace",
                Universe.OnePiece, Faction.WhitebeardPirates, CardRarity.Epic,
                7, 7, 6, Keyword.None, CardTag.Human | CardTag.Elite | CardTag.Fire,
                ActIf(ConditionType.AllyUnitCount, 3,
                    TriggerType.TurnEnd, TargetRule.Self, TargetSelection.All,
                    FxTransform(aceForm, 3))));

            AddRift(Unit("RFT_024", "Sosuke Aizen",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Epic,
                6, 6, 6, Keyword.None, CardTag.Human | CardTag.Elite | CardTag.Magic,
                Act(TriggerType.Attack, TargetRule.Self, TargetSelection.All,
                    FxTransform(aizenForm, 0))));

            AddRift(Unit("RFT_025", "Son Gohan",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Epic,
                7, 6, 7, Keyword.None, CardTag.Human | CardTag.Elite | CardTag.Melee,
                ActIf(ConditionType.SelfHealthLessThan, 15,
                    TriggerType.TurnStart, TargetRule.Self, TargetSelection.All,
                    FxTransform(gohanForm, 3))));

            AddRift(Unit("RFT_026", "Giorno Giovanna",
                Universe.JoJo, Faction.Passione, CardRarity.Epic,
                6, 6, 6, Keyword.None, CardTag.Human | CardTag.Elite | CardTag.Support,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    Fx(EffectType.GainAttack, 1),
                    Fx(EffectType.GainHealth, 1)),
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 4))));

            AddRift(Unit("RFT_027", "Eren Yeager",
                Universe.AttackOnTitan, Faction.SurveyCorps, CardRarity.Epic,
                6, 5, 6, Keyword.None, CardTag.Human | CardTag.Elite | CardTag.Melee,
                Act(TriggerType.DamageTaken, TargetRule.Self, TargetSelection.All,
                    FxTransform(erenForm, 0))));

            // ----- Units: Legendary -----

            AddRift(Unit("RFT_028", "Toji Fushiguro",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Legendary,
                7, 8, 6, Keyword.Stealth | Keyword.Pierce | Keyword.Unstoppable,
                CardTag.Human | CardTag.Elite | CardTag.Assassin));

            AddRift(Unit("RFT_029", "Muzan Kibutsuji",
                Universe.DemonSlayer, Faction.Demons, CardRarity.Legendary,
                8, 8, 8, Keyword.Lifesteal,
                CardTag.Demon | CardTag.Elite | CardTag.Boss | CardTag.Dark,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    FxApply(StatusType.Poison, 2, 3))));

            AddRift(Unit("RFT_030", "Madara Uchiha",
                Universe.Naruto, Faction.Akatsuki, CardRarity.Legendary,
                8, 9, 8, Keyword.None,
                CardTag.Human | CardTag.Elite | CardTag.Boss | CardTag.Fire,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Random,
                    Fx(EffectType.DestroyCard)),
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Random,
                    Fx(EffectType.DestroyCard))));

            // ----- Spells -----

            RiftSpell("RFT_031", "Rasengan", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 3)));

            RiftSpell("RFT_032", "Water Breathing: Eleventh Form", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 3),
                    Fx(EffectType.DrawCard, 1)));

            RiftSpell("RFT_033", "Gomu Gomu Pistol", CardRarity.Common, 1,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2)),
                Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 1)));

            RiftSpell("RFT_034", "Getsuga Tensho", CardRarity.Common, 3,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 5)));

            RiftSpell("RFT_035", "Senzu Bean", CardRarity.Common, 1,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 6)));

            RiftSpell("RFT_036", "Kamehameha", CardRarity.Rare, 4,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 6)),
                Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3)));

            RiftSpell("RFT_037", "Chidori", CardRarity.Rare, 3,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 4),
                    FxStatus(EffectType.Stun, 0, 1)));

            RiftSpell("RFT_038", "Unlimited Blade Works", CardRarity.Rare, 5,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 2),
                    Fx(EffectType.GainEnergy, 1)));

            RiftSpell("RFT_039", "Domain Expansion", CardRarity.Rare, 5,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.Silence),
                    Fx(EffectType.DealDamage, 2)));

            RiftSpell("RFT_040", "Za Warudo", CardRarity.Epic, 6,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    FxApply(StatusType.Freeze, 0, 2)),
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1)));

            RiftSpell("RFT_041", "Black Flash", CardRarity.Epic, 4,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 7)));

            RiftSpell("RFT_042", "Eye of the Moon", CardRarity.Legendary, 8,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 4),
                    FxApply(StatusType.Freeze, 0, 2),
                    FxApply(StatusType.Weakness, 0, 3)));

            // ----- Equipment -----

            UnitGear("RFT_043", "Leaf Headband", CardRarity.Common, 1,
                1, 2, Keyword.None, "Equip: Choose a friendly unit. It gets +1/+2.");

            UnitGear("RFT_044", "Straw Hat", CardRarity.Common, 1,
                2, 1, Keyword.Rush, "Equip: Choose a friendly unit. It gets +2/+1 and Rush.");

            UnitGear("RFT_045", "Wado Ichimonji", CardRarity.Rare, 2,
                2, 0, Keyword.QuickAttack, "Equip: Choose a friendly unit. It gets +2 ATK and Quick Attack.");

            UnitGear("RFT_046", "Hunter's Cloak", CardRarity.Rare, 3,
                1, 3, Keyword.Stealth, "Equip: Choose a friendly unit. It gets +1/+3 and Stealth.");

            LeaderGear("RFT_047", "Chaldea Mystic Code", CardRarity.Rare, 3, 4,
                "Equip to your Leader for 4 turns.\nAt the start of your turn, restore 2 HP to your Leader.",
                Fx(EffectType.Heal, 2));

            UnitGear("RFT_048", "Playful Cloud", CardRarity.Epic, 4,
                3, 2, Keyword.DoubleStrike, "Equip: Choose a friendly unit. It gets +3/+2 and Double Strike.");

            UnitGear("RFT_049", "Excalibur", CardRarity.Epic, 5,
                4, 2, Keyword.Shield | Keyword.Lifesteal, "Equip: Choose a friendly unit. It gets +4/+2, Shield and Lifesteal.");

            LeaderGear("RFT_050", "Holy Grail", CardRarity.Legendary, 6, 3,
                "Equip to your Leader for 3 turns.\nAt the start of your turn, draw a card, restore 3 HP and gain 1 Energy.",
                Fx(EffectType.DrawCard, 1),
                Fx(EffectType.Heal, 3),
                Fx(EffectType.GainEnergy, 1));
        }

        // ---------- Chain Cards (Keyword ใหม่ + Fast spell ตอบโต้ได้ในเทิร์นศัตรู) ----------

        private static CardData FastSpell(
            string id, string name, CardRarity rarity, int cost,
            Keyword extraKeywords, params CardActionData[] actions)
        {
            return Add(Create(id, name, CardType.Spell,
                Universe.Original, Faction.Neutral, rarity,
                cost, 0, 0, Keyword.Fast | extraKeywords, CardTag.Magic,
                CardSource.Starter, actions));
        }

        private static CardData OriginalUnit(
            string id, string name, CardRarity rarity,
            int cost, int attack, int health,
            Keyword keywords, CardTag tags, params CardActionData[] actions)
        {
            return Add(Unit(id, name, Universe.Original, Faction.Neutral, rarity,
                cost, attack, health, keywords, tags, actions));
        }

        private static void BuildChainSet()
        {
            // ----- Units: Keyword ใหม่ -----

            OriginalUnit("CHN_001", "Bloodrage Berserker", CardRarity.Common,
                3, 3, 4, Keyword.Enrage, CardTag.Human | CardTag.Melee);

            OriginalUnit("CHN_002", "Iron Retaliator", CardRarity.Common,
                3, 2, 5, Keyword.Retaliate, CardTag.Human | CardTag.Tank);

            OriginalUnit("CHN_003", "Thornbark Warden", CardRarity.Common,
                2, 1, 4, Keyword.Thorns | Keyword.Taunt, CardTag.Beast | CardTag.Tank);

            OriginalUnit("CHN_004", "Frenzied Dire Wolf", CardRarity.Common,
                2, 2, 3, Keyword.Frenzy, CardTag.Beast | CardTag.Melee);

            OriginalUnit("CHN_005", "Veilstep Dancer", CardRarity.Rare,
                3, 3, 3, Keyword.Elusive, CardTag.Human | CardTag.Assassin);

            OriginalUnit("CHN_006", "Mossback Troll", CardRarity.Common,
                4, 4, 5, Keyword.Regenerate, CardTag.Beast | CardTag.Melee);

            OriginalUnit("CHN_007", "Gallows Executioner", CardRarity.Rare,
                4, 4, 3, Keyword.LethalStrike, CardTag.Human | CardTag.Melee);

            CardData golem = OriginalUnit("CHN_008", "Siege Golem", CardRarity.Rare,
                4, 7, 6, Keyword.Overload, CardTag.Machine | CardTag.Tank);

            golem.OverloadAmount = 2;

            OriginalUnit("CHN_009", "Mirror Twin", CardRarity.Rare,
                3, 3, 2, Keyword.Echo, CardTag.Spirit | CardTag.Magic);

            OriginalUnit("CHN_010", "Shade Sentinel", CardRarity.Epic,
                5, 4, 6, Keyword.Elusive | Keyword.Retaliate, CardTag.Spirit | CardTag.Elite);

            OriginalUnit("CHN_011", "Bleeding Stalker", CardRarity.Rare,
                3, 3, 2, Keyword.Stealth, CardTag.Human | CardTag.Assassin,
                Act(TriggerType.Attack, TargetRule.EnemyUnit, TargetSelection.Random,
                    FxApply(StatusType.Bleed, 1, 3)));

            OriginalUnit("CHN_012", "Colossus of Rage", CardRarity.Epic,
                6, 6, 7, Keyword.Enrage | Keyword.Frenzy, CardTag.Beast | CardTag.Elite | CardTag.Melee);

            OriginalUnit("CHN_013", "Grave Warden", CardRarity.Epic,
                5, 4, 5, Keyword.Regenerate | Keyword.Thorns, CardTag.Undead | CardTag.Elite);

            OriginalUnit("CHN_014", "Hex Witch", CardRarity.Rare,
                3, 2, 3, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Disarm, 0, 2),
                    FxApply(StatusType.Curse, 0, 3)));

            // ----- Fast spells: เล่นตอบโต้ได้ในเทิร์นศัตรู -----

            FastSpell("CHN_016", "Counter Magic", CardRarity.Rare, 3, Keyword.None,
                Act(TriggerType.Play, TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.Negate)));

            FastSpell("CHN_017", "Parry", CardRarity.Common, 1, Keyword.None,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.DefenseUp, 3, 1),
                    Fx(EffectType.Heal, 2)));

            FastSpell("CHN_018", "Ambush Shot", CardRarity.Common, 2, Keyword.None,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 3)));

            FastSpell("CHN_019", "Flash Heal", CardRarity.Common, 1, Keyword.None,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    Fx(EffectType.Heal, 4)));

            FastSpell("CHN_020", "Mirror Barrier", CardRarity.Rare, 2, Keyword.None,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Barrier, 5, 0)));

            CardData fracture = FastSpell("CHN_021", "Time Fracture", CardRarity.Epic, 4,
                Keyword.Overload,
                Act(TriggerType.Play, TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.Negate)),
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1)));

            fracture.OverloadAmount = 2;

            FastSpell("CHN_022", "Exposing Strike", CardRarity.Rare, 2, Keyword.None,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2),
                    FxApply(StatusType.Exposed, 0, 2)));

            FastSpell("CHN_023", "Vengeful Oath", CardRarity.Rare, 3, Keyword.None,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    FxKeyword(EffectType.AddKeyword, Keyword.Retaliate),
                    Fx(EffectType.GainAttack, 2)));

            FastSpell("CHN_024", "Bloodletting Hex", CardRarity.Common, 2, Keyword.None,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Bleed, 2, 3)));

            FastSpell("CHN_025", "Armor Break", CardRarity.Common, 1, Keyword.None,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 1),
                    FxApply(StatusType.DefenseDown, 2, 3)));

            // ----- Spell ปกติ ที่ใช้ Keyword / Status ใหม่ -----

            Add(Create("CHN_026", "Echo Bolt", CardType.Spell,
                Universe.Original, Faction.Neutral, CardRarity.Common,
                3, 0, 0, Keyword.Echo, CardTag.Magic | CardTag.Lightning,
                CardSource.Starter,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                        Fx(EffectType.DealDamage, 3))
                }));

            Add(Spell("CHN_027", "War Chant", CardRarity.Common, 3,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    FxApply(StatusType.AttackUp, 2, 2))));
        }

        // ---------- ถูกเล็งเป้า / ลงสุสาน / ดูหัวกอง ----------

        private static CardActionData ActTargeted(
            TargetedSource by, TargetedSide from,
            TargetRule rule, TargetSelection selection,
            params EffectData[] effects)
        {
            CardActionData action = Act(TriggerType.OnTarget, rule, selection, effects);

            action.TargetedBy = by;
            action.TargetedFrom = from;

            return action;
        }

        // LookTop: ดูหัวกอง count ใบ หยิบ 1 ใบเข้ามือ (กรองประเภท / ค่าใช้จ่ายได้)
        private static EffectData FxLook(
            int count, PickFilter pick = PickFilter.Any, int maxCost = 0)
        {
            EffectData effect = Fx(EffectType.LookTop, count);

            effect.PickType = pick;
            effect.PickMaxCost = maxCost;

            return effect;
        }

        private static void BuildTriggerSet()
        {
            // ----- When targeted (ถูกเล็งเป้า) -----

            OriginalUnit("TGT_001", "Mirror Guardian", CardRarity.Rare,
                3, 2, 5, Keyword.Taunt, CardTag.Spirit | CardTag.Tank,
                ActTargeted(TargetedSource.Attack, TargetedSide.Enemy,
                    TargetRule.EventSource, TargetSelection.All,
                    Fx(EffectType.DealDamage, 2)));

            OriginalUnit("TGT_002", "Spellward Monk", CardRarity.Common,
                3, 2, 4, Keyword.None, CardTag.Human | CardTag.Support,
                ActTargeted(TargetedSource.SpellOrAbility, TargetedSide.Enemy,
                    TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.GainAttack, 2)));

            OriginalUnit("TGT_003", "Spiteful Wraith", CardRarity.Rare,
                4, 3, 4, Keyword.None, CardTag.Spirit | CardTag.Magic,
                ActTargeted(TargetedSource.Any, TargetedSide.Enemy,
                    TargetRule.EventSource, TargetSelection.All,
                    FxStatus(EffectType.Stun, 0, 1)));

            OriginalUnit("TGT_004", "Thornhide Boar", CardRarity.Common,
                2, 2, 3, Keyword.None, CardTag.Beast | CardTag.Melee,
                ActTargeted(TargetedSource.Attack, TargetedSide.Any,
                    TargetRule.Self, TargetSelection.All,
                    FxKeyword(EffectType.AddKeyword, Keyword.Shield)));

            OriginalUnit("TGT_005", "Phantom Duelist", CardRarity.Epic,
                5, 4, 4, Keyword.Retaliate, CardTag.Spirit | CardTag.Elite,
                ActTargeted(TargetedSource.Attack, TargetedSide.Enemy,
                    TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3)));

            OriginalUnit("TGT_006", "Guardian Spirit", CardRarity.Rare,
                3, 2, 4, Keyword.None, CardTag.Spirit | CardTag.Support,
                ActTargeted(TargetedSource.SpellOrAbility, TargetedSide.Ally,
                    TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1)));

            OriginalUnit("TGT_007", "Decoy Totem", CardRarity.Common,
                1, 0, 3, Keyword.Taunt, CardTag.Machine | CardTag.Tank,
                ActTargeted(TargetedSource.Any, TargetedSide.Any,
                    TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 2)));

            // ----- When sent to the Graveyard (ลงสุสานจากทางใดก็ได้) -----

            OriginalUnit("GRV_001", "Ashen Martyr", CardRarity.Common,
                2, 2, 2, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.Graveyard, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 2)));

            OriginalUnit("GRV_002", "Gravebound Scholar", CardRarity.Rare,
                3, 2, 3, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Graveyard, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1)));

            OriginalUnit("GRV_003", "Soulfire Imp", CardRarity.Common,
                1, 1, 1, Keyword.None, CardTag.Demon | CardTag.Magic,
                Act(TriggerType.Graveyard, TargetRule.EnemyUnit, TargetSelection.Random,
                    Fx(EffectType.DealDamage, 1)));

            OriginalUnit("GRV_004", "Reaping Phantom", CardRarity.Epic,
                5, 4, 4, Keyword.None, CardTag.Undead | CardTag.Elite,
                Act(TriggerType.Graveyard, TargetRule.AllAllies, TargetSelection.All,
                    Fx(EffectType.GainAttack, 1)));

            OriginalUnit("GRV_005", "Relic Hoarder", CardRarity.Rare,
                2, 3, 2, Keyword.None, CardTag.Human | CardTag.Support,
                Act(TriggerType.Graveyard, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 4)));

            OriginalUnit("GRV_006", "Lich Archivist", CardRarity.Epic,
                6, 5, 6, Keyword.None, CardTag.Undead | CardTag.Magic | CardTag.Elite,
                Act(TriggerType.Graveyard, TargetRule.AllyLeader, TargetSelection.All,
                    FxLook(3, PickFilter.Spell)));

            // Spell ก็ยิง Trigger Graveyard ตอนใช้เสร็จ
            Add(Spell("GRV_007", "Final Gift", CardRarity.Common, 3,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 3)),
                Act(TriggerType.Graveyard, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 3))));

            // ----- Look at the top (ดูหัวกอง เลือกหยิบ) -----

            OriginalUnit("LKT_001", "Fortune Seer", CardRarity.Common,
                2, 1, 3, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxLook(3)));

            Add(Spell("LKT_002", "Arcane Index", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxLook(4, PickFilter.Spell))));

            OriginalUnit("LKT_003", "Scout Captain", CardRarity.Rare,
                3, 3, 3, Keyword.None, CardTag.Human | CardTag.Ranged,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxLook(3, PickFilter.Unit)));

            Add(Spell("LKT_004", "Quick Dig", CardRarity.Common, 1,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxLook(2))));

            OriginalUnit("LKT_005", "Oracle of Echoes", CardRarity.Epic,
                5, 4, 5, Keyword.None, CardTag.Spirit | CardTag.Magic | CardTag.Elite,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxLook(5)));

            Add(Spell("LKT_006", "Hand-Picked", CardRarity.Rare, 3,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxLook(6, PickFilter.Any, 3))));

            Add(Spell("LKT_007", "Armory Call", CardRarity.Rare, 2,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxLook(5, PickFilter.Equipment))));
        }

        // ---------- ตัวช่วยสร้างการ์ด ----------

        private static CardData Add(CardData data)
        {
            cards.Add(data);
            byId[data.CardID] = data;

            return data;
        }

        private static CardData Create(
            string id,
            string name,
            CardType type,
            Universe universe,
            Faction faction,
            CardRarity rarity,
            int cost,
            int attack,
            int health,
            Keyword keywords,
            CardTag tags,
            CardSource source,
            CardActionData[] actions)
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();

            // ป้องกัน Unity เก็บกวาด asset ที่สร้างจากโค้ดตอนเปลี่ยนซีน
            data.hideFlags = HideFlags.DontUnloadUnusedAsset;

            data.name = id;
            data.CardID = id;
            data.CardName = name;
            data.Version = "1.0";
            data.Description = "";

            data.Type = type;
            data.Rarity = rarity;
            data.Source = source;
            data.Universe = universe;
            data.Faction = faction;
            data.Tags = tags;
            data.Keywords = keywords;

            data.Cost = cost;
            data.Attack = attack;
            data.Health = health;

            data.Actions = new List<CardActionData>(actions);

            // รูปการ์ด (ถ้ามีไฟล์ใน Resources/CardArt/{CardID}.png)
            data.Artwork = CardArtLoader.Load(id);

            return data;
        }

        private static CardData Unit(
            string id, string name,
            Universe universe, Faction faction, CardRarity rarity,
            int cost, int attack, int health,
            Keyword keywords, CardTag tags,
            params CardActionData[] actions)
        {
            return Create(id, name, CardType.Unit, universe, faction, rarity,
                cost, attack, health, keywords, tags, CardSource.Starter, actions);
        }

        // Token ใช้ overload นี้เพื่อกำหนด Source เอง
        private static CardData Unit(
            string id, string name,
            Universe universe, Faction faction, CardRarity rarity,
            int cost, int attack, int health,
            Keyword keywords, CardTag tags, CardSource source)
        {
            return Create(id, name, CardType.Unit, universe, faction, rarity,
                cost, attack, health, keywords, tags, source,
                new CardActionData[0]);
        }

        private static CardData Spell(
            string id, string name, CardRarity rarity, int cost,
            params CardActionData[] actions)
        {
            return Create(id, name, CardType.Spell,
                Universe.Original, Faction.Neutral, rarity,
                cost, 0, 0, Keyword.None, CardTag.Magic,
                CardSource.Starter, actions);
        }

        private static CardActionData Act(
            TriggerType trigger,
            TargetRule rule,
            TargetSelection selection,
            params EffectData[] effects)
        {
            CardActionData action = new CardActionData();

            action.Trigger = trigger;
            action.Target = rule;
            action.Selection = selection;
            action.Effects = new List<EffectData>(effects);

            return action;
        }

        private static EffectData Fx(EffectType type, int value = 0)
        {
            EffectData effect = new EffectData();

            effect.Type = type;
            effect.Value = value;

            return effect;
        }

        private static EffectData FxHeal(EffectType type, int value, int healOwnerPerHit)
        {
            EffectData effect = Fx(type, value);

            effect.HealOwnerPerHit = healOwnerPerHit;

            return effect;
        }

        private static EffectData FxCard(EffectType type, CardData card)
        {
            EffectData effect = Fx(type);

            effect.Card = card;

            return effect;
        }

        private static EffectData FxKeyword(EffectType type, Keyword keyword)
        {
            EffectData effect = Fx(type);

            effect.Keyword = keyword;

            return effect;
        }


        // Action ที่ทำงานเมื่อเข้าเงื่อนไข (เช่น Combo)
        private static CardActionData ActIf(
            ConditionType condition,
            int conditionValue,
            TriggerType trigger,
            TargetRule rule,
            TargetSelection selection,
            params EffectData[] effects)
        {
            CardActionData action = Act(trigger, rule, selection, effects);

            ConditionData data = new ConditionData();

            data.Condition = condition;
            data.Value = conditionValue;

            action.Conditions.Add(data);

            return action;
        }

        // ApplyStatus: Value = ความแรง, Duration 0 = ค่าเริ่มต้นของสถานะ
        private static EffectData FxApply(StatusType status, int value, int duration)
        {
            EffectData effect = Fx(EffectType.ApplyStatus, value);

            effect.Status = status;
            effect.Duration = duration;

            return effect;
        }

        // Transform ชั่วคราว (duration > 0) หรือถาวร (0)
        private static EffectData FxTransform(CardData card, int duration)
        {
            EffectData effect = FxCard(EffectType.Transform, card);

            effect.Duration = duration;

            return effect;
        }

        private static EffectData FxStatus(EffectType type, int value, int duration)
        {
            EffectData effect = Fx(type, value);

            effect.Duration = duration;

            return effect;
        }
    }
}
