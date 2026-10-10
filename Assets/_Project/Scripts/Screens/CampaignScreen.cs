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

        private void Start()
        {
            RectTransform root = UIFactory.CreateScreenRoot("CampaignCanvas");

            UIFactory.CreateTitle(root, "CAMPAIGN");

            RectTransform content;

            ScreenKit.AddScroll(
                root, "StageList", 0.1f, 0.14f, 0.9f, 0.88f, out content);

            UIFactory.AddVertical(content, 10f, 12);

            foreach (CampaignStage stage in CampaignLibrary.Stages)
                BuildRow(content, stage);

            message = ScreenKit.AddLabel(
                root, "", 26, TextAnchor.MiddleCenter,
                0.25f, 0.02f, 0.9f, 0.12f);

            ScreenKit.AddButton(root, "Back",
                () => SceneLoader.Go(GameScene.MainMenu),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);
        }

        private void BuildRow(RectTransform content, CampaignStage stage)
        {
            PlayerProfile profile = SaveManager.Profile;

            bool unlocked = CampaignService.IsUnlocked(stage);
            bool cleared = profile.IsStageCompleted(stage.Id);

            Image row = UIFactory.CreatePanel(
                content, "Stage_" + stage.Id, UIFactory.Panel);

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

            Text label = UIFactory.CreateText(
                row.transform, text.ToString(), 24, UIFactory.TextColor,
                TextAnchor.MiddleLeft);

            UIFactory.SetAnchors(label.rectTransform, 0.02f, 0.05f, 0.76f, 0.95f);

            Button play = ScreenKit.AddButton(
                row.transform,
                cleared ? "Replay" : "Play",
                () => OnPlay(stage),
                unlocked ? UIFactory.Success : UIFactory.Muted,
                0.80f, 0.25f, 0.97f, 0.75f);

            play.interactable = unlocked;
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
            string error = CampaignService.StartStage(stage);

            if (error != null)
                message.text = "<color=#FF8080>" + error + "</color>";
        }
    }
}
