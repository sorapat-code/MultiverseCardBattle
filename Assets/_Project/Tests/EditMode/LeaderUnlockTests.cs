using NUnit.Framework;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Tests
{
    /// <summary>เริ่มเกมมี Leader แค่ Goku (กับ No Leader) ตัวอื่นปลดล็อกด้วยการชนะด่านใน Story</summary>
    public class LeaderUnlockTests
    {
        private static PlayerProfile NewProfile()
        {
            return new PlayerProfile(new SaveData());
        }

        [Test]
        public void NewProfile_OnlyGokuIsUnlocked()
        {
            PlayerProfile profile = NewProfile();

            foreach (LeaderData leader in LeaderLibrary.All)
            {
                if (!leader.PlayerSelectable)
                    continue;

                Assert.AreEqual(
                    leader.Id == LeaderLibrary.GokuId,
                    LeaderLibrary.IsUnlocked(profile, leader),
                    leader.Id);
            }
        }

        [Test]
        public void ClearingTheStage_UnlocksTheLeader()
        {
            PlayerProfile profile = NewProfile();

            LeaderData naruto = LeaderLibrary.Get(LeaderLibrary.NarutoId);

            Assert.IsFalse(LeaderLibrary.IsUnlocked(profile, naruto));

            profile.MarkStageCompleted(naruto.UnlockStageId);

            Assert.IsTrue(LeaderLibrary.IsUnlocked(profile, naruto));
        }

        [Test]
        public void UnlockStages_ExistInTheCampaign()
        {
            foreach (LeaderData leader in LeaderLibrary.All)
            {
                if (!string.IsNullOrEmpty(leader.UnlockStageId))
                    Assert.IsNotNull(CampaignLibrary.Get(leader.UnlockStageId), leader.Id);
            }
        }

        [Test]
        public void UsableId_FallsBackToGokuWhenSavedLeaderIsLocked_AndKeepsNoLeader()
        {
            PlayerProfile profile = NewProfile();

            profile.Data.LeaderId = LeaderLibrary.TonyId;

            Assert.AreEqual(LeaderLibrary.GokuId, LeaderLibrary.UsableId(profile));

            profile.Data.LeaderId = "";

            Assert.AreEqual("", LeaderLibrary.UsableId(profile));

            profile.MarkStageCompleted(LeaderLibrary.Get(LeaderLibrary.TonyId).UnlockStageId);
            profile.Data.LeaderId = LeaderLibrary.TonyId;

            Assert.AreEqual(LeaderLibrary.TonyId, LeaderLibrary.UsableId(profile));
        }
    }
}
