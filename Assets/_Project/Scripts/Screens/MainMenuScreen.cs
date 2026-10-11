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
            "<b>Battle tools</b>: the Graveyard buttons show destroyed cards, and Opp. Hand only shows how many cards your opponent holds.\n" +
            "<b>Keys</b>: Space - end turn.  Esc - cancel the current selection or close a window.  Hover a card to see it larger.\n\n" +
            "<b>Deck-out</b>: if your deck ever reaches 0 cards you lose immediately - watch your draws!\n\n" +
            "<b>Progress</b>: win battles to earn Gold and campaign reward cards. " +
            "Spend Gold in the Shop to open packs, then build your deck in the Deck Builder.\n" +
            "<b>Deck rules</b>: at least 30 cards (no maximum), up to 3 copies of a card (Unique cards: 1 copy, and only 1 Unique card per deck). In the deck builder, left-click a card to add it and right-click to remove it. Secret cards appear in your Collection only after you unlock them.\n<b>Collection</b>: click a card to see it full size. Disenchant spare copies into Dust and spend Dust to craft cards you are missing.\n" +
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
        private RectTransform rewardsPanel;
        private RectTransform optionsPanel;
        private Button rewardsMenuButton;
        private Text devMessage;
        private RectTransform milestoneList;
        private Text dailyGoldText;
        private Text milestoneGoldText;
        private Button dailyMenuButton;
        private Button milestoneMenuButton;
        private Text achievementSummary;

        private Text message;
        private Text optionsMessage;
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
            rewardsPanel = CreatePanel("RewardsPanel");
            optionsPanel = CreatePanel("OptionsPanel");
            leaderPanel = CreatePanel("LeaderPanel");
            statsPanel = CreatePanel("StatsPanel");

            BuildMain();
            BuildStats();
            BuildShop();
            BuildHelp();
            BuildGlossary();
            BuildAchievements();
            BuildDaily();
            BuildMilestones();
            BuildDev();
            BuildRewardsMenu();
            BuildOptionsMenu();
            BuildLeaderMenu();

            GameSettings.Apply();

            Show(mainPanel);

            // เปลี่ยนขนาดตัวอักษรแล้วโหลดเมนูใหม่: กลับมาที่หน้า Options
            if (openOptionsOnStart)
            {
                openOptionsOnStart = false;

                Show(optionsPanel);
            }
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
            if (rewardsPanel != null)
                rewardsPanel.gameObject.SetActive(panel == rewardsPanel);
            if (optionsPanel != null)
                optionsPanel.gameObject.SetActive(panel == optionsPanel);
            if (creditsPanel != null)
                creditsPanel.gameObject.SetActive(panel == creditsPanel);
            if (leaderPanel != null)
                leaderPanel.gameObject.SetActive(panel == leaderPanel);
            if (statsPanel != null)
                statsPanel.gameObject.SetActive(panel == statsPanel);

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

            UIFactory.SetButtonLabel(
                rewardsMenuButton,
                DailyService.HasClaimable(profile) || MilestoneService.HasClaimable(profile)
                    ? "Rewards (!)" : "Rewards");
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
                0.24f, 0.26f, 0.76f, 0.81f);

            goldText = ScreenKit.AddLabel(
                mainPanel, "", 30, TextAnchor.MiddleRight,
                0.74f, 0.93f, 0.98f, 0.99f, FontStyle.Bold);

            goldText.color = UIFactory.Gold;

            UIFactory.AddTextShadow(goldText);

            // เมนูหลัก: ปุ่มเล่นใหญ่ 2 ปุ่ม + หมวดจัดการ + ปุ่มหมวดย่อย (Rewards / Options)
            float y = 0.715f;
            const float height = 0.085f;
            const float step = 0.105f;

            ScreenKit.AddButton(mainPanel, "Story Mode",
                () => SceneLoader.Go(GameScene.Campaign),
                UIFactory.Accent, 0.28f, y, 0.72f, y + height, 36);

            y -= step;

            ScreenKit.AddButton(mainPanel, "Quick Battle",
                StartQuickBattle,
                UIFactory.Success, 0.28f, y, 0.72f, y + height, 36);

            y -= step;

            ScreenKit.AddButton(mainPanel, "Deck Builder",
                () => SceneLoader.Go(GameScene.DeckBuilder),
                UIFactory.Accent, 0.28f, y, 0.49f, y + height);

            ScreenKit.AddButton(mainPanel, "Collection",
                () => SceneLoader.Go(GameScene.Collection),
                UIFactory.Accent, 0.51f, y, 0.72f, y + height);

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
                UIFactory.Accent, 0.28f, y, 0.49f, y + height);

            rewardsMenuButton = ScreenKit.AddButton(mainPanel, "Rewards",
                () => Show(rewardsPanel),
                UIFactory.Accent, 0.51f, y, 0.72f, y + height);

            y -= step;

            ScreenKit.AddButton(mainPanel, "Help & Options",
                () => Show(optionsPanel),
                UIFactory.Muted, 0.28f, y, 0.49f, y + height, 26);

