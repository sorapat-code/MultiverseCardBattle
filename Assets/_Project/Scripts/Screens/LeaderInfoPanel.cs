using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// หน้าต่างอ่านความสามารถของ Leader (ทั้งของผู้เล่นและบอสศัตรู) ระหว่างต่อสู้
    /// แสดง: ชื่อ / คำบรรยาย / สกิลและต้นทุน / เงื่อนไขมิเตอร์ Power (พร้อมค่าปัจจุบัน) / การ์ดไม้ตาย
    /// เปิดจากปุ่ม "i" ข้างรูป Leader และเปิดเองตอนเริ่มด่านบอส
    /// ระหว่างเปิด BattleScreen หยุด AI / อินพุตทั้งหมด (เหมือนหน้าตั้งค่า)
    /// </summary>
    public class LeaderInfoPanel : MonoBehaviour
    {
        private RectTransform backdrop;
        private Text header;
        private Text body;
        private Text closeLabel;
        private Image accent;

        public bool IsOpen
        {
            get { return backdrop != null && backdrop.gameObject.activeSelf; }
        }

        public static LeaderInfoPanel Create(Transform parent)
        {
            RectTransform root = UIFactory.CreateRect("LeaderInfo", parent);

            UIFactory.Stretch(root);

            LeaderInfoPanel panel = root.gameObject.AddComponent<LeaderInfoPanel>();

            panel.backdrop = root;

            panel.Build();

            root.gameObject.SetActive(false);

            return panel;
        }

        private void Build()
        {
            Image dim = backdrop.gameObject.AddComponent<Image>();

            dim.color = new Color(0f, 0f, 0f, 0.75f);

            Button dimButton = backdrop.gameObject.AddComponent<Button>();

            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Close);

            Image card = ScreenKit.AddPanel(
                backdrop, "Card", new Color(0.06f, 0.045f, 0.04f, 0.98f),
                0.25f, 0.1f, 0.75f, 0.92f);

            Button keep = card.gameObject.AddComponent<Button>();

            keep.transition = Selectable.Transition.None;

            RectTransform c = card.rectTransform;

            // แถบสีธีมของ Leader ด้านบน
            accent = UIFactory.CreatePanel(c, "Accent", UIFactory.Gold);
            accent.raycastTarget = false;

            UIFactory.SetAnchors(accent.rectTransform, 0.03f, 0.935f, 0.97f, 0.945f);

            header = ScreenKit.AddLabel(
                c, "", 34, TextAnchor.MiddleLeft, 0.05f, 0.82f, 0.95f, 0.93f);

            header.supportRichText = true;
            header.resizeTextForBestFit = true;
            header.resizeTextMinSize = 16;
            header.resizeTextMaxSize = 34;

            body = ScreenKit.AddLabel(
                c, "", 24, TextAnchor.UpperLeft, 0.05f, 0.2f, 0.95f, 0.82f);

            body.supportRichText = true;
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 12;
            body.resizeTextMaxSize = 24;

            Button close = ScreenKit.AddButton(
                c, "Got it", Close, UIFactory.Accent, 0.32f, 0.04f, 0.68f, 0.16f, 28);

            closeLabel = close.GetComponentInChildren<Text>();
        }

        /// <summary>
        /// เปิดหน้าต่าง power = ค่ามิเตอร์ปัจจุบัน finisherGiven = ได้การ์ดไม้ตายแล้วหรือยัง
        /// intro = true ใช้ตอนเริ่มด่านบอส (หัวข้อ "BOSS BATTLE")
        /// </summary>
        public void Open(
            LeaderData leader, int power, bool finisherGiven, bool isEnemy, bool intro)
        {
            if (leader == null)
                return;

            Color theme = leader.Theme;

            accent.color = theme;

            string themeHex = ColorUtility.ToHtmlStringRGB(theme);

            string tag = intro ? "BOSS BATTLE" : (isEnemy ? "ENEMY LEADER" : "YOUR LEADER");

            header.text =
                "<size=20><color=#" + themeHex + ">" + tag + "</color></size>\n" +
                "<b>" + leader.Name + "</b>";

            string text = "";

            if (!string.IsNullOrEmpty(leader.Title))
                text += "<i>" + leader.Title + "</i>\n";

            if (!string.IsNullOrEmpty(leader.Flavor))
                text += leader.Flavor + "\n";

            text += "\n<b><color=#FFB040>Skill - " + leader.SkillName + "</color></b>  (" +
                    leader.SkillCostText + ", once per turn)\n" + leader.SkillText + "\n";

            string powerLine = finisherGiven
                ? "Finisher already in hand"
                : leader.PowerName + " " + power + " / " + leader.PowerMax;

            text += "\n<b><color=#FFB040>Finisher - " + leader.PowerName + "</color></b>  (" +
                    powerLine + ")\n" + leader.PowerText + "\n";

            CardData finisher = StarterCardLibrary.Get(leader.FinisherId);

            if (finisher != null)
            {
                text += "\n<b>" + finisher.CardName + "</b> (cost " + finisher.Cost + ")\n" +
                        CardTextGenerator.Describe(finisher);
            }

            if (isEnemy)
                text += "\n\n<color=#9AA3B5>You cannot see the enemy hand. " +
                        "The game announces when the finisher is ready.</color>";

            body.text = text;

            closeLabel.text = intro ? "Begin the battle" : "Got it";

            backdrop.gameObject.SetActive(true);
            backdrop.SetAsLastSibling();
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            backdrop.gameObject.SetActive(false);
        }
    }
}
