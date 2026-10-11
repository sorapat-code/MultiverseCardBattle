using System.Collections.Generic;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// ความคืบหน้า Story Mode: ตรวจจบบท แจกรางวัลบท และจำว่าต้องแสดงฉากหลังชนะอะไร
    /// (ใช้ด่านและเซฟของ Campaign เดิม)
    /// </summary>
    public static class StoryService
    {
        // ด่านที่เพิ่งผ่านครั้งแรก รอเล่นฉากหลังชนะตอนกลับเข้าหน้า Story
        public static string PendingOutroStageId;

        // บทที่เพิ่งจบ รอเล่น Epilogue
        public static int PendingEpilogueChapter;

        public static int ClearedStagesInChapter(PlayerProfile profile, StoryChapter chapter)
        {
            int count = 0;

            foreach (string id in chapter.StageIds)
            {
                if (profile.IsStageCompleted(id))
                    count++;
            }

            return count;
        }

        public static bool IsChapterComplete(PlayerProfile profile, StoryChapter chapter)
        {
            return chapter != null &&
                ClearedStagesInChapter(profile, chapter) >= chapter.StageIds.Count;
        }

        public static bool IsChapterRewarded(PlayerProfile profile, StoryChapter chapter)
        {
            return profile.Data.StoryChaptersRewarded.Contains("chapter_" + chapter.Number);
        }

        /// <summary>บทที่ผู้เล่นควรเล่นต่อ (บทแรกที่ยังไม่จบ) null = จบทุกบทแล้ว</summary>
        public static StoryChapter CurrentChapter(PlayerProfile profile)
        {
            foreach (StoryChapter chapter in StoryLibrary.Chapters)
            {
                if (!IsChapterComplete(profile, chapter))
                    return chapter;
            }

            return null;
        }

        /// <summary>
        /// เรียกหลังผ่านด่านครั้งแรก: จำฉากหลังชนะ และแจกรางวัลบทถ้าบทนั้นจบ
        /// เพิ่มข้อความสรุปลง lines (เรียกก่อนบันทึกเซฟ)
        /// </summary>
        public static void OnStageFirstCleared(
            PlayerProfile profile, string stageId, List<string> lines)
        {
            if (profile == null || string.IsNullOrEmpty(stageId))
                return;

            PendingOutroStageId = stageId;

            StoryChapter chapter = StoryLibrary.ChapterOf(stageId);

            if (chapter == null ||
                !IsChapterComplete(profile, chapter) ||
                IsChapterRewarded(profile, chapter))
                return;

            profile.Data.StoryChaptersRewarded.Add("chapter_" + chapter.Number);

            profile.AddGold(chapter.RewardGold);
            profile.AddDust(chapter.RewardDust);

            PendingEpilogueChapter = chapter.Number;

            if (lines != null)
            {
                lines.Add(
                    "Chapter " + chapter.Number + " complete: " + chapter.Title +
                    "  (+" + chapter.RewardGold + " Gold, +" +
                    chapter.RewardDust + " Dust)");
            }
        }

        public static void ClearPending()
        {
            PendingOutroStageId = null;
            PendingEpilogueChapter = 0;
        }
    }
}
