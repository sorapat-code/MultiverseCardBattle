using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
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
    public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public event Action<CardView> Clicked;
        public event Action<CardView> HoverEntered;
        public event Action<CardView> HoverExited;

        // เริ่มลากการ์ด (หน้า Battle ใช้ลากลงสนาม / ลากไปตี) ถ้าไม่มีใครรับ จะส่งต่อให้ ScrollRect ด้านบน
        public event Action<CardView> DragStarted;

        private bool forwardingDrag;

        public CardInstance Instance { get; private set; }
        public CardData Data { get; private set; }

        private Image background;
        private Image artFrame;
        private Image artwork;
        private bool masteryFrame = true;
        private AspectRatioFitter artFitter;
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

        private Image typeBox;
        private Text typeText;

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

        /// <summary>
        /// เลย์เอาต์ตามแบบการ์ด: รูปเต็มใบ, ค่าใช้จ่ายซ้ายบน, ชื่อด้านบน,
        /// กล่องข้อความด้านล่าง, พลังโจมตีซ้ายล่าง, พลังชีวิตขวาล่าง
        /// </summary>
        private void Build(Vector2 size)
        {
            scale = size.x / 150f;

            group = gameObject.AddComponent<CanvasGroup>();

            // กรอบนอก (สีตามความหายาก)
            background = gameObject.AddComponent<Image>();
            background.color = new Color(0.55f, 0.58f, 0.65f, 1f);

            UIShapes.MakeRounded(background, 10f * scale);

            outline = gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(5f * scale, -5f * scale);
            outline.enabled = false;

            RectTransform root = transform as RectTransform;

            // กรอบรูป: ตัดรูปให้เป็นมุมมนและครอบเต็มช่อง (ไม่บิดสัดส่วน)
            artFrame = UIFactory.CreateRoundedPanel(
                root, "ArtFrame", new Color(0.25f, 0.3f, 0.35f, 1f),
                8f * scale);

            UIFactory.SetAnchors(artFrame.rectTransform, 0.03f, 0.025f, 0.97f, 0.975f);

            artFrame.raycastTarget = false;

            Mask mask = artFrame.gameObject.AddComponent<Mask>();

            mask.showMaskGraphic = true;

            artwork = UIFactory.CreatePanel(
                artFrame.transform, "Art", Color.white);

            artwork.raycastTarget = false;

            RectTransform artRect = artwork.rectTransform;

            artRect.anchorMin = new Vector2(0.5f, 0.5f);
            artRect.anchorMax = new Vector2(0.5f, 0.5f);
            artRect.pivot = new Vector2(0.5f, 0.5f);

            artFitter = artwork.gameObject.AddComponent<AspectRatioFitter>();
            artFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            stateText = UIFactory.CreateText(
                artFrame.transform, "", Font(16),
                new Color(1f, 0.9f, 0.4f, 1f),
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(stateText.rectTransform);

            AddShadow(stateText);

            // ชื่อ (แถบโปร่งแสงด้านบน)
            Image nameBox = UIFactory.CreateRoundedPanel(
                root, "NameBox", new Color(0f, 0f, 0f, 0.5f), 5f * scale);

            UIFactory.SetAnchors(nameBox.rectTransform, 0.2f, 0.9f, 0.96f, 0.975f);

            nameBox.raycastTarget = false;

            nameText = UIFactory.CreateText(
                nameBox.transform, "", Font(16), Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(nameText.rectTransform);

            nameText.rectTransform.offsetMin = new Vector2(10f * scale, 0f);
            nameText.rectTransform.offsetMax = new Vector2(-4f * scale, 0f);

            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 7;
            nameText.resizeTextMaxSize = Font(16);

            // ป้ายประเภทการ์ด (UNIT / SPELL ...) ใต้ชื่อ
            typeBox = UIFactory.CreateRoundedPanel(
                root, "TypeBox", new Color(0.2f, 0.45f, 0.8f, 0.95f), 4f * scale);

            UIFactory.SetAnchors(typeBox.rectTransform, 0.26f, 0.845f, 0.62f, 0.9f);

            typeBox.raycastTarget = false;

            typeText = UIFactory.CreateText(
                typeBox.transform, "", Font(10), Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(typeText.rectTransform);

            typeText.resizeTextForBestFit = true;
            typeText.resizeTextMinSize = 6;
            typeText.resizeTextMaxSize = Font(10);

            // กล่องข้อความด้านล่าง
            Image bodyBox = UIFactory.CreateRoundedPanel(
                root, "BodyBox", new Color(0f, 0f, 0f, 0.58f), 6f * scale);

            UIFactory.SetAnchors(bodyBox.rectTransform, 0.06f, 0.16f, 0.94f, 0.44f);

            bodyBox.raycastTarget = false;

            bodyText = UIFactory.CreateText(
                bodyBox.transform, "", Font(14),
                new Color(0.95f, 0.96f, 0.98f, 1f),
                TextAnchor.MiddleCenter);

            UIFactory.Stretch(bodyText.rectTransform);

            bodyText.rectTransform.offsetMin = new Vector2(4f * scale, 2f * scale);
            bodyText.rectTransform.offsetMax = new Vector2(-4f * scale, -2f * scale);

            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 6;
            bodyText.resizeTextMaxSize = Font(14);

            // ค่าใช้จ่าย (วงกลมซ้ายบน)
            Image costBox = AddCircle(root, "CostBox", 0.12f, 0.9f, 0.22f,
                new Color(0.1f, 0.65f, 0.85f, 1f), size);

            costText = UIFactory.CreateText(
                costBox.transform, "0", Font(18), Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(costText.rectTransform);

            // พลังโจมตี (ซ้ายล่าง) / พลังชีวิต (ขวาล่าง)
            attackBox = AddCircle(root, "AttackBox", 0.11f, 0.075f, 0.18f,
                new Color(0.8f, 0.15f, 0.15f, 1f), size);

            attackText = UIFactory.CreateText(
                attackBox.transform, "0", Font(15), Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(attackText.rectTransform);

            healthBox = AddCircle(root, "HealthBox", 0.89f, 0.075f, 0.18f,
                new Color(0.12f, 0.75f, 0.3f, 1f), size);

            healthText = UIFactory.CreateText(
                healthBox.transform, "0", Font(15), Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(healthText.rectTransform);

            // ป้ายจำนวน (เช่น x2) ตรงกลางด้านล่าง
            badgeText = UIFactory.CreateText(
                root, "", Font(22), new Color(1f, 0.9f, 0.4f, 1f),
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.SetAnchors(badgeText.rectTransform, 0.3f, 0.0f, 0.7f, 0.15f);

            AddShadow(badgeText);
        }

        private static void AddShadow(Text text)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();

            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }

        // วงกลมที่มีจุดศูนย์กลางและเส้นผ่านศูนย์กลาง (สัดส่วนของความกว้างการ์ด)
        private static Image AddCircle(
            RectTransform parent, string name,
            float centerX, float centerY, float diameter,
            Color color, Vector2 size)
        {
            Image image = UIFactory.CreatePanel(parent, name, color);

            UIShapes.MakeCircle(image);

            float heightFraction = diameter * size.x / size.y;

            UIFactory.SetAnchors(
                image.rectTransform,
                centerX - diameter / 2f, centerY - heightFraction / 2f,
                centerX + diameter / 2f, centerY + heightFraction / 2f);

            image.raycastTarget = false;

            Outline ring = image.gameObject.AddComponent<Outline>();

            ring.effectColor = new Color(1f, 1f, 1f, 0.85f);
            ring.effectDistance = new Vector2(1f, -1f);

            return image;
        }

        private int Font(int baseSize)
        {
            return Mathf.Max(6, Mathf.RoundToInt(baseSize * scale));
        }

        // ---------- แสดงผล ----------

        /// <summary>
        /// แสดงการ์ดในการต่อสู้ (ค่าปัจจุบัน, Keyword, สถานะ)
        /// </summary>
        public void ShowInstance(CardInstance card)
        {
            Instance = card;
            Data = card == null ? null : card.Data;

            // กรอบ Mastery ใช้กับการ์ดของผู้เล่น (ฝั่ง 1) เท่านั้น ไม่ใช่การ์ดศัตรู
            masteryFrame = card != null && card.OwnerID != 2;

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

            string keywords = CardTextGenerator.KeywordText(
                card.Keywords, data.EffectiveImmuneAmount);

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

            masteryFrame = true;

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
            background.color = RarityBorder(data.Rarity);

            // Mastery: เลเวลสูงขึ้นได้กรอบ Bronze / Gold / Platinum
            if (masteryFrame && MasteryService.IsEligible(data))
            {
                Color? frame = MasteryService.FrameColor(
                    MasteryService.GetLevel(SaveManager.Profile, data.CardID));

                if (frame.HasValue)
                    background.color = frame.Value;
            }

            artFrame.color = UniverseColor(data.Universe, data.Type);

            nameText.text = data.CardName;
            nameText.color = Color.white;

            typeText.text = TypeLabel(data.Type);
            typeBox.color = TypeColor(data.Type);

            if (data.Artwork != null)
            {
                Sprite sprite = data.Artwork;

                artwork.enabled = true;
                artwork.sprite = sprite;
                artwork.color = Color.white;

                artFitter.aspectRatio =
                    sprite.rect.height > 0f
                        ? sprite.rect.width / sprite.rect.height
                        : 1f;

                stateText.text = "";
            }
            else
            {
                artwork.enabled = false;

                // ยังไม่มีรูป: แสดงตัวอักษรย่อของชื่อใหญ่ๆ แทน
                stateText.text = Initials(data.CardName);
                stateText.fontSize = Font(44);
            }

            stateText.color = new Color(1f, 1f, 1f, 0.5f);
        }

        private static string Initials(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            string[] words =
                name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length >= 2)
                return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpper();

            return name.Length >= 2
                ? name.Substring(0, 2).ToUpper()
                : name.ToUpper();
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

                text.Append(StatusInfo.DisplayName(status.Type));

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

            stateText.fontSize = Font(16);

            stateText.text = label;
            stateText.color = new Color(1f, 0.9f, 0.4f, 1f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Clicked != null)
                Clicked(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (DragStarted != null)
            {
                DragStarted(this);

                return;
            }

            forwardingDrag = true;

            if (transform.parent != null)
            {
                ExecuteEvents.ExecuteHierarchy(
                    transform.parent.gameObject, eventData, ExecuteEvents.beginDragHandler);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (forwardingDrag && transform.parent != null)
            {
                ExecuteEvents.ExecuteHierarchy(
                    transform.parent.gameObject, eventData, ExecuteEvents.dragHandler);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (forwardingDrag && transform.parent != null)
            {
                ExecuteEvents.ExecuteHierarchy(
                    transform.parent.gameObject, eventData, ExecuteEvents.endDragHandler);
            }

            forwardingDrag = false;
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
                case Universe.DarkSouls: return new Color(0.3f, 0.28f, 0.25f, 1f);
                case Universe.SoloLeveling: return new Color(0.2f, 0.12f, 0.4f, 1f);
                case Universe.DevilMayCry: return new Color(0.08f, 0.2f, 0.45f, 1f);
                case Universe.Overlord: return new Color(0.32f, 0.08f, 0.14f, 1f);
                case Universe.JujutsuKaisen: return new Color(0.3f, 0.2f, 0.5f, 1f);
                case Universe.DemonSlayer: return new Color(0.5f, 0.12f, 0.2f, 1f);
                case Universe.AttackOnTitan: return new Color(0.28f, 0.34f, 0.2f, 1f);
                case Universe.ChainsawMan: return new Color(0.6f, 0.2f, 0.1f, 1f);
                default: return new Color(0.25f, 0.3f, 0.35f, 1f);
            }
        }

        private static string TypeLabel(CardType type)
        {
            switch (type)
            {
                case CardType.Spell: return "SPELL";
                case CardType.Equipment: return "EQUIPMENT";
                case CardType.Leader: return "LEADER";
                default: return "UNIT";
            }
        }

        private static Color TypeColor(CardType type)
        {
            switch (type)
            {
                case CardType.Spell: return new Color(0.62f, 0.3f, 0.85f, 0.95f);
                case CardType.Equipment: return new Color(0.85f, 0.55f, 0.15f, 0.95f);
                case CardType.Leader: return new Color(0.85f, 0.2f, 0.3f, 0.95f);
                default: return new Color(0.2f, 0.45f, 0.8f, 0.95f);
            }
        }

        // สีกรอบการ์ดตามความหายาก
        private static Color RarityBorder(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Rare: return new Color(0.35f, 0.6f, 1f, 1f);
                case CardRarity.Epic: return new Color(0.7f, 0.45f, 1f, 1f);
                case CardRarity.Legendary: return new Color(1f, 0.75f, 0.25f, 1f);
                case CardRarity.Secret: return new Color(1f, 0.4f, 0.5f, 1f);
                default: return new Color(0.55f, 0.58f, 0.65f, 1f);
            }
        }
    }
}
