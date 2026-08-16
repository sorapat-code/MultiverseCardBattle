using System.Collections.Generic;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Battle;

namespace MultiverseCardBattle.Deck
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
}