using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Screens
{
    public class MainMenuScreen : MonoBehaviour
    {
        private Text message;
        private Button resetButton;
        private bool resetArmed;

        private void Start()
        {
            RectTransform root = UIFactory.CreateScreenRoot("MainMenuCanvas");

            UIFactory.CreateTitle(root, "MULTIVERSE CARD BATTLE");

            ScreenKit.AddLabel(
                root, "Heroes from every universe, one battlefield", 28,
                TextAnchor.MiddleCenter, 0.2f, 0.82f, 0.8f, 0.9f);

            float y = 0.70f;
            const float height = 0.08f;
            const float step = 0.10f;

            ScreenKit.AddButton(root, "Campaign",
                () => SceneLoader.Go(GameScene.Campaign),
                UIFactory.Accent, 0.35f, y, 0.65f, y + height);

            y -= step;

            ScreenKit.AddButton(root, "Quick Battle",
                StartQuickBattle,
                UIFactory.Success, 0.35f, y, 0.65f, y + height);

            y -= step;

            ScreenKit.AddButton(root, "Deck Builder",
                () => SceneLoader.Go(GameScene.DeckBuilder),
                UIFactory.Accent, 0.35f, y, 0.65f, y + height);

            y -= step;

            ScreenKit.AddButton(root, "Collection",
                () => SceneLoader.Go(GameScene.Collection),
                UIFactory.Accent, 0.35f, y, 0.65f, y + height);

            y -= step;

            resetButton = ScreenKit.AddButton(root, "Reset Progress",
                OnResetClicked,
                UIFactory.Muted, 0.35f, y, 0.65f, y + height, 24);

            y -= step;

#if !UNITY_WEBGL
            ScreenKit.AddButton(root, "Quit",
                Application.Quit,
                UIFactory.Danger, 0.35f, y, 0.65f, y + height);
#endif

            message = ScreenKit.AddLabel(
                root, "", 26, TextAnchor.MiddleCenter,
                0.1f, 0.02f, 0.9f, 0.1f);
        }

        private void StartQuickBattle()
        {
            string error = CampaignService.StartQuickBattle();

            if (error != null)
                message.text = "<color=#FF8080>" + error + "</color>";
        }

        // กดสองครั้งเพื่อยืนยัน กันกดผิด
        private void OnResetClicked()
        {
            if (!resetArmed)
            {
                resetArmed = true;

                UIFactory.SetButtonLabel(resetButton, "Press again to confirm");

                return;
            }

            SaveManager.ResetProgress();

            resetArmed = false;

            UIFactory.SetButtonLabel(resetButton, "Reset Progress");

            message.text = "Progress reset. Starter collection restored.";
        }
    }
}
