using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ฉากบทสนทนาเต็มจอของ Story Mode: พิมพ์ข้อความทีละตัว คลิกเพื่อไปต่อ ปุ่ม Skip ข้ามทั้งหมด
    /// </summary>
    public class StoryDialogue : MonoBehaviour
    {
        private const float CharsPerSecond = 70f;

        private IList<DialogueLine> lines;
        private Action onDone;

        private int index;
        private bool typing;
        private bool finished;

        private Text speakerText;
        private Text bodyText;
        private Text hintText;

        private Coroutine routine;

        public static StoryDialogue Play(
            Transform parent, IList<DialogueLine> lines, Action onDone)
        {
            if (lines == null || lines.Count == 0)
            {
                if (onDone != null)
                    onDone();

                return null;
            }

            Image overlay = UIFactory.CreatePanel(
                parent, "StoryDialogue", new Color(0f, 0f, 0f, 0.88f));

            UIFactory.Stretch(overlay.rectTransform);

            StoryDialogue dialogue = overlay.gameObject.AddComponent<StoryDialogue>();

            dialogue.lines = lines;
            dialogue.onDone = onDone;

            dialogue.Build(overlay);
            dialogue.Show(0);

            return dialogue;
        }

        private void Build(Image overlay)
        {
            Button click = overlay.gameObject.AddComponent<Button>();

            click.targetGraphic = overlay;
            click.transition = Selectable.Transition.None;
            click.onClick.AddListener(Advance);

            Image box = ScreenKit.AddPanel(
                overlay.transform, "Box", UIFactory.Panel,
                0.08f, 0.07f, 0.92f, 0.40f);

            box.raycastTarget = false;

            speakerText = ScreenKit.AddLabel(
                box.transform, "", 34, TextAnchor.MiddleLeft,
                0.03f, 0.78f, 0.7f, 0.97f, FontStyle.Bold);

            bodyText = ScreenKit.AddLabel(
                box.transform, "", 32, TextAnchor.UpperLeft,
                0.03f, 0.16f, 0.97f, 0.78f);

            hintText = ScreenKit.AddLabel(
                box.transform, "Click to continue", 20, TextAnchor.MiddleRight,
                0.6f, 0.02f, 0.97f, 0.15f);

            hintText.color = new Color(1f, 1f, 1f, 0.5f);

            speakerText.raycastTarget = false;
            bodyText.raycastTarget = false;
            hintText.raycastTarget = false;

            ScreenKit.AddButton(
                overlay.transform, "Skip", Finish, UIFactory.Muted,
                0.86f, 0.9f, 0.98f, 0.98f, 24);
        }

        private void Show(int lineIndex)
        {
            index = lineIndex;

            DialogueLine line = lines[index];

            bool narrator = string.IsNullOrEmpty(line.Speaker);

            speakerText.text = narrator ? "" : line.Speaker;
            speakerText.color = SpeakerColor(line.Speaker);

            bodyText.fontStyle = narrator ? FontStyle.Italic : FontStyle.Normal;
            bodyText.color = narrator
                ? new Color(0.8f, 0.8f, 0.85f, 1f)
                : UIFactory.TextColor;

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(Type(line.Text));
        }

        private IEnumerator Type(string text)
        {
            typing = true;

            bodyText.text = "";

            float shown = 0f;

            while (shown < text.Length)
            {
                shown += CharsPerSecond * Time.unscaledDeltaTime;

                int count = Mathf.Min(text.Length, Mathf.FloorToInt(shown));

                bodyText.text = text.Substring(0, count);

                yield return null;
            }

            bodyText.text = text;

            typing = false;
        }

        private void Advance()
        {
            if (finished)
                return;

            // คลิกระหว่างพิมพ์ = แสดงทั้งบรรทัดทันที
            if (typing)
            {
                if (routine != null)
                    StopCoroutine(routine);

                bodyText.text = lines[index].Text;

                typing = false;

                return;
            }

            if (index + 1 >= lines.Count)
            {
                Finish();

                return;
            }

            Show(index + 1);
        }

        private void Finish()
        {
            if (finished)
                return;

            finished = true;

            Action done = onDone;

            Destroy(gameObject);

            if (done != null)
                done();
        }

        private static Color SpeakerColor(string speaker)
        {
            if (string.IsNullOrEmpty(speaker))
                return Color.white;

            if (speaker == StoryLibrary.Guide)
                return new Color(0.95f, 0.8f, 0.4f, 1f);

            if (speaker == StoryLibrary.Hero)
                return new Color(0.55f, 0.8f, 1f, 1f);

            return new Color(1f, 0.55f, 0.5f, 1f);
        }
    }
}
