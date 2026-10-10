using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    public class BattlePlayer
    {
        public const int MaxFieldSize = 7;

        public int PlayerID;

        public string Name;

        public int LeaderHealth;
        public int MaxLeaderHealth;

        public int CurrentEnergy;
        public int MaxEnergy;

        // นับจำนวนครั้งที่จั่วจากเด็คว่าง (ความเสียหาย Fatigue เพิ่มขึ้นเรื่อยๆ)
        public int FatigueCounter;

        // จำนวนการ์ดที่ใช้ไปแล้วในเทิร์นนี้ (ใช้กับ Combo)
        public int CardsPlayedThisTurn;

        public Deck Deck;
        public Hand Hand;

        public List<CardInstance> Field = new List<CardInstance>();
        public List<CardInstance> Graveyard = new List<CardInstance>();
        public List<CardInstance> Removed = new List<CardInstance>();

        public BattlePlayer(int playerID, int startingHealth)
        {
            PlayerID = playerID;

            Name = "Player " + playerID;

            MaxLeaderHealth = startingHealth;
            LeaderHealth = startingHealth;

            CurrentEnergy = 0;
            MaxEnergy = 0;

            Deck = new Deck();
            Deck.OwnerID = playerID;

            Hand = new Hand();
            Hand.OwnerID = playerID;
        }

        /// <summary>
        /// สร้างเด็คจากรายการ CardData แล้วสับ
        /// </summary>
        public void LoadDeck(IEnumerable<CardData> cardDataList)
        {
            Deck.Clear();

            if (cardDataList != null)
            {
                foreach (CardData data in cardDataList)
                {
                    if (data == null)
                        continue;

                    Deck.Add(new CardInstance(data));
                }
            }

            Deck.Shuffle();
        }

        public bool HasTaunt()
        {
            for (int i = 0; i < Field.Count; i++)
            {
                if (Field[i].HasKeyword(Keyword.Taunt) &&
                    !Field[i].IsHidden)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// มียูนิต Guard (ที่ไม่ได้ซ่อนตัว) อยู่ ศัตรูจึงโจมตี Leader ของผู้เล่นนี้ไม่ได้
        /// </summary>
        public bool HasGuard()
        {
            for (int i = 0; i < Field.Count; i++)
            {
                if (Field[i].HasKeyword(Keyword.Guard) &&
                    !Field[i].IsHidden)
                    return true;
            }

            return false;
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

            card.OwnerID = PlayerID;

            switch (zone)
            {
                case CardZone.Deck:
                    Deck.Add(card);
                    break;

                case CardZone.Hand:
                    // มือเต็ม: การ์ดถูกเผาทิ้งลงสุสาน
                    if (!Hand.Add(card))
                    {
                        card.Zone = CardZone.Graveyard;
                        Graveyard.Add(card);
                    }
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
