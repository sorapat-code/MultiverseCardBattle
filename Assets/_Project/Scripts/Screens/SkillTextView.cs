using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// แผงแสดงสกิลของการ์ด: ศัพท์เกม (Keyword / สถานะ / Battlecry ฯลฯ) ถูกขีดเส้นใต้
    /// เอาเมาส์ชี้ (หรือคลิก) แล้วมีกรอบคำอธิบายเด้งขึ้นมาข้างแผง
    ///
    /// ใช้ Text แบบเดิมของ UGUI ซึ่งไม่มีขีดเส้นใต้ในตัว จึงจัดวางทีละคำเอง:
    /// วัดความกว้างแต่ละคำ ตัดบรรทัดเอง ลดขนาดตัวอักษรลงจนพอดีกรอบ
    /// แล้ววางเส้นใต้เป็น Image บางๆ ใต้คำที่เป็นศัพท์
    /// </summary>
    public class SkillTextView : MonoBehaviour
    {
        private const float UnderlineThickness = 2f;
        private const float TooltipWidth = 360f;
        private const float TooltipPadding = 10f;
        private const int TooltipFontSize = 18;

        private static readonly Color TermColor = new Color(1f, 0.86f, 0.45f, 1f);

        private RectTransform container;
        private RectTransform tooltipParent;
        private Text template;

        private RectTransform tooltip;
        private Text tooltipText;

        private int maxFont = 16;
        private int minFont = 9;

        private string currentText = "";
        private List<string[]> currentTerms = new List<string[]>();
        private string currentSignature = "";
        private float builtWidth = -1f;
        private float builtHeight = -1f;
        private bool dirty;

        public bool HasContent
        {
            get { return !string.IsNullOrEmpty(currentText); }
        }

        /// <summary>สร้างแผงที่ตำแหน่งสัดส่วน (0-1) ของ parent; tooltipParent คือที่ที่กรอบคำอธิบายจะไปอยู่ (ปกติคือราก Canvas)</summary>
        public static SkillTextView Create(
            Transform parent,
            RectTransform tooltipParent,
            float xMin, float yMin, float xMax, float yMax,
            int maxFont, int minFont)
        {
            RectTransform rect = UIFactory.CreateRect("SkillText", parent);

            UIFactory.SetAnchors(rect, xMin, yMin, xMax, yMax);

            SkillTextView view = rect.gameObject.AddComponent<SkillTextView>();

            view.container = rect;
            view.tooltipParent = tooltipParent;
            view.maxFont = maxFont;
            view.minFont = minFont;

            // ใช้ Text ต้นแบบ (ปิดไว้) เพื่อวัดความกว้างด้วยฟอนต์เดียวกับที่วาดจริง
            view.template = UIFactory.CreateText(
                rect, "", maxFont, UIFactory.TextColor, TextAnchor.UpperLeft);

            view.template.fontSize = maxFont;

            view.template.horizontalOverflow = HorizontalWrapMode.Overflow;
            view.template.verticalOverflow = VerticalWrapMode.Overflow;
            view.template.supportRichText = false;
            view.template.raycastTarget = false;

            return view;
        }

        /// <summary>ตั้งข้อความสกิลกับรายการศัพท์ { ชื่อ, คำอธิบาย } ข้อความว่าง = ล้างแผง</summary>
        public void Show(string text, List<string[]> terms)
        {
            string newText = text ?? "";
            List<string[]> newTerms = terms ?? new List<string[]>();

            // เนื้อหาเดิม (หน้าจอวาดใหม่บ่อย) ไม่ต้องจัดวางใหม่ และไม่ปิด tooltip ที่กำลังชี้อยู่
            if (newText == currentText && Signature(newText, newTerms) == currentSignature)
                return;

            currentText = newText;
            currentTerms = newTerms;
            currentSignature = Signature(newText, newTerms);

            dirty = true;

            HideTooltip();
        }

        public void Clear()
        {
            Show("", null);
        }

        private void LateUpdate()
        {
            if (container == null)
                return;

            Rect rect = container.rect;

            // เนื้อหาเปลี่ยน / กรอบเพิ่งถูกจัดวาง (ความกว้างยังเป็น 0 ตอนสั่ง Show) / ขนาดเปลี่ยน -> วาดใหม่
            if (rect.width < 4f || rect.height < 4f)
                return;

            if (!dirty &&
                Mathf.Approximately(rect.width, builtWidth) &&
                Mathf.Approximately(rect.height, builtHeight))
                return;

            dirty = false;
            builtWidth = rect.width;
            builtHeight = rect.height;

            Rebuild(rect.width, rect.height);
        }

        private void OnDisable()
        {
            HideTooltip();

            // ซ่อนแล้วเปิดใหม่ให้วาดใหม่ (กัน Hover ค้าง)
            dirty = true;
        }

        private static string Signature(string text, List<string[]> terms)
        {
            StringBuilder sb = new StringBuilder(text);

            sb.Append('\u0001');

            foreach (string[] term in terms)
                sb.Append(term[0]).Append('\u0002').Append(term[1]).Append('\u0003');

            return sb.ToString();
        }

        // ---------- การจัดวาง ----------

        private class Piece
        {
            public string Text;
            public string Description; // != null = เป็นศัพท์ (ขีดเส้นใต้)
            public bool LineBreak;
            public float Width;
            public float X;
            public float Y;
        }

        private void Rebuild(float width, float height)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);

                if (child.gameObject == template.gameObject)
                    continue;

                Destroy(child.gameObject);
            }

            if (string.IsNullOrEmpty(currentText))
            {
                template.text = "";

                return;
            }

            List<Piece> pieces = Tokenize(currentText, currentTerms);

            int size = maxFont;
            float lineHeight = 0f;

            // ลดขนาดตัวอักษรลงจนทั้งก้อนพอดีความสูงของกรอบ
            for (; size >= minFont; size--)
            {
                float total = Layout(pieces, size, width, out lineHeight);

                if (total <= height || size == minFont)
                    break;
            }

            if (size < minFont)
                size = minFont;

            lineHeight = 0f;

            Layout(pieces, size, width, out lineHeight);

            foreach (Piece piece in pieces)
            {
                if (piece.LineBreak || string.IsNullOrEmpty(piece.Text))
                    continue;

                CreatePiece(piece, size, lineHeight);
            }

            // ต้นแบบใช้วัดอย่างเดียว ไม่ให้เหลือตัวอักษรค้างบนจอ
            template.text = "";
        }

        // จัดตำแหน่งทุกชิ้น คืนความสูงรวม
        private float Layout(List<Piece> pieces, int size, float width, out float lineHeight)
        {
            template.fontSize = size;

            template.text = "Ag";

            lineHeight = Mathf.Ceil(template.preferredHeight);

            if (lineHeight < size)
                lineHeight = size * 1.2f;

            template.text = "A A";
            float spaced = template.preferredWidth;

            template.text = "AA";
            float spaceWidth = Mathf.Max(2f, spaced - template.preferredWidth);

            float x = 0f;
            float y = 0f;
            bool lineHasContent = false;

            foreach (Piece piece in pieces)
            {
                if (piece.LineBreak)
                {
                    x = 0f;
                    y += lineHeight;
                    lineHasContent = false;

                    continue;
                }

                template.text = piece.Text;
                piece.Width = template.preferredWidth;

                float needed = (lineHasContent ? spaceWidth : 0f) + piece.Width;

                if (lineHasContent && x + needed > width)
                {
                    x = 0f;
                    y += lineHeight;
                    lineHasContent = false;
                }

                if (lineHasContent)
                    x += spaceWidth;

                piece.X = x;
                piece.Y = y;

                x += piece.Width;
                lineHasContent = true;
            }

            return y + lineHeight;
        }

        // ตัดข้อความเป็นชิ้น: คำธรรมดา / ศัพท์ (ใช้ชื่อยาวสุดก่อน ไม่สนตัวพิมพ์เล็กใหญ่) / ขึ้นบรรทัดใหม่
        private static List<Piece> Tokenize(string text, List<string[]> terms)
        {
            List<Piece> pieces = new List<Piece>();

            Dictionary<string, string> descriptions =
                new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

            List<string> names = new List<string>();

            foreach (string[] term in terms)
            {
                if (string.IsNullOrEmpty(term[0]) || descriptions.ContainsKey(term[0]))
                    continue;

                descriptions[term[0]] = term[1];
                names.Add(term[0]);
            }

            names.Sort((a, b) => b.Length.CompareTo(a.Length));

            Regex regex = null;

            if (names.Count > 0)
            {
                StringBuilder pattern = new StringBuilder("(?<![A-Za-z0-9])(");

                for (int i = 0; i < names.Count; i++)
                {
                    if (i > 0)
                        pattern.Append('|');

                    pattern.Append(Regex.Escape(names[i]));
                }

                pattern.Append(")(?![A-Za-z0-9])");

                regex = new Regex(pattern.ToString(), RegexOptions.IgnoreCase);
            }

            string[] lines = text.Split('\n');

            for (int l = 0; l < lines.Length; l++)
            {
                if (l > 0)
                    pieces.Add(new Piece { LineBreak = true });

                string line = lines[l];
                int position = 0;

                if (regex != null)
                {
                    foreach (Match match in regex.Matches(line))
                    {
                        AddWords(pieces, line.Substring(position, match.Index - position));

                        pieces.Add(new Piece
                        {
                            Text = match.Value,
                            Description = descriptions[match.Value]
                        });

                        position = match.Index + match.Length;
                    }
                }

                AddWords(pieces, line.Substring(position));
            }

            return pieces;
        }

        private static void AddWords(List<Piece> pieces, string chunk)
        {
            foreach (string word in chunk.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
                pieces.Add(new Piece { Text = word });
        }

        // ---------- วาดชิ้นส่วน ----------

        private void CreatePiece(Piece piece, int size, float lineHeight)
        {
            bool isTerm = piece.Description != null;

            Text text = UIFactory.CreateText(
                container, piece.Text, size,
                isTerm ? TermColor : UIFactory.TextColor,
                TextAnchor.UpperLeft,
                FontStyle.Normal);

            // CreateText คูณขนาดด้วยสเกลตัวอักษรของผู้เล่น: ตั้งเองให้ตรงกับที่ใช้วัดความกว้าง
            text.fontSize = size;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = text.rectTransform;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            rect.sizeDelta = new Vector2(piece.Width, lineHeight);
            rect.anchoredPosition = new Vector2(piece.X, -piece.Y);

            if (!isTerm)
                return;

            UIFactory.AddTextShadow(text);

            text.raycastTarget = true;

            Image line = UIFactory.CreatePanel(rect, "Underline", TermColor);

            line.raycastTarget = false;

            line.rectTransform.anchorMin = new Vector2(0f, 0f);
            line.rectTransform.anchorMax = new Vector2(1f, 0f);
            line.rectTransform.pivot = new Vector2(0.5f, 0f);
            line.rectTransform.sizeDelta = new Vector2(0f, UnderlineThickness);
            line.rectTransform.anchoredPosition = new Vector2(0f, 1f);

            TermHover hover = text.gameObject.AddComponent<TermHover>();

            hover.Owner = this;
            hover.Name = piece.Text;
            hover.Description = piece.Description;
        }

        // ---------- กรอบคำอธิบาย ----------

        private void BuildTooltip()
        {
            if (tooltip != null || tooltipParent == null)
                return;

            Image background = UIFactory.CreatePanel(
                tooltipParent, "TermTooltip", new Color(0.04f, 0.05f, 0.08f, 0.97f));

            background.raycastTarget = false;

            tooltip = background.rectTransform;

            tooltip.anchorMin = new Vector2(0.5f, 0.5f);
            tooltip.anchorMax = new Vector2(0.5f, 0.5f);
            tooltip.pivot = new Vector2(0f, 1f);

            Outline outline = background.gameObject.AddComponent<Outline>();

            outline.effectColor = UIFactory.Gold;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            tooltipText = UIFactory.CreateText(
                tooltip, "", TooltipFontSize, UIFactory.TextColor, TextAnchor.UpperLeft);

            tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
            tooltipText.raycastTarget = false;

            tooltipText.rectTransform.anchorMin = new Vector2(0f, 1f);
            tooltipText.rectTransform.anchorMax = new Vector2(0f, 1f);
            tooltipText.rectTransform.pivot = new Vector2(0f, 1f);
            tooltipText.rectTransform.anchoredPosition = new Vector2(TooltipPadding, -TooltipPadding);

            tooltip.gameObject.SetActive(false);
        }

        internal void ShowTooltip(RectTransform anchor, string name, string description)
        {
            BuildTooltip();

            if (tooltip == null)
                return;

            float innerWidth = TooltipWidth - TooltipPadding * 2f;

            tooltipText.text = "<b><color=#FFDB73>" + name + "</color></b>\n" + description;
            tooltipText.supportRichText = true;
            tooltipText.rectTransform.sizeDelta = new Vector2(innerWidth, 10f);

            float textHeight = Mathf.Ceil(tooltipText.preferredHeight);

            tooltipText.rectTransform.sizeDelta = new Vector2(innerWidth, textHeight);
            tooltip.sizeDelta = new Vector2(TooltipWidth, textHeight + TooltipPadding * 2f);

            // วางข้างขวาของแผง ระดับเดียวกับคำที่ชี้ (พิกัดโลก -> พิกัดของ tooltipParent)
            Vector3[] panel = new Vector3[4];
            Vector3[] word = new Vector3[4];

            container.GetWorldCorners(panel);
            anchor.GetWorldCorners(word);

            Vector3 panelRight = tooltipParent.InverseTransformPoint(panel[2]);
            Vector3 wordTop = tooltipParent.InverseTransformPoint(word[1]);

            Rect bounds = tooltipParent.rect;

            float x = panelRight.x + 12f;
            float y = wordTop.y;

            x = Mathf.Clamp(x, bounds.xMin + 4f, bounds.xMax - TooltipWidth - 4f);
            y = Mathf.Clamp(y, bounds.yMin + tooltip.sizeDelta.y + 4f, bounds.yMax - 4f);

            // พิกัดโลกของ parent อิงจุด pivot; anchor ของกรอบอยู่กลาง parent จึงลบจุดกลางออก
            tooltip.anchoredPosition = new Vector2(x, y) - bounds.center;

            tooltip.SetAsLastSibling();
            tooltip.gameObject.SetActive(true);
        }

        internal void HideTooltip()
        {
            if (tooltip != null)
                tooltip.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (tooltip != null)
                Destroy(tooltip.gameObject);
        }

        /// <summary>ตัวรับเมาส์ของคำที่เป็นศัพท์ (ชี้ = แสดง, ออก = ซ่อน, คลิก = สลับ สำหรับจอสัมผัส)</summary>
        private class TermHover : MonoBehaviour,
            IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
        {
            public SkillTextView Owner;
            public string Name;
            public string Description;

            private bool shown;

            public void OnPointerEnter(PointerEventData eventData)
            {
                Show();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                shown = false;

                if (Owner != null)
                    Owner.HideTooltip();
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (shown)
                {
                    shown = false;

                    if (Owner != null)
                        Owner.HideTooltip();
                }
                else
                {
                    Show();
                }
            }

            private void Show()
            {
                shown = true;

                if (Owner != null)
                    Owner.ShowTooltip((RectTransform)transform, Name, Description);
            }
        }
    }
}
