using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Effects
{
    public static class EffectExecutor
    {
        public static void Execute(
            EffectData effect,
            EffectContext context)
        {
            if (effect == null)
                return;

            if (context == null)
                return;

            switch (effect.Type)
            {
                // Damage
                case EffectType.DealDamage:
                case EffectType.DealTrueDamage:
                    DealDamage(effect, context);
                    break;

                // Heal
                case EffectType.Heal:
                    Heal(effect, context);
                    break;

                // Stats
                case EffectType.GainAttack:
                    ModifyAttack(effect, context);
                    break;

                case EffectType.GainHealth:
                    ModifyHealth(effect, context);
                    break;

                case EffectType.SetAttack:
                    SetAttack(effect, context);
                    break;

                case EffectType.SetHealth:
                    SetHealth(effect, context);
                    break;

                // Cost
                case EffectType.ReduceCost:
                    ModifyCost(effect, context, -effect.Value);
                    break;

                case EffectType.IncreaseCost:
                    ModifyCost(effect, context, effect.Value);
                    break;

                case EffectType.SetCost:
                    SetCost(effect, context);
                    break;

                // Card
                case EffectType.DrawCard:
                    DrawCards(effect, context);
                    break;

                case EffectType.DiscardCard:
                    DiscardCard(effect, context);
                    break;
                case EffectType.DestroyCard:
                    DestroyCard(effect, context);
                    break;

                case EffectType.ReturnToHand:
                    ReturnToHand(effect, context);
                    break;

                case EffectType.MoveToDeck:
                    MoveToDeck(effect, context);
                    break;

                case EffectType.Banish:
                    Banish(effect, context);
                    break;

                case EffectType.Summon:
                    Summon(effect, context);
                    break;

                case EffectType.CreateCard:
                    CreateCard(effect, context);
                    break;

                case EffectType.Discover:
                    Discover(effect, context);
                    break;

                case EffectType.Transform:
                    Transform(effect, context);
                    break;

                // Resource
                case EffectType.GainEnergy:
                    GainEnergy(effect, context);
                    break;

                case EffectType.SpendEnergy:
                    SpendEnergy(effect, context);
                    break;
            }
        }

        private static void DealDamage(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard != null)
            {
                context.TargetCard.CurrentHealth -= effect.Value;

                if (context.TargetCard.CurrentHealth < 0)
                    context.TargetCard.CurrentHealth = 0;

                return;
            }

            if (context.TargetPlayer != null)
            {
                context.TargetPlayer.LeaderHealth -= effect.Value;

                if (context.TargetPlayer.LeaderHealth < 0)
                    context.TargetPlayer.LeaderHealth = 0;
            }
        }

        private static void Heal(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard != null)
            {
                context.TargetCard.CurrentHealth += effect.Value;

                if (context.TargetCard.CurrentHealth >
                    context.TargetCard.Data.Health)
                {
                    context.TargetCard.CurrentHealth =
                        context.TargetCard.Data.Health;
                }

                return;
            }

            if (context.TargetPlayer != null)
            {
                context.TargetPlayer.LeaderHealth += effect.Value;

                if (context.TargetPlayer.LeaderHealth >
                    context.TargetPlayer.MaxLeaderHealth)
                {
                    context.TargetPlayer.LeaderHealth =
                        context.TargetPlayer.MaxLeaderHealth;
                }
            }
        }

        private static void ModifyAttack(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.CurrentAttack += effect.Value;
        }

        private static void ModifyHealth(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.CurrentHealth += effect.Value;
        }

        private static void SetAttack(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.CurrentAttack = effect.Value;
        }

        private static void SetHealth(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.CurrentHealth = effect.Value;
        }

        private static void ModifyCost(
            EffectData effect,
            EffectContext context,
            int amount)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.CurrentCost += amount;

            if (context.TargetCard.CurrentCost < 0)
                context.TargetCard.CurrentCost = 0;
        }

        private static void SetCost(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.CurrentCost = effect.Value;

            if (context.TargetCard.CurrentCost < 0)
                context.TargetCard.CurrentCost = 0;
        }

        private static void DrawCards(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetPlayer == null)
                return;

            for (int i = 0; i < effect.Value; i++)
            {
                CardInstance card =
                    context.TargetPlayer.Deck.Draw();

                if (card == null)
                    return;

                context.TargetPlayer.Hand.Add(card);
            }
        }

        private static void DiscardCard(
            EffectData effect,
            EffectContext context)
            
        {
            if (context.TargetPlayer == null)
                return;

            CardInstance card =
                context.TargetPlayer.Hand.GetCard(0);

            if (card == null)
                return;

            if (context.TargetPlayer.Hand.Remove(card))
            {
                card.Zone = CardZone.Graveyard;
                context.TargetPlayer.Graveyard.Add(card);
            }
        }
        
        private static void DestroyCard(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            if (context.TargetPlayer == null)
                return;

            if (!context.TargetPlayer.RemoveCard(
                context.TargetCard))
                return;

            context.TargetPlayer.AddToZone(
                context.TargetCard,
                CardZone.Graveyard);
        }

        private static void ReturnToHand(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            if (context.TargetPlayer == null)
                return;

            if (!context.TargetPlayer.RemoveCard(
                context.TargetCard))
                return;

            context.TargetPlayer.AddToZone(
                context.TargetCard,
                CardZone.Hand);
        }

        private static void MoveToDeck(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            if (context.TargetPlayer == null)
                return;

            if (!context.TargetPlayer.RemoveCard(
                context.TargetCard))
                return;

            context.TargetPlayer.AddToZone(
                context.TargetCard,
                CardZone.Deck);
        }

        private static void Banish(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            if (context.TargetPlayer == null)
                return;

            if (!context.TargetPlayer.RemoveCard(
                context.TargetCard))
                return;

            context.TargetPlayer.AddToZone(
                context.TargetCard,
                CardZone.Removed);
        }

        private static void Summon(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            if (context.TargetPlayer == null)
                return;

            if (!context.TargetPlayer.RemoveCard(
                context.TargetCard))
                return;

            context.TargetPlayer.AddToZone(
                context.TargetCard,
                CardZone.Field);
        }
        private static void CreateCard(
            EffectData effect,
            EffectContext context)
        {
            if (effect.Card == null)
                return;

            if (context.TargetPlayer == null)
                return;

            CardInstance card =
                new CardInstance(effect.Card);

            context.TargetPlayer.Hand.Add(card);
        }
        private static void Discover(
            EffectData effect,
            EffectContext context)
        {
            if (context.CardPool == null)
                return;

            int optionCount = effect.Value;

            if (optionCount <= 0)
                return;

            context.DiscoverResult =
                DiscoverResolver.CreateOptions(
                    context.CardPool,
                    optionCount);
        }
        private static void Transform(
            EffectData effect,
            EffectContext context)
        {
            if (effect.Card == null)
                return;

            if (context.TargetCard == null)
                return;

            context.TargetCard.Data = effect.Card;

            context.TargetCard.CurrentCost =
                effect.Card.Cost;

            context.TargetCard.CurrentAttack =
                effect.Card.Attack;

            context.TargetCard.CurrentHealth =
                effect.Card.Health;
        }

        private static void GainEnergy(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetPlayer == null)
                return;

            context.TargetPlayer.CurrentEnergy += effect.Value;

            if (context.TargetPlayer.CurrentEnergy >
                context.TargetPlayer.MaxEnergy)
            {
                context.TargetPlayer.CurrentEnergy =
                    context.TargetPlayer.MaxEnergy;
            }
        }

        private static void SpendEnergy(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetPlayer == null)
                return;

            context.TargetPlayer.CurrentEnergy -= effect.Value;

            if (context.TargetPlayer.CurrentEnergy < 0)
                context.TargetPlayer.CurrentEnergy = 0;
        }
    }
}