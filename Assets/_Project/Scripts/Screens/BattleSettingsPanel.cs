using System;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// หน้าตั้งค่าระหว่างเล่น (เปิดจากปุ่ม Settings บนสนาม หรือกด Esc ตอนไม่มีอะไรให้ยกเลิก)
    /// มีเสียง (Master / Music / Effects / Mute), ความเร็วแอนิเมชัน, ความเร็วคู่ต่อสู้ และ
    /// ปุ่มยอมแพ้ (Surrender) ที่ต้องกดยืนยันอีกครั้ง
    /// ระหว่างเปิด BattleScreen หยุด AI / อินพุตทั้งหมด
    /// </summary>
    public class BattleSettingsPanel : MonoBehaviour
    {
        private RectTransform backdrop;
        private RectTransform confirmRoot;

        private Button muteButton;
        private Button animButton;
        private Button aiButton;

        private Action onSurrender;

        public bool IsOpen
        {
            get { return backdrop != null && backdrop.gameObject.activeSelf; }
        }

        // ---------- สร้าง ----------

        public static BattleSettingsPanel Create(Transform parent, Action onSurrender)
        {
            RectTransform root = UIFactory.CreateRect("BattleSettings", parent);

            UIFactory.Stretch(root);

            BattleSettingsPanel panel = root.gameObject.AddComponent<BattleSettingsPanel>();

            panel.backdrop = root;
            panel.onSurrender = onSurrender;

            panel.Build();

            root.gameObject.SetActive(false);

            return panel;
        }

        private void Build()
        {
            // พื้นหลังมืด + กันคลิกทะลุลงสนาม (คลิกพื้นที่ว่างเพื่อปิด)
            Image dim = backdrop.gameObject.AddComponent<Image>();

            dim.color = new Color(0f, 0f, 0f, 0.7f);

            Button dimButton = backdrop.gameObject.AddComponent<Button>();

            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Close);

            Image card = ScreenKit.AddPanel(
                backdrop, "Card", new Color(0.07f, 0.05f, 0.04f, 0.97f),
                0.27f, 0.12f, 0.73f, 0.9f);

            // คลิกในกรอบไม่ปิด
            Button keep = card.gameObject.AddComponent<Button>();

            keep.transition = Selectable.Transition.None;

            RectTransform c = card.rectTransform;

            Text title = ScreenKit.AddLabel(
                c, "SETTINGS", 46, TextAnchor.MiddleCenter, 0.05f, 0.9f, 0.95f, 0.99f, FontStyle.Bold);

            title.color = UIFactory.Gold;

            UIFactory.CreateDivider(c, 0.06f, 0.895f, 0.94f, 0.9f);

            AddVolumeRow(c, "Master", 0.78f, GameSettings.Master, v => GameSettings.Master = v);
            AddVolumeRow(c, "Music", 0.68f, GameSettings.Music, v => GameSettings.Music = v);
            AddVolumeRow(c, "Effects", 0.58f, GameSettings.Sfx, v => GameSettings.Sfx = v);

            muteButton = ScreenKit.AddButton(c, "", ToggleMute, UIFactory.Muted, 0.06f, 0.47f, 0.94f, 0.55f, 24);

            animButton = ScreenKit.AddButton(
                c, "",
                () =>
                {
                    float[] options = GameSettings.AnimOptions;
                    int index = NearestIndex(options, GameSettings.AnimSpeed);

                    GameSettings.AnimSpeed = options[(index + 1) % options.Length];

                    Apply();
                },
                UIFactory.Muted, 0.06f, 0.36f, 0.94f, 0.44f, 24);

            aiButton = ScreenKit.AddButton(
                c, "",
                () =>
                {
                    GameSettings.AiSpeed = (GameSettings.AiSpeed + 1) % GameSettings.AiSpeedNames.Length;

                    Apply();
                },
                UIFactory.Muted, 0.06f, 0.25f, 0.94f, 0.33f, 24);

            UIFactory.CreateDivider(c, 0.06f, 0.205f, 0.94f, 0.21f);

            ScreenKit.AddButton(c, "Resume", Close, UIFactory.Accent, 0.06f, 0.05f, 0.48f, 0.17f, 28);

            ScreenKit.AddButton(
                c, "Surrender", () => confirmRoot.gameObject.SetActive(true),
                UIFactory.Danger, 0.52f, 0.05f, 0.94f, 0.17f, 28);

            BuildConfirm(c);

            RefreshLabels();
        }

        // ยืนยันการยอมแพ้ (คลุมกรอบตั้งค่า)
        private void BuildConfirm(RectTransform parent)
        {
            Image cover = ScreenKit.AddPanel(
                parent, "Confirm", new Color(0.05f, 0.03f, 0.03f, 0.98f), 0.04f, 0.25f, 0.96f, 0.75f);

            confirmRoot = cover.rectTransform;

            Button block = cover.gameObject.AddComponent<Button>();

            block.transition = Selectable.Transition.None;

            Text question = ScreenKit.AddLabel(
                confirmRoot, "Surrender this match?", 40, TextAnchor.MiddleCenter,
                0.05f, 0.55f, 0.95f, 0.92f, FontStyle.Bold);

            question.color = new Color(1f, 0.55f, 0.5f, 1f);

            ScreenKit.AddLabel(
                confirmRoot, "It counts as a defeat.", 26, TextAnchor.MiddleCenter,
                0.05f, 0.4f, 0.95f, 0.58f);

            ScreenKit.AddButton(
                confirmRoot, "Cancel", () => confirmRoot.gameObject.SetActive(false),
                UIFactory.Muted, 0.06f, 0.08f, 0.48f, 0.3f, 28);

            ScreenKit.AddButton(
                confirmRoot, "Yes, surrender",
                () =>
                {
                    confirmRoot.gameObject.SetActive(false);

                    Close();

                    if (onSurrender != null)
                        onSurrender();
                },
                UIFactory.Danger, 0.52f, 0.08f, 0.94f, 0.3f, 28);

            confirmRoot.gameObject.SetActive(false);
        }

        private void AddVolumeRow(
            RectTransform parent, string label, float y, float value, Action<float> setter)
        {
            ScreenKit.AddLabel(parent, label, 26, TextAnchor.MiddleLeft, 0.06f, y, 0.26f, y + 0.08f);

            Text valueText = ScreenKit.AddLabel(
                parent, Mathf.RoundToInt(value * 100f) + "%", 26, TextAnchor.MiddleRight,
                0.8f, y, 0.94f, y + 0.08f);

            Slider slider = MainMenuScreen.CreateSlider(parent, 0.28f, y, 0.78f, y + 0.08f);

            slider.SetValueWithoutNotify(value);

            slider.onValueChanged.AddListener(v =>
            {
                setter(v);

                valueText.text = Mathf.RoundToInt(v * 100f) + "%";

                Sfx.RefreshVolumes();
                Sfx.RefreshMusicMute();
            });

            slider.gameObject.AddComponent<SliderReleaseHook>().OnRelease = () =>
            {
                SaveManager.Save();

                Sfx.Play(SoundId.Click);
            };
        }

        // ---------- ควบคุม ----------

        public void Open()
        {
            if (IsOpen)
                return;

            confirmRoot.gameObject.SetActive(false);

            backdrop.gameObject.SetActive(true);
            backdrop.SetAsLastSibling();

            RefreshLabels();
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            SaveManager.Save();

            backdrop.gameObject.SetActive(false);
        }

        private void ToggleMute()
        {
            Sfx.Muted = !Sfx.Muted;

            SaveManager.Save();

            Sfx.RefreshMusicMute();

            RefreshLabels();

            Sfx.Play(SoundId.Click);
        }

        private void Apply()
        {
            GameSettings.Apply();

            SaveManager.Save();

            RefreshLabels();
        }

        private void RefreshLabels()
        {
            UIFactory.SetButtonLabel(muteButton, Sfx.Muted ? "Mute All: On" : "Mute All: Off");

            UIFactory.SetButtonLabel(
                animButton, "Animation Speed: " + GameSettings.AnimSpeed.ToString("0.#") + "x");

            UIFactory.SetButtonLabel(
                aiButton, "Opponent Speed: " + GameSettings.AiSpeedNames[GameSettings.AiSpeed]);
        }

        private static int NearestIndex(float[] options, float value)
        {
            int best = 0;

            for (int i = 1; i < options.Length; i++)
            {
                if (Mathf.Abs(options[i] - value) < Mathf.Abs(options[best] - value))
                    best = i;
            }

            return best;
        }
    }
}
