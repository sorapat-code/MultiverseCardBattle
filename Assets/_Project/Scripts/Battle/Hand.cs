using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    public class Hand
    {
        public const int DefaultMaxSize = 10;

        private List<CardInstance> cards = new List<CardInstance>();

        // เจ้าของมือ (PlayerID) ถ้า > 0 จะกำหนด OwnerID ให้การ์ดที่เข้ามา
        public int OwnerID;

        public int MaxSize = DefaultMaxSize;

        public int Count
        {
            get { return cards.Count; }
        }

        public bool IsFull
        {
            get { return cards.Count >= MaxSize; }
        }

        public IReadOnlyList<CardInstance> Cards
        {
            get { return cards; }
        }

        /// <summary>
        /// คืน false ถ้าการ์ดเป็น null หรือมือเต็ม
        /// </summary>
        public bool Add(CardInstance card)
        {
            if (card == null)
                return false;

            if (cards.Count >= MaxSize)
                return false;

            if (OwnerID > 0)
                card.OwnerID = OwnerID;

            card.Zone = CardZone.Hand;
            cards.Add(card);

            return true;
        }

        public bool Remove(CardInstance card)
        {
            if (card == null)
                return false;

            return cards.Remove(card);
        }

        public bool Contains(CardInstance card)
        {
            return cards.Contains(card);
        }

        public CardInstance GetCard(int index)
        {
            if (index < 0 || index >= cards.Count)
                return null;

            return cards[index];
        }

        public void Clear()
        {
            cards.Clear();
        }
    }
}
