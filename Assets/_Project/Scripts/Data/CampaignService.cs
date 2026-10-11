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
            BattleSetup.EnemyLeaderId = stage.EnemyLeaderId;
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
            BattleSetup.EnemyLeaderId = null;
            BattleSetup.Stage = null;

            SceneLoader.Go(GameScene.Battle);

            return null;
        }

        /// <summary>
        /// แมตช์สอนเล่น: เด็คและมือเปิดถูกจัดไว้ คู่ซ้อมลงยูนิตแต่ไม่โจมตี
        /// ไม่นับสถิติ ไม่มีรางวัลปกติ (ได้ Gold โบนัสครั้งแรกที่ผ่าน)
        /// </summary>
        public static string StartTutorial()
        {
            BattleSetup.Reset();

            BattleSetup.Tutorial = true;
            BattleSetup.Difficulty = AiDifficulty.Normal;

            BattleSetup.PlayerDeck = MultiverseCardBattle.Battle.TutorialSetup.BuildPlayerDeck();
            BattleSetup.EnemyDeck = MultiverseCardBattle.Battle.TutorialSetup.BuildEnemyDeck();

            if (BattleSetup.PlayerDeck.Count == 0 || BattleSetup.EnemyDeck.Count == 0)
                return "Tutorial cards are missing";

            BattleSetup.PlayerName = "You";
            BattleSetup.EnemyName = "Training Dummy";
            BattleSetup.PlayerLeaderId = LeaderLibrary.UsableId(SaveManager.Profile);
            BattleSetup.EnemyLeaderId = null;
            BattleSetup.EnemyHealth = MultiverseCardBattle.Battle.TutorialSetup.EnemyHealth;

            // ผู้เล่นเริ่มก่อนเสมอ ไม่มี Mulligan
            BattleSetup.UseCoinFlip = false;
            BattleSetup.UseMulligan = false;

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

            BattleSetup.Difficulty = GameSettings.AiLevel;

            // โยนเหรียญสุ่มว่าใครเริ่มก่อน
            BattleSetup.UseCoinFlip = true;
            BattleSetup.UseMulligan = true;

            BattleSetup.PlayerDeck = StarterDecks.ToCards(deck.CardIDs);
            BattleSetup.PlayerName = "You";
            BattleSetup.PlayerLeaderId = LeaderLibrary.UsableId(profile);

            return null;
        }

        public const int GoldFirstClear = 120;
        public const int GoldStageReplay = 40;
        public const int GoldQuickWin = 30;
        public const int GoldLoss = 10;
        public const int TutorialGold = 100;

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

            // Tutorial: ไม่นับสถิติ/รางวัล แค่จำว่าเล่นจบแล้ว
            if (BattleSetup.Tutorial)
            {
                if (playerWon)
                {
                    bool first = !profile.Data.TutorialDone;

                    profile.Data.TutorialDone = true;

                    if (first)
                    {
                        profile.AddGold(TutorialGold);

                        lines.Add("Tutorial complete! Gold +" + TutorialGold);
                    }
                    else
                    {
                        lines.Add("Tutorial complete!");
                    }
                }
                else
                {
                    lines.Add("Tutorial not finished - try again from the main menu.");
                }

                SaveManager.Save();

                return lines;
            }

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

            // ระดับความยาก: ชนะบนระดับยากได้ Gold มากขึ้น (แพ้ไม่ปรับ)
            if (playerWon && BattleSetup.Difficulty != AiDifficulty.Normal)
            {
                gold = Mathf.Max(
                    1, Mathf.RoundToInt(
                        gold * AiDifficultyInfo.GoldScale(BattleSetup.Difficulty)));
            }

            profile.AddGold(gold);

            lines.Add("Gold +" + gold +
                (BattleSetup.Difficulty != AiDifficulty.Normal
                    ? " (" + AiDifficultyInfo.NameOf(BattleSetup.Difficulty) + ")"
                    : ""));

            // สถิติสำหรับ Achievement
            profile.Data.StatBattles++;

            if (playerWon)
                profile.Data.StatWins++;

            // ประวัติแมตช์ + สถิติรวม
            MatchRecord record = new MatchRecord();

            record.Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            record.Result = playerWon ? 1 : (summary != null && summary.Draw ? 2 : 0);
            record.Opponent = string.IsNullOrEmpty(BattleSetup.EnemyName)
                ? "Opponent"
                : BattleSetup.EnemyName;
            record.Mode = stage != null ? stage.Name : "Quick Battle";
            record.Difficulty = (int)BattleSetup.Difficulty;
            record.Gold = gold;

            if (summary != null)
            {
                record.Turns = summary.Turns;
                record.HealthLeft = Mathf.Max(0, summary.PlayerHealth);
                record.CardsPlayed = summary.CardsPlayed;
                record.UnitsKilled = summary.UnitsKilled;
                record.LeaderDamage = summary.LeaderDamage;
            }

            StatsService.Record(
                profile, record, summary != null ? summary.PlayedCardIds : null);

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
