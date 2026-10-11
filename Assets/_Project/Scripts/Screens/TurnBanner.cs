using UnityEngine;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ป้ายสลับเทิร์นกลางจอ: แถบเรืองแสงกว้างเต็มจอ ตัวอักษรพุ่งเข้ามาแล้วค้างครู่หนึ่งก่อนจางหาย
    /// (ใช้เวลา unscaled ไม่บล็อกอินพุต ไม่หยุดเกม) ทำลายตัวเองเมื่อจบ
    /// </summary>
    public class TurnBanner : MonoBehaviour
    {
        private const float FadeIn = 0.22f;
        private const float Hold = 0.75f;
        private const float FadeOut = 0.35f;

        private RectTransform text;
        private CanvasGroup group;
        private float age;

        public static void Play(RectTransform parent, bool yourTurn)
        {
            if (parent == null)
                return;

            RectTransform root = UIFactory.CreateRect("TurnBanner", parent);

            UIFactory.SetAnchors(root, 0f, 0.43f, 1f, 0.57f);

            CanvasGroup canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Color color = yourTurn
                ? new Color(0.35f, 0.9f, 0.55f, 1f)
                : new Color(1f, 0.55f, 0.3f, 1f);

            // แถบสีดำโปร่งไล่จางที่ปลายซ้ายขวา + เส้นสีบนล่าง
            Image band = UIFactory.CreatePanel(root, "Band", new Color(0f, 0f, 0f, 0.7f));

            band.sprite = UIShapes.Divider;
            band.raycastTarget = false;

            UIFactory.Stretch(band.rectTransform);

            Image top = UIFactory.CreatePanel(root, "Top", color);

            top.sprite = UIShapes.Divider;
            top.raycastTarget = false;

            UIFactory.SetAnchors(top.rectTransform, 0.1f, 0.96f, 0.9f, 1f);

            Image bottom = UIFactory.CreatePanel(root, "Bottom", color);

            bottom.sprite = UIShapes.Divider;
            bottom.raycastTarget = false;

            UIFactory.SetAnchors(bottom.rectTransform, 0.1f, 0f, 0.9f, 0.04f);

            Text label = UIFactory.CreateText(
                root, yourTurn ? "YOUR TURN" : "OPPONENT'S TURN", 72, color,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            label.raycastTarget = false;

            UIFactory.Stretch(label.rectTransform);

            UIFactory.AddTextShadow(label);

            TurnBanner banner = root.gameObject.AddComponent<TurnBanner>();

            banner.group = canvasGroup;
            banner.text = label.rectTransform;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            float total = FadeIn + Hold + FadeOut;

            if (age >= total)
            {
                Destroy(gameObject);
                return;
            }

            if (age < FadeIn)
            {
                float t = age / FadeIn;
                float ease = 1f - (1f - t) * (1f - t);

                group.alpha = ease;

                // ตัวอักษรพุ่งเข้าจากใหญ่ไปปกติ
                text.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, ease);
            }
            else if (age < FadeIn + Hold)
            {
                group.alpha = 1f;
                text.localScale = Vector3.one;
            }
            else
            {
                float t = (age - FadeIn - Hold) / FadeOut;

                group.alpha = 1f - t;
                text.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, t);
            }
        }
    }
}
