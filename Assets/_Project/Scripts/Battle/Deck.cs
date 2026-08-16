using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    public class Deck
    {
        private List<CardInstance> cards = new List<CardInstance>();

        public int Count
        {
            get { return cards.Count; }
        }

        public void Add(CardInstance card)
        {
            if (card == null)
                return;

            card.Zone = CardZone.Deck;
            cards.Add(card);
        }

        public CardInstance Draw()
        {
            if (cards.Count == 0)
                return null;

            CardInstance card = cards[0];

            cards.RemoveAt(0);

            card.Zone = CardZone.Hand;

            return card;
        }

        public void Shuffle()
        {
            System.Random random = new System.Random();

            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                CardInstance temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }
        }
    }
}