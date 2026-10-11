using System.Collections.Generic;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// รายการด่าน Campaign ทั้งหมด (เพิ่มด่านใหม่ได้ที่นี่โดยไม่ต้องแก้ระบบอื่น)
    /// </summary>
    public static class CampaignLibrary
    {
        private static List<CampaignStage> stages;

        public static IReadOnlyList<CampaignStage> Stages
        {
            get
            {
                Build();
                return stages;
            }
        }

        public static CampaignStage Get(string id)
        {
            Build();

            foreach (CampaignStage stage in stages)
            {
                if (stage.Id == id)
                    return stage;
            }

            return null;
        }

        private static void Build()
        {
            if (stages != null)
                return;

            stages = new List<CampaignStage>();

            stages.Add(new CampaignStage
            {
                Id = "stage_01",
                Name = "Training Grounds",
                Description = "A rookie sparring partner. Learn the basics.",
                EnemyName = "Training Dummy Captain",
                EnemyHealth = 20,
                EnemyDeckIds = StarterDecks.Build(
                    ("SUP_006", 4), ("SUP_011", 4), ("SUP_010", 4),
                    ("SUP_001", 3), ("SUP_002", 3), ("SUP_004", 3),
                    ("SUP_003", 3), ("MAG_001", 3), ("MAG_003", 3)),
                RewardCardIds = new List<string> { "SUP_007", "SUP_009", "MAG_015", "MAG_011", "EQP_001" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_02",
                Name = "Bandit Road",
                Description = "Fast, aggressive raiders ambush travelers.",
                EnemyName = "Bandit Chief",
                EnemyHealth = 25,
                EnemyLeaderId = LeaderLibrary.BanditChiefId,
                RequiresStageId = "stage_01",
                EnemyDeckIds = StarterDecks.Build(
                    ("SUP_006", 3), ("SUP_002", 4), ("SUP_009", 3),
                    ("SUP_007", 3), ("SUP_016", 3), ("SUP_017", 2),
                    ("SUP_003", 3), ("MAG_001", 3), ("MAG_015", 3),
                    ("SUP_005", 3)),
                RewardCardIds = new List<string> { "SUP_016", "SUP_017", "SUP_012", "MAG_008", "EQP_006" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_03",
                Name = "Shadow Alley",
                Description = "Stealth and poison. Watch your back.",
                EnemyName = "Night Broker",
                EnemyHealth = 30,
                RequiresStageId = "stage_02",
                EnemyDeckIds = StarterDecks.Build(
                    ("SUP_007", 4), ("SUP_010", 3), ("SUP_020", 3),
                    ("SUP_014", 3), ("SUP_017", 2), ("SUP_003", 3),
                    ("SUP_009", 3), ("MAG_008", 3), ("MAG_001", 3),
                    ("MAG_015", 3)),
                RewardCardIds = new List<string> { "SUP_014", "SUP_020", "MAG_008", "SUP_013", "EQP_002" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_04",
                Name = "Temple Siege",
                Description = "Taunts, shields and healing. Break the wall.",
                EnemyName = "High Priestess",
                EnemyHealth = 30,
                EnemyLeaderId = LeaderLibrary.HighPriestessId,
                RequiresStageId = "stage_03",
                EnemyDeckIds = StarterDecks.Build(
                    ("SUP_011", 4), ("SUP_008", 4), ("SUP_015", 3),
                    ("SUP_019", 3), ("SUP_004", 3), ("SUP_005", 3),
                    ("SUP_012", 2), ("MAG_003", 3), ("MAG_005", 3),
                    ("MAG_011", 2)),
                RewardCardIds = new List<string> { "SUP_015", "SUP_019", "MAG_005", "MAG_009", "SUP_021", "EQP_003" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_05",
                Name = "Mage Tower",
                Description = "Burn, stun and silence from a tower of spellcasters.",
                EnemyName = "Archmage",
                EnemyHealth = 30,
                RequiresStageId = "stage_04",
                EnemyDeckIds = StarterDecks.Build(
                    ("SUP_012", 3), ("SUP_013", 3), ("SUP_014", 3),
                    ("SUP_018", 3), ("SUP_001", 2), ("SUP_003", 2),
                    ("MAG_001", 3), ("MAG_002", 3), ("MAG_007", 3),
                    ("MAG_006", 3), ("MAG_010", 2)),
                RewardCardIds = new List<string> { "SUP_018", "MAG_007", "MAG_010", "MAG_002", "MAG_012", "EQP_007" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_06",
                Name = "Titan Gate",
                Description = "Huge bodies and area damage guard the gate.",
                EnemyName = "Gatekeeper",
                EnemyHealth = 35,
                EnemyLeaderId = LeaderLibrary.GatekeeperId,
                RequiresStageId = "stage_05",
                EnemyDeckIds = StarterDecks.Build(
                    ("SUP_015", 3), ("SUP_011", 3), ("SUP_008", 3),
                    ("SUP_019", 3), ("SUP_023", 2), ("SUP_026", 2),
                    ("SUP_024", 2), ("SUP_021", 3), ("MAG_004", 4),
                    ("MAG_012", 1), ("SUP_005", 4)),
                RewardCardIds = new List<string> { "SUP_026", "MAG_004", "MAG_013", "EQP_004" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_07",
                Name = "Dragon Peak",
                Description = "The final boss waits at the top of the world.",
                EnemyName = "Ancient Dragon Lord",
                EnemyHealth = 40,
                RequiresStageId = "stage_06",
                EnemyDeckIds = StarterDecks.Build(
                    ("SUP_025", 3), ("SUP_024", 3), ("SUP_022", 3),
                    ("SUP_023", 2), ("SUP_017", 3), ("SUP_021", 3),
                    ("SUP_018", 3), ("MAG_012", 2), ("MAG_007", 3),
                    ("MAG_004", 2), ("MAG_014", 1), ("SUP_020", 2)),
                RewardCardIds = new List<string> { "SUP_022", "SUP_025", "MAG_012", "EQP_008" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_08",
                Name = "Grand Line Showdown",
                Description = "A rubber-limbed captain and his swordsman crew set sail for glory.",
                EnemyName = "Pirate Emperor",
                EnemyHealth = 35,
                EnemyLeaderId = LeaderLibrary.PirateEmperorId,
                RequiresStageId = "stage_07",
                EnemyDeckIds = StarterDecks.Build(
                    ("SUP_003", 4), ("SUP_004", 3), ("SUP_012", 4),
                    ("SUP_005", 2), ("SUP_002", 2), ("SUP_017", 3),
                    ("SUP_016", 3), ("SUP_020", 2), ("MAG_001", 3),
                    ("MAG_007", 3), ("MAG_011", 1)),
                RewardCardIds = new List<string> { "SUP_024", "SUP_023", "MAG_013" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_09",
                Name = "Soul Reaper's Trial",
                Description = "Spirit blades, stand power and a berserk substitute reaper.",
                EnemyName = "Captain of the Gotei 13",
                EnemyHealth = 40,
                RequiresStageId = "stage_08",
                EnemyDeckIds = StarterDecks.Build(
                    ("SUP_013", 4), ("SUP_016", 3), ("SUP_020", 3),
                    ("SUP_021", 3), ("SUP_014", 3), ("SUP_022", 2),
                    ("SUP_025", 2), ("SUP_018", 2), ("MAG_002", 3),
                    ("MAG_008", 3), ("MAG_010", 2)),
                RewardCardIds = new List<string> { "MAG_014", "SUP_026", "EQP_005" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_10",
                Name = "Gods of the Multiverse",
                Description = "Heroes and gods from every universe gather for the final battle.",
                EnemyName = "The Multiverse Sovereign",
                EnemyHealth = 45,
                EnemyLeaderId = LeaderLibrary.SovereignId,
                RequiresStageId = "stage_09",
                EnemyDeckIds = StarterDecks.Build(
                    ("SUP_022", 3), ("SUP_023", 3), ("SUP_024", 3),
                    ("SUP_025", 3), ("SUP_026", 3), ("SUP_021", 3),
                    ("SUP_018", 3), ("MAG_012", 3), ("MAG_013", 2),
                    ("MAG_014", 2), ("MAG_007", 2)),
                RewardCardIds = new List<string> { "MAG_014" }
            });
        }
    }
}
