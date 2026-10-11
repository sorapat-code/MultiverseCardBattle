using System.Collections.Generic;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Decks
{
    public class DeckBuilder
    {
        public static MultiverseCardBattle.Battle.Deck CreateDeck(
            List<CardData> cardDataList)
        {
            MultiverseCardBattle.Battle.Deck deck =
                new MultiverseCardBattle.Battle.Deck();

            foreach (CardData cardData in cardDataList)
            {
                if (cardData == null)
                    continue;

                CardInstance cardInstance =
                    new CardInstance(cardData);

                deck.Add(cardInstance);
            }

            deck.Shuffle();

            return deck;
        }
    }

    /// <summary>
    /// กติกาการสร้างเด็คของผู้เล่น
    /// </summary>
    public static class DeckRules
    {
        public const int MinDeckSize = 30;

        public const int DefaultMaxCopies = 3;

        // ใส่ซ้ำได้ 3 ใบ ยกเว้นการ์ด Unique (และการ์ดลับ) ที่ใส่ได้ 1 ใบ
        public static int MaxCopies(CardData card)
        {
            if (card == null)
                return 0;

            if ((card.Keywords & Keyword.Unique) != 0 ||
                card.Rarity == CardRarity.Secret)
                return 1;

            return DefaultMaxCopies;
        }

        /// <summary>การ์ดที่มี Keyword Unique ใบแรกในเด็ค (null = ไม่มี) ใส่ Unique ได้ 1 ใบต่อเด็ค</summary>
        public static CardData FindUnique(DeckList deck)
        {
            if (deck == null)
                return null;

            foreach (string id in deck.CardIDs)
            {
                CardData card = CardRegistry.Get(id);

                if (card != null && (card.Keywords & Keyword.Unique) != 0)
                    return card;
            }

            return null;
        }

        /// <summary>
        /// คืนรายการปัญหาของเด็ค (ว่าง = ใช้ได้)
        /// profile เป็น null = ไม่ตรวจว่าเป็นเจ้าของการ์ดหรือไม่
        /// </summary>
        public static List<string> Validate(
            DeckList deck,
            PlayerProfile profile)
        {
            List<string> problems = new List<string>();

            if (deck == null)
            {
                problems.Add("No deck selected");
                return problems;
            }

            if (deck.CardIDs.Count < MinDeckSize)
            {
                problems.Add(
                    "Deck needs at least " + MinDeckSize + " cards (has " +
                    deck.CardIDs.Count + ")");
            }

            Dictionary<string, int> counts = new Dictionary<string, int>();

            int uniqueCards = 0;

            foreach (string id in deck.CardIDs)
            {
                if (!counts.ContainsKey(id))
                    counts[id] = 0;

                counts[id]++;

                CardData counted = CardRegistry.Get(id);

                if (counted != null && (counted.Keywords & Keyword.Unique) != 0)
                    uniqueCards++;
            }

            if (uniqueCards > 1)
                problems.Add("A deck can have only 1 Unique card");

            foreach (KeyValuePair<string, int> pair in counts)
            {
                CardData card = CardRegistry.Get(pair.Key);

                if (card == null)
                {
                    problems.Add("Unknown card " + pair.Key);
                    continue;
                }

                if (!CardRegistry.IsDeckable(card))
                {
                    problems.Add(card.CardName + " cannot be put in a deck");
                    continue;
                }

                if (pair.Value > MaxCopies(card))
                {
                    problems.Add(
                        card.CardName + ": max " + MaxCopies(card) + " copies");
                }

                if (profile != null &&
                    pair.Value > profile.GetOwnedCount(pair.Key))
                {
                    problems.Add(card.CardName + ": you do not own enough copies");
                }
            }

            return problems;
        }
    }
}
