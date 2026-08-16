using System;
using System.Collections.Generic;

namespace MultiverseCardBattle.Cards
{
    public static class DiscoverResolver
    {
        public static DiscoverResult CreateOptions(
            CardPool pool,
            int optionCount)
        {
            DiscoverResult result =
                new DiscoverResult();

            if (pool == null)
                return result;

            if (optionCount <= 0)
                return result;

            List<CardData> available =
                new List<CardData>(pool.Cards);

            if (available.Count == 0)
                return result;

            Random random = new Random();

            while (
                result.Options.Count < optionCount &&
                available.Count > 0)
            {
                int index =
                    random.Next(available.Count);

                CardData card =
                    available[index];

                available.RemoveAt(index);

                result.Options.Add(card);
            }

            return result;
        }
    }
}