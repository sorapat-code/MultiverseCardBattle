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

            // โยนเหรียญสุ่มว่าใครเริ่มก่อน
            BattleSetup.UseCoinFlip = true;
            BattleSetup.UseMulligan = true;

            BattleSetup.PlayerDeck = StarterDecks.ToCards(deck.CardIDs);
            BattleSetup.PlayerName = "You";

            return null;
        }

        public const int GoldFirstClear = 120;
        public const int GoldStageReplay = 40;
        public const int GoldQuickWin = 30;
        public const int GoldLoss = 10;

        /// <summary>
        /// เรียกเมื่อการต่อสู้จบ: บันทึกความคืบหน้า/รางวัล/Gold คืนข้อความสรุปให้แสดง
        /// </summary>
        /// <summary>การ์ดลับที่เจอในการต่อสู้ ปลดล็อกเข้า Collection 1 ใบ (ครั้งแรกเท่านั้น)</summary>
        public static List<string> UnlockSecrets(
            PlayerProfile profile, IEnumerable<string> ids)
        {
            List<string> lines = new List<string>();

            if (profile == null || ids == null)
                return lines;

            foreach (string id in ids)
            {
                CardData card = CardRegistry.Get(id);

                if (card == null || profile.GetOwnedCount(id) > 0)
                    continue;

                profile.AddCard(id, 1);

                lines.Add("Secret card unlocked: " + card.CardName);
            }

            return lines;
        }

        public static List<string> OnBattleFinished(
            bool playerWon, BattleSummary summary = null)
        {
            List<string> lines = new List<string>();

            CampaignStage stage = BattleSetup.Stage;

            PlayerProfile profile = SaveManager.Profile;

            int gold;

            if (playerWon && stage != null)
            {
                bool firstClear = !profile.IsStageCompleted(stage.Id);

                profile.MarkStageCompleted(stage.Id);

                lines.Add("Stage cleared: " + stage.Name);

                if (firstClear)
                    StoryService.OnStageFirstCleared(profile, stage.Id, lines);

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

                gold = firstClear ? GoldFirstClear : GoldStageReplay;
            }
            else
            {
                gold = playerWon ? GoldQuickWin : GoldLoss;
            }

            profile.AddGold(gold);

            lines.Add("Gold +" + gold);

            // สถิติสำหรับ Achievement
            profile.Data.StatBattles++;

            if (playerWon)
                profile.Data.StatWins++;

            DailyService.ReportBattle(profile, playerWon, summary);

            if (summary != null)
            {
                lines.AddRange(MasteryService.ApplyBattle(
                    profile, summary.PlayedCardIds, playerWon));

                if (summary.SecretSummoned)
                    profile.Data.StatSecrets++;

                lines.AddRange(UnlockSecrets(profile, summary.SecretIds));

                if (playerWon)
                {
                    if (summary.PlayerMaxHealth > 0 &&
                        summary.PlayerHealth >= summary.PlayerMaxHealth)
                    {
                        profile.Data.StatFlawlessWins++;
                    }

                    if (summary.PlayerHealth > 0 && summary.PlayerHealth <= 3)
                        profile.Data.StatComebackWins++;

                    if (summary.Turns > 0 && summary.Turns <= 6)
                        profile.Data.StatFastWins++;

                    if (summary.OpponentDeckOut)
                        profile.Data.StatDeckOutWins++;
                }
            }

            lines.AddRange(
                AchievementService.ToLines(AchievementService.Check(profile)));

            SaveManager.Save();

            return lines;
        }
    }
}
