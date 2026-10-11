using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Decks;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// สูตรโกงสำหรับ Dev ใช้เทสการ์ดทุกใบ เปิดได้เฉพาะ Editor / Development Build
    /// </summary>
    public static class DevCheats
    {
        public static bool Enabled
        {
            get { return Application.isEditor || Debug.isDebugBuild; }
        }

        /// <summary>การ์ดทั้งหมดที่เล่นได้ (รวมการ์ดลับ ไม่รวม Token) เรียงตาม ID</summary>
        public static List<CardData> AllCards()
        {
            List<CardData> list = new List<CardData>();

            foreach (CardData card in CardRegistry.All)
            {
                if (CardRegistry.IsDeckable(card) ||
                    card.CardID == StarterCardLibrary.DevilTriggerId)
                    list.Add(card);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.CardID, b.CardID));

            return list;
        }

        /// <summary>ใส่การ์ดทุกใบเข้าคอลเลกชันให้ครบจำนวนที่ใส่เด็คได้ (รวมการ์ดลับ) คืนจำนวนใบที่เพิ่ม</summary>
        public static int UnlockAllCards(PlayerProfile profile)
        {
            if (profile == null)
                return 0;

            int added = 0;

            foreach (CardData card in CardRegistry.All)
            {
                if (!CardRegistry.IsDeckable(card))
                    continue;

                int missing = DeckRules.MaxCopies(card) - profile.GetOwnedCount(card.CardID);

                if (missing <= 0)
                    continue;

                profile.AddCard(card.CardID, missing);

                added += missing;
            }

            return added;
        }

        public static void AddCurrency(PlayerProfile profile, int gold, int dust)
        {
            if (profile == null)
                return;

            profile.AddGold(gold);
            profile.AddDust(dust);
        }

        public static void ClearAllStages(PlayerProfile profile)
        {
            if (profile == null)
                return;

            foreach (CampaignStage stage in CampaignLibrary.Stages)
                profile.MarkStageCompleted(stage.Id);
        }
    }
}
