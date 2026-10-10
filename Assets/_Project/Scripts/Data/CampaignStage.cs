using System.Collections.Generic;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// ด่าน 1 ด่านของ Campaign: ศัตรู, เด็คศัตรู และรางวัลที่ได้เมื่อชนะครั้งแรก
    /// </summary>
    public class CampaignStage
    {
        public string Id;
        public string Name;
        public string Description;

        public string EnemyName;
        public int EnemyHealth;

        // ID การ์ดในเด็คศัตรู (ใส่ซ้ำตามจำนวนใบ)
        public List<string> EnemyDeckIds;

        // การ์ดรางวัลเมื่อผ่านด่านครั้งแรก (จำนวนใบที่ได้ = MaxCopies ของการ์ด)
        public List<string> RewardCardIds;

        // ต้องผ่านด่านนี้ก่อนจึงเล่นได้ (null = เล่นได้เลย)
        public string RequiresStageId;
    }
}
