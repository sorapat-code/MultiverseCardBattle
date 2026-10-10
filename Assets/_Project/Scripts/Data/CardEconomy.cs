using System;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Decks;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// ย่อยการ์ดเป็น Dust และใช้ Dust คราฟการ์ด (ตัวเลขเรียงตามความหายาก)
    /// การ์ดลับ/Token/Leader ย่อยและคราฟไม่ได้
    /// </summary>
    public static class CardEconomy
    {
        public static int DisenchantValue(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return 5;
                case CardRarity.Rare: return 20;
                case CardRarity.Epic: return 100;
                case CardRarity.Legendary: return 400;
                default: return 0;
            }
        }

        public static int CraftCost(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return 40;
                case CardRarity.Rare: return 100;
                case CardRarity.Epic: return 400;
                case CardRarity.Legendary: return 1600;
                default: return 0;
            }
        }

        /// <summary>จำนวนใบที่ย่อยได้โดยไม่กระทบเด็คใบใด (เหลือพอให้เด็คที่ใช้อยู่ครบ)</summary>
        public static int DisenchantableCopies(PlayerProfile profile, CardData card)
        {
            if (profile == null || card == null || !CardRegistry.IsCollectible(card))
                return 0;

            int owned = profile.GetOwnedCount(card.CardID);

            return Math.Max(0, owned - profile.MaxCopiesInDecks(card.CardID));
        }

        /// <summary>ใบส่วนเกิน: เกินจำนวนที่ใส่เด็คได้สูงสุด และไม่อยู่ในเด็คใด</summary>
        public static int ExtraCopies(PlayerProfile profile, CardData card)
        {
            if (profile == null || card == null || !CardRegistry.IsCollectible(card))
                return 0;

            int keep = Math.Max(
                DeckRules.MaxCopies(card),
                profile.MaxCopiesInDecks(card.CardID));

            return Math.Max(0, profile.GetOwnedCount(card.CardID) - keep);
        }

        public static bool Disenchant(
            PlayerProfile profile, CardData card, int count, out string message)
        {
            message = null;

            if (profile == null || card == null || count <= 0)
            {
                message = "Nothing to disenchant";
                return false;
            }

            if (!CardRegistry.IsCollectible(card))
            {
                message = "This card cannot be disenchanted";
                return false;
            }

            int available = DisenchantableCopies(profile, card);

            if (available <= 0)
            {
                message = profile.GetOwnedCount(card.CardID) > 0
                    ? "Every copy is used in one of your decks"
                    : "You do not own this card";
                return false;
            }

            int amount = Math.Min(count, available);

            profile.RemoveCard(card.CardID, amount);

            int dust = DisenchantValue(card.Rarity) * amount;

            profile.AddDust(dust);

            profile.Data.StatDisenchanted += amount;

            message = "Disenchanted " + card.CardName + " x" + amount +
                " (+" + dust + " Dust)";

            return true;
        }

        /// <summary>ย่อยใบส่วนเกินของทุกการ์ด คืนจำนวน Dust ที่ได้</summary>
        public static int DisenchantAllExtras(PlayerProfile profile, out int cards)
        {
            cards = 0;

            if (profile == null)
                return 0;

            int dust = 0;

            foreach (CardData card in CardRegistry.All)
            {
                int extra = ExtraCopies(profile, card);

                if (extra <= 0)
                    continue;

                profile.RemoveCard(card.CardID, extra);

                dust += DisenchantValue(card.Rarity) * extra;
                cards += extra;
            }

            if (dust > 0)
            {
                profile.AddDust(dust);
                profile.Data.StatDisenchanted += cards;
            }

            return dust;
        }

        public static bool CanCraft(
            PlayerProfile profile, CardData card, out string reason)
        {
            reason = null;

            if (card != null && !CardRegistry.IsCollectible(card) &&
                CardRegistry.IsDeckable(card))
            {
                reason = "Secret cards cannot be crafted - they must be unlocked";
                return false;
            }

            if (profile == null || card == null || !CardRegistry.IsCollectible(card))
            {
                reason = "This card cannot be crafted";
                return false;
            }

            if (profile.GetOwnedCount(card.CardID) >= DeckRules.MaxCopies(card))
            {
                reason = "You already own the maximum copies (" +
                    DeckRules.MaxCopies(card) + ")";
                return false;
            }

            int cost = CraftCost(card.Rarity);

            if (profile.Dust < cost)
            {
                reason = "Not enough Dust (" + cost + " needed)";
                return false;
            }

            return true;
        }

        public static bool Craft(
            PlayerProfile profile, CardData card, out string message)
        {
            if (!CanCraft(profile, card, out message))
                return false;

            int cost = CraftCost(card.Rarity);

            profile.TrySpendDust(cost);
            profile.AddCard(card.CardID, 1);

            profile.Data.StatCrafted++;

            message = "Crafted " + card.CardName + " (-" + cost + " Dust)";

            return true;
        }
    }
}
