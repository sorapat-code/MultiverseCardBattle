using UnityEngine;
using UnityEngine.EventSystems;
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

        private const float LungeTime = 0.3f;
        private const float LungeDistance = 46f;

        private float lungeAge = -1f;
        private Vector2 lungeDir;
        private Vector3 lungeBase;

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

        /// <summary>พุ่งไปทางทิศที่กำหนดแล้วกลับที่ (ตอนยูนิตโจมตี)</summary>
        public static void Lunge(Component target, Vector2 direction)
        {
            if (target == null)
                return;

            CardFx fx = Get(target);

            if (fx.lungeAge >= 0f)
                return;

            fx.lungeDir = direction.normalized;
            fx.lungeBase = fx.transform.localPosition;
            fx.lungeAge = 0f;
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

                UIShapes.MakeRounded(image, 8f);

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

            if (lungeAge >= 0f)
            {
                lungeAge += dt;

                float t = Mathf.Clamp01(lungeAge / LungeTime);

                // พุ่งออกเร็ว (ครึ่งแรก) แล้วกลับช้ากว่า
                float k = t < 0.4f
                    ? t / 0.4f
                    : 1f - (t - 0.4f) / 0.6f;

                Vector2 offset = lungeDir * (LungeDistance * k * k);

                transform.localPosition = lungeBase + new Vector3(offset.x, offset.y, 0f);

                if (t >= 1f)
                {
                    lungeAge = -1f;
                    transform.localPosition = lungeBase;
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
    /// <summary>
    /// การ์ดในมือขยายขึ้นเล็กน้อยและลอยทับใบอื่นตอนเมาส์ชี้
    /// </summary>
    public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverScale = 1.14f;

        private float current = 1f;
        private bool hovering;
        private Canvas canvas;

        public static void Add(Component target)
        {
            if (target == null || target.GetComponent<CardHover>() != null)
                return;

            CardHover hover = target.gameObject.AddComponent<CardHover>();

            // Canvas ซ้อนไว้ล่วงหน้า (ปิดการจัดลำดับ) เพื่อให้ยกขึ้นทับใบอื่นได้โดยไม่ทำให้เมาส์กะพริบ
            hover.canvas = target.gameObject.AddComponent<Canvas>();
            hover.canvas.overrideSorting = false;

            target.gameObject.AddComponent<GraphicRaycaster>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;

            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 5;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
        }

        private void Update()
        {
            float target = hovering ? HoverScale : 1f;

            current = Mathf.Lerp(
                current, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));

            transform.localScale = Vector3.one * current;

            if (!hovering && Mathf.Abs(current - 1f) < 0.003f)
            {
                current = 1f;

                if (canvas != null)
                    canvas.overrideSorting = false;
            }
        }
    }
}
