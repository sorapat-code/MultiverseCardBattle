using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// เมนูหลัก + แผงร้านค้า (ซื้อแพ็ค) + แผงวิธีเล่น อยู่ในซีนเดียวกัน
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        private static readonly Vector2 PackCardSize = new Vector2(210f, 294f);

        private const string HelpText =
            "<b>Goal</b>: reduce the enemy Leader's HP to 0.\n\n" +
            "<b>Start</b>: a coin flip decides who goes first. The second player draws an extra card and gets The Coin (+1 Energy).\n" +
            "<b>Turns</b>: each turn you draw a card and gain 1 max Energy (up to 10). Cards cost Energy.\n" +
            "<b>Play</b>: click a card in your hand. Units join your field (max 7); spells act immediately. " +
            "If a card needs a target, click a highlighted (red) target.\n" +
            "<b>Attack</b>: click one of your ready units (green outline), then click an enemy unit or the enemy Leader. " +
            "A unit cannot attack the turn it is played unless it has Charge or Rush.\n\n" +
            "<b>Keywords</b>\n" +
            "Taunt, Charge, Rush, Shield, Lifesteal, Backstab, Stealth, Double Strike, Pierce, Quick Attack, Overwhelm, " +
            "Unstoppable, Guard, Ward, Immune, Reborn, Shadowbound, Unique, Battlecry, Deathrattle, Spellburst, Aura, Combo, Evolve, Adapt...\n" +
            "<b>Status</b>: Stun, Silence, Freeze, Burn, Poison, Barrier, Regeneration and more.\n" +
            "See <b>Keywords & Status</b> in the main menu for every keyword, status and game term (Discover, Energy, Graveyard...).\n" +
            "While battling, hover a card to see it larger with an explanation of its keywords underneath.\n\n" +
            "<b>Battle tools</b>: the Graveyard buttons show destroyed cards, and Opp. Hand shows the cards in your opponent's hand.\n" +
            "<b>Keys</b>: Space - end turn.  Esc - cancel the current selection or close a window.  Hover a card to see it larger.\n\n" +
            "<b>Deck-out</b>: if your deck ever reaches 0 cards you lose immediately - watch your draws!\n\n" +
            "<b>Progress</b>: win battles to earn Gold and campaign reward cards. " +
            "Spend Gold in the Shop to open packs, then build your deck in the Deck Builder.\n" +
            "<b>Deck rules</b>: 20-30 cards, up to 3 copies of a card (Unique cards: 1). Secret cards appear in your Collection only after you unlock them.\n<b>Collection</b>: click a card to see it full size. Disenchant spare copies into Dust and spend Dust to craft cards you are missing.\n" +
            "<b>Achievements</b>: complete goals for Gold and Dust. Some are hidden...";

        private RectTransform root;
        private RectTransform mainPanel;
        private RectTransform shopPanel;
        private RectTransform helpPanel;
        private RectTransform glossaryPanel;
        private RectTransform achievementPanel;
        private RectTransform achievementList;
        private RectTransform poolPanel;
        private RectTransform poolGrid;
        private Text poolTitle;
        private Text poolSummary;
        private RectTransform dailyPanel;
        private RectTransform dailyContent;
        private RectTransform milestonePanel;
        private RectTransform devPanel;
        private Text devMessage;
        private RectTransform milestoneList;
        private Text dailyGoldText;
        private Text milestoneGoldText;
        private Button dailyMenuButton;
        private Button milestoneMenuButton;
        private Text achievementSummary;

        private Text message;
        private Text goldText;
        private Text shopGoldText;
        private Text shopMessage;
        private RectTransform packRow;

        private Button resetButton;
        private Button soundButton;
        private bool resetArmed;

        private readonly System.Random rng = new System.Random();

        private void Start()
        {
            root = UIFactory.CreateScreenRoot("MainMenuCanvas");

            mainPanel = CreatePanel("MainPanel");
            shopPanel = CreatePanel("ShopPanel");
            helpPanel = CreatePanel("HelpPanel");
            glossaryPanel = CreatePanel("GlossaryPanel");
            achievementPanel = CreatePanel("AchievementPanel");
            dailyPanel = CreatePanel("DailyPanel");
            milestonePanel = CreatePanel("MilestonePanel");
            devPanel = CreatePanel("DevPanel");

            BuildMain();
            BuildShop();
            BuildHelp();
            BuildGlossary();
            BuildAchievements();
            BuildDaily();
            BuildMilestones();
            BuildDev();

            Show(mainPanel);
        }

        private RectTransform CreatePanel(string name)
        {
            RectTransform panel = UIFactory.CreateRect(name, root);

            UIFactory.Stretch(panel);

            return panel;
        }

        private void Show(RectTransform panel)
        {
            mainPanel.gameObject.SetActive(panel == mainPanel);
            shopPanel.gameObject.SetActive(panel == shopPanel);
            helpPanel.gameObject.SetActive(panel == helpPanel);
            glossaryPanel.gameObject.SetActive(panel == glossaryPanel);
            achievementPanel.gameObject.SetActive(panel == achievementPanel);
            dailyPanel.gameObject.SetActive(panel == dailyPanel);
            if (poolPanel != null)
                poolPanel.gameObject.SetActive(panel == poolPanel);
            milestonePanel.gameObject.SetActive(panel == milestonePanel);
            if (devPanel != null)
                devPanel.gameObject.SetActive(panel == devPanel);

            RefreshMenuNotices();
            RefreshGold();
        }

        private void RefreshGold()
        {
            string gold = "Gold: " + SaveManager.Profile.Gold +
                "     <color=#99D9FF>Dust: " + SaveManager.Profile.Dust + "</color>";

            if (goldText != null)
                goldText.text = gold;

            if (shopGoldText != null)
                shopGoldText.text = gold;

            if (dailyGoldText != null)
                dailyGoldText.text = gold;

            if (milestoneGoldText != null)
                milestoneGoldText.text = gold;
        }

        // ปุ่มเมนูมี "(!)" เมื่อมีรางวัลให้รับ
        private void RefreshMenuNotices()
        {
            if (dailyMenuButton == null)
                return;

            PlayerProfile profile = SaveManager.Profile;

            UIFactory.SetButtonLabel(
                dailyMenuButton,
                DailyService.HasClaimable(profile) ? "Daily Rewards (!)" : "Daily Rewards");

            UIFactory.SetButtonLabel(
                milestoneMenuButton,
                MilestoneService.HasClaimable(profile) ? "Milestones (!)" : "Milestones");
        }

        // ---------- เมนูหลัก ----------

        private void BuildMain()
        {
            Sprite logo = UIArt.Get("logo");

            if (logo != null)
            {
                // มีไฟล์โลโก้ใน Resources/UI/logo ให้ใช้แทนข้อความ
                Image logoImage = UIFactory.CreatePanel(
                    mainPanel, "Logo", Color.white);

                logoImage.sprite = logo;
                logoImage.preserveAspect = true;
                logoImage.raycastTarget = false;

                UIFactory.SetAnchors(
                    logoImage.rectTransform, 0.25f, 0.84f, 0.75f, 1f);
            }
            else
            {
                Text title = UIFactory.CreateTitle(
                    mainPanel, "MULTIVERSE CARD BATTLE");

                title.fontSize = 72;
            }

            Text subtitle = ScreenKit.AddLabel(
                mainPanel, "Heroes from every universe, one battlefield", 28,
                TextAnchor.MiddleCenter, 0.2f, 0.82f, 0.8f, 0.89f,
                FontStyle.Italic);

            subtitle.color = UIFactory.GoldDim;

            ScreenKit.AddPanel(
                mainPanel, "ButtonFrame", new Color(0f, 0f, 0f, 0.35f),
                0.19f, 0.2f, 0.81f, 0.795f);

            goldText = ScreenKit.AddLabel(
                mainPanel, "", 30, TextAnchor.MiddleRight,
                0.74f, 0.93f, 0.98f, 0.99f, FontStyle.Bold);

            goldText.color = UIFactory.Gold;

            UIFactory.AddTextShadow(goldText);

            // ปุ่มเมนู 2 คอลัมน์ 6 แถว + ปุ่มออกตรงกลางแถวสุดท้าย
            float y = 0.725f;
            const float height = 0.07f;
            const float step = 0.082f;

            ScreenKit.AddButton(mainPanel, "Story Mode",
                () => SceneLoader.Go(GameScene.Campaign),
                UIFactory.Accent, 0.22f, y, 0.49f, y + height);

            ScreenKit.AddButton(mainPanel, "Quick Battle",
                StartQuickBattle,
                UIFactory.Success, 0.51f, y, 0.78f, y + height);

            y -= step;

            ScreenKit.AddButton(mainPanel, "Shop",
                () =>
                {
                    ClearPackRow();

                    shopMessage.text =
                        "A pack has " + PackService.CardsPerPack +
                        " cards. Choose a pack and check its Card Pool.";

                    Show(shopPanel);
                },
                UIFactory.Accent, 0.22f, y, 0.49f, y + height);

            ScreenKit.AddButton(mainPanel, "Deck Builder",
                () => SceneLoader.Go(GameScene.DeckBuilder),
                UIFactory.Accent, 0.51f, y, 0.78f, y + height);

            y -= step;

            ScreenKit.AddButton(mainPanel, "Collection",
                () => SceneLoader.Go(GameScene.Collection),
                UIFactory.Accent, 0.22f, y, 0.49f, y + height);

            ScreenKit.AddButton(mainPanel, "Achievements",
                () =>
                {
                    RebuildAchievements();

                    Show(achievementPanel);
                },
                UIFactory.Accent, 0.51f, y, 0.78f, y + height);

            y -= step;

            dailyMenuButton = ScreenKit.AddButton(mainPanel, "Daily Rewards",
                () =>
                {
                    RebuildDaily();

                    Show(dailyPanel);
                },
                UIFactory.Success, 0.22f, y, 0.49f, y + height, 26);

            milestoneMenuButton = ScreenKit.AddButton(mainPanel, "Milestones",
                () =>
                {
                    RebuildMilestones();

                    Show(milestonePanel);
                },
                UIFactory.Accent, 0.51f, y, 0.78f, y + height, 26);

            y -= step;

            ScreenKit.AddButton(mainPanel, "How to Play",
                () => Show(helpPanel),
                UIFactory.Muted, 0.22f, y, 0.49f, y + height);

            ScreenKit.AddButton(mainPanel, "Keywords & Status",
                () => Show(glossaryPanel),
                UIFactory.Muted, 0.51f, y, 0.78f, y + height, 24);

            y -= step;

            soundButton = ScreenKit.AddButton(mainPanel, "",
                ToggleSound,
                UIFactory.Muted, 0.22f, y, 0.49f, y + height, 26);

            RefreshSoundLabel();

            resetButton = ScreenKit.AddButton(mainPanel, "Reset Progress",
                OnResetClicked,
                UIFactory.Muted, 0.51f, y, 0.78f, y + height, 24);

#if !UNITY_WEBGL
            y -= step;

            ScreenKit.AddButton(mainPanel, "Quit",
                Application.Quit,
                UIFactory.Danger, 0.36f, y, 0.64f, y + height);
#endif

            message = ScreenKit.AddLabel(
                mainPanel, "", 26, TextAnchor.MiddleCenter,
                0.1f, 0.02f, 0.9f, 0.1f);
        }

        private void StartQuickBattle()
        {
            string error = CampaignService.StartQuickBattle();

            if (error != null)
                message.text = "<color=#FF8080>" + error + "</color>";
        }

        private void ToggleSound()
        {
            Sfx.Muted = !Sfx.Muted;

            SaveManager.Save();

            Sfx.RefreshMusicMute();

            RefreshSoundLabel();

            // เปิดเสียงแล้วให้ได้ยินตัวอย่าง
            Sfx.Play(SoundId.Click);
        }

        private void RefreshSoundLabel()
        {
            UIFactory.SetButtonLabel(
                soundButton, Sfx.Muted ? "Sound: Off" : "Sound: On");
        }

        // กดสองครั้งเพื่อยืนยัน กันกดผิด
        private void OnResetClicked()
        {
            if (!resetArmed)
            {
                resetArmed = true;

                UIFactory.SetButtonLabel(resetButton, "Press again to confirm");

                return;
            }

            SaveManager.ResetProgress();

            resetArmed = false;

            UIFactory.SetButtonLabel(resetButton, "Reset Progress");

            RefreshGold();
            RefreshSoundLabel();
            Sfx.RefreshMusicMute();

            message.text = "Progress reset. Starter collection restored.";
        }

        // ---------- ร้านค้า ----------

        private void BuildShop()
        {
            UIFactory.CreateTitle(shopPanel, "SHOP");

            shopGoldText = ScreenKit.AddLabel(
                shopPanel, "", 34, TextAnchor.MiddleCenter,
                0.3f, 0.82f, 0.7f, 0.89f, FontStyle.Bold);

            shopGoldText.color = UIFactory.Gold;

            // ซองคู่: Core (ซ้าย) และ Rift (ขวา) แต่ละซองมีปุ่มดู Card Pool
            BuildPackColumn(CardSet.Core, 0.08f, 0.46f, UIFactory.Success);
            BuildPackColumn(CardSet.Rift, 0.54f, 0.92f, new Color(0.35f, 0.3f, 0.7f, 1f));

            ScreenKit.AddLabel(
                shopPanel,
                "Odds: Common 68%   Rare 22%   Epic 8%   Legendary 2%   -   every pack has at least one Rare or better",
                20, TextAnchor.MiddleCenter, 0.1f, 0.585f, 0.9f, 0.625f);

            packRow = UIFactory.CreateZone(
                shopPanel, "PackRow", 0.03f, 0.2f, 0.97f, 0.58f);

            UIFactory.AddHorizontalRow(packRow, 14f);

            shopMessage = ScreenKit.AddLabel(
                shopPanel, "", 26, TextAnchor.MiddleCenter,
                0.2f, 0.1f, 0.8f, 0.18f);

            ScreenKit.AddButton(shopPanel, "Back",
                () => Show(mainPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);

            BuildPool();
        }

        private void BuildPackColumn(CardSet set, float x0, float x1, Color color)
        {
            ScreenKit.AddLabel(
                shopPanel, CardSetInfo.Description(set), 20, TextAnchor.MiddleCenter,
                x0, 0.715f, x1, 0.77f).color = UIFactory.GoldDim;

            ScreenKit.AddButton(shopPanel,
                "Buy " + CardSetInfo.Name(set) + "  (" + PackService.PriceOf(set) + " Gold)",
                () => BuyPack(set),
                color, x0, 0.775f, x1, 0.855f, 28);

            ScreenKit.AddButton(shopPanel,
                "View Card Pool (" + PackService.PoolOf(set).Count + " cards)",
                () => ShowPool(set),
                UIFactory.Muted, x0 + 0.04f, 0.64f, x1 - 0.04f, 0.71f, 22);
        }

        private void ClearPackRow()
        {
            ScreenKit.ClearNow(packRow);
        }

        private void BuyPack(CardSet set)
        {
            string error;

            List<PackPull> pulls =
                PackService.Buy(SaveManager.Profile, rng, set, out error);

            if (pulls == null)
            {
                shopMessage.text = "<color=#FF8080>" + error + "</color>";
                return;
            }

            SaveManager.Save();

            Sfx.Play(SoundId.Pack);

            ShowUnlocked(AchievementService.Check(SaveManager.Profile));

            ClearPackRow();

            int newCards = 0;

            foreach (PackPull pull in pulls)
            {
                CardView view = CardView.Create(packRow, PackCardSize);

                view.ShowData(pull.Card, pull.IsNew ? "NEW!" : "");

                if (pull.IsNew)
                {
                    view.SetHighlight(CardHighlight.Ready);
                    newCards++;
                }
            }

            shopMessage.text = newCards > 0
                ? newCards + " new card(s) added to your collection!"
                : "No new cards this time - extra copies added.";

            RefreshGold();
        }

        // ---------- Card Pool ----------

        private void BuildPool()
        {
            poolPanel = CreatePanel("PoolPanel");

            poolTitle = UIFactory.CreateTitle(poolPanel, "CARD POOL");

            poolSummary = ScreenKit.AddLabel(
                poolPanel, "", 26, TextAnchor.MiddleCenter,
                0.2f, 0.84f, 0.8f, 0.9f, FontStyle.Bold);

            poolSummary.color = UIFactory.Gold;

            ScreenKit.AddScroll(
                poolPanel, "PoolScroll", 0.03f, 0.14f, 0.97f, 0.84f, out poolGrid);

            UIFactory.AddGrid(poolGrid, new Vector2(210f, 294f), 14f, 14);

            ScreenKit.AddButton(poolPanel, "Back",
                () => Show(shopPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);

            poolPanel.gameObject.SetActive(false);
        }

        // แสดงการ์ดทั้งหมดที่ออกจากซองนี้ เรียงจากหายากสุด การ์ดที่ยังไม่มีจะมืด
        private void ShowPool(CardSet set)
        {
            ScreenKit.ClearNow(poolGrid);

            PlayerProfile profile = SaveManager.Profile;

            List<CardData> pool = PackService.PoolOf(set);

            pool.Sort((a, b) =>
            {
                int byRarity = b.Rarity.CompareTo(a.Rarity);

                if (byRarity != 0)
                    return byRarity;

                int byCost = a.Cost.CompareTo(b.Cost);

                return byCost != 0 ? byCost : string.CompareOrdinal(a.CardID, b.CardID);
            });

            int owned = 0;

            foreach (CardData card in pool)
            {
                int count = profile.GetOwnedCount(card.CardID);

                if (count > 0)
                    owned++;

                CardView view = CardView.Create(poolGrid, new Vector2(210f, 294f));

                view.ShowData(card, count > 0 ? "x" + count : "");
                view.SetDimmed(count == 0);
            }

            poolTitle.text = CardSetInfo.Name(set).ToUpperInvariant() + " - CARD POOL";

            poolSummary.text = pool.Count == 0
                ? "No cards in this pack yet"
                : "Owned " + owned + " / " + pool.Count + " different cards";

            Show(poolPanel);
        }

        private void ShowUnlocked(List<AchievementDef> list)
        {
            foreach (AchievementDef def in list)
            {
                Toast.Show(
                    root, "Achievement unlocked: " + def.Name,
                    AchievementService.RewardText(def));
            }
        }

        // ---------- Achievement ----------

        private void BuildAchievements()
        {
            UIFactory.CreateTitle(achievementPanel, "ACHIEVEMENTS");

            achievementSummary = ScreenKit.AddLabel(
                achievementPanel, "", 28, TextAnchor.MiddleCenter,
                0.3f, 0.82f, 0.7f, 0.89f, FontStyle.Bold);

            achievementSummary.color = UIFactory.Gold;

            ScreenKit.AddScroll(
                achievementPanel, "AchievementScroll",
                0.08f, 0.14f, 0.92f, 0.82f, out achievementList);

            UIFactory.AddVertical(achievementList, 8f, 10);

            ScreenKit.AddButton(achievementPanel, "Back",
                () => Show(mainPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);
        }

        private void RebuildAchievements()
        {
            ScreenKit.ClearNow(achievementList);

            PlayerProfile profile = SaveManager.Profile;

            achievementSummary.text =
                AchievementService.UnlockedCount(profile) + " / " +
                AchievementService.All.Count + " unlocked";

            foreach (AchievementDef def in AchievementService.All)
            {
                bool done = AchievementService.IsUnlocked(profile, def);

                bool hidden = def.Hidden && !done;

                int target = def.GetTarget();
                int progress = Mathf.Min(def.Progress(profile), target);

                Image row = UIFactory.CreateFramedPanel(
                    achievementList, "Row_" + def.Id,
                    done
                        ? new Color(0.22f, 0.17f, 0.08f, 0.96f)
                        : UIFactory.Panel);

                row.rectTransform.sizeDelta = new Vector2(0f, 96f);

                Text title = UIFactory.CreateText(
                    row.transform,
                    hidden ? "???" : def.Name,
                    28, done ? UIFactory.Gold : UIFactory.TextColor,
                    TextAnchor.MiddleLeft, FontStyle.Bold);

                UIFactory.SetAnchors(title.rectTransform, 0.02f, 0.5f, 0.6f, 0.95f);

                Text description = UIFactory.CreateText(
                    row.transform,
                    hidden ? "Hidden achievement" : def.Description,
                    22, new Color(0.8f, 0.76f, 0.68f, 1f), TextAnchor.MiddleLeft);

                UIFactory.SetAnchors(description.rectTransform, 0.02f, 0.05f, 0.6f, 0.5f);

                Text reward = UIFactory.CreateText(
                    row.transform,
                    hidden ? "Reward: ???" : "Reward: " + AchievementService.RewardText(def),
                    22, UIFactory.GoldDim, TextAnchor.MiddleRight);

                UIFactory.SetAnchors(reward.rectTransform, 0.6f, 0.5f, 0.97f, 0.95f);

                Text state = UIFactory.CreateText(
                    row.transform,
                    done
                        ? "<color=#70E080>UNLOCKED</color>"
                        : (hidden ? "" : progress + " / " + target),
                    24, UIFactory.TextColor, TextAnchor.MiddleRight, FontStyle.Bold);

                UIFactory.SetAnchors(state.rectTransform, 0.6f, 0.05f, 0.97f, 0.5f);
            }
        }

        // ---------- Daily Rewards ----------

        private void BuildDaily()
        {
            UIFactory.CreateTitle(dailyPanel, "DAILY REWARDS");

            dailyGoldText = ScreenKit.AddLabel(
                dailyPanel, "", 30, TextAnchor.MiddleRight,
                0.62f, 0.93f, 0.98f, 0.99f, FontStyle.Bold);

            dailyGoldText.color = UIFactory.Gold;

            dailyContent = UIFactory.CreateZone(
                dailyPanel, "DailyContent", 0.04f, 0.13f, 0.96f, 0.9f);

            ScreenKit.AddButton(dailyPanel, "Back",
                () => Show(mainPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);
        }

        private void RebuildDaily()
        {
            ScreenKit.ClearNow(dailyContent);

            PlayerProfile profile = SaveManager.Profile;

            DailyService.EnsureToday(profile);

            // ----- Daily Login -----
            Text loginTitle = ScreenKit.AddLabel(
                dailyContent, "Daily Login  -  come back every day (a missed day restarts the 7-day cycle)",
                26, TextAnchor.MiddleLeft, 0f, 0.88f, 1f, 1f, FontStyle.Bold);

            loginTitle.color = UIFactory.Gold;

            int claimedDays = DailyService.ClaimedDaysInCycle(profile);
            bool canClaim = DailyService.CanClaimLogin(profile);
            int nextDay = DailyService.NextLoginDay(profile);

            int cells = DailyService.LoginRewards.Length;

            for (int i = 0; i < cells; i++)
            {
                float x0 = i / (float)cells + 0.004f;
                float x1 = (i + 1) / (float)cells - 0.004f;

                bool done = i < claimedDays;
                bool isNext = canClaim && i + 1 == nextDay;

                Image cell = UIFactory.CreateFramedPanel(
                    dailyContent, "Login" + (i + 1),
                    done
                        ? new Color(0.22f, 0.17f, 0.08f, 0.96f)
                        : (isNext ? new Color(0.3f, 0.22f, 0.08f, 0.98f) : UIFactory.Panel));

                UIFactory.SetAnchors(cell.rectTransform, x0, 0.62f, x1, 0.86f);

                Text day = UIFactory.CreateText(
                    cell.transform, "Day " + (i + 1), 24,
                    done || isNext ? UIFactory.Gold : UIFactory.TextColor,
                    TextAnchor.MiddleCenter, FontStyle.Bold);

                UIFactory.SetAnchors(day.rectTransform, 0.02f, 0.62f, 0.98f, 0.98f);

                Text reward = UIFactory.CreateText(
                    cell.transform,
                    done ? "<color=#70E080>CLAIMED</color>" : DailyService.LoginRewards[i].Text,
                    20, UIFactory.TextColor, TextAnchor.MiddleCenter);

                UIFactory.SetAnchors(reward.rectTransform, 0.02f, 0.05f, 0.98f, 0.6f);
            }

            Button claim = ScreenKit.AddButton(
                dailyContent,
                canClaim ? "Claim Day " + nextDay + " Reward" : "Come back tomorrow",
                () =>
                {
                    DailyService.ClaimLogin(SaveManager.Profile);

                    SaveManager.Save();

                    Sfx.Play(SoundId.Pack);

                    RebuildDaily();
                    RefreshGold();
                    RefreshMenuNotices();
                },
                canClaim ? UIFactory.Success : UIFactory.Muted,
                0.3f, 0.5f, 0.7f, 0.6f, 26);

            claim.interactable = canClaim;

            // ----- Daily Quests -----
            System.TimeSpan left = DailyService.TimeUntilReset;

            Text questTitle = ScreenKit.AddLabel(
                dailyContent,
                "Daily Quests  -  new quests in " + (int)left.TotalHours + "h " + left.Minutes + "m",
                26, TextAnchor.MiddleLeft, 0f, 0.4f, 1f, 0.48f, FontStyle.Bold);

            questTitle.color = UIFactory.Gold;

            RectTransform questZone = UIFactory.CreateZone(
                dailyContent, "Quests", 0f, 0.1f, 1f, 0.4f);

            UIFactory.AddVertical(questZone, 8f, 0);

            foreach (DailyQuestState state in DailyService.Quests(profile))
            {
                QuestDef def = DailyService.GetDef(state.Id);

                if (def == null)
                    continue;

                bool complete = DailyService.IsComplete(state);

                Image row = UIFactory.CreateFramedPanel(
                    questZone, "Quest_" + def.Id,
                    state.Claimed
                        ? new Color(0.2f, 0.16f, 0.1f, 0.9f)
                        : UIFactory.Panel);

                row.rectTransform.sizeDelta = new Vector2(0f, 76f);

                string reward = new LoginReward { Gold = def.RewardGold, Dust = def.RewardDust }.Text;

                Text title = UIFactory.CreateText(
                    row.transform, def.Description, 26,
                    state.Claimed ? UIFactory.GoldDim : UIFactory.TextColor,
                    TextAnchor.MiddleLeft, FontStyle.Bold);

                UIFactory.SetAnchors(title.rectTransform, 0.02f, 0.08f, 0.5f, 0.92f);

                Text progress = UIFactory.CreateText(
                    row.transform,
                    Mathf.Min(state.Progress, def.Target) + " / " + def.Target,
                    26, UIFactory.TextColor, TextAnchor.MiddleCenter);

                UIFactory.SetAnchors(progress.rectTransform, 0.5f, 0.08f, 0.62f, 0.92f);

                Text rewardText = UIFactory.CreateText(
                    row.transform, reward, 22, UIFactory.GoldDim,
                    TextAnchor.MiddleCenter);

                UIFactory.SetAnchors(rewardText.rectTransform, 0.62f, 0.08f, 0.8f, 0.92f);

                string questId = def.Id;

                Button button = ScreenKit.AddButton(
                    row.transform,
                    state.Claimed ? "Claimed" : (complete ? "Claim" : "In progress"),
                    () =>
                    {
                        DailyService.Claim(SaveManager.Profile, questId);

                        SaveManager.Save();

                        Sfx.Play(SoundId.Pack);

                        RebuildDaily();
                        RefreshGold();
                        RefreshMenuNotices();
                    },
                    complete && !state.Claimed ? UIFactory.Success : UIFactory.Muted,
                    0.82f, 0.12f, 0.98f, 0.88f, 22);

                button.interactable = complete && !state.Claimed;
            }

            bool bonus = DailyService.CanClaimBonus(profile);

            Button bonusButton = ScreenKit.AddButton(
                dailyContent,
                profile.Data.QuestBonusClaimed
                    ? "Bonus claimed"
                    : "All quests bonus: +" + DailyService.BonusGold + " Gold  +" +
                      DailyService.BonusDust + " Dust",
                () =>
                {
                    DailyService.ClaimBonus(SaveManager.Profile);

                    SaveManager.Save();

                    Sfx.Play(SoundId.Pack);

                    RebuildDaily();
                    RefreshGold();
                    RefreshMenuNotices();
                },
                bonus ? UIFactory.Success : UIFactory.Muted,
                0.25f, 0f, 0.75f, 0.09f, 24);

            bonusButton.interactable = bonus;
        }

        // ---------- Collection Milestones ----------

        // ---------- Dev (เฉพาะ Editor / Development Build) ----------

        private readonly List<Button> devButtons = new List<Button>();

        private void BuildDev()
        {
            UIFactory.CreateTitle(devPanel, "DEV CHEATS");

            devMessage = ScreenKit.AddLabel(
                devPanel, "Dev cheats unlocked.", 26,
                TextAnchor.MiddleCenter, 0.1f, 0.12f, 0.9f, 0.22f);

            devButtons.Add(ScreenKit.AddButton(devPanel, "Unlock ALL cards (max copies, incl. secrets)",
                () =>
                {
                    int added = DevCheats.UnlockAllCards(SaveManager.Profile);

                    SaveManager.Save();
                    RefreshGold();

                    devMessage.text = "Added " + added + " cards to your collection.";
                },
                UIFactory.Accent, 0.25f, 0.72f, 0.75f, 0.82f));

            devButtons.Add(ScreenKit.AddButton(devPanel, "+10,000 Gold  /  +5,000 Dust",
                () =>
                {
                    DevCheats.AddCurrency(SaveManager.Profile, 10000, 5000);

                    SaveManager.Save();
                    RefreshGold();

                    devMessage.text = "Added 10,000 Gold and 5,000 Dust.";
                },
                UIFactory.Accent, 0.25f, 0.59f, 0.75f, 0.69f));

            devButtons.Add(ScreenKit.AddButton(devPanel, "Clear all Story stages",
                () =>
                {
                    DevCheats.ClearAllStages(SaveManager.Profile);

                    SaveManager.Save();

                    devMessage.text = "All Story stages marked as cleared.";
                },
                UIFactory.Accent, 0.25f, 0.46f, 0.75f, 0.56f));

            ScreenKit.AddLabel(
                devPanel,
                "In battle (Numpad):  1 = full Energy   2 = draw a card   3 = heal your Leader\n" +
                "4 = deal 10 to enemy Leader   5 = next card   - = previous card   6 = add selected card to hand",
                22, TextAnchor.MiddleCenter, 0.05f, 0.3f, 0.95f, 0.44f);

            devButtons.Add(ScreenKit.AddButton(devPanel, "Back",
                () => Show(mainPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f));

            foreach (Button button in devButtons)
                ButtonSelectGlow.Add(button);
        }

        // ปุ่มลับในเมนูหลัก: ↑ ↑ ↓ ↓ ← → เปิดแผง Dev (ไม่มีปุ่มบนจอ)
        private string secretKeys = "";

        private void Update()
        {
            if (mainPanel == null || !mainPanel.gameObject.activeSelf)
            {
                secretKeys = "";
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            char pressed = '\0';

            if (keyboard.upArrowKey.wasPressedThisFrame) pressed = 'U';
            else if (keyboard.downArrowKey.wasPressedThisFrame) pressed = 'D';
            else if (keyboard.leftArrowKey.wasPressedThisFrame) pressed = 'L';
            else if (keyboard.rightArrowKey.wasPressedThisFrame) pressed = 'R';
            else if (keyboard.anyKey.wasPressedThisFrame) secretKeys = "";

            if (pressed == '\0')
                return;

            secretKeys += pressed;

            if (secretKeys.Length > DevCheats.Sequence.Length)
                secretKeys = secretKeys.Substring(secretKeys.Length - DevCheats.Sequence.Length);

            if (secretKeys != DevCheats.Sequence)
                return;

            secretKeys = "";

            DevCheats.Unlocked = true;

            Show(devPanel);
            SelectFirstDevButton();
        }

        // เลือกปุ่มแรกของแผง Dev เพื่อให้กดลูกศร ↑ ↓ ← → เลื่อนปุ่ม และ Enter/Space กดได้
        private void SelectFirstDevButton()
        {
            if (devButtons.Count == 0 || UnityEngine.EventSystems.EventSystem.current == null)
                return;

            UnityEngine.EventSystems.EventSystem.current
                .SetSelectedGameObject(devButtons[0].gameObject);
        }

        private void BuildMilestones()
        {
            UIFactory.CreateTitle(milestonePanel, "COLLECTION MILESTONES");

            milestoneGoldText = ScreenKit.AddLabel(
                milestonePanel, "", 30, TextAnchor.MiddleRight,
                0.62f, 0.93f, 0.98f, 0.99f, FontStyle.Bold);

            milestoneGoldText.color = UIFactory.Gold;

            ScreenKit.AddScroll(
                milestonePanel, "MilestoneScroll",
                0.08f, 0.14f, 0.92f, 0.9f, out milestoneList);

            UIFactory.AddVertical(milestoneList, 8f, 10);

            ScreenKit.AddButton(milestonePanel, "Back",
                () => Show(mainPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);
        }

        private void RebuildMilestones()
        {
            ScreenKit.ClearNow(milestoneList);

            PlayerProfile profile = SaveManager.Profile;

            foreach (MilestoneDef def in MilestoneService.All)
            {
                bool claimed = MilestoneService.IsClaimed(profile, def);
                bool reached = MilestoneService.IsReached(profile, def);

                Image row = UIFactory.CreateFramedPanel(
                    milestoneList, "Row_" + def.Id,
                    claimed
                        ? new Color(0.2f, 0.16f, 0.1f, 0.9f)
                        : UIFactory.Panel);

                row.rectTransform.sizeDelta = new Vector2(0f, 96f);

                Text title = UIFactory.CreateText(
                    row.transform, def.Title, 28,
                    claimed ? UIFactory.GoldDim : UIFactory.Gold,
                    TextAnchor.MiddleLeft, FontStyle.Bold);

                UIFactory.SetAnchors(title.rectTransform, 0.02f, 0.5f, 0.55f, 0.95f);

                Text description = UIFactory.CreateText(
                    row.transform,
                    def.Description + "   (" +
                    Mathf.Min(def.Progress(profile), def.Target) + " / " + def.Target + ")",
                    22, new Color(0.8f, 0.76f, 0.68f, 1f), TextAnchor.MiddleLeft);

                UIFactory.SetAnchors(description.rectTransform, 0.02f, 0.05f, 0.55f, 0.5f);

                Text reward = UIFactory.CreateText(
                    row.transform, def.RewardText, 24, UIFactory.GoldDim,
                    TextAnchor.MiddleCenter);

                UIFactory.SetAnchors(reward.rectTransform, 0.55f, 0.1f, 0.77f, 0.9f);

                string milestoneId = def.Id;

                Button button = ScreenKit.AddButton(
                    row.transform,
                    claimed ? "Claimed" : (reached ? "Claim" : "Locked"),
                    () =>
                    {
                        MilestoneService.Claim(SaveManager.Profile, milestoneId);

                        SaveManager.Save();

                        Sfx.Play(SoundId.Pack);

                        RebuildMilestones();
                        RefreshGold();
                        RefreshMenuNotices();
                    },
                    reached && !claimed ? UIFactory.Success : UIFactory.Muted,
                    0.79f, 0.15f, 0.98f, 0.85f, 24);

                button.interactable = reached && !claimed;
            }
        }

        // ---------- Keyword / Status ----------

        private void BuildGlossary()
        {
            UIFactory.CreateTitle(glossaryPanel, "KEYWORDS & STATUS");

            RectTransform content;

            ScreenKit.AddScroll(
                glossaryPanel, "GlossaryScroll",
                0.06f, 0.14f, 0.94f, 0.88f, out content);

            UIFactory.AddVertical(content, 0f, 24);

            Text text = UIFactory.CreateText(
                content, GameGlossary.Build(), 24, UIFactory.TextColor,
                TextAnchor.UpperLeft);

            text.verticalOverflow = VerticalWrapMode.Overflow;

            ContentSizeFitter fitter =
                text.gameObject.AddComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            ScreenKit.AddButton(glossaryPanel, "Back",
                () => Show(mainPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);
        }

        // ---------- วิธีเล่น ----------

        private void BuildHelp()
        {
            UIFactory.CreateTitle(helpPanel, "HOW TO PLAY");

            ScreenKit.AddPanel(
                helpPanel, "HelpBackground", UIFactory.Panel,
                0.06f, 0.14f, 0.94f, 0.88f);

            Text help = ScreenKit.AddLabel(
                helpPanel, HelpText, 24, TextAnchor.UpperLeft,
                0.08f, 0.16f, 0.92f, 0.86f);

            help.resizeTextForBestFit = true;
            help.resizeTextMinSize = 14;
            help.resizeTextMaxSize = 24;

            ScreenKit.AddButton(helpPanel, "Back",
                () => Show(mainPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);
        }
    }
}
