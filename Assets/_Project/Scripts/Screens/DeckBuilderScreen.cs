using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Decks;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// สร้าง/แก้เด็ค: คลิกการ์ดทางซ้ายเพื่อเพิ่ม คลิกรายการทางขวาเพื่อเอาออก
    /// บันทึกอัตโนมัติทุกครั้งที่แก้
    /// </summary>
    public class DeckBuilderScreen : MonoBehaviour
    {
        private static readonly Vector2 CardSize = new Vector2(130f, 182f);

        private PlayerProfile profile;
        private int deckIndex;

        private readonly Dictionary<string, CardView> views =
            new Dictionary<string, CardView>();

        private RectTransform deckList;
        private RectTransform collectionGrid;
        private CardFilterBar filters;
        private Text deckTitle;
        private Text countText;
        private Text statusText;
        private Text messageText;

        private DeckList Deck
        {
            get { return profile.Decks[deckIndex]; }
        }

        private void Start()
        {
            profile = SaveManager.Profile;
            deckIndex = Mathf.Clamp(
                profile.Data.ActiveDeckIndex, 0, profile.Decks.Count - 1);

            BuildUI();
            RefreshAll();
        }

        private void BuildUI()
        {
            RectTransform root = UIFactory.CreateScreenRoot("DeckBuilderCanvas");

            Text heading = ScreenKit.AddLabel(
                root, "DECK BUILDER", 44, TextAnchor.MiddleLeft,
                0.03f, 0.92f, 0.3f, 1f, FontStyle.Bold);

            heading.color = UIFactory.Gold;

            UIFactory.AddTextShadow(heading);

            // แถบเลือกเด็ค
            ScreenKit.AddButton(root, "<", () => ChangeDeck(-1),
                UIFactory.Accent, 0.32f, 0.93f, 0.36f, 0.99f);

            deckTitle = ScreenKit.AddLabel(
                root, "", 28, TextAnchor.MiddleCenter,
                0.365f, 0.93f, 0.595f, 0.99f, FontStyle.Bold);

            deckTitle.color = UIFactory.Gold;

            ScreenKit.AddButton(root, ">", () => ChangeDeck(1),
                UIFactory.Accent, 0.6f, 0.93f, 0.64f, 0.99f);

            ScreenKit.AddButton(root, "New", NewDeck,
                UIFactory.Success, 0.66f, 0.93f, 0.74f, 0.99f, 24);

            ScreenKit.AddButton(root, "Delete", DeleteDeck,
                UIFactory.Danger, 0.75f, 0.93f, 0.83f, 0.99f, 24);

            ScreenKit.AddButton(root, "Use Deck", UseDeck,
                UIFactory.Accent, 0.84f, 0.93f, 0.97f, 0.99f, 24);

            // คอลเลกชัน (ซ้าย) พร้อมแถบกรอง
            filters = new CardFilterBar(
                root, 0.02f, 0.855f, 0.62f, 0.915f,
                id => profile.GetOwnedCount(id));

            filters.Changed += OnFilterChanged;

            ScreenKit.AddScroll(
                root, "Collection", 0.02f, 0.12f, 0.62f, 0.845f,
                out collectionGrid);

            UIFactory.AddGrid(collectionGrid, CardSize, 10f, 10);

            BuildCollection(collectionGrid);

            // รายการในเด็ค (ขวา)
            countText = ScreenKit.AddLabel(
                root, "", 28, TextAnchor.MiddleCenter,
                0.64f, 0.84f, 0.98f, 0.91f, FontStyle.Bold);

            countText.color = UIFactory.Gold;

            ScreenKit.AddScroll(
                root, "DeckList", 0.64f, 0.26f, 0.98f, 0.84f, out deckList);

            UIFactory.AddVertical(deckList, 4f, 6);

            statusText = ScreenKit.AddLabel(
                root, "", 22, TextAnchor.UpperLeft,
                0.64f, 0.12f, 0.98f, 0.26f);

            messageText = ScreenKit.AddLabel(
                root, "", 24, TextAnchor.MiddleCenter,
                0.25f, 0.02f, 0.75f, 0.11f);

            ScreenKit.AddButton(root, "Back",
                () => SceneLoader.Go(GameScene.MainMenu),
                UIFactory.Muted, 0.02f, 0.02f, 0.16f, 0.1f);

            ScreenKit.AddButton(root, "Clear Deck", ClearDeck,
                UIFactory.Muted, 0.83f, 0.02f, 0.98f, 0.1f, 24);
        }

        private void BuildCollection(RectTransform grid)
        {
            List<CardData> cards = new List<CardData>();

            foreach (CardData data in CardRegistry.All)
            {
                if (CardRegistry.IsVisibleInCollection(data, profile))
                    cards.Add(data);
            }

            cards.Sort(CollectionScreen.CompareCards);

            ScreenKit.ClearNow(grid);

            views.Clear();

            foreach (CardData data in cards)
            {
                if (!filters.Matches(data))
                    continue;

                CardView view = CardView.Create(grid, CardSize);

                view.ShowData(data);
                view.Clicked += OnCollectionCardClicked;

                views[data.CardID] = view;
            }
        }

        private void OnFilterChanged()
        {
            BuildCollection(collectionGrid);

            RefreshAll();
        }

        // ---------- แก้เด็ค ----------

        private void OnCollectionCardClicked(CardView view)
        {
            if (view == null || view.Data == null)
                return;

            string id = view.Data.CardID;

            DeckList deck = Deck;

            if (profile.GetOwnedCount(id) <= deck.CountOf(id))
            {
                Say("You do not own another copy of " + view.Data.CardName);
                return;
            }

            if (deck.CountOf(id) >= DeckRules.MaxCopies(view.Data))
            {
                Say("Max " + DeckRules.MaxCopies(view.Data) +
                    " copies of " + view.Data.CardName);
                return;
            }

            if (deck.CardIDs.Count >= DeckRules.MaxDeckSize)
            {
                Say("The deck is full (" + DeckRules.MaxDeckSize + " cards)");
                return;
            }

            deck.CardIDs.Add(id);

            Changed();
        }

        private void RemoveOne(string id)
        {
            Deck.CardIDs.Remove(id);

            Changed();
        }

        private void ClearDeck()
        {
            Deck.CardIDs.Clear();

            Changed();
        }

        private void Changed()
        {
            SaveManager.Save();

            RefreshAll();
        }

        // ---------- จัดการเด็ค ----------

        private void ChangeDeck(int delta)
        {
            int count = profile.Decks.Count;

            deckIndex = (deckIndex + delta + count) % count;

            RefreshAll();
        }

        private void NewDeck()
        {
            DeckList created = profile.AddDeck(null);

            if (created == null)
            {
                Say("You can have at most " + PlayerProfile.MaxDecks + " decks");
                return;
            }

            deckIndex = profile.Decks.Count - 1;

            Changed();
        }

        private void DeleteDeck()
        {
            if (!profile.RemoveDeck(Deck))
            {
                Say("You need at least one deck");
                return;
            }

            deckIndex = Mathf.Clamp(deckIndex, 0, profile.Decks.Count - 1);

            Changed();
        }

        private void UseDeck()
        {
            List<string> problems = DeckRules.Validate(Deck, profile);

            if (problems.Count > 0)
            {
                Say("Cannot use this deck: " + problems[0]);
                return;
            }

            profile.SetActiveDeck(deckIndex);

            SaveManager.Save();

            Say("\"" + Deck.Name + "\" is now your battle deck");

            RefreshAll();
        }

        // ---------- แสดงผล ----------

        private void RefreshAll()
        {
            DeckList deck = Deck;

            bool active = profile.ActiveDeck == deck;

            deckTitle.text =
                deck.Name + "  (" + (deckIndex + 1) + "/" + profile.Decks.Count + ")" +
                (active ? "  [ACTIVE]" : "");

            countText.text =
                "Cards: " + deck.CardIDs.Count + " / " + DeckRules.MaxDeckSize +
                "  (min " + DeckRules.MinDeckSize + ")";

            foreach (KeyValuePair<string, CardView> pair in views)
            {
                int owned = profile.GetOwnedCount(pair.Key);
                int inDeck = deck.CountOf(pair.Key);

                pair.Value.ShowData(pair.Value.Data, inDeck + "/" + owned);
                pair.Value.SetDimmed(owned == 0);
                pair.Value.SetHighlight(
                    inDeck > 0 ? CardHighlight.Ready : CardHighlight.None);
            }

            RebuildDeckList(deck);

            List<string> problems = DeckRules.Validate(deck, profile);

            if (problems.Count == 0)
            {
                statusText.text = "<color=#70E080>Deck is valid</color>";
            }
            else
            {
                StringBuilder text = new StringBuilder();

                for (int i = 0; i < problems.Count && i < 3; i++)
                    text.Append("<color=#FF9090>- ").Append(problems[i]).Append("</color>\n");

                statusText.text = text.ToString();
            }
        }

        private void RebuildDeckList(DeckList deck)
        {
            ScreenKit.ClearNow(deckList);

            Dictionary<string, int> counts = new Dictionary<string, int>();

            foreach (string id in deck.CardIDs)
            {
                if (!counts.ContainsKey(id))
                    counts[id] = 0;

                counts[id]++;
            }

            List<CardData> unique = new List<CardData>();

            foreach (string id in counts.Keys)
            {
                CardData data = CardRegistry.Get(id);

                if (data != null)
                    unique.Add(data);
            }

            unique.Sort(CollectionScreen.CompareCards);

            foreach (CardData data in unique)
            {
                string id = data.CardID;

                Button row = UIFactory.CreateButton(
                    deckList,
                    "(" + data.Cost + ")  " + data.CardName + "   x" + counts[id],
                    () => RemoveOne(id),
                    UIFactory.Panel,
                    24);

                row.GetComponent<RectTransform>().sizeDelta =
                    new Vector2(0f, 44f);
            }
        }

        private void Say(string text)
        {
            messageText.text = text;
        }
    }
}
