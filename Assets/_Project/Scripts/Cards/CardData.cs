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

        [Header("Stats")]

        public int Cost;
        public int Attack;
        public int Health;

        [Header("Actions")]

        public List<CardActionData> Actions = new();
    }
}