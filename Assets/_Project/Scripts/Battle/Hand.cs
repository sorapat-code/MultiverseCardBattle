using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    public class Hand
    {
        private List<CardInstance> cards = new List<CardInstance>();

        public int Count
        {
            get { return cards.Count; }
        }

        public IReadOnlyList<CardInstance> Cards
        {
            get { return cards; }
        }

        public bool Add(CardInstance card)
        {
            if (card == null)
                return false;

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