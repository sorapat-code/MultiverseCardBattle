using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Decks;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// หน้าต่างดูการ์ดเต็มใบแบบอ่านอย่างเดียว (การ์ดใหญ่ + ความสามารถ + คำอธิบายศัพท์)
    /// หน้าตาเดียวกับหน้าต่างใน Collection แต่ไม่มีปุ่ม Craft / Disenchant ใช้กับ Card Pool
    /// คลิกที่ว่างหรือปุ่ม Close เพื่อปิด
    /// </summary>
    public class CardDetailModal
    {
        private readonly RectTransform parent;

        private RectTransform modal;
        private CardView view;
        private Text info;
        private Text abilities;
        private Text hints;

        public CardDetailModal(RectTransform parent)
        {
            this.parent = parent;
        }

        public bool IsOpen
        {
            get { return modal != null && modal.gameObject.activeSelf; }
        }

        /// <summary>เปิดหน้าต่าง owned &lt; 0 = ไม่แสดงจำนวนที่มี</summary>
        public void Show(CardData card, int owned)
        {
            if (card == null)
                return;

            Build();

            modal.gameObject.SetActive(true);
            modal.SetAsLastSibling();

            view.ShowData(card, owned > 0 ? "x" + owned : "");
            view.SetDimmed(owned == 0);

            string ownedLine = owned >= 0
                ? "\nOwned: " + owned + " / " + DeckRules.MaxCopies(card)
                : "";

            info.text =
                card.CardName + "\n" +
                "<size=22>" + CardFilterBar.Pretty(card.Rarity.ToString()) + " " +
                card.Type + "  -  " + CardFilterBar.Pretty(card.Universe.ToString()) +
                ownedLine + "</size>";

            string text = CardTextGenerator.Describe(card);

            abilities.text = text.Length > 0 ? "<b>Abilities</b>\n" + text : "";

            hints.text = GameGlossary.HintsFor(card, null);
        }

        public void Hide()
        {
            if (modal != null)
                modal.gameObject.SetActive(false);
        }

        private void Build()
        {
            if (modal != null)
                return;

            modal = UIFactory.CreateRect("CardDetailModal", parent);

            UIFactory.Stretch(modal);

            // พื้นมืด: คลิกที่ว่างเพื่อปิด
            Image dim = UIFactory.CreatePanel(
                modal, "Dim", new Color(0f, 0f, 0f, 0.88f));

            UIFactory.Stretch(dim.rectTransform);

            Button dimButton = dim.gameObject.AddComponent<Button>();

            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Hide);

            view = CardView.Create(modal, new Vector2(390f, 546f));

            RectTransform viewRect = view.GetComponent<RectTransform>();

            viewRect.anchorMin = new Vector2(0.3f, 0.52f);
            viewRect.anchorMax = new Vector2(0.3f, 0.52f);
            viewRect.anchoredPosition = Vector2.zero;

            ScreenKit.AddPanel(
                modal, "InfoPanel", new Color(0.12f, 0.08f, 0.06f, 0.96f),
                0.5f, 0.17f, 0.93f, 0.86f);

            info = ScreenKit.AddLabel(
                modal, "", 28, TextAnchor.UpperLeft,
                0.52f, 0.68f, 0.91f, 0.85f, FontStyle.Bold);

            abilities = ScreenKit.AddLabel(
                modal, "", 22, TextAnchor.UpperLeft,
                0.52f, 0.455f, 0.91f, 0.68f);

            abilities.resizeTextForBestFit = true;
            abilities.resizeTextMinSize = 12;
            abilities.resizeTextMaxSize = 22;

            hints = ScreenKit.AddLabel(
                modal, "", 18, TextAnchor.UpperLeft,
                0.52f, 0.26f, 0.91f, 0.455f);

            hints.resizeTextForBestFit = true;
            hints.resizeTextMinSize = 10;
            hints.resizeTextMaxSize = 18;

            ScreenKit.AddButton(
                modal, "Close", Hide, UIFactory.Muted,
                0.78f, 0.18f, 0.92f, 0.25f, 22);

            modal.gameObject.SetActive(false);
        }
    }
}
