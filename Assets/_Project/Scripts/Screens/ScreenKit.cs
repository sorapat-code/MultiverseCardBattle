using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ตัวช่วยวางปุ่ม/ข้อความแบบสัดส่วน (0-1) สำหรับหน้าจอที่สร้างด้วยโค้ด
    /// </summary>
    public static class ScreenKit
    {
        public static Button AddButton(
            Transform parent,
            string label,
            UnityAction onClick,
            Color color,
            float xMin, float yMin, float xMax, float yMax,
            int fontSize = 28)
        {
            Button button =
                UIFactory.CreateButton(parent, label, onClick, color, fontSize);

            UIFactory.SetAnchors(
                button.GetComponent<RectTransform>(),
                xMin, yMin, xMax, yMax);

            return button;
        }

        public static Text AddLabel(
            Transform parent,
            string content,
            int fontSize,
            TextAnchor anchor,
            float xMin, float yMin, float xMax, float yMax,
            FontStyle style = FontStyle.Normal)
        {
            Text text = UIFactory.CreateText(
                parent, content, fontSize, UIFactory.TextColor, anchor, style);

            UIFactory.SetAnchors(text.rectTransform, xMin, yMin, xMax, yMax);

            return text;
        }

        public static Image AddPanel(
            Transform parent,
            string name,
            Color color,
            float xMin, float yMin, float xMax, float yMax)
        {
            Image image = UIFactory.CreateFramedPanel(parent, name, color);

            UIFactory.SetAnchors(image.rectTransform, xMin, yMin, xMax, yMax);

            return image;
        }

        public static ScrollRect AddScroll(
            Transform parent,
            string name,
            float xMin, float yMin, float xMax, float yMax,
            out RectTransform content)
        {
            ScrollRect scroll =
                UIFactory.CreateScrollView(parent, name, out content);

            UIFactory.SetAnchors(
                scroll.GetComponent<RectTransform>(),
                xMin, yMin, xMax, yMax);

            return scroll;
        }

        /// <summary>
        /// ลบลูกทั้งหมดทันที (Destroy ปกติจะลบตอนจบเฟรม ทำให้ Layout คำนวณผิดในเฟรมเดียวกัน)
        /// </summary>
        public static void ClearNow(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);

                child.gameObject.SetActive(false);
                child.SetParent(null, false);

                Object.Destroy(child.gameObject);
            }
        }
    }
}
