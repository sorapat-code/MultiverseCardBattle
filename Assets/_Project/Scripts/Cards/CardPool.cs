using System.Collections.Generic;

namespace MultiverseCardBattle.Cards
{
    public class CardPool
    {
        private List<CardData> cards =
            new List<CardData>();

        public int Count
        {
            get { return cards.Count; }
        }

        public bool Add(CardData card)
        {
            if (card == null)
                return false;

            if (cards.Contains(card))
                return false;

            cards.Add(card);
            return true;
        }

        public bool Remove(CardData card)
        {
            if (card == null)
                return false;

            return cards.Remove(card);
        }

        public bool Contains(CardData card)
        {
            if (card == null)
                return false;

            return cards.Contains(card);
        }

        public CardData GetCard(int index)
        {
            if (index < 0 || index >= cards.Count)
                return null;

            return cards[index];
        }

        public IReadOnlyList<CardData> Cards
        {
            get { return cards; }
        }

        public void Clear()
        {
            cards.Clear();
        }
    }
}