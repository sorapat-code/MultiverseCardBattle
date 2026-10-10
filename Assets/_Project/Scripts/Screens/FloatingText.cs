using UnityEngine;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ตัวเลขที่ลอยขึ้นแล้วจางหายไป (ดาเมจ / การรักษา)
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        private const float Lifetime = 1.0f;
        private const float RiseSpeed = 90f;

        private Text text;
        private float age;

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

            Outline outline = label.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            FloatingText floating = label.gameObject.AddComponent<FloatingText>();

            floating.text = label;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            transform.position += Vector3.up * (RiseSpeed * Time.unscaledDeltaTime);

            if (text != null)
            {
                Color color = text.color;

                color.a = Mathf.Clamp01(1f - age / Lifetime);

                text.color = color;
            }

            if (age >= Lifetime)
                Destroy(gameObject);
        }
    }
}
