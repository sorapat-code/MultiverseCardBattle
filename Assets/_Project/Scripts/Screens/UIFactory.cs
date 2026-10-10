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
        public static readonly Color Background = new Color(0.08f, 0.09f, 0.13f, 1f);
        public static readonly Color Panel = new Color(0.14f, 0.16f, 0.22f, 0.95f);
        public static readonly Color Accent = new Color(0.25f, 0.45f, 0.85f, 1f);
        public static readonly Color Danger = new Color(0.75f, 0.25f, 0.25f, 1f);
        public static readonly Color Success = new Color(0.25f, 0.65f, 0.35f, 1f);
        public static readonly Color Muted = new Color(0.35f, 0.38f, 0.45f, 1f);
        public static readonly Color TextColor = new Color(0.95f, 0.96f, 0.98f, 1f);

        private static Font cachedFont;

        public static Font DefaultFont
        {
            get
            {
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

            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
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

            UIShapes.MakeRounded(image, 10f);

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

            return button;
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

            background.color = new Color(0f, 0f, 0f, 0.25f);

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
        public static RectTransform CreateScreenRoot(string name)
        {
            EnsureEventSystem();

            Canvas canvas = CreateCanvas(name);

            Image background = CreatePanel(canvas.transform, "Background", Background);

            Stretch(background.rectTransform);

            return canvas.GetComponent<RectTransform>();
        }

        public static Text CreateTitle(Transform parent, string title)
        {
            Text text = CreateText(
                parent, title, 56, TextColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            SetAnchors(text.rectTransform, 0.2f, 0.9f, 0.8f, 1f);

            return text;
        }
    }
}