#if !UNITY_WEBGL
            ScreenKit.AddButton(mainPanel, "Quit",
                Application.Quit,
                UIFactory.Danger, 0.51f, y, 0.72f, y + height, 26);
#endif

            // Leader ที่เลือกไว้ก่อนเข้าแมตช์
            leaderMenuButton = ScreenKit.AddButton(mainPanel, "",
                () =>
                {
                    RefreshLeaderMenu();

                    Show(leaderPanel);
                },
                UIFactory.Accent, 0.28f, 0.17f, 0.72f, 0.25f, 26);

            RefreshLeaderMenuButton();

            // แถวล่าง: ระดับความยาก AI / สถิติ / Tutorial
            difficultyButton = ScreenKit.AddButton(mainPanel, "",
                () =>
                {
                    GameSettings.AiLevel = (AiDifficulty)(((int)GameSettings.AiLevel + 1) % 3);

                    SaveManager.Save();

                    RefreshDifficultyButton();

                    message.text = AiDifficultyInfo.Describe(GameSettings.AiLevel);
                },
                UIFactory.Muted, 0.28f, 0.085f, 0.42f, 0.155f, 22);

            ScreenKit.AddButton(mainPanel, "Stats & History",
                () =>
                {
                    RebuildStats();

                    Show(statsPanel);
                },
                UIFactory.Accent, 0.43f, 0.085f, 0.57f, 0.155f, 22);

            tutorialButton = ScreenKit.AddButton(mainPanel, "",
                StartTutorial,
                UIFactory.Success, 0.58f, 0.085f, 0.72f, 0.155f, 22);

            RefreshDifficultyButton();

            message = ScreenKit.AddLabel(
                mainPanel, "", 26, TextAnchor.MiddleCenter,
                0.1f, 0.01f, 0.9f, 0.08f);

            // เปิดเกมครั้งแรก: ชวนให้เล่น Tutorial
            if (!SaveManager.Profile.Data.TutorialDone)
                message.text = "New here? Try the Tutorial!";
        }

        private Button difficultyButton;
        private Button tutorialButton;
        private RectTransform statsPanel;
        private RectTransform statsList;
        private Text statsOverview;

        private void RefreshDifficultyButton()
        {
            AiDifficulty level = GameSettings.AiLevel;

            UIFactory.SetButtonLabel(
                difficultyButton, "Difficulty: " + AiDifficultyInfo.NameOf(level));

            UIFactory.SetButtonLabel(
                tutorialButton,
                SaveManager.Profile.Data.TutorialDone ? "Tutorial" : "Tutorial (!)");
        }

        private void StartTutorial()
        {
            string error = CampaignService.StartTutorial();

            if (error != null)
                message.text = "<color=#FF8080>" + error + "</color>";
        }

        // ---------- สถิติและประวัติแมตช์ ----------

        private void BuildStats()
        {
            UIFactory.CreateTitle(statsPanel, "STATS & HISTORY");

            statsOverview = ScreenKit.AddLabel(
                statsPanel, "", 22, TextAnchor.UpperLeft,
                0.04f, 0.14f, 0.40f, 0.84f);

            statsOverview.supportRichText = true;

            ScreenKit.AddLabel(
                statsPanel, "Recent matches", 28, TextAnchor.MiddleLeft,
                0.44f, 0.82f, 0.96f, 0.88f, FontStyle.Bold).color = UIFactory.Gold;

            ScreenKit.AddScroll(
                statsPanel, "HistoryScroll",
                0.44f, 0.14f, 0.96f, 0.82f, out statsList);

            UIFactory.AddVertical(statsList, 6f, 8);

            ScreenKit.AddButton(statsPanel, "Back",
                () => Show(mainPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);
        }

        private void RebuildStats()
        {
            PlayerProfile profile = SaveManager.Profile;

            statsOverview.text = StatsService.BuildOverview(profile);

            ScreenKit.ClearNow(statsList);

            List<MatchRecord> history = profile.Data.History;

            if (history == null || history.Count == 0)
            {
                Image emptyRow = UIFactory.CreateFramedPanel(
                    statsList, "Empty", UIFactory.Panel);

                emptyRow.rectTransform.sizeDelta = new Vector2(0f, 90f);

                Text empty = UIFactory.CreateText(
                    emptyRow.transform, "No matches yet. Win or lose, they show up here.",
                    24, UIFactory.GoldDim, TextAnchor.MiddleCenter);

                UIFactory.Stretch(empty.rectTransform);

                return;
            }

            // ใหม่สุดอยู่บนสุด
            for (int i = history.Count - 1; i >= 0; i--)
            {
                MatchRecord record = history[i];

                Color tint = record.Result == 1
                    ? new Color(0.10f, 0.22f, 0.12f, 0.95f)
                    : (record.Result == 2
                        ? new Color(0.22f, 0.2f, 0.08f, 0.95f)
                        : new Color(0.24f, 0.1f, 0.1f, 0.95f));

                Image row = UIFactory.CreateFramedPanel(
                    statsList, "Match_" + i, tint);

                row.rectTransform.sizeDelta = new Vector2(0f, 84f);

                Text top = UIFactory.CreateText(
                    row.transform, StatsService.FormatRecord(record),
                    24, UIFactory.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold);

                top.supportRichText = true;

                UIFactory.SetAnchors(top.rectTransform, 0.02f, 0.5f, 0.98f, 0.95f);

                Text detail = UIFactory.CreateText(
                    row.transform, StatsService.FormatRecordDetail(record),
                    20, new Color(0.8f, 0.76f, 0.68f, 1f), TextAnchor.MiddleLeft);

                UIFactory.SetAnchors(detail.rectTransform, 0.02f, 0.05f, 0.98f, 0.5f);
            }
        }

        // ---------- Leader ----------

        private RectTransform leaderPanel;
        private Button leaderMenuButton;
        private Text leaderDetail;
        private readonly List<Button> leaderChoiceButtons = new List<Button>();
        private readonly List<string> leaderChoiceIds = new List<string>();

        private void BuildLeaderMenu()
        {
            UIFactory.CreateTitle(leaderPanel, "CHOOSE YOUR LEADER");

            // ซ้าย: รายชื่อ (บนสุด = ไม่ใช้ Leader)
            float y = 0.68f;
            const float height = 0.08f;
            const float step = 0.095f;

            AddLeaderChoice("", "No Leader", y, height);

            y -= step;

            foreach (LeaderData leader in LeaderLibrary.All)
            {
                if (!leader.PlayerSelectable)
                    continue;

                AddLeaderChoice(leader.Id, leader.Name, y, height);

                y -= step;
            }

            // ขวา: รายละเอียดสกิล + เงื่อนไขไม้ตาย
            ScreenKit.AddPanel(
                leaderPanel, "LeaderDetailBack", new Color(0f, 0f, 0f, 0.45f),
                0.36f, 0.18f, 0.95f, 0.78f);

            leaderDetail = ScreenKit.AddLabel(
                leaderPanel, "", 26, TextAnchor.UpperLeft,
                0.38f, 0.2f, 0.93f, 0.76f);

            leaderDetail.supportRichText = true;
            leaderDetail.resizeTextForBestFit = true;
            leaderDetail.resizeTextMinSize = 14;
            leaderDetail.resizeTextMaxSize = 26;

            ScreenKit.AddButton(leaderPanel, "Back",
                () =>
                {
                    SaveManager.Save();

                    RefreshLeaderMenuButton();

                    Show(mainPanel);
                },
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);

            RefreshLeaderMenu();
        }

        private void AddLeaderChoice(string id, string label, float y, float height)
        {
            string captured = id;

            Button button = ScreenKit.AddButton(leaderPanel, label,
                () =>
                {
                    SaveManager.Profile.Data.LeaderId = captured;

                    SaveManager.Save();

                    RefreshLeaderMenu();
                },
                UIFactory.Muted, 0.05f, y, 0.33f, y + height, 24);

            leaderChoiceButtons.Add(button);
            leaderChoiceIds.Add(id);
        }

        private void RefreshLeaderMenu()
        {
            string selected = SaveManager.Profile.Data.LeaderId ?? "";

            for (int i = 0; i < leaderChoiceButtons.Count; i++)
            {
                bool on = leaderChoiceIds[i] == selected;

                leaderChoiceButtons[i].GetComponent<Image>().color =
                    on ? UIFactory.Success : UIFactory.Muted;
            }

            LeaderData leader = LeaderLibrary.Get(selected);

            if (leader == null)
            {
                leaderDetail.text =
                    "<b>No Leader</b>\n\nPlay without a Leader skill. " +
                    "Choose a Leader to get a skill you can use once per turn " +
                    "and a finisher card that joins your hand when you meet its condition.";

                return;
            }

            CardData finisher = StarterCardLibrary.Get(leader.FinisherId);

            string finisherText = finisher != null
                ? "<b>" + finisher.CardName + "</b> (Cost " + finisher.Cost + ")\n" + CardTextGenerator.Describe(finisher)
                : "";

            leaderDetail.text =
                "<size=34><b><color=#FFB040>" + leader.Name + "</color></b></size>\n" +
                "<i>" + leader.Title + "</i>\n\n" +
                "<b>Skill - " + leader.SkillName + "</b> (" + leader.SkillCostText + ")\n" + leader.SkillText + "\n\n" +
                "<b>Finisher condition - " + leader.PowerName + " " + leader.PowerMax + "</b>\n" +
                leader.PowerText + "\n\n" + finisherText;
        }

        private void RefreshLeaderMenuButton()
        {
            LeaderData leader = LeaderLibrary.Get(SaveManager.Profile.Data.LeaderId);

            UIFactory.SetButtonLabel(
                leaderMenuButton,
                "Leader: " + (leader != null ? leader.Name : "None"));
        }

        private void BuildRewardsMenu()
        {
            UIFactory.CreateTitle(rewardsPanel, "REWARDS");

            float y = 0.62f;
            const float height = 0.09f;
            const float step = 0.12f;

            ScreenKit.AddButton(rewardsPanel, "Achievements",
                () =>
                {
                    RebuildAchievements();

                    Show(achievementPanel);
                },
                UIFactory.Accent, 0.3f, y, 0.7f, y + height);

            y -= step;

            dailyMenuButton = ScreenKit.AddButton(rewardsPanel, "Daily Rewards",
                () =>
                {
                    RebuildDaily();

                    Show(dailyPanel);
                },
                UIFactory.Success, 0.3f, y, 0.7f, y + height);

            y -= step;

            milestoneMenuButton = ScreenKit.AddButton(rewardsPanel, "Milestones",
                () =>
                {
                    RebuildMilestones();

                    Show(milestonePanel);
                },
                UIFactory.Accent, 0.3f, y, 0.7f, y + height);

            ScreenKit.AddButton(rewardsPanel, "Back",
                () => Show(mainPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);
        }

        // ---------- Options ----------

        private Button muteButton;
        private Button fullscreenButton;
        private Button vsyncButton;
        private Button fpsButton;
        private Button animButton;
        private Button aiButton;
        private Button textButton;
        private static bool openOptionsOnStart;
        private RectTransform creditsPanel;

        private void BuildOptionsMenu()
        {
            UIFactory.CreateTitle(optionsPanel, "OPTIONS");

            // ซ้าย: เสียง + จอภาพ
            AddOptionsHeader("AUDIO", 0.06f, 0.48f, 0.82f);

            AddVolumeRow("Master", 0.73f, GameSettings.Master,
                v => { GameSettings.Master = v; });
            AddVolumeRow("Music", 0.65f, GameSettings.Music,
                v => { GameSettings.Music = v; });
            AddVolumeRow("Effects", 0.57f, GameSettings.Sfx,
                v => { GameSettings.Sfx = v; });

            muteButton = ScreenKit.AddButton(optionsPanel, "",
                ToggleSound, UIFactory.Muted, 0.06f, 0.48f, 0.48f, 0.55f, 24);

            AddOptionsHeader("DISPLAY", 0.06f, 0.48f, 0.40f);

            fullscreenButton = ScreenKit.AddButton(optionsPanel, "",
                () =>
                {
                    GameSettings.Fullscreen = !GameSettings.Fullscreen;
                    ApplySettings();
                },
                UIFactory.Muted, 0.06f, 0.26f, 0.48f, 0.33f, 24);

            vsyncButton = ScreenKit.AddButton(optionsPanel, "",
                () =>
                {
                    GameSettings.VSync = !GameSettings.VSync;
                    ApplySettings();
                },
                UIFactory.Muted, 0.06f, 0.18f, 0.48f, 0.25f, 24);

            fpsButton = ScreenKit.AddButton(optionsPanel, "",
                () =>
                {
                    int[] options = GameSettings.FpsOptions;
                    int index = System.Array.IndexOf(options, GameSettings.FpsLimit);

                    GameSettings.FpsLimit = options[(index + 1) % options.Length];
                    ApplySettings();
                },
                UIFactory.Muted, 0.06f, 0.10f, 0.48f, 0.17f, 24);

            // ขวา: เกมเพลย์ + ข้อมูล
            AddOptionsHeader("GAMEPLAY", 0.52f, 0.94f, 0.82f);

            animButton = ScreenKit.AddButton(optionsPanel, "",
                () =>
                {
                    float[] options = GameSettings.AnimOptions;
                    int index = NearestIndex(options, GameSettings.AnimSpeed);

                    GameSettings.AnimSpeed = options[(index + 1) % options.Length];
                    ApplySettings();
                },
                UIFactory.Muted, 0.52f, 0.73f, 0.94f, 0.80f, 24);

            aiButton = ScreenKit.AddButton(optionsPanel, "",
                () =>
                {
                    GameSettings.AiSpeed =
                        (GameSettings.AiSpeed + 1) % GameSettings.AiSpeedNames.Length;
                    ApplySettings();
                },
                UIFactory.Muted, 0.52f, 0.65f, 0.94f, 0.72f, 24);

            // ขนาดตัวอักษร: ใช้ตอนสร้างหน้าจอ จึงโหลดเมนูใหม่เพื่อให้เห็นผลทันที
            textButton = ScreenKit.AddButton(optionsPanel, "",
                () =>
                {
                    float[] options = GameSettings.TextScaleOptions;
                    int index = NearestIndex(options, GameSettings.TextScale);

                    GameSettings.TextScale = options[(index + 1) % options.Length];
                    ApplySettings();

                    openOptionsOnStart = true;

                    SceneLoader.Go(GameScene.MainMenu);
                },
                UIFactory.Muted, 0.52f, 0.57f, 0.94f, 0.64f, 24);

            AddOptionsHeader("INFO", 0.52f, 0.94f, 0.49f);

            ScreenKit.AddButton(optionsPanel, "How to Play",
                () => Show(helpPanel),
                UIFactory.Accent, 0.52f, 0.41f, 0.94f, 0.48f, 24);

            ScreenKit.AddButton(optionsPanel, "Keywords & Status",
                () => Show(glossaryPanel),
                UIFactory.Accent, 0.52f, 0.33f, 0.94f, 0.40f, 24);

            ScreenKit.AddButton(optionsPanel, "Credits",
                () => Show(creditsPanel),
                UIFactory.Accent, 0.52f, 0.25f, 0.94f, 0.32f, 24);

            ScreenKit.AddButton(optionsPanel, "Defaults",
                () =>
                {
                    GameSettings.ResetToDefaults();
                    ApplySettings();
                    RebuildOptionsSliders();

                    optionsMessage.text = "Options restored to defaults.";
                },
                UIFactory.Muted, 0.52f, 0.17f, 0.94f, 0.24f, 24);

            resetButton = ScreenKit.AddButton(optionsPanel, "Reset Progress",
                OnResetClicked,
                UIFactory.Danger, 0.52f, 0.10f, 0.94f, 0.16f, 24);

            ScreenKit.AddLabel(
                optionsPanel, "Version " + Application.version, 20,
                TextAnchor.MiddleRight, 0.7f, 0.02f, 0.97f, 0.07f).color = UIFactory.GoldDim;

            optionsMessage = ScreenKit.AddLabel(
                optionsPanel, "", 24, TextAnchor.MiddleCenter,
                0.22f, 0.02f, 0.7f, 0.09f);

            ScreenKit.AddButton(optionsPanel, "Back",
                () =>
                {
                    SaveManager.Save();

                    Show(mainPanel);
                },
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.09f);

            RefreshOptionLabels();
            BuildCredits();
        }

        private readonly List<System.Action> sliderRefreshers = new List<System.Action>();

        private void AddOptionsHeader(string title, float x0, float x1, float y)
        {
            Text header = ScreenKit.AddLabel(
                optionsPanel, title, 26, TextAnchor.MiddleLeft,
                x0, y, x1, y + 0.05f, FontStyle.Bold);

            header.color = UIFactory.Gold;

            UIFactory.CreateDivider(optionsPanel, x0, y - 0.004f, x1, y + 0.001f);
        }

        private void AddVolumeRow(
            string label, float y, float value, System.Action<float> setter)
        {
            ScreenKit.AddLabel(
                optionsPanel, label, 24, TextAnchor.MiddleLeft,
                0.06f, y, 0.16f, y + 0.07f);

            Text valueText = ScreenKit.AddLabel(
                optionsPanel, "", 24, TextAnchor.MiddleRight,
                0.42f, y, 0.48f, y + 0.07f);

            Slider slider = CreateSlider(optionsPanel, 0.17f, y, 0.41f, y + 0.07f);

            slider.SetValueWithoutNotify(value);

            valueText.text = Mathf.RoundToInt(value * 100f) + "%";

            slider.onValueChanged.AddListener(v =>
            {
                setter(v);

                valueText.text = Mathf.RoundToInt(v * 100f) + "%";

                Sfx.RefreshVolumes();
                Sfx.RefreshMusicMute();
            });

            // ปล่อยเมาส์แล้วเล่นเสียงตัวอย่าง + บันทึก
            slider.gameObject.AddComponent<SliderReleaseHook>().OnRelease = () =>
            {
                SaveManager.Save();

                Sfx.Play(SoundId.Click);
            };

            sliderRefreshers.Add(() =>
            {
                float current = setterValue(label);

                slider.SetValueWithoutNotify(current);

                valueText.text = Mathf.RoundToInt(current * 100f) + "%";
            });
        }

        private static float setterValue(string label)
        {
            if (label == "Master") return GameSettings.Master;
            if (label == "Music") return GameSettings.Music;

            return GameSettings.Sfx;
        }

        private void RebuildOptionsSliders()
        {
            foreach (System.Action refresh in sliderRefreshers)
                refresh();
        }

        private void ApplySettings()
        {
            GameSettings.Apply();

            SaveManager.Save();

            RefreshOptionLabels();
        }

        private void RefreshOptionLabels()
        {
            UIFactory.SetButtonLabel(
                muteButton, Sfx.Muted ? "Mute All: On" : "Mute All: Off");

            UIFactory.SetButtonLabel(
                fullscreenButton,
                GameSettings.Fullscreen ? "Fullscreen: On" : "Fullscreen: Off (Windowed)");

            UIFactory.SetButtonLabel(
                vsyncButton, GameSettings.VSync ? "VSync: On" : "VSync: Off");

            UIFactory.SetButtonLabel(
                fpsButton,
                GameSettings.FpsLimit > 0
                    ? "FPS Limit: " + GameSettings.FpsLimit
                    : "FPS Limit: None");

            UIFactory.SetButtonLabel(
                animButton, "Animation Speed: " + GameSettings.AnimSpeed.ToString("0.#") + "x");

            UIFactory.SetButtonLabel(
                aiButton,
                "Opponent Speed: " + GameSettings.AiSpeedNames[GameSettings.AiSpeed]);

            UIFactory.SetButtonLabel(
                textButton,
                "Text Size: " + GameSettings.TextScaleNames[
                    NearestIndex(GameSettings.TextScaleOptions, GameSettings.TextScale)]);
        }

        private static int NearestIndex(float[] options, float value)
        {
            int best = 0;

            for (int i = 1; i < options.Length; i++)
            {
                if (Mathf.Abs(options[i] - value) < Mathf.Abs(options[best] - value))
                    best = i;
            }

            return best;
        }

        private void BuildCredits()
        {
            creditsPanel = CreatePanel("CreditsPanel");

            UIFactory.CreateTitle(creditsPanel, "CREDITS");

            Text text = ScreenKit.AddLabel(
                creditsPanel,
                "<b>Multiverse Card Battle</b>\n\n" +
                "A graduation project\n" +
                "Game design, programming and card design: Sorapat\n" +
                "Built with Unity\n\n" +
                "Music and sound effects are generated in code.\n" +
                "Font: Inter (SIL Open Font License 1.1)\n\n" +
                "Thank you for playing!",
                30, TextAnchor.MiddleCenter, 0.2f, 0.2f, 0.8f, 0.85f);

            text.supportRichText = true;

            ScreenKit.AddLabel(
                creditsPanel, "Version " + Application.version, 22,
                TextAnchor.MiddleCenter, 0.3f, 0.12f, 0.7f, 0.18f).color = UIFactory.GoldDim;

            ScreenKit.AddButton(creditsPanel, "Back",
                () => Show(optionsPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);

            creditsPanel.gameObject.SetActive(false);
        }

        internal static Slider CreateSlider(
            Transform parent, float x0, float y0, float x1, float y1)
        {
            RectTransform root = UIFactory.CreateRect("Slider", parent);

            UIFactory.SetAnchors(root, x0, y0, x1, y1);

            RectTransform track = UIFactory.CreateRect("Track", root);

            UIFactory.SetAnchors(track, 0f, 0.4f, 1f, 0.6f);

            Image trackImage = track.gameObject.AddComponent<Image>();

            trackImage.color = new Color(0f, 0f, 0f, 0.55f);

            RectTransform fillArea = UIFactory.CreateRect("Fill Area", root);

            UIFactory.SetAnchors(fillArea, 0f, 0.4f, 1f, 0.6f);

            RectTransform fill = UIFactory.CreateRect("Fill", fillArea);

            Image fillImage = fill.gameObject.AddComponent<Image>();

            fillImage.color = UIFactory.Gold;

            fill.sizeDelta = Vector2.zero;

            RectTransform handleArea = UIFactory.CreateRect("Handle Slide Area", root);

            UIFactory.Stretch(handleArea);

            handleArea.offsetMin = new Vector2(12f, 0f);
            handleArea.offsetMax = new Vector2(-12f, 0f);

            RectTransform handle = UIFactory.CreateRect("Handle", handleArea);

            Image handleImage = handle.gameObject.AddComponent<Image>();

            handleImage.color = UIFactory.TextColor;

            handle.anchorMin = new Vector2(0f, 0.1f);
            handle.anchorMax = new Vector2(0f, 0.9f);
            handle.sizeDelta = new Vector2(24f, 0f);

            Slider slider = root.gameObject.AddComponent<Slider>();

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
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
            RefreshOptionLabels();
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

            // เก็บค่าตั้งค่า (Options) ไว้ ไม่ให้หายตอนรีเซ็ตความคืบหน้า
            SaveData oldData = SaveManager.Profile.Data;

            SaveManager.ResetProgress();

            GameSettings.CopyFrom(oldData);

            SaveManager.Save();

            resetArmed = false;

            UIFactory.SetButtonLabel(resetButton, "Reset Progress");

            RefreshGold();
            RefreshSoundLabel();
            Sfx.RefreshMusicMute();

            optionsMessage.text = "Progress reset. Starter collection restored.";
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
                () => Show(rewardsPanel),
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
                () => Show(rewardsPanel),
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
            if (!DevCheats.CanUnlock ||
                mainPanel == null || !mainPanel.gameObject.activeSelf)
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
                () => Show(rewardsPanel),
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
                () => Show(optionsPanel),
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
                () => Show(optionsPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);
        }
    }

    /// <summary>ส่งสัญญาณเมื่อปล่อยเมาส์จากสไลเดอร์ (ใช้บันทึกค่าและเล่นเสียงตัวอย่าง)</summary>
    public class SliderReleaseHook : MonoBehaviour, UnityEngine.EventSystems.IPointerUpHandler
    {
        public System.Action OnRelease;

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (OnRelease != null)
                OnRelease();
        }
    }
}
