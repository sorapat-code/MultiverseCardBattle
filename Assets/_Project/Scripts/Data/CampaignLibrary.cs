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
                    ("STR_001", 4), ("STR_002", 4), ("STR_003", 4),
                    ("STR_004", 3), ("STR_010", 3), ("STR_012", 3),
                    ("STR_013", 3), ("SPL_001", 3), ("SPL_003", 3)),
                RewardCardIds = new List<string> { "STR_012", "SPL_004", "HSR_001", "HSR_003" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_02",
                Name = "Bandit Road",
                Description = "Fast, aggressive raiders ambush travelers.",
                EnemyName = "Bandit Chief",
                EnemyHealth = 25,
                RequiresStageId = "stage_01",
                EnemyDeckIds = StarterDecks.Build(
                    ("STR_001", 3), ("STR_004", 3), ("STR_010", 3),
                    ("STR_012", 3), ("STR_023", 3), ("STR_006", 3),
                    ("STR_011", 2), ("STR_014", 3), ("SPL_001", 3),
                    ("SPL_002", 2), ("STR_007", 2)),
                RewardCardIds = new List<string> { "STR_023", "STR_005", "NAR_002", "NAR_003", "SPL_014" }
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
                    ("STR_006", 4), ("STR_008", 4), ("STR_007", 3),
                    ("STR_011", 3), ("STR_004", 3), ("STR_017", 3),
                    ("STR_001", 2), ("SPL_007", 3), ("SPL_002", 3),
                    ("SPL_001", 2)),
                RewardCardIds = new List<string> { "STR_008", "SPL_007", "HSR_002", "OPC_003", "SPL_011" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_04",
                Name = "Temple Siege",
                Description = "Taunts, shields and healing. Break the wall.",
                EnemyName = "High Priestess",
                EnemyHealth = 30,
                RequiresStageId = "stage_03",
                EnemyDeckIds = StarterDecks.Build(
                    ("STR_002", 4), ("STR_009", 4), ("STR_022", 4),
                    ("STR_003", 3), ("STR_005", 3), ("STR_013", 3),
                    ("STR_014", 3), ("SPL_003", 3), ("SPL_010", 3)),
                RewardCardIds = new List<string> { "STR_022", "SPL_010", "STR_011", "BLE_002", "OPC_004", "MYT_004" }
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
                    ("STR_017", 4), ("STR_018", 4), ("STR_019", 4),
                    ("STR_013", 3), ("STR_003", 2), ("STR_016", 3),
                    ("SPL_002", 3), ("SPL_005", 3), ("SPL_006", 2),
                    ("SPL_001", 2)),
                RewardCardIds = new List<string> { "STR_019", "SPL_009", "SPL_008", "WUW_001", "FAT_003", "SPL_012", "MYT_005" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_06",
                Name = "Titan Gate",
                Description = "Huge bodies and area damage guard the gate.",
                EnemyName = "Gatekeeper",
                EnemyHealth = 35,
                RequiresStageId = "stage_05",
                EnemyDeckIds = StarterDecks.Build(
                    ("STR_005", 4), ("STR_021", 3), ("STR_024", 4),
                    ("STR_009", 3), ("STR_002", 3), ("STR_014", 3),
                    ("STR_015", 3), ("STR_007", 3), ("SPL_006", 2),
                    ("SPL_007", 2)),
                RewardCardIds = new List<string> { "STR_015", "STR_016", "STR_024", "SPL_006", "NAR_001", "HSR_004", "SPL_013" }
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
                    ("STR_020", 3), ("STR_021", 3), ("STR_024", 3),
                    ("STR_016", 3), ("STR_005", 3), ("STR_009", 3),
                    ("STR_011", 3), ("STR_018", 3), ("STR_019", 2),
                    ("SPL_006", 2), ("SPL_007", 2)),
                RewardCardIds = new List<string> { "STR_021", "STR_020", "OPC_002", "MYT_003", "WUW_002" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_08",
                Name = "Grand Line Showdown",
                Description = "A rubber-limbed captain and his swordsman crew set sail for glory.",
                EnemyName = "Pirate Emperor",
                EnemyHealth = 35,
                RequiresStageId = "stage_07",
                EnemyDeckIds = StarterDecks.Build(
                    ("OPC_001", 2), ("OPC_002", 2), ("STR_004", 3),
                    ("STR_011", 3), ("STR_012", 3), ("STR_013", 2),
                    ("STR_009", 2), ("STR_007", 2), ("SPL_002", 3),
                    ("SPL_001", 3), ("SPL_007", 2)),
                RewardCardIds = new List<string> { "OPC_001", "DBZ_002", "NAR_004", "BLE_004" }
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
                    ("BLE_001", 2), ("BLE_002", 3), ("JJB_001", 2),
                    ("STR_010", 3), ("STR_006", 3), ("STR_007", 3),
                    ("STR_017", 2), ("STR_018", 3), ("SPL_007", 2),
                    ("SPL_002", 3), ("SPL_001", 3)),
                RewardCardIds = new List<string> { "BLE_001", "JJB_001", "BLE_003", "JJB_003", "DBZ_004", "SPL_015" }
            });

            stages.Add(new CampaignStage
            {
                Id = "stage_10",
                Name = "Gods of the Multiverse",
                Description = "Heroes and gods from every universe gather for the final battle.",
                EnemyName = "The Multiverse Sovereign",
                EnemyHealth = 45,
                RequiresStageId = "stage_09",
                EnemyDeckIds = StarterDecks.Build(
                    ("MYT_001", 1), ("MYT_002", 1), ("JJB_002", 1),
                    ("FAT_001", 1), ("DBZ_001", 1), ("NAR_001", 2),
                    ("HSR_002", 2), ("WUW_001", 2), ("STR_020", 2),
                    ("STR_021", 2), ("STR_009", 2), ("STR_024", 2),
                    ("STR_016", 2), ("SPL_006", 2), ("SPL_007", 2)),
                RewardCardIds = new List<string>
                {
                    "MYT_001", "MYT_002", "JJB_002", "FAT_001", "DBZ_001",
                    "FAT_002", "DBZ_003", "DBZ_005"
                }
            });
        }
    }
}
