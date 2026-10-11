using System.Collections.Generic;
using NUnit.Framework;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Tests
{
    /// <summary>ทดสอบความครบถ้วนของเนื้อเรื่อง Story Mode และการแจกรางวัลจบบท</summary>
    public class StoryTests
    {
        private static PlayerProfile NewProfile()
        {
            StoryService.ClearPending();

            return new PlayerProfile(new SaveData());
        }

        [Test]
        public void Story_HasFiveChaptersCoveringEveryStageOnce()
        {
            Assert.AreEqual(5, StoryLibrary.Chapters.Count);

            HashSet<string> seen = new HashSet<string>();

            foreach (StoryChapter chapter in StoryLibrary.Chapters)
            {
                Assert.AreEqual(2, chapter.StageIds.Count);
                Assert.Greater(chapter.RewardGold, 0);
                Assert.IsNotEmpty(chapter.Epilogue);

                foreach (string id in chapter.StageIds)
                {
                    Assert.IsNotNull(CampaignLibrary.Get(id), id);
                    Assert.IsTrue(seen.Add(id), id + " used twice");
                }
            }

            Assert.AreEqual(CampaignLibrary.Stages.Count, seen.Count);
        }

        [Test]
        public void EveryStage_HasIntroAndOutroDialogue()
        {
            foreach (CampaignStage stage in CampaignLibrary.Stages)
            {
                StoryBeat beat = StoryLibrary.GetBeat(stage.Id);

                Assert.IsNotNull(beat, stage.Id);
                Assert.GreaterOrEqual(beat.Intro.Count, 3, stage.Id);
                Assert.GreaterOrEqual(beat.Outro.Count, 2, stage.Id);

                foreach (DialogueLine line in beat.Intro)
                    Assert.IsNotEmpty(line.Text, stage.Id);

                foreach (DialogueLine line in beat.Outro)
                    Assert.IsNotEmpty(line.Text, stage.Id);
            }
        }

        [Test]
        public void ChapterOrder_FollowsStageUnlockOrder()
        {
            string previous = null;

            foreach (StoryChapter chapter in StoryLibrary.Chapters)
            {
                foreach (string id in chapter.StageIds)
                {
                    CampaignStage stage = CampaignLibrary.Get(id);

                    Assert.AreEqual(previous, stage.RequiresStageId, id);

                    previous = id;
                }
            }
        }

        [Test]
        public void ChapterReward_IsGivenOnceWhenSecondStageCleared()
        {
            PlayerProfile profile = NewProfile();

            StoryChapter chapter = StoryLibrary.GetChapter(1);

            List<string> lines = new List<string>();

            profile.MarkStageCompleted(chapter.StageIds[0]);
            StoryService.OnStageFirstCleared(profile, chapter.StageIds[0], lines);

            Assert.AreEqual(0, profile.Gold);
            Assert.AreEqual(chapter.StageIds[0], StoryService.PendingOutroStageId);
            Assert.AreEqual(0, StoryService.PendingEpilogueChapter);

            profile.MarkStageCompleted(chapter.StageIds[1]);
            StoryService.OnStageFirstCleared(profile, chapter.StageIds[1], lines);

            Assert.AreEqual(chapter.RewardGold, profile.Gold);
            Assert.AreEqual(1, StoryService.PendingEpilogueChapter);
            Assert.IsTrue(StoryService.IsChapterRewarded(profile, chapter));

            // เรียกซ้ำจะไม่แจกอีก
            StoryService.OnStageFirstCleared(profile, chapter.StageIds[1], lines);

            Assert.AreEqual(chapter.RewardGold, profile.Gold);

            StoryService.ClearPending();
        }

        [Test]
        public void CurrentChapter_AdvancesAsChaptersFinish()
        {
            PlayerProfile profile = NewProfile();

            Assert.AreEqual(1, StoryService.CurrentChapter(profile).Number);

            foreach (string id in StoryLibrary.GetChapter(1).StageIds)
                profile.MarkStageCompleted(id);

            Assert.AreEqual(2, StoryService.CurrentChapter(profile).Number);

            foreach (StoryChapter chapter in StoryLibrary.Chapters)
            {
                foreach (string id in chapter.StageIds)
                    profile.MarkStageCompleted(id);
            }

            Assert.IsNull(StoryService.CurrentChapter(profile));
        }
    }
}
