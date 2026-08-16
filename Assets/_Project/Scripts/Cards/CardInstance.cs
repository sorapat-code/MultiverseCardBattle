namespace MultiverseCardBattle.Cards
{
    public class CardInstance
    {
        public CardData Data;

        public int CurrentCost;
        public int CurrentAttack;
        public int CurrentHealth;

        public CardZone Zone;

        public CardInstance(CardData data)
        {
            Data = data;

            CurrentCost = data.Cost;
            CurrentAttack = data.Attack;
            CurrentHealth = data.Health;

            Zone = CardZone.Deck;
        }
    }
}