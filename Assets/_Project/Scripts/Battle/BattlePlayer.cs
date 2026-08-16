using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    public class BattlePlayer
    {
        public int PlayerID;

        public int LeaderHealth;
        public int MaxLeaderHealth;

        public int CurrentEnergy;
        public int MaxEnergy;

        public Deck Deck;
        public Hand Hand;

        public List<CardInstance> Field = new List<CardInstance>();
        public List<CardInstance> Graveyard = new List<CardInstance>();
        public List<CardInstance> Removed = new List<CardInstance>();

        public BattlePlayer(int playerID, int startingHealth)
        {
            PlayerID = playerID;

            MaxLeaderHealth = startingHealth;
            LeaderHealth = startingHealth;

            CurrentEnergy = 0;
            MaxEnergy = 0;

            Deck = new Deck();
            Hand = new Hand();
        }
        public bool RemoveCard(CardInstance card)
        {
            if (card == null)
                return false;

            if (Hand.Remove(card))
                return true;

            if (Field.Remove(card))
                return true;

            if (Graveyard.Remove(card))
                return true;

            if (Removed.Remove(card))
                return true;

            return false;
        }

        public void AddToZone(CardInstance card, CardZone zone)
        {
            if (card == null)
                return;

            switch (zone)
            {
                case CardZone.Deck:
                    Deck.Add(card);
                    break;

                case CardZone.Hand:
                    Hand.Add(card);
                    break;

                case CardZone.Field:
                    card.Zone = CardZone.Field;
                    Field.Add(card);
                    break;

                case CardZone.Graveyard:
                    card.Zone = CardZone.Graveyard;
                    Graveyard.Add(card);
                    break;

                case CardZone.Removed:
                    card.Zone = CardZone.Removed;
                    Removed.Add(card);
                    break;
            }
        }
    }
}