using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ปุ่มขยายขึ้นเล็กน้อยและสว่างขึ้นเมื่อเอาเมาส์ชี้ หดกลับเมื่อกด ให้ปุ่มทุกปุ่มมีการตอบสนอง
    /// ใช้สเกลของ Transform จึงไม่กระทบ Layout ของปุ่มข้างเคียง
    /// </summary>
    public class ButtonHoverFx : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float HoverScale = 1.04f;
        private const float PressScale = 0.97f;
        private const float Speed = 14f;

        private Button button;
        private Shadow glow;
        private bool hovered;
        private bool pressed;

        /// <summary>ติดเอฟเฟกต์ชี้/กดให้ปุ่ม</summary>
        public static void Attach(Button button)
        {
            if (button == null)
                return;

            ButtonHoverFx fx = button.gameObject.AddComponent<ButtonHoverFx>();

            fx.button = button;

            // ขอบเรืองสีทองรอบปุ่ม เปิดเฉพาะตอนชี้
            fx.glow = button.gameObject.AddComponent<Outline>();
            fx.glow.effectColor = new Color(1f, 0.85f, 0.45f, 0.55f);
            fx.glow.effectDistance = new Vector2(2f, -2f);
            fx.glow.enabled = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = Interactable;

            if (glow != null)
                glow.enabled = hovered;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;

            if (glow != null)
                glow.enabled = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = Interactable;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        private bool Interactable
        {
            get { return button != null && button.interactable; }
        }

        private void OnDisable()
        {
            hovered = false;
            pressed = false;

            transform.localScale = Vector3.one;

            if (glow != null)
                glow.enabled = false;
        }

        private void Update()
        {
            float target = pressed ? PressScale : (hovered ? HoverScale : 1f);

            float current = transform.localScale.x;

            if (Mathf.Abs(current - target) < 0.001f)
                return;

            float next = Mathf.Lerp(current, target, 1f - Mathf.Exp(-Speed * Time.unscaledDeltaTime));

            transform.localScale = new Vector3(next, next, 1f);
        }
    }
}
