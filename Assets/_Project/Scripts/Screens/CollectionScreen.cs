using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// แสดงการ์ดทั้งหมดในเกม การ์ดที่ยังไม่มีจะจางลง
    /// </summary>
    public class CollectionScreen : MonoBehaviour
    {
        private static readonly Vector2 CardSize = new Vector2(150f, 210f);

        private RectTransform grid;
        private Text summary;
        private Button filterButton;
        private bool ownedOnly;

        private void Start()
        {
            RectTransform root = UIFactory.CreateScreenRoot("CollectionCanvas");

            UIFactory.CreateTitle(root, "COLLECTION");

            ScreenKit.AddScroll(
                root, "CardGrid", 0.03f, 0.13f, 0.97f, 0.89f, out grid);

            UIFactory.AddGrid(grid, CardSize, 14f, 14);

            summary = ScreenKit.AddLabel(
                root, "", 26, TextAnchor.MiddleCenter,
                0.3f, 0.02f, 0.7f, 0.11f);

            ScreenKit.AddButton(root, "Back",
                () => SceneLoader.Go(GameScene.MainMenu),
                UIFactory.Muted, 0.03f, 0.03f, 0.2f, 0.11f);

            filterButton = ScreenKit.AddButton(root, "Show: All",
                ToggleFilter,
                UIFactory.Accent, 0.8f, 0.03f, 0.97f, 0.11f, 24);

            Rebuild();
        }

        private void ToggleFilter()
        {
            ownedOnly = !ownedOnly;

            UIFactory.SetButtonLabel(
                filterButton, ownedOnly ? "Show: Owned" : "Show: All");

            Rebuild();
        }

        private void Rebuild()
        {
            ScreenKit.ClearNow(grid);

            PlayerProfile profile = SaveManager.Profile;

            List<CardData> cards = new List<CardData>();

            foreach (CardData data in CardRegistry.All)
            {
                if (CardRegistry.IsCollectible(data))
                    cards.Add(data);
            }

            cards.Sort(CompareCards);

            int ownedKinds = 0;
            int totalCopies = 0;

            foreach (CardData data in cards)
            {
                int owned = profile.GetOwnedCount(data.CardID);

                if (owned > 0)
                {
                    ownedKinds++;
                    totalCopies += owned;
                }

                if (ownedOnly && owned == 0)
                    continue;

                CardView view = CardView.Create(grid, CardSize);

                view.ShowData(data, "x" + owned);
                view.SetDimmed(owned == 0);
            }

            summary.text =
                "Unique cards: " + ownedKinds + " / " + cards.Count +
                "   Total copies: " + totalCopies;
        }

        public static int CompareCards(CardData a, CardData b)
        {
            int cost = a.Cost.CompareTo(b.Cost);

            if (cost != 0)
                return cost;

            return string.CompareOrdinal(a.CardName, b.CardName);
        }
    }
}
