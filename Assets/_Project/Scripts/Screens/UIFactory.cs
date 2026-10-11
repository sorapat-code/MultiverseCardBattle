using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ตัวช่วยสร้าง uGUI ด้วยโค้ด (ทุกหน้าจอสร้าง UI ตอนรัน จึงไม่ต้องแก้ Scene)
    /// ใช้ Legacy Text เพื่อไม่ต้อง Import TextMeshPro Essentials
    /// </summary>
    public static class UIFactory
    {
        // ธีมแฟนตาซี: ไม้เข้ม ทอง และกระดาษหนัง
        public static readonly Color Background = new Color(0.08f, 0.06f, 0.05f, 1f);
        public static readonly Color Panel = new Color(0.17f, 0.12f, 0.09f, 0.95f);
        public static readonly Color Accent = new Color(0.66f, 0.46f, 0.16f, 1f);
        public static readonly Color Danger = new Color(0.66f, 0.2f, 0.18f, 1f);
        public static readonly Color Success = new Color(0.27f, 0.52f, 0.28f, 1f);
        public static readonly Color Muted = new Color(0.34f, 0.3f, 0.27f, 1f);
        public static readonly Color TextColor = new Color(0.97f, 0.93f, 0.83f, 1f);

        // สีทองของกรอบและหัวข้อ
        public static readonly Color Gold = new Color(0.9f, 0.72f, 0.33f, 1f);
        public static readonly Color GoldDim = new Color(0.62f, 0.48f, 0.22f, 1f);

        private static Font cachedFont;
        private static Font regularFont;
        private static Font boldFont;
        private static Font italicFont;
        private static bool fontsLoaded;

        // ฟอนต์ที่แถมมากับเกม (Resources/Fonts: Inter, SIL OFL) เหมือนกันทุกเครื่อง
        private static void LoadFonts()
        {
            if (fontsLoaded)
                return;

            fontsLoaded = true;

            regularFont = Resources.Load<Font>("Fonts/Inter-Regular");
            boldFont = Resources.Load<Font>("Fonts/Inter-Bold");
            italicFont = Resources.Load<Font>("Fonts/Inter-Italic");
        }

        /// <summary>ตั้งฟอนต์และสไตล์ให้ตัวอักษร (ใช้ไฟล์ตัวหนา/เอียงจริง ไม่ปล่อยให้ Unity ทำหนาเทียม)</summary>
        public static void ApplyFont(Text text, FontStyle style)
        {
            LoadFonts();

            if (regularFont == null)
            {
                text.font = DefaultFont;
                text.fontStyle = style;
                return;
            }

            bool bold = style == FontStyle.Bold || style == FontStyle.BoldAndItalic;
            bool italic = style == FontStyle.Italic || style == FontStyle.BoldAndItalic;

            if (bold && boldFont != null)
            {
                text.font = boldFont;
                text.fontStyle = italic ? FontStyle.Italic : FontStyle.Normal;
            }
            else if (italic && !bold && italicFont != null)
            {
                text.font = italicFont;
                text.fontStyle = FontStyle.Normal;
            }
            else
            {
                text.font = regularFont;
                text.fontStyle = style;
            }
        }

        public static Font DefaultFont
        {
            get
            {
                LoadFonts();

                if (cachedFont == null && regularFont != null)
                    cachedFont = regularFont;

                if (cachedFont == null)
                {
                    // ฟอนต์เซอริฟแนวแฟนตาซี (ใช้ตัวแรกที่เครื่องมี) ไม่มีก็ใช้ฟอนต์ในตัว
                    cachedFont = Font.CreateDynamicFontFromOSFont(
                        new[] { "Segoe UI", "Arial", "Helvetica" },
                        18);

                    if (cachedFont != null && cachedFont.fontNames.Length == 0)
                        cachedFont = null;
                }

                if (cachedFont == null)
                {
                    cachedFont =
                        Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                if (cachedFont == null)
                {
                    cachedFont =
                        Font.CreateDynamicFontFromOSFont("Arial", 16);
                }

                return cachedFont;
            }
        }

        // ---------- โครงสร้างพื้นฐาน ----------

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject go = new GameObject("EventSystem");

            go.AddComponent<EventSystem>();

            InputSystemUIInputModule module =
                go.AddComponent<InputSystemUIInputModule>();

            // ใช้ Action เริ่มต้นของ Input System (เมาส์/ทัช/คีย์บอร์ด)
            module.AssignDefaultActions();
        }

        public static Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name);

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));

            RectTransform rect = go.GetComponent<RectTransform>();

            rect.SetParent(parent, false);

            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // ตั้งตำแหน่งแบบสัดส่วน (0-1) ของ Parent
        public static void SetAnchors(
            RectTransform rect,
            float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static RectTransform CreateZone(
            Transform parent, string name,
            float xMin, float yMin, float xMax, float yMax)
        {
            RectTransform rect = CreateRect(name, parent);

            SetAnchors(rect, xMin, yMin, xMax, yMax);

            return rect;
        }

        // ---------- Image / Text / Button ----------

        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateRect(name, parent);

            Image image = rect.gameObject.AddComponent<Image>();

            image.color = color;

            return image;
        }

        /// <summary>
        /// แผงสี่เหลี่ยมมุมมน
        /// </summary>
        public static Image CreateRoundedPanel(
            Transform parent, string name, Color color, float cornerSize = 12f)
        {
            Image image = CreatePanel(parent, name, color);

            UIShapes.MakeRounded(image, cornerSize);

            return image;
        }

        /// <summary>
        /// กรอบเส้นทองรอบ Parent (Image มุมมนแบบเส้น) ไม่รับการคลิก
        /// </summary>
        public static Image AddRing(
            Transform parent, Color color, float cornerSize = 10f)
        {
            Image ring = CreatePanel(parent, "Ring", color);

            ring.sprite = UIShapes.Ring;
            ring.type = Image.Type.Sliced;
            ring.pixelsPerUnitMultiplier = 16f / Mathf.Max(1f, cornerSize);
            ring.raycastTarget = false;

            Stretch(ring.rectTransform);

            return ring;
        }

        /// <summary>
        /// แผงมีกรอบทอง: พื้นสีเข้มมุมมน + เส้นกรอบ
        /// </summary>
        public static Image CreateFramedPanel(
            Transform parent, string name, Color color, float cornerSize = 12f)
        {
            Image panel = CreateRoundedPanel(parent, name, color, cornerSize);

            AddRing(panel.transform, GoldDim, cornerSize);

            return panel;
        }

        /// <summary>
        /// เส้นคั่นสีทองจางที่ปลาย กำหนดตำแหน่งแบบสัดส่วน
        /// </summary>
        public static Image CreateDivider(
            Transform parent, float xMin, float yMin, float xMax, float yMax)
        {
            Image line = CreatePanel(parent, "Divider", Gold);

            line.sprite = UIShapes.Divider;
            line.raycastTarget = false;

            SetAnchors(line.rectTransform, xMin, yMin, xMax, yMax);

            return line;
        }

        public static Text CreateText(
            Transform parent,
            string content,
            int fontSize,
            Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter,
            FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = CreateRect("Text", parent);

            Text text = rect.gameObject.AddComponent<Text>();

            ApplyFont(text, style);
            text.text = content;
            text.fontSize = Mathf.RoundToInt(fontSize * GameSettings.TextScale);
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.supportRichText = true;

            return text;
        }

        public static Button CreateButton(
            Transform parent,
            string label,
            UnityAction onClick,
            Color background,
            int fontSize = 28)
        {
            RectTransform rect = CreateRect("Button_" + label, parent);

            Image image = rect.gameObject.AddComponent<Image>();

            image.color = background;

            // ปุ่มนูนสีตามธีม + กรอบทองบางๆ
            image.sprite = UIShapes.Bevel;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 16f / 10f;

            AddRing(rect, GoldDim, 10f);

            Button button = rect.gameObject.AddComponent<Button>();

            button.targetGraphic = image;

            // ปุ่มที่ปิดใช้งานจะจางลง ปุ่มที่กดอยู่จะเข้มขึ้น
            ColorBlock colors = button.colors;

            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);

            button.colors = colors;

            button.onClick.AddListener(() => Sfx.Play(SoundId.Click));

            if (onClick != null)
                button.onClick.AddListener(onClick);

            Text text = CreateText(
                rect, label, fontSize, TextColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            Stretch(text.rectTransform);

            // ตัวอักษรใหญ่ขึ้นแล้วล้นปุ่ม: ย่อให้พอดีแทนที่จะถูกตัด
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = text.fontSize;

            AddTextShadow(text);

            return button;
        }

        /// <summary>เงาตัวอักษรให้อ่านง่ายบนพื้นหลายสี</summary>
        public static void AddTextShadow(Text text)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();

            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }

        public static void SetButtonLabel(Button button, string label)
        {
            Text text = button.GetComponentInChildren<Text>();

            if (text != null)
                text.text = label;
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.Destroy(parent.GetChild(i).gameObject);
        }

        // ---------- Layout ----------

        public static HorizontalLayoutGroup AddHorizontalRow(
            RectTransform rect,
            float spacing,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            HorizontalLayoutGroup layout =
                rect.gameObject.AddComponent<HorizontalLayoutGroup>();

            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return layout;
        }

        /// <summary>
        /// ScrollView แนวตั้ง ได้ content สำหรับใส่ Layout Group + ลูก
        /// </summary>
        public static ScrollRect CreateScrollView(
            Transform parent,
            string name,
            out RectTransform content)
        {
            RectTransform root = CreateRect(name, parent);

            Image background = root.gameObject.AddComponent<Image>();

            background.color = new Color(0f, 0f, 0f, 0.32f);

            UIShapes.MakeRounded(background, 12f);

            AddRing(root, GoldDim, 12f);

            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = CreateRect("Viewport", root);

            Stretch(viewport);

            viewport.gameObject.AddComponent<RectMask2D>();

            content = CreateRect("Content", viewport);

            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);

            ContentSizeFitter fitter =
                content.gameObject.AddComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            return scroll;
        }

        public static GridLayoutGroup AddGrid(
            RectTransform content,
            Vector2 cellSize,
            float spacing,
            int padding)
        {
            GridLayoutGroup grid =
                content.gameObject.AddComponent<GridLayoutGroup>();

            grid.cellSize = cellSize;
            grid.spacing = new Vector2(spacing, spacing);
            grid.padding = new RectOffset(padding, padding, padding, padding);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;

            return grid;
        }

        public static VerticalLayoutGroup AddVertical(
            RectTransform content,
            float spacing,
            int padding)
        {
            VerticalLayoutGroup layout =
                content.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return layout;
        }

        // ---------- หน้าจอทั่วไป ----------

        /// <summary>
        /// สร้าง Canvas เต็มจอพร้อมพื้นหลัง คืน RectTransform ของ Canvas
        /// </summary>
        public static RectTransform CreateScreenRoot(
            string name, bool battle = false)
        {
            EnsureEventSystem();

            Canvas canvas = CreateCanvas(name);

            Image background = CreatePanel(canvas.transform, "Background", Color.white);

            // ใช้รูปพื้นหลังจริงถ้ามีใน Resources/UI ไม่งั้นใช้ภาพไล่เฉดจากโค้ด
            Sprite art = UIArt.Get(battle ? "bg_battle" : "bg_menu");

            background.sprite = art != null
                ? art
                : (battle ? UIShapes.BattleBackdrop : UIShapes.MenuBackdrop);

            background.raycastTarget = false;

            Stretch(background.rectTransform);

            // ฝุ่นประกายลอยและขอบจอมืด ให้ทุกหน้ามีบรรยากาศ
            AmbientFx.Attach(canvas.transform, battle);

            return canvas.GetComponent<RectTransform>();
        }

        public static Text CreateTitle(Transform parent, string title)
        {
            Text text = CreateText(
                parent, title, 60, Gold,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            SetAnchors(text.rectTransform, 0.15f, 0.9f, 0.85f, 1f);

            AddTextShadow(text);

            // เส้นทองใต้หัวข้อ
            CreateDivider(parent, 0.2f, 0.893f, 0.8f, 0.9f);

            return text;
        }
    }
}
