using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Screens
{
    public enum CardHighlight
    {
        None,

        // ลงการ์ดได้ / โจมตีได้
        Ready,

        // กำลังเลือกอยู่
        Selected,

        // เป็นเป้าหมายที่เลือกได้
        Target
    }

    /// <summary>
    /// การ์ด 1 ใบบนหน้าจอ สร้างด้วยโค้ดผ่าน CardView.Create
    /// แสดงได้ 2 แบบ: CardInstance (ในการต่อสู้) หรือ CardData (คอลเลกชัน/เด็ค)
    /// </summary>
    public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<CardView> Clicked;
        public event Action<CardView> HoverEntered;
        public event Action<CardView> HoverExited;

        public CardInstance Instance { get; private set; }
        public CardData Data { get; private set; }

        private Image background;
        private Image artwork;
        private Outline outline;
        private CanvasGroup group;

        private Text nameText;
        private Text costText;
        private Text attackText;
        private Text healthText;
        private Text bodyText;
        private Text badgeText;
        private Text stateText;

        private Image attackBox;
        private Image healthBox;

        private float scale = 1f;

        // ---------- สร้าง ----------

        public static CardView Create(Transform parent, Vector2 size)
        {
            RectTransform rect = UIFactory.CreateRect("Card", parent);

            rect.sizeDelta = size;

            CardView view = rect.gameObject.AddComponent<CardView>();

            view.Build(size);

            return view;
        }

        private void Build(Vector2 size)
        {
            scale = size.x / 150f;

            group = gameObject.AddComponent<CanvasGroup>();

            background = gameObject.AddComponent<Image>();
            background.color = new Color(0.2f, 0.22f, 0.3f, 1f);

            outline = gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(5f * scale, -5f * scale);
            outline.enabled = false;

            RectTransform root = transform as RectTransform;

            // ช่องรูป
            artwork = AddBox(root, "Art", 0.06f, 0.46f, 0.94f, 0.82f,
                new Color(0f, 0f, 0f, 0.28f));

            stateText = UIFactory.CreateText(
                artwork.transform, "", Font(16),
                new Color(1f, 0.9f, 0.4f, 1f),
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(stateText.rectTransform);

            // ค่าใช้จ่าย
            Image costBox = AddBox(root, "CostBox", 0f, 0.83f, 0.24f, 1f,
                new Color(0.2f, 0.4f, 0.85f, 1f));

            costText = UIFactory.CreateText(
                costBox.transform, "0", Font(24), Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(costText.rectTransform);

            // ชื่อ
            nameText = UIFactory.CreateText(
                root, "", Font(18), Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.SetAnchors(nameText.rectTransform, 0.25f, 0.83f, 0.98f, 1f);

            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 8;
            nameText.resizeTextMaxSize = Font(18);

            // คำอธิบาย
            bodyText = UIFactory.CreateText(
                root, "", Font(14), new Color(0.92f, 0.93f, 0.96f, 1f),
                TextAnchor.UpperCenter);

            UIFactory.SetAnchors(bodyText.rectTransform, 0.05f, 0.17f, 0.95f, 0.45f);

            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 7;
            bodyText.resizeTextMaxSize = Font(14);

            // พลังโจมตี / พลังชีวิต
            attackBox = AddBox(root, "AttackBox", 0f, 0f, 0.28f, 0.16f,
                new Color(0.8f, 0.3f, 0.2f, 1f));

            attackText = UIFactory.CreateText(
                attackBox.transform, "0", Font(24), Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(attackText.rectTransform);

            healthBox = AddBox(root, "HealthBox", 0.72f, 0f, 1f, 0.16f,
                new Color(0.25f, 0.65f, 0.35f, 1f));

            healthText = UIFactory.CreateText(
                healthBox.transform, "0", Font(24), Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(healthText.rectTransform);

            // ป้ายจำนวน (เช่น x2)
            badgeText = UIFactory.CreateText(
                root, "", Font(22), new Color(1f, 0.9f, 0.4f, 1f),
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.SetAnchors(badgeText.rectTransform, 0.3f, 0f, 0.7f, 0.16f);
        }

        private int Font(int baseSize)
        {
            return Mathf.Max(6, Mathf.RoundToInt(baseSize * scale));
        }

        private static Image AddBox(
            RectTransform parent, string name,
            float xMin, float yMin, float xMax, float yMax, Color color)
        {
            Image image = UIFactory.CreatePanel(parent, name, color);

            UIFactory.SetAnchors(image.rectTransform, xMin, yMin, xMax, yMax);

            image.raycastTarget = false;

            return image;
        }

        // ---------- แสดงผล ----------

        /// <summary>
        /// แสดงการ์ดในการต่อสู้ (ค่าปัจจุบัน, Keyword, สถานะ)
        /// </summary>
        public void ShowInstance(CardInstance card)
        {
            Instance = card;
            Data = card == null ? null : card.Data;

            if (card == null || card.Data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            CardData data = card.Data;

            ApplyCommon(data);

            costText.text = card.CurrentCost.ToString();

            costText.color = card.CurrentCost < data.Cost
                ? new Color(0.6f, 1f, 0.6f, 1f)
                : (card.CurrentCost > data.Cost
                    ? new Color(1f, 0.6f, 0.6f, 1f)
                    : Color.white);

            bool hasStats = data.Type == CardType.Unit;

            attackBox.gameObject.SetActive(hasStats);
            healthBox.gameObject.SetActive(hasStats);

            attackText.text = card.CurrentAttack.ToString();

            attackText.color = card.CurrentAttack > data.Attack
                ? new Color(0.6f, 1f, 0.6f, 1f)
                : Color.white;

            healthText.text = card.CurrentHealth.ToString();

            healthText.color = card.CurrentHealth < card.MaxHealth
                ? new Color(1f, 0.6f, 0.6f, 1f)
                : (card.MaxHealth > data.Health
                    ? new Color(0.6f, 1f, 0.6f, 1f)
                    : Color.white);

            StringBuilder body = new StringBuilder();

            string keywords = CardTextGenerator.KeywordText(card.Keywords);

            if (keywords.Length > 0)
                body.Append("<b>").Append(keywords).Append("</b>");

            string statuses = StatusText(card);

            if (statuses.Length > 0)
            {
                if (body.Length > 0)
                    body.Append('\n');

                body.Append("<color=#FFB060>").Append(statuses).Append("</color>");
            }

            string actions = CardTextGenerator.ActionsText(data);

            if (actions.Length > 0)
            {
                if (body.Length > 0)
                    body.Append('\n');

                body.Append(actions);
            }

            bodyText.text = body.ToString();

            badgeText.text = "";
        }

        /// <summary>
        /// แสดงการ์ดจากข้อมูล (ค่าตั้งต้น) พร้อมป้ายข้อความ เช่น "x2"
        /// </summary>
        public void ShowData(CardData data, string badge = "")
        {
            Instance = null;
            Data = data;

            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            ApplyCommon(data);

            costText.text = data.Cost.ToString();
            costText.color = Color.white;

            bool hasStats = data.Type == CardType.Unit;

            attackBox.gameObject.SetActive(hasStats);
            healthBox.gameObject.SetActive(hasStats);

            attackText.text = data.Attack.ToString();
            attackText.color = Color.white;

            healthText.text = data.Health.ToString();
            healthText.color = Color.white;

            bodyText.text = CardTextGenerator.Describe(data);

            badgeText.text = badge ?? "";
        }

        private void ApplyCommon(CardData data)
        {
            background.color = UniverseColor(data.Universe, data.Type);

            nameText.text = data.CardName;
            nameText.color = RarityColor(data.Rarity);

            if (data.Artwork != null)
            {
                artwork.sprite = data.Artwork;
                artwork.color = Color.white;
                artwork.preserveAspect = true;
                stateText.text = "";
            }
            else
            {
                artwork.sprite = null;
                artwork.color = new Color(0f, 0f, 0f, 0.28f);
                stateText.text = data.Type.ToString().ToUpper();
            }

            stateText.color = new Color(1f, 1f, 1f, 0.35f);
        }

        private static string StatusText(CardInstance card)
        {
            if (card.Statuses.Count == 0)
                return "";

            StringBuilder text = new StringBuilder();

            foreach (StatusEffect status in card.Statuses)
            {
                if (text.Length > 0)
                    text.Append(", ");

                text.Append(status.Type);

                if (status.Value > 0)
                    text.Append(' ').Append(status.Value);
            }

            return text.ToString();
        }

        // ---------- สถานะภาพ ----------

        public void SetHighlight(CardHighlight highlight)
        {
            switch (highlight)
            {
                case CardHighlight.Ready:
                    outline.enabled = true;
                    outline.effectColor = new Color(0.3f, 0.95f, 0.4f, 1f);
                    break;

                case CardHighlight.Selected:
                    outline.enabled = true;
                    outline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
                    break;

                case CardHighlight.Target:
                    outline.enabled = true;
                    outline.effectColor = new Color(1f, 0.25f, 0.25f, 1f);
                    break;

                default:
                    outline.enabled = false;
                    break;
            }
        }

        public void SetDimmed(bool dimmed)
        {
            group.alpha = dimmed ? 0.55f : 1f;
        }

        /// <summary>
        /// ข้อความสถานะบนช่องรูป เช่น "SLEEPING" (ยูนิตที่เพิ่งลงสนาม)
        /// </summary>
        public void SetStateLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return;

            stateText.text = label;
            stateText.color = new Color(1f, 0.9f, 0.4f, 1f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Clicked != null)
                Clicked(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (HoverEntered != null)
                HoverEntered(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (HoverExited != null)
                HoverExited(this);
        }

        /// <summary>
        /// ปิดการรับเมาส์ (ใช้กับการ์ดพรีวิว ไม่ให้บังการ์ดอื่น)
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            group.blocksRaycasts = interactable;
        }

        // ---------- สี ----------

        private static Color UniverseColor(Universe universe, CardType type)
        {
            if (type == CardType.Spell)
                return new Color(0.35f, 0.22f, 0.45f, 1f);

            switch (universe)
            {
                case Universe.Naruto: return new Color(0.55f, 0.35f, 0.15f, 1f);
                case Universe.OnePiece: return new Color(0.15f, 0.35f, 0.55f, 1f);
                case Universe.Bleach: return new Color(0.2f, 0.2f, 0.25f, 1f);
                case Universe.DragonBall: return new Color(0.6f, 0.4f, 0.1f, 1f);
                case Universe.JoJo: return new Color(0.5f, 0.2f, 0.45f, 1f);
                case Universe.WutheringWaves: return new Color(0.15f, 0.45f, 0.45f, 1f);
                case Universe.HonkaiStarRail: return new Color(0.3f, 0.3f, 0.6f, 1f);
                case Universe.Fate: return new Color(0.5f, 0.2f, 0.25f, 1f);
                case Universe.Mythology: return new Color(0.4f, 0.4f, 0.2f, 1f);
                default: return new Color(0.25f, 0.3f, 0.35f, 1f);
            }
        }

        private static Color RarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Rare: return new Color(0.5f, 0.75f, 1f, 1f);
                case CardRarity.Epic: return new Color(0.8f, 0.55f, 1f, 1f);
                case CardRarity.Legendary: return new Color(1f, 0.7f, 0.2f, 1f);
                case CardRarity.Secret: return new Color(1f, 0.4f, 0.5f, 1f);
                default: return Color.white;
            }
        }
    }
}
