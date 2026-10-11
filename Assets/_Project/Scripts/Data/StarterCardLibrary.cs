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
    public static partial class StarterCardLibrary
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
        public const string ShadowCloneId = "TKN_002";
        public const string TailedBeastBombId = "TKN_TAILED_BEAST_BOMB";
        public const string HollowPurpleId = "TKN_HOLLOW_PURPLE";
        public const string ProtonCannonId = "TKN_PROTON_CANNON";

        // บอสท้ายบท
        public const string HeroEchoId = "TKN_HERO_ECHO";
        public const string AmbushId = "TKN_AMBUSH";
        public const string DivineJudgmentId = "TKN_DIVINE_JUDGMENT";
        public const string GateCollapseId = "TKN_GATE_COLLAPSE";
        public const string EmperorWrathId = "TKN_EMPEROR_WRATH";
        public const string WorldRewriteId = "TKN_WORLD_REWRITE";

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

            // ไม้ตายของ Leader Naruto: ถล่มทั้งสนามฝั่งศัตรู
            Add(Create(
                TailedBeastBombId, "Tailed Beast Bomb", CardType.Spell,
                Universe.Naruto, Faction.LeafVillage, CardRarity.Legendary,
                5, 0, 0, Keyword.None,
                CardTag.Magic | CardTag.Token,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                        Fx(EffectType.DealDamage, 6)),
                    Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                        Fx(EffectType.DealDamage, 6))
                }));

            // ไม้ตายของ Leader Gojo: ลบยูนิตที่เลือกพร้อมสร้างความเสียหายให้ Leader ศัตรู
            Add(Create(
                HollowPurpleId, "Hollow Purple", CardType.Spell,
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Legendary,
                5, 0, 0, Keyword.None,
                CardTag.Magic | CardTag.Token,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                        Fx(EffectType.DealDamage, 10)),
                    Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                        Fx(EffectType.DealDamage, 6))
                }));

            // ไม้ตายของ Leader Tony: ยิงหนักใส่ Leader ศัตรู
            Add(Create(
                ProtonCannonId, "Proton Cannon", CardType.Spell,
                Universe.Original, Faction.Neutral, CardRarity.Legendary,
                4, 0, 0, Keyword.None,
                CardTag.Magic | CardTag.Token,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                        Fx(EffectType.DealDamage, 9)),
                    Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                        Fx(EffectType.DealDamage, 2))
                }));

            // ---------- การ์ดของบอสท้ายบท ----------

            Add(Unit(
                HeroEchoId, "Hero's Echo",
                Universe.Original, Faction.Neutral, CardRarity.Rare,
                2, 2, 2, Keyword.None,
                CardTag.Spirit | CardTag.Token,
                CardSource.Token));

            Add(Create(
                AmbushId, "Ambush!", CardType.Spell,
                Universe.Original, Faction.Neutral, CardRarity.Legendary,
                3, 0, 0, Keyword.None,
                CardTag.Magic | CardTag.Token,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                        Fx(EffectType.DealDamage, 5))
                }));

            Add(Create(
                DivineJudgmentId, "Divine Judgment", CardType.Spell,
                Universe.Original, Faction.Neutral, CardRarity.Legendary,
                4, 0, 0, Keyword.None,
                CardTag.Magic | CardTag.Token,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                        Fx(EffectType.DealDamage, 4)),
                    Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                        Fx(EffectType.Heal, 6))
                }));

            Add(Create(
                GateCollapseId, "Gate Collapse", CardType.Spell,
                Universe.Original, Faction.Neutral, CardRarity.Legendary,
                5, 0, 0, Keyword.None,
                CardTag.Magic | CardTag.Token,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                        Fx(EffectType.DealDamage, 6))
                }));

            Add(Create(
                EmperorWrathId, "Emperor's Wrath", CardType.Spell,
                Universe.Original, Faction.Neutral, CardRarity.Legendary,
                4, 0, 0, Keyword.None,
                CardTag.Magic | CardTag.Token,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                        Fx(EffectType.DealDamage, 7)),
                    Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                        Fx(EffectType.DealDamage, 3))
                }));

            Add(Create(
                WorldRewriteId, "World Rewrite", CardType.Spell,
                Universe.Original, Faction.Neutral, CardRarity.Legendary,
                6, 0, 0, Keyword.None,
                CardTag.Magic | CardTag.Token,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                        Fx(EffectType.DealDamage, 12)),
                    Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                        Fx(EffectType.DealDamage, 4))
                }));

            CardData clone = Add(Unit(
                "TKN_002", "Shadow Clone",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Common,
                1, 1, 1, Keyword.None,
                CardTag.Human | CardTag.Token,
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

            // ---------- การ์ดทั่วไป (ตัวประกอบ + เวทมนตร์) ----------
            // อยู่ในไฟล์ StarterCardLibrary.Roster.cs
            BuildRoster();
        }

        // ---------- ตัวช่วยสำหรับ Rift set (ตอนนี้ยังไม่มีการ์ดใน set นี้) ----------

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

            return Add(data);
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

            return Add(data);
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

        // DealDamage ที่ได้ความเสียหายเพิ่มเมื่อเป้าหมายมี HP ไม่เกิน maxHealth (ก่อนโดนโจมตี)
        private static EffectData FxDamageBonus(int value, int bonus, int maxHealth)
        {
            EffectData effect = Fx(EffectType.DealDamage, value);

            effect.BonusValue = bonus;
            effect.BonusIfHealthAtMost = maxHealth;

            return effect;
        }

        // DrawCard ที่ถ้าจั่วได้ Spell จะลด Cost ลง discount จนจบเทิร์น
        private static EffectData FxDrawSpellDiscount(int count, int discount)
        {
            EffectData effect = Fx(EffectType.DrawCard, count);

            effect.DrawnSpellDiscount = discount;

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
