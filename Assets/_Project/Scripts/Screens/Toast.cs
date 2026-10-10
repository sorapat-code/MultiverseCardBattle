using MultiverseCardBattle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ป้ายแจ้งเตือนสั้นๆ ที่ลอยลงมาจากขอบบน แล้วจางหายเอง (เช่น ปลดล็อก Achievement)
    /// </summary>
    public class Toast : MonoBehaviour
    {
        private const float Life = 4f;
        private const float SlideTime = 0.25f;
        private const float FadeTime = 0.6f;

        private static int active;

        private RectTransform rect;
        private CanvasGroup group;
        private float age;
        private float targetY;

        public static void Show(Transform parent, string title, string body)
        {
            if (parent == null)
                return;

            Image panel = UIFactory.CreateFramedPanel(
                parent, "Toast", new Color(0.12f, 0.08f, 0.06f, 0.96f), 14f);

            panel.raycastTarget = false;

            RectTransform rect = panel.rectTransform;

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(680f, 100f);

            Text titleText = UIFactory.CreateText(
                panel.transform, title, 28, UIFactory.Gold,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.SetAnchors(titleText.rectTransform, 0.03f, 0.5f, 0.97f, 0.96f);

            titleText.raycastTarget = false;

            UIFactory.AddTextShadow(titleText);

            Text bodyText = UIFactory.CreateText(
                panel.transform, body, 22, UIFactory.TextColor,
                TextAnchor.MiddleCenter);

            UIFactory.SetAnchors(bodyText.rectTransform, 0.03f, 0.05f, 0.97f, 0.52f);

            bodyText.raycastTarget = false;

            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();

            group.blocksRaycasts = false;
            group.interactable = false;

            Toast toast = panel.gameObject.AddComponent<Toast>();

            toast.rect = rect;
            toast.group = group;
            toast.targetY = -20f - active * 112f;

            active++;

            rect.anchoredPosition = new Vector2(0f, 120f);
            rect.SetAsLastSibling();

            Sfx.Play(SoundId.Win);
        }

        private void OnDestroy()
        {
            active = Mathf.Max(0, active - 1);
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            float slide = Mathf.Clamp01(age / SlideTime);

            slide = 1f - (1f - slide) * (1f - slide);

            rect.anchoredPosition = new Vector2(
                0f, Mathf.Lerp(120f, targetY, slide));

            float fade = Mathf.Clamp01((Life - age) / FadeTime);

            group.alpha = fade;

            if (age >= Life)
                Destroy(gameObject);
        }
    }
}
