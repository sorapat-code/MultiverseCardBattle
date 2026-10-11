using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Screens
{
    public class CampaignScreen : MonoBehaviour
    {
        private const float RowHeight = 150f;

        private Text message;

        private RectTransform root;
        private RectTransform listContent;

        private void Start()
        {
            root = UIFactory.CreateScreenRoot("CampaignCanvas");

            UIFactory.CreateTitle(root, "STORY MODE");

            ScreenKit.AddScroll(
                root, "StageList", 0.1f, 0.14f, 0.9f, 0.88f, out listContent);

            UIFactory.AddVertical(listContent, 10f, 12);

            message = ScreenKit.AddLabel(
                root, "", 26, TextAnchor.MiddleCenter,
                0.25f, 0.02f, 0.9f, 0.12f);

            ScreenKit.AddButton(root, "Back",
                () => SceneLoader.Go(GameScene.MainMenu),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);

            RebuildList();

            PlayPendingStory();
        }

        // ฉากหลังชนะด่าน / จบบท ที่รอเล่นหลังกลับจากการต่อสู้
        private void PlayPendingStory()
        {
            string stageId = StoryService.PendingOutroStageId;
            int epilogue = StoryService.PendingEpilogueChapter;

            StoryService.ClearPending();

            if (string.IsNullOrEmpty(stageId))
                return;

            StoryBeat beat = StoryLibrary.GetBeat(stageId);

            StoryChapter chapter =
                epilogue > 0 ? StoryLibrary.GetChapter(epilogue) : null;

            System.Action afterOutro = null;

            if (chapter != null)
            {
                afterOutro = () => StoryDialogue.Play(root, chapter.Epilogue, null);
            }

            if (beat != null && beat.Outro.Count > 0)
                StoryDialogue.Play(root, beat.Outro, afterOutro);
            else if (afterOutro != null)
                afterOutro();
        }

        private void RebuildList()
        {
            ScreenKit.ClearNow(listContent);

            PlayerProfile profile = SaveManager.Profile;

            foreach (StoryChapter chapter in StoryLibrary.Chapters)
            {
                BuildChapterHeader(listContent, profile, chapter);

                foreach (string stageId in chapter.StageIds)
                {
                    CampaignStage stage = CampaignLibrary.Get(stageId);

                    if (stage != null)
                        BuildRow(listContent, stage);
                }
            }
        }

        private void BuildChapterHeader(
            RectTransform content, PlayerProfile profile, StoryChapter chapter)
        {
            int cleared = StoryService.ClearedStagesInChapter(profile, chapter);
            bool complete = StoryService.IsChapterComplete(profile, chapter);

            Image header = UIFactory.CreateFramedPanel(
                content, "Chapter_" + chapter.Number,
                new Color(0.25f, 0.17f, 0.08f, 0.97f));

            header.rectTransform.sizeDelta = new Vector2(0f, 120f);

            string status = complete
                ? "<color=#70E080>COMPLETE</color>"
                : "<color=#FFE070>" + cleared + " / " + chapter.StageIds.Count + "</color>";

            Text label = UIFactory.CreateText(
                header.transform,
                "<b>CHAPTER " + chapter.Number + "  -  " + chapter.Title +
                "</b>   " + status +
                "\n" + chapter.Synopsis +
                "\nChapter reward: " + chapter.RewardGold + " Gold, " +
                chapter.RewardDust + " Dust" +
                (StoryService.IsChapterRewarded(profile, chapter) ? "  (claimed)" : ""),
                24, UIFactory.TextColor, TextAnchor.MiddleLeft);

            UIFactory.SetAnchors(label.rectTransform, 0.02f, 0.05f, 0.98f, 0.95f);
        }

        private void BuildRow(RectTransform content, CampaignStage stage)
        {
            PlayerProfile profile = SaveManager.Profile;

            bool unlocked = CampaignService.IsUnlocked(stage);
            bool cleared = profile.IsStageCompleted(stage.Id);

            Image row = UIFactory.CreateFramedPanel(
                content, "Stage_" + stage.Id,
                unlocked ? UIFactory.Panel : new Color(0.1f, 0.08f, 0.07f, 0.9f));

            row.rectTransform.sizeDelta = new Vector2(0f, RowHeight);

            string status = cleared
                ? "<color=#70E080>CLEARED</color>"
                : (unlocked
                    ? "<color=#FFE070>AVAILABLE</color>"
                    : "<color=#A0A0A0>LOCKED</color>");

            StringBuilder text = new StringBuilder();

            text.Append("<b>").Append(stage.Name).Append("</b>   ").Append(status);
            text.Append('\n').Append(stage.Description);
            text.Append("\nEnemy: ").Append(stage.EnemyName)
                .Append("  (HP ").Append(stage.EnemyHealth).Append(")");

            if (!cleared && stage.RewardCardIds != null &&
                stage.RewardCardIds.Count > 0)
            {
                text.Append("\nReward: ").Append(RewardNames(stage));
            }

            if (!unlocked)
            {
                CampaignStage required =
                    CampaignLibrary.Get(stage.RequiresStageId);

                text.Append("\nClear \"")
                    .Append(required != null ? required.Name : stage.RequiresStageId)
                    .Append("\" first");
            }

            // รูปศัตรู: ใช้ Resources/UI/portrait_<ชื่อ> ถ้ามี ไม่งั้นตัวอักษรย่อ
            Image ring = UIFactory.CreatePanel(row.transform, "PortraitRing", UIFactory.Gold);

            UIShapes.MakeCircle(ring);

            ring.raycastTarget = false;

            UIFactory.SetAnchors(ring.rectTransform, 0.015f, 0.12f, 0.015f, 0.12f);

            ring.rectTransform.anchorMin = new Vector2(0.012f, 0.5f);
            ring.rectTransform.anchorMax = new Vector2(0.012f, 0.5f);
            ring.rectTransform.pivot = new Vector2(0f, 0.5f);
            ring.rectTransform.anchoredPosition = Vector2.zero;
            ring.rectTransform.sizeDelta = new Vector2(96f, 96f);

            Image face = UIFactory.CreatePanel(
                ring.transform, "Portrait", new Color(0.1f, 0.07f, 0.05f, 1f));

            UIShapes.MakeCircle(face);

            face.raycastTarget = false;

            UIFactory.Stretch(face.rectTransform);

            face.rectTransform.offsetMin = new Vector2(4f, 4f);
            face.rectTransform.offsetMax = new Vector2(-4f, -4f);

            face.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            Sprite art = UIArt.Get(UIArt.PortraitName(stage.EnemyName)) ??
                UIArt.Get("portrait_enemy");

            if (art != null)
            {
                Image art2 = UIFactory.CreatePanel(face.transform, "Art", Color.white);

                art2.sprite = art;
                art2.raycastTarget = false;

                UIFactory.Stretch(art2.rectTransform);
            }
            else
            {
                Text initial = UIFactory.CreateText(
                    face.transform,
                    string.IsNullOrEmpty(stage.EnemyName)
                        ? "?" : stage.EnemyName.Substring(0, 1).ToUpperInvariant(),
                    48, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);

                UIFactory.Stretch(initial.rectTransform);
            }

            Text label = UIFactory.CreateText(
                row.transform, text.ToString(), 24, UIFactory.TextColor,
                TextAnchor.MiddleLeft);

            UIFactory.SetAnchors(label.rectTransform, 0.105f, 0.05f, 0.78f, 0.95f);

            Button play = ScreenKit.AddButton(
                row.transform,
                cleared ? "Replay" : "Play",
                () => OnPlay(stage),
                unlocked ? UIFactory.Success : UIFactory.Muted,
                0.80f, cleared ? 0.1f : 0.25f, 0.97f, cleared ? 0.47f : 0.75f);

            play.interactable = unlocked;

            // ด่านที่ผ่านแล้วอ่านเนื้อเรื่องซ้ำได้
            if (cleared)
            {
                ScreenKit.AddButton(
                    row.transform, "Story",
                    () => OnRecap(stage),
                    UIFactory.Accent,
                    0.80f, 0.53f, 0.97f, 0.9f, 24);
            }
        }

        private static string RewardNames(CampaignStage stage)
        {
            StringBuilder names = new StringBuilder();

            foreach (string id in stage.RewardCardIds)
            {
                CardData card = CardRegistry.Get(id);

                if (card == null)
                    continue;

                if (names.Length > 0)
                    names.Append(", ");

                names.Append(card.CardName);
            }

            return names.ToString();
        }

        private void OnPlay(CampaignStage stage)
        {
            if (!CampaignService.IsUnlocked(stage))
            {
                message.text = "<color=#FF8080>This stage is locked</color>";

                return;
            }

            StoryBeat beat = StoryLibrary.GetBeat(stage.Id);

            bool firstTime = !SaveManager.Profile.IsStageCompleted(stage.Id);

            // ครั้งแรก: เล่นฉากก่อนสู้ก่อนเข้าด่าน
            if (firstTime && beat != null && beat.Intro.Count > 0)
            {
                StoryDialogue.Play(root, beat.Intro, () => StartStage(stage));

                return;
            }

            StartStage(stage);
        }

        private void StartStage(CampaignStage stage)
        {
            string error = CampaignService.StartStage(stage);

            if (error != null)
                message.text = "<color=#FF8080>" + error + "</color>";
        }

        // อ่านเนื้อเรื่องของด่านที่ผ่านแล้วอีกครั้ง (ก่อนสู้ + หลังชนะ)
        private void OnRecap(CampaignStage stage)
        {
            StoryBeat beat = StoryLibrary.GetBeat(stage.Id);

            if (beat == null)
                return;

            List<DialogueLine> all = new List<DialogueLine>(beat.Intro);

            all.AddRange(beat.Outro);

            StoryDialogue.Play(root, all, null);
        }
    }
}
