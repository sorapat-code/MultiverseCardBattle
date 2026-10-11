using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ไฮไลต์ปุ่มที่ถูกเลือกด้วยคีย์บอร์ด (ลูกศรเลื่อน, Enter/Space กด) ใช้กับแผง Dev
    /// </summary>
    public class ButtonSelectGlow : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private Outline outline;

        public static void Add(Button button)
        {
            if (button == null)
                return;

            ButtonSelectGlow glow = button.gameObject.AddComponent<ButtonSelectGlow>();

            glow.outline = button.gameObject.AddComponent<Outline>();

            glow.outline.effectColor = new Color(1f, 0.9f, 0.4f, 1f);
            glow.outline.effectDistance = new Vector2(5f, -5f);
            glow.outline.enabled = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (outline != null)
                outline.enabled = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (outline != null)
                outline.enabled = false;
        }
    }
}
