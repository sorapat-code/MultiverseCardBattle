using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data.Import
{
    /// <summary>
    /// เขียนการ์ดออกเป็น CSV รูปแบบเดียวกับที่ CardImporter อ่านได้
    /// ใช้ย้ายการ์ดที่สร้างจากโค้ดมาเป็นไฟล์ แล้วแก้ต่อใน Excel / Google Sheets
    /// </summary>
    public static class CardCsvWriter
    {
        public static readonly string[] Columns =
        {
            "ID", "Name", "Type", "Rarity", "Universe", "Faction", "Tags",
            "Cost", "Attack", "Health", "Keywords", "Abilities",
            "ImmuneAmount", "AuraAttack", "AuraHealth", "EvolvesInto", "EvolveAfterTurns",
            "Source", "Description"
        };

        public static string Header()
        {
            return string.Join(",", Columns);
        }

        public static string Write(IEnumerable<CardData> cards)
        {
            StringBuilder text = new StringBuilder();

            text.Append(Header()).Append('\n');

            foreach (CardData card in cards)
            {
                if (card != null)
                    text.Append(Row(card)).Append('\n');
            }

            return text.ToString();
        }

        public static string Row(CardData card)
        {
            string[] cells =
            {
                card.CardID,
                card.CardName,
                card.Type.ToString(),
                card.Rarity.ToString(),
                card.Universe.ToString(),
                card.Faction.ToString(),
                CardScript.TagList(card.Tags, "|"),
                Number(card.Cost),
                Number(card.Attack),
                Number(card.Health),
                CardScript.KeywordList(card.Keywords, "|"),
                CardScript.Format(card.Actions),
                Number(card.ImmuneAmount),
                Number(card.AuraAttack),
                Number(card.AuraHealth),
                card.EvolvesInto != null ? card.EvolvesInto.CardID : "",
                Number(card.EvolveAfterTurns),
                card.Source.ToString(),
                card.Description
            };

            for (int i = 0; i < cells.Length; i++)
                cells[i] = CsvReader.Escape(cells[i]);

            return string.Join(",", cells);
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
