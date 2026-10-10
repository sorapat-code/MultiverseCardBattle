using UnityEngine;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// เอฟเฟกต์สั้นๆ บนการ์ด: เด้งเข้า (ลงสนาม) และวาบสีแดง/เขียว (โดนดาเมจ/ฮีล)
    /// ใช้เวลาแบบ unscaled เพื่อไม่ขึ้นกับ Time.timeScale
    /// </summary>
    public class CardFx : MonoBehaviour
    {
        private const float PopTime = 0.22f;
        private const float FlashTime = 0.35f;

        private float popAge = -1f;
        private float flashAge = -1f;
        private Image flash;
        private Color flashColor;

        public static void Pop(Component target)
        {
            if (target == null)
                return;

            CardFx fx = Get(target);

            fx.popAge = 0f;
            fx.transform.localScale = Vector3.one * 0.6f;
        }

        public static void Flash(Component target, Color color)
        {
            if (target == null)
                return;

            CardFx fx = Get(target);

            if (fx.flash == null)
            {
                Image image = UIFactory.CreatePanel(
                    fx.transform, "Flash", new Color(1f, 1f, 1f, 0f));

                UIFactory.Stretch(image.rectTransform);

                image.raycastTarget = false;

                fx.flash = image;
            }

            fx.flashColor = color;
            fx.flashAge = 0f;
            fx.flash.transform.SetAsLastSibling();
        }

        private static CardFx Get(Component target)
        {
            CardFx fx = target.GetComponent<CardFx>();

            return fx != null ? fx : target.gameObject.AddComponent<CardFx>();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (popAge >= 0f)
            {
                popAge += dt;

                float t = Mathf.Clamp01(popAge / PopTime);

                // เด้งเกินเล็กน้อยแล้วกลับที่
                float scale = 0.6f + 0.4f * t + 0.12f * Mathf.Sin(t * Mathf.PI);

                transform.localScale = Vector3.one * scale;

                if (t >= 1f)
                {
                    popAge = -1f;
                    transform.localScale = Vector3.one;
                }
            }

            if (flashAge >= 0f && flash != null)
            {
                flashAge += dt;

                float t = Mathf.Clamp01(flashAge / FlashTime);

                Color c = flashColor;

                c.a = flashColor.a * (1f - t);

                flash.color = c;

                if (t >= 1f)
                    flashAge = -1f;
            }
        }
    }
}
