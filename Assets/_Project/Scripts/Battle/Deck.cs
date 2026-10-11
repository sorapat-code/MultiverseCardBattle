using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    public class Deck
    {
        private List<CardInstance> cards = new List<CardInstance>();

        // เจ้าของเด็ค (PlayerID) ถ้า > 0 จะกำหนด OwnerID ให้การ์ดที่เข้ามา
        public int OwnerID;

        public int Count
        {
            get { return cards.Count; }
        }

        public IReadOnlyList<CardInstance> Cards
        {
            get { return cards; }
        }

        public void Add(CardInstance card)
        {
            if (card == null)
                return;

            if (OwnerID > 0)
                card.OwnerID = OwnerID;

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

        public bool Remove(CardInstance card)
        {
            return cards.Remove(card);
        }

        /// <summary>ย้ายการ์ดในกองไปไว้ล่างสุด</summary>
        public void MoveToBottom(CardInstance card)
        {
            if (cards.Remove(card))
                cards.Add(card);
        }

        /// <summary>ย้ายการ์ดในกองไปไว้บนสุด (จั่วใบถัดไป)</summary>
        public void MoveToTop(CardInstance card)
        {
            if (cards.Remove(card))
                cards.Insert(0, card);
        }

        public void Clear()
        {
            cards.Clear();
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
