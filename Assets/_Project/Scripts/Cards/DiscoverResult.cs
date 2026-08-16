using System.Collections.Generic;

namespace MultiverseCardBattle.Cards
{
    public class DiscoverResult
    {
        public List<CardData> Options =
            new List<CardData>();

        public CardData SelectedCard;

        public bool HasSelection
        {
            get { return SelectedCard != null; }
        }

        public void Select(CardData card)
        {
            if (card == null)
                return;

            if (!Options.Contains(card))
                return;

            SelectedCard = card;
        }

        public CardInstance CreateSelectedInstance()
        {
            if (SelectedCard == null)
                return null;

            return new CardInstance(SelectedCard);
        }

        public void Clear()
        {
            Options.Clear();
            SelectedCard = null;
        }
    }
}