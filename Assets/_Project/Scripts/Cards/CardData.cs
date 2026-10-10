using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Effects;

namespace MultiverseCardBattle.Cards
{
    [CreateAssetMenu(
        fileName = "New Card",
        menuName = "Multiverse Card Battle/Card")]
    public class CardData : ScriptableObject
    {
        [Header("Basic Information")]

        public string CardID;
        public string CardName;
        public string Version;

        [TextArea]
        public string Description;

        public Sprite Artwork;

        [Header("Classification")]

        public CardType Type;
        public CardRarity Rarity;
        public CardSource Source;
        public Universe Universe;
        public Faction Faction;
        public CardTag Tags;

        [Header("Keywords")]

        public Keyword Keywords;

        [Header("Stats")]

        public int Cost;
        public int Attack;
        public int Health;

        [Header("Immune (ลดความเสียหายทุกครั้งที่โดน 0 = ใช้ค่าตั้งต้น)")]

        public const int DefaultImmuneAmount = 3;

        public int ImmuneAmount;

        public int EffectiveImmuneAmount
        {
            get { return ImmuneAmount > 0 ? ImmuneAmount : DefaultImmuneAmount; }
        }

        [Header("Aura (ให้ยูนิตพวกเดียวกันตัวอื่นขณะอยู่บนสนาม)")]

        public int AuraAttack;
        public int AuraHealth;

        [Header("Evolve")]

        // เปลี่ยนเป็นร่างนี้เมื่ออยู่บนสนามครบ EvolveAfterTurns เทิร์นของเจ้าของ
        public CardData EvolvesInto;
        public int EvolveAfterTurns;

        [Header("Actions")]

        public List<CardActionData> Actions = new();
    }
}