using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;
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

            // ---------- Units ----------

            Add(Unit("STR_001", "Leaf Genin",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Common,
                1, 1, 2, Keyword.None, CardTag.Human | CardTag.Melee));

            Add(Unit("STR_002", "Shield Guardian",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                2, 1, 4, Keyword.Taunt, CardTag.Human | CardTag.Tank));

            Add(Unit("STR_003", "Field Medic",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                2, 2, 2, Keyword.None, CardTag.Human | CardTag.Support,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 4))));

            Add(Unit("STR_004", "Pirate Sharpshooter",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Common,
                3, 3, 2, Keyword.None, CardTag.Human | CardTag.Ranged,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2))));

            Add(Unit("STR_005", "Iron Golem",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                4, 3, 6, Keyword.Taunt, CardTag.Machine | CardTag.Tank));

            Add(Unit("STR_006", "Mist Assassin",
                Universe.Naruto, Faction.MistVillage, CardRarity.Common,
                3, 3, 2, Keyword.Stealth, CardTag.Human | CardTag.Assassin));

            Add(Unit("STR_007", "Vampire Knight",
                Universe.Original, Faction.Neutral, CardRarity.Rare,
                3, 2, 4, Keyword.Lifesteal, CardTag.Undead | CardTag.Melee));

            Add(Unit("STR_008", "Venom Serpent",
                Universe.Mythology, Faction.Underworld, CardRarity.Rare,
                2, 1, 2, Keyword.Poison, CardTag.Beast));

            Add(Unit("STR_009", "Olympian Paladin",
                Universe.Mythology, Faction.Olympus, CardRarity.Rare,
                4, 3, 3, Keyword.Shield, CardTag.God | CardTag.Melee));

            Add(Unit("STR_010", "Soul Society Scout",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Common,
                3, 3, 1, Keyword.Charge, CardTag.Spirit | CardTag.Melee));

            Add(Unit("STR_011", "Twin Blade Duelist",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Rare,
                4, 3, 3, Keyword.DoubleStrike, CardTag.Human | CardTag.Melee));

            Add(Unit("STR_012", "Raid Skirmisher",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                3, 3, 2, Keyword.Rush, CardTag.Human | CardTag.Melee));

            Add(Unit("STR_013", "Sage Scholar",
                Universe.Fate, Faction.Chaldea, CardRarity.Common,
                2, 1, 3, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1))));

            Add(Unit("STR_014", "War Drummer",
                Universe.Mythology, Faction.Asgard, CardRarity.Common,
                3, 2, 3, Keyword.None, CardTag.Human | CardTag.Support,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    Fx(EffectType.GainAttack, 1))));

            Add(Unit("STR_015", "Death Cultist",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                3, 2, 2, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Death, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3))));

            Add(Unit("STR_016", "Spirit Caller",
                Universe.Fate, Faction.Chaldea, CardRarity.Rare,
                4, 2, 3, Keyword.None, CardTag.Spirit | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxCard(EffectType.Summon, wisp),
                    FxCard(EffectType.Summon, wisp))));

            Add(Unit("STR_017", "Pyromancer",
                Universe.Original, Faction.Neutral, CardRarity.Rare,
                4, 3, 4, Keyword.None, CardTag.Human | CardTag.Magic | CardTag.Fire,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxStatus(EffectType.Burn, 2, 3))));

            Add(Unit("STR_018", "Frost Witch",
                Universe.Mythology, Faction.Asgard, CardRarity.Rare,
                3, 2, 3, Keyword.None, CardTag.Human | CardTag.Magic | CardTag.Ice,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxStatus(EffectType.Stun, 0, 1))));

            Add(Unit("STR_019", "Silencer Monk",
                Universe.WutheringWaves, Faction.Jinzhou, CardRarity.Rare,
                3, 3, 2, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.Silence))));

            Add(Unit("STR_020", "Ancient Dragon",
                Universe.Mythology, Faction.Neutral, CardRarity.Legendary,
                7, 6, 7, Keyword.None, CardTag.Dragon | CardTag.Boss,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3))));

            Add(Unit("STR_021", "Titan Colossus",
                Universe.Original, Faction.Neutral, CardRarity.Epic,
                6, 6, 6, Keyword.Taunt, CardTag.Machine | CardTag.Elite));

            Add(Unit("STR_022", "Temple Priest",
                Universe.Mythology, Faction.Olympus, CardRarity.Common,
                3, 2, 3, Keyword.None, CardTag.Human | CardTag.Support,
                Act(TriggerType.TurnStart, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 2))));

            Add(Unit("STR_023", "Berserker",
                Universe.Fate, Faction.Chaldea, CardRarity.Common,
                2, 2, 3, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.DamageTaken, TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.GainAttack, 1))));

            Add(Unit("STR_024", "Gladiator",
                Universe.Original, Faction.Neutral, CardRarity.Epic,
                5, 5, 5, Keyword.None, CardTag.Human | CardTag.Elite | CardTag.Melee));

            // ---------- Spells ----------

            Add(Spell("SPL_001", "Lightning Bolt", CardRarity.Common, 1,
                Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3))));

            Add(Spell("SPL_002", "Fireball", CardRarity.Common, 3,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 4))));

            Add(Spell("SPL_003", "Healing Light", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 5))));

            Add(Spell("SPL_004", "Power Surge", CardRarity.Common, 1,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    Fx(EffectType.GainAttack, 2),
                    Fx(EffectType.GainHealth, 2))));

            Add(Spell("SPL_005", "Arcane Insight", CardRarity.Common, 3,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 2))));

            Add(Spell("SPL_006", "Mass Smite", CardRarity.Epic, 5,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3))));

            Add(Spell("SPL_007", "Assassinate", CardRarity.Rare, 4,
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

        private static EffectData FxStatus(EffectType type, int value, int duration)
        {
            EffectData effect = Fx(type, value);

            effect.Duration = duration;

            return effect;
        }
    }
}
