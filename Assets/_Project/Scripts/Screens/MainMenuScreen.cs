using System.Collections.Generic;
using UnityEngine;
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
            "Taunt, Charge, Rush, Shield, Lifesteal, Poison, Stealth, Double Strike, Pierce, Quick Attack, Overwhelm, " +
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

            BuildMain();
            BuildShop();
            BuildHelp();
            BuildGlossary();
            BuildAchievements();

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

            // ปุ่มเมนู 2 คอลัมน์ 5 แถว + ปุ่มออกตรงกลางแถวสุดท้าย
            float y = 0.70f;
            const float height = 0.08f;
            const float step = 0.095f;

            ScreenKit.AddButton(mainPanel, "Campaign",
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
                        " cards with at least one Rare or better";

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
                0.3f, 0.80f, 0.7f, 0.88f, FontStyle.Bold);

            shopGoldText.color = UIFactory.Gold;

            ScreenKit.AddButton(shopPanel,
                "Buy Pack  (" + PackService.PackPrice + " Gold)",
                BuyPack,
                UIFactory.Success, 0.35f, 0.68f, 0.65f, 0.78f, 30);

            ScreenKit.AddLabel(
                shopPanel,
                "Odds: Common 68%   Rare 22%   Epic 8%   Legendary 2%",
                22, TextAnchor.MiddleCenter, 0.2f, 0.62f, 0.8f, 0.67f);

            packRow = UIFactory.CreateZone(
                shopPanel, "PackRow", 0.03f, 0.2f, 0.97f, 0.6f);

            UIFactory.AddHorizontalRow(packRow, 14f);

            shopMessage = ScreenKit.AddLabel(
                shopPanel, "", 26, TextAnchor.MiddleCenter,
                0.2f, 0.1f, 0.8f, 0.18f);

            ScreenKit.AddButton(shopPanel, "Back",
                () => Show(mainPanel),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);
        }

        private void ClearPackRow()
        {
            ScreenKit.ClearNow(packRow);
        }

        private void BuyPack()
        {
            string error;

            List<PackPull> pulls =
                PackService.Buy(SaveManager.Profile, rng, out error);

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
