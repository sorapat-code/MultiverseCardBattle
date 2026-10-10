using System;
using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// 1 ใบที่ได้จากแพ็ค และเป็นการ์ดใหม่ที่ผู้เล่นยังไม่เคยมีหรือไม่
    /// </summary>
    public class PackPull
    {
        public CardData Card;
        public bool IsNew;
    }

    /// <summary>
    /// ร้านค้า: ซื้อแพ็คการ์ดด้วย Gold
    /// แพ็คมี 5 ใบ รับประกันอย่างน้อย 1 ใบระดับ Rare ขึ้นไป
    /// </summary>
    public static class PackService
    {
        public const int PackPrice = 100;
        public const int CardsPerPack = 5;

        // น้ำหนักความหายาก (รวม 100)
        private const int WeightCommon = 68;
        private const int WeightRare = 22;
        private const int WeightEpic = 8;
        private const int WeightLegendary = 2;

        /// <summary>
        /// ซื้อแพ็ค: หัก Gold, ใส่การ์ดเข้าคอลเลกชัน คืน null พร้อมข้อความถ้าซื้อไม่ได้
        /// (ผู้เรียกต้องเรียก SaveManager.Save() เอง)
        /// </summary>
        public static List<PackPull> Buy(
            PlayerProfile profile,
            Random rng,
            out string error)
        {
            error = null;

            if (profile == null)
            {
                error = "No profile";
                return null;
            }

            if (profile.Gold < PackPrice)
            {
                error = "Not enough gold (" + PackPrice + " needed)";
                return null;
            }

            List<CardData> cards = RollCards(rng);

            if (cards.Count == 0)
            {
                error = "No cards available";
                return null;
            }

            profile.TrySpendGold(PackPrice);

            List<PackPull> pulls = new List<PackPull>();

            foreach (CardData card in cards)
            {
                PackPull pull = new PackPull();

                pull.Card = card;
                pull.IsNew = profile.GetOwnedCount(card.CardID) == 0;

                profile.AddCard(card.CardID, 1);

                pulls.Add(pull);
            }

            return pulls;
        }

        /// <summary>
        /// สุ่มการ์ด 1 แพ็ค (ไม่แตะโปรไฟล์) ใช้ทดสอบได้
        /// </summary>
        public static List<CardData> RollCards(Random rng)
        {
            if (rng == null)
                rng = new Random();

            Dictionary<CardRarity, List<CardData>> byRarity =
                new Dictionary<CardRarity, List<CardData>>();

            List<CardData> all = new List<CardData>();

            foreach (CardData card in CardRegistry.All)
            {
                if (!CardRegistry.IsCollectible(card))
                    continue;

                // Secret ไม่ออกจากแพ็ค
                if (card.Rarity == CardRarity.Secret)
                    continue;

                all.Add(card);

                if (!byRarity.ContainsKey(card.Rarity))
                    byRarity[card.Rarity] = new List<CardData>();

                byRarity[card.Rarity].Add(card);
            }

            List<CardData> result = new List<CardData>();

            if (all.Count == 0)
                return result;

            bool hasRarePlus = false;

            for (int i = 0; i < CardsPerPack; i++)
            {
                bool lastSlot = i == CardsPerPack - 1;

                CardRarity rarity =
                    (lastSlot && !hasRarePlus)
                        ? RollRarePlus(rng)
                        : RollRarity(rng);

                CardData pick = Pick(byRarity, rarity, all, rng);

                if (pick.Rarity != CardRarity.Common)
                    hasRarePlus = true;

                result.Add(pick);
            }

            return result;
        }

        private static CardRarity RollRarity(Random rng)
        {
            int roll = rng.Next(100);

            if (roll < WeightCommon)
                return CardRarity.Common;

            roll -= WeightCommon;

            if (roll < WeightRare)
                return CardRarity.Rare;

            roll -= WeightRare;

            if (roll < WeightEpic)
                return CardRarity.Epic;

            return CardRarity.Legendary;
        }

        private static CardRarity RollRarePlus(Random rng)
        {
            int total = WeightRare + WeightEpic + WeightLegendary;

            int roll = rng.Next(total);

            if (roll < WeightRare)
                return CardRarity.Rare;

            roll -= WeightRare;

            if (roll < WeightEpic)
                return CardRarity.Epic;

            return CardRarity.Legendary;
        }

        private static CardData Pick(
            Dictionary<CardRarity, List<CardData>> byRarity,
            CardRarity rarity,
            List<CardData> all,
            Random rng)
        {
            List<CardData> pool;

            if (byRarity.TryGetValue(rarity, out pool) && pool.Count > 0)
                return pool[rng.Next(pool.Count)];

            // ไม่มีการ์ดระดับนี้ ใช้การ์ดอะไรก็ได้
            return all[rng.Next(all.Count)];
        }
    }
}
