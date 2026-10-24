using UnityEngine;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ตัวเลขที่เด้งขยายแล้วลอยขึ้นและจางหายไป (ดาเมจ / การรักษา)
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        private const float Lifetime = 1.2f;
        private const float RiseDistance = 110f;
        private const float PopTime = 0.18f;
        private const float PopScale = 1.7f;
        private const float FadeStart = 0.55f;

        private Text text;
        private float age;
        private Vector3 origin;
        private float drift;

        public static void Spawn(
            Transform parent,
            Vector3 worldPosition,
            string content,
            Color color)
        {
            Text label = UIFactory.CreateText(
                parent, content, 48, color,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            label.rectTransform.sizeDelta = new Vector2(200f, 80f);
            label.rectTransform.position = worldPosition;

            label.fontSize = Mathf.RoundToInt(label.fontSize * 1.25f);

            Outline outline = label.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);

            Shadow shadow = label.gameObject.AddComponent<Shadow>();

            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -5f);

            FloatingText floating = label.gameObject.AddComponent<FloatingText>();

            floating.text = label;
            floating.origin = worldPosition;
            floating.drift = Random.Range(-30f, 30f);

            label.transform.localScale = Vector3.one * PopScale;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(age / Lifetime);

            // เด้งใหญ่แล้วหดกลับขนาดจริง จากนั้นลอยขึ้นแบบชะลอ เอียงไปข้างเล็กน้อย
            float pop = Mathf.Clamp01(age / PopTime);
            float scale = Mathf.Lerp(PopScale, 1f, 1f - (1f - pop) * (1f - pop));

            transform.localScale = Vector3.one * scale;

            float rise = 1f - (1f - t) * (1f - t);

            transform.position = origin + new Vector3(drift * rise, RiseDistance * rise, 0f);

            if (text != null)
            {
                Color color = text.color;

                color.a = Mathf.Clamp01(1f - Mathf.InverseLerp(FadeStart, 1f, t));

                text.color = color;
            }

            if (age >= Lifetime)
                Destroy(gameObject);
        }
    }
}
