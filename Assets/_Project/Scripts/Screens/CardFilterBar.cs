using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// แถบกรองการ์ดที่ใช้ร่วมกันในหน้า Collection และ Deck Builder:
    /// ค้นหาชื่อ + มี/ไม่มี + ประเภท + ความหายาก + จักรวาล + ค่าใช้จ่าย
    /// กดปุ่มเพื่อวนค่าตัวกรอง เรียก Matches(card) เพื่อตรวจว่าการ์ดผ่านตัวกรองหรือไม่
    /// </summary>
    public class CardFilterBar
    {
        private enum OwnMode
        {
            All,
            Owned,
            Missing
        }

        // ค่าใช้จ่ายตั้งแต่ค่านี้ขึ้นไปรวมเป็นกลุ่มเดียว
        private const int CostCap = 7;

        public event Action Changed;

        private readonly Func<string, int> ownedCount;

        private OwnMode ownMode = OwnMode.All;

        // -1 = ทั้งหมด
        private int typeIndex = -1;
        private int rarityIndex = -1;
        private int universeIndex = -1;
        private int cost = -1;

        private string search = "";

        private Button ownButton;
        private Button typeButton;
        private Button rarityButton;
        private Button universeButton;
        private Button costButton;

        private static readonly CardType[] Types =
        {
            CardType.Unit, CardType.Spell, CardType.Equipment
        };

        private static readonly CardRarity[] Rarities =
            (CardRarity[])Enum.GetValues(typeof(CardRarity));

        private static readonly Universe[] Universes =
            (Universe[])Enum.GetValues(typeof(Universe));

        public CardFilterBar(
            Transform parent,
            float xMin, float yMin, float xMax, float yMax,
            Func<string, int> ownedCount)
        {
            this.ownedCount = ownedCount;

            RectTransform bar = UIFactory.CreateZone(
                parent, "FilterBar", xMin, yMin, xMax, yMax);

            // สัดส่วนความกว้างของแต่ละช่อง (รวมกันเท่ากับ 1 รวมช่องว่าง)
            float[] widths = { 0.22f, 0.14f, 0.14f, 0.15f, 0.19f, 0.12f };

            const float gap = 0.008f;

            float x = 0f;

            RectTransform[] cells = new RectTransform[widths.Length];

            for (int i = 0; i < widths.Length; i++)
            {
                cells[i] = UIFactory.CreateRect("Cell" + i, bar);

                UIFactory.SetAnchors(cells[i], x, 0f, x + widths[i] - gap, 1f);

                x += widths[i];
            }

            BuildSearch(cells[0]);

            ownButton = AddCycleButton(cells[1], () => Cycle(ref ownMode));
            typeButton = AddCycleButton(cells[2], CycleType);
            rarityButton = AddCycleButton(cells[3], CycleRarity);
            universeButton = AddCycleButton(cells[4], CycleUniverse);
            costButton = AddCycleButton(cells[5], CycleCost);

            RefreshLabels();
        }

        // ---------- ตรวจการ์ด ----------

        public bool Matches(CardData data)
        {
            if (data == null)
                return false;

            if (ownMode != OwnMode.All && ownedCount != null)
            {
                int owned = ownedCount(data.CardID);

                if (ownMode == OwnMode.Owned && owned <= 0)
                    return false;

                if (ownMode == OwnMode.Missing && owned > 0)
                    return false;
            }

            if (typeIndex >= 0 && data.Type != Types[typeIndex])
                return false;

            if (rarityIndex >= 0 && data.Rarity != Rarities[rarityIndex])
                return false;

            if (universeIndex >= 0 && data.Universe != Universes[universeIndex])
                return false;

            if (cost >= 0)
            {
                if (cost >= CostCap ? data.Cost < CostCap : data.Cost != cost)
                    return false;
            }

            if (search.Length > 0 && !MatchesSearch(data))
                return false;

            return true;
        }

        private bool MatchesSearch(CardData data)
        {
            string text = search.ToLowerInvariant();

            return Contains(data.CardName, text) ||
                   Contains(data.CardID, text) ||
                   Contains(Pretty(data.Universe.ToString()), text) ||
                   Contains(Pretty(data.Faction.ToString()), text);
        }

        private static bool Contains(string value, string lowerSearch)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.ToLowerInvariant().Contains(lowerSearch);
        }

        // ---------- ตัวควบคุม ----------

        private void BuildSearch(RectTransform cell)
        {
            Image back = cell.gameObject.AddComponent<Image>();

            back.color = new Color(0f, 0f, 0f, 0.45f);

            UIShapes.MakeRounded(back, 10f);

            InputField input = cell.gameObject.AddComponent<InputField>();

            Text text = UIFactory.CreateText(
                cell, "", 20, UIFactory.TextColor, TextAnchor.MiddleLeft);

            UIFactory.Stretch(text.rectTransform);

            text.rectTransform.offsetMin = new Vector2(10f, 0f);
            text.rectTransform.offsetMax = new Vector2(-10f, 0f);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            Text placeholder = UIFactory.CreateText(
                cell, "Search name...", 20, new Color(1f, 1f, 1f, 0.4f),
                TextAnchor.MiddleLeft, FontStyle.Italic);

            UIFactory.Stretch(placeholder.rectTransform);

            placeholder.rectTransform.offsetMin = new Vector2(10f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-10f, 0f);

            input.targetGraphic = back;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 30;

            input.onValueChanged.AddListener(value =>
            {
                search = value == null ? "" : value.Trim();

                RaiseChanged();
            });
        }

        private Button AddCycleButton(RectTransform cell, Action onClick)
        {
            Button button = UIFactory.CreateButton(
                cell, "", () => onClick(), UIFactory.Panel, 18);

            UIFactory.Stretch(button.GetComponent<RectTransform>());

            Text label = button.GetComponentInChildren<Text>();

            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 18;

            return button;
        }

        private void Cycle(ref OwnMode mode)
        {
            mode = (OwnMode)(((int)mode + 1) % 3);

            Apply();
        }

        private void CycleType()
        {
            typeIndex = Next(typeIndex, Types.Length);

            Apply();
        }

        private void CycleRarity()
        {
            rarityIndex = Next(rarityIndex, Rarities.Length);

            Apply();
        }

        private void CycleUniverse()
        {
            universeIndex = Next(universeIndex, Universes.Length);

            Apply();
        }

        private void CycleCost()
        {
            cost = Next(cost, CostCap + 1);

            Apply();
        }

        // -1 (ทั้งหมด) -> 0 -> 1 -> ... -> count-1 -> -1
        private static int Next(int current, int count)
        {
            int next = current + 1;

            return next >= count ? -1 : next;
        }

        private void Apply()
        {
            RefreshLabels();

            RaiseChanged();
        }

        private void RaiseChanged()
        {
            if (Changed != null)
                Changed();
        }

        private void RefreshLabels()
        {
            UIFactory.SetButtonLabel(ownButton,
                ownMode == OwnMode.All ? "Owned: All" :
                (ownMode == OwnMode.Owned ? "Owned: Yes" : "Owned: No"));

            UIFactory.SetButtonLabel(typeButton,
                "Type: " + (typeIndex < 0 ? "All" : Types[typeIndex].ToString()));

            UIFactory.SetButtonLabel(rarityButton,
                "Rarity: " + (rarityIndex < 0 ? "All" : Rarities[rarityIndex].ToString()));

            UIFactory.SetButtonLabel(universeButton,
                universeIndex < 0
                    ? "Universe: All"
                    : Pretty(Universes[universeIndex].ToString()));

            UIFactory.SetButtonLabel(costButton,
                "Cost: " + (cost < 0 ? "All" : (cost >= CostCap ? CostCap + "+" : cost.ToString())));

            Tint(ownButton, ownMode != OwnMode.All);
            Tint(typeButton, typeIndex >= 0);
            Tint(rarityButton, rarityIndex >= 0);
            Tint(universeButton, universeIndex >= 0);
            Tint(costButton, cost >= 0);
        }

        // ตัวกรองที่ใช้อยู่จะเป็นสีเด่น
        private static void Tint(Button button, bool active)
        {
            button.GetComponent<Image>().color =
                active ? UIFactory.Accent : UIFactory.Panel;
        }

        // "HonkaiStarRail" -> "Honkai Star Rail"
        public static string Pretty(string raw)
        {
            StringBuilder name = new StringBuilder();

            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && char.IsUpper(raw[i]) && !char.IsUpper(raw[i - 1]))
                    name.Append(' ');

                name.Append(raw[i]);
            }

            return name.ToString();
        }
    }
}
