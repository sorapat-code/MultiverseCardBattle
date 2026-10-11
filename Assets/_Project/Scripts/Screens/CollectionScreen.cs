using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Decks;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// แสดงการ์ดทั้งหมดในเกม การ์ดที่ยังไม่มีจะจางลง
    /// </summary>
    public class CollectionScreen : MonoBehaviour
    {
        private static readonly Vector2 CardSize = new Vector2(150f, 210f);

        private RectTransform root;
        private RectTransform grid;
        private Text summary;
        private Text dustText;
        private CardFilterBar filters;
        private Button extrasButton;
        private bool extrasArmed;

        // หน้าต่างดูการ์ดเต็มใบ + คราฟ/ย่อย
        private RectTransform modal;
        private CardData modalCard;
        private CardView modalView;
        private Text modalInfo;
        private Text modalHints;
        private Text modalMessage;
        private Button craftButton;
        private Button disenchantButton;
        private bool gridDirty;

        private void Start()
        {
            root = UIFactory.CreateScreenRoot("CollectionCanvas");

            UIFactory.CreateTitle(root, "COLLECTION");

            filters = new CardFilterBar(
                root, 0.03f, 0.835f, 0.97f, 0.89f,
                id => SaveManager.Profile.GetOwnedCount(id));

            filters.Changed += Rebuild;

            ScreenKit.AddScroll(
                root, "CardGrid", 0.03f, 0.13f, 0.97f, 0.825f, out grid);

            UIFactory.AddGrid(grid, CardSize, 14f, 14);

            summary = ScreenKit.AddLabel(
                root, "", 26, TextAnchor.MiddleCenter,
                0.24f, 0.02f, 0.72f, 0.11f);

            dustText = ScreenKit.AddLabel(
                root, "", 30, TextAnchor.MiddleRight,
                0.7f, 0.93f, 0.98f, 0.99f, FontStyle.Bold);

            dustText.color = new Color(0.6f, 0.85f, 1f, 1f);

            UIFactory.AddTextShadow(dustText);

            extrasButton = ScreenKit.AddButton(root, "Disenchant Extras",
                OnExtrasClicked, UIFactory.Muted, 0.74f, 0.03f, 0.97f, 0.11f, 24);

            summary.color = UIFactory.Gold;

            ScreenKit.AddButton(root, "Back",
                () => SceneLoader.Go(GameScene.MainMenu),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);

            Rebuild();
        }

        private void Rebuild()
        {
            ScreenKit.ClearNow(grid);

            PlayerProfile profile = SaveManager.Profile;

            List<CardData> cards = new List<CardData>();

            foreach (CardData data in CardRegistry.All)
            {
                if (CardRegistry.IsVisibleInCollection(data, profile))
                    cards.Add(data);
            }

            cards.Sort(CompareCards);

            int ownedKinds = 0;
            int totalCopies = 0;
            int shown = 0;

            foreach (CardData data in cards)
            {
                int owned = profile.GetOwnedCount(data.CardID);

                if (owned > 0)
                {
                    ownedKinds++;
                    totalCopies += owned;
                }

                if (!filters.Matches(data))
                    continue;

                shown++;

                CardView view = CardView.Create(grid, CardSize);

                view.ShowData(data, "x" + owned);
                view.SetDimmed(owned == 0);
                view.Clicked += OnCardClicked;
            }

            RefreshDust();

            summary.text =
                "Unique cards: " + ownedKinds + " / " + cards.Count +
                "   Total copies: " + totalCopies +
                "   Showing: " + shown;
        }

        private void RefreshDust()
        {
            dustText.text = "Dust: " + SaveManager.Profile.Dust;
        }

        // ---------- ย่อยใบส่วนเกินทั้งหมด ----------

        private void OnExtrasClicked()
        {
            PlayerProfile profile = SaveManager.Profile;

            int cards;
            int preview = 0;
            int previewCards = 0;

            foreach (CardData card in CardRegistry.All)
            {
                int extra = CardEconomy.ExtraCopies(profile, card);

                preview += extra * CardEconomy.DisenchantValue(card.Rarity);
                previewCards += extra;
            }

            if (previewCards == 0)
            {
                extrasArmed = false;

                UIFactory.SetButtonLabel(extrasButton, "No extra cards");

                return;
            }

            if (!extrasArmed)
            {
                extrasArmed = true;

                UIFactory.SetButtonLabel(
                    extrasButton,
                    "Confirm: " + previewCards + " cards = +" + preview + " Dust");

                return;
            }

            extrasArmed = false;

            int dust = CardEconomy.DisenchantAllExtras(profile, out cards);

            SaveManager.Save();

            UIFactory.SetButtonLabel(extrasButton, "Disenchant Extras");

            Rebuild();

            ShowUnlocked(AchievementService.Check(profile));

            summary.text = "Disenchanted " + cards + " extra cards (+" + dust + " Dust)";
        }

        private void ShowUnlocked(System.Collections.Generic.List<AchievementDef> list)
        {
            foreach (AchievementDef def in list)
            {
                Toast.Show(
                    root, "Achievement unlocked: " + def.Name,
                    AchievementService.RewardText(def));
            }

            RefreshDust();
        }

        // ---------- ดูการ์ดเต็มใบ ----------

        private void OnCardClicked(CardView view)
        {
            if (view == null || view.Data == null)
                return;

            modalCard = view.Data;

            BuildModal();

            modal.gameObject.SetActive(true);
            modal.SetAsLastSibling();

            modalMessage.text = "";

            RefreshModal();
        }

        private void BuildModal()
        {
            if (modal != null)
                return;

            modal = UIFactory.CreateRect("CardModal", root);

            UIFactory.Stretch(modal);

            // พื้นมืด: คลิกที่ว่างเพื่อปิด
            Image dim = UIFactory.CreatePanel(
                modal, "Dim", new Color(0f, 0f, 0f, 0.88f));

            UIFactory.Stretch(dim.rectTransform);

            Button dimButton = dim.gameObject.AddComponent<Button>();

            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(CloseModal);

            modalView = CardView.Create(modal, new Vector2(390f, 546f));

            RectTransform viewRect = modalView.GetComponent<RectTransform>();

            viewRect.anchorMin = new Vector2(0.3f, 0.52f);
            viewRect.anchorMax = new Vector2(0.3f, 0.52f);
            viewRect.anchoredPosition = Vector2.zero;

            ScreenKit.AddPanel(
                modal, "InfoPanel", new Color(0.12f, 0.08f, 0.06f, 0.96f),
                0.5f, 0.17f, 0.93f, 0.86f);

            modalInfo = ScreenKit.AddLabel(
                modal, "", 28, TextAnchor.UpperLeft,
                0.52f, 0.6f, 0.91f, 0.84f, FontStyle.Bold);

            modalHints = ScreenKit.AddLabel(
                modal, "", 20, TextAnchor.UpperLeft,
                0.52f, 0.33f, 0.91f, 0.6f);

            modalHints.resizeTextForBestFit = true;
            modalHints.resizeTextMinSize = 12;
            modalHints.resizeTextMaxSize = 20;

            modalMessage = ScreenKit.AddLabel(
                modal, "", 22, TextAnchor.MiddleCenter,
                0.52f, 0.25f, 0.91f, 0.33f);

            craftButton = ScreenKit.AddButton(
                modal, "Craft", OnCraftClicked, UIFactory.Success,
                0.52f, 0.18f, 0.66f, 0.25f, 22);

            disenchantButton = ScreenKit.AddButton(
                modal, "Disenchant", OnDisenchantClicked, UIFactory.Danger,
                0.67f, 0.18f, 0.83f, 0.25f, 22);

            ScreenKit.AddButton(
                modal, "Close", CloseModal, UIFactory.Muted,
                0.84f, 0.18f, 0.92f, 0.25f, 22);

            modal.gameObject.SetActive(false);
        }

        private void RefreshModal()
        {
            PlayerProfile profile = SaveManager.Profile;

            CardData card = modalCard;

            int owned = profile.GetOwnedCount(card.CardID);

            modalView.ShowData(card, "x" + owned);
            modalView.SetDimmed(owned == 0);

            modalInfo.text =
                card.CardName + "\n" +
                "<size=22>" + CardFilterBar.Pretty(card.Rarity.ToString()) + " " +
                card.Type + "  -  " + CardFilterBar.Pretty(card.Universe.ToString()) + "\n" +
                "Owned: " + owned + " / " + DeckRules.MaxCopies(card) +
                "      Dust: " + profile.Dust + "\n" +
                "Craft: " + CardEconomy.CraftCost(card.Rarity) +
                " Dust      Disenchant: +" + CardEconomy.DisenchantValue(card.Rarity) +
                " Dust" + MasteryLine(profile, card) + "</size>";

            modalHints.text = GameGlossary.HintsFor(card, null);

            string reason;

            bool canCraft = CardEconomy.CanCraft(profile, card, out reason);

            craftButton.interactable = canCraft;

            UIFactory.SetButtonLabel(
                craftButton, "Craft -" + CardEconomy.CraftCost(card.Rarity));

            int spare = CardEconomy.DisenchantableCopies(profile, card);

            disenchantButton.interactable = spare > 0;

            UIFactory.SetButtonLabel(
                disenchantButton,
                "Disenchant +" + CardEconomy.DisenchantValue(card.Rarity));

            if (!canCraft && string.IsNullOrEmpty(modalMessage.text))
                modalMessage.text = "<color=#A0A0A0>" + reason + "</color>";
        }

        // Mastery (เฉพาะการ์ด Epic ขึ้นไป): เลเวล XP รางวัลถัดไป และกรอบ
        private static string MasteryLine(PlayerProfile profile, CardData card)
        {
            if (!MasteryService.IsEligible(card))
                return "";

            int xp = MasteryService.GetXp(profile, card.CardID);
            int level = MasteryService.LevelFor(xp);

            string frame = MasteryService.FrameName(level);

            if (level >= MasteryService.MaxLevel)
            {
                return "\n<color=#FFE070>Mastery Lv " + level + " (MAX)</color>" +
                       (frame.Length > 0 ? "  -  " + frame : "");
            }

            int next = MasteryService.XpAt(level + 1);

            return "\n<color=#FFE070>Mastery Lv " + level + "</color>  " +
                   xp + " / " + next + " XP  -  next: " +
                   MasteryService.RewardText(level + 1) +
                   (frame.Length > 0 ? "  -  " + frame : "");
        }

        private void OnCraftClicked()
        {
            string message;

            PlayerProfile profile = SaveManager.Profile;

            bool ok = CardEconomy.Craft(profile, modalCard, out message);

            modalMessage.text = ok
                ? "<color=#70E080>" + message + "</color>"
                : "<color=#FF9090>" + message + "</color>";

            AfterEconomyChange(ok);
        }

        private void OnDisenchantClicked()
        {
            string message;

            PlayerProfile profile = SaveManager.Profile;

            bool ok = CardEconomy.Disenchant(profile, modalCard, 1, out message);

            modalMessage.text = ok
                ? "<color=#70E080>" + message + "</color>"
                : "<color=#FF9090>" + message + "</color>";

            AfterEconomyChange(ok);
        }

        private void AfterEconomyChange(bool changed)
        {
            if (changed)
            {
                SaveManager.Save();

                gridDirty = true;

                ShowUnlocked(AchievementService.Check(SaveManager.Profile));
            }

            RefreshModal();
        }

        private void CloseModal()
        {
            if (modal != null)
                modal.gameObject.SetActive(false);

            if (gridDirty)
            {
                gridDirty = false;

                Rebuild();
            }
        }

        public static int CompareCards(CardData a, CardData b)
        {
            int cost = a.Cost.CompareTo(b.Cost);

            if (cost != 0)
                return cost;

            return string.CompareOrdinal(a.CardName, b.CardName);
        }
    }
}
