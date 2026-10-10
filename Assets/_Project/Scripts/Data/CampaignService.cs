using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Decks;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// เชื่อมโปรไฟล์ผู้เล่น + ด่าน Campaign เข้ากับซีน Battle
    /// </summary>
    public static class CampaignService
    {
        public static bool IsUnlocked(CampaignStage stage)
        {
            if (stage == null)
                return false;

            if (string.IsNullOrEmpty(stage.RequiresStageId))
                return true;

            return SaveManager.Profile.IsStageCompleted(stage.RequiresStageId);
        }

        /// <summary>
        /// ตั้งค่า BattleSetup แล้วโหลดซีน Battle
        /// คืนข้อความผิดพลาด (เช่น เด็คไม่ถูกต้อง) หรือ null ถ้าเริ่มสำเร็จ
        /// </summary>
        public static string StartStage(CampaignStage stage)
        {
            if (stage == null)
                return "Unknown stage";

            if (!IsUnlocked(stage))
                return "This stage is locked";

            string error = PreparePlayerSide();

            if (error != null)
                return error;

            BattleSetup.EnemyDeck = StarterDecks.ToCards(stage.EnemyDeckIds);
            BattleSetup.EnemyName = stage.EnemyName;
            BattleSetup.EnemyHealth = stage.EnemyHealth;
            BattleSetup.Stage = stage;

            SceneLoader.Go(GameScene.Battle);

            return null;
        }

        /// <summary>
        /// สู้แบบทันที กับเด็คศัตรูสุ่มจากด่านใดด่านหนึ่ง (ไม่ได้รางวัล)
        /// </summary>
        public static string StartQuickBattle()
        {
            string error = PreparePlayerSide();

            if (error != null)
                return error;

            IReadOnlyList<CampaignStage> stages = CampaignLibrary.Stages;

            CampaignStage pick = stages[Random.Range(0, stages.Count)];

            BattleSetup.EnemyDeck = StarterDecks.ToCards(pick.EnemyDeckIds);
            BattleSetup.EnemyName = pick.EnemyName;
            BattleSetup.EnemyHealth = pick.EnemyHealth;
            BattleSetup.Stage = null;

            SceneLoader.Go(GameScene.Battle);

            return null;
        }

        private static string PreparePlayerSide()
        {
            PlayerProfile profile = SaveManager.Profile;

            DeckList deck = profile.ActiveDeck;

            List<string> problems = DeckRules.Validate(deck, profile);

            if (problems.Count > 0)
                return "Your active deck is not valid: " + problems[0];

            BattleSetup.Reset();

            BattleSetup.PlayerDeck = StarterDecks.ToCards(deck.CardIDs);
            BattleSetup.PlayerName = "You";

            return null;
        }

        /// <summary>
        /// เรียกเมื่อการต่อสู้จบ: บันทึกความคืบหน้า/รางวัล คืนข้อความสรุปให้แสดง
        /// </summary>
        public static List<string> OnBattleFinished(bool playerWon)
        {
            List<string> lines = new List<string>();

            CampaignStage stage = BattleSetup.Stage;

            if (stage == null || !playerWon)
                return lines;

            PlayerProfile profile = SaveManager.Profile;

            bool firstClear = !profile.IsStageCompleted(stage.Id);

            profile.MarkStageCompleted(stage.Id);

            lines.Add("Stage cleared: " + stage.Name);

            if (firstClear && stage.RewardCardIds != null)
            {
                foreach (string cardId in stage.RewardCardIds)
                {
                    CardData card = CardRegistry.Get(cardId);

                    if (card == null)
                        continue;

                    int copies = DeckRules.MaxCopies(card);

                    profile.AddCard(cardId, copies);

                    lines.Add("Reward: " + card.CardName + " x" + copies);
                }
            }

            SaveManager.Save();

            return lines;
        }
    }
}
