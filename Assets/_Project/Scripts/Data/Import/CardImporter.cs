using System;
using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data.Import
{
    /// <summary>
    /// แปลงข้อความ CSV ของการ์ดเป็น CardData
    ///
    /// หัวตาราง (ลำดับอะไรก็ได้ ช่องที่ไม่ใส่ใช้ค่าเริ่มต้น):
    ///   ID, Name, Type, Rarity, Universe, Faction, Tags, Cost, Attack, Health,
    ///   Keywords, Abilities, AuraAttack, AuraHealth, EvolvesInto, EvolveAfterTurns,
    ///   Source, Description
    /// ดูรูปแบบเต็มได้ใน docs/CardDatabase.md
    /// </summary>
    public static class CardImporter
    {
        private class Row
        {
            public string File;
            public int Line;
            public Dictionary<string, string> Fields;
            public CardData Card;
            public bool Failed;

            public string Label
            {
                get
                {
                    string id;

                    Fields.TryGetValue("id", out id);

                    return File + " row " + Line + " (" + id + ")";
                }
            }

            public string Get(string key)
            {
                string value;

                return Fields.TryGetValue(key, out value) ? value : "";
            }
        }

        // ชื่อหัวตารางที่ใช้แทนกันได้ -> ชื่อหลัก
        private static readonly Dictionary<string, string> Aliases =
            new Dictionary<string, string>
            {
                { "id", "id" }, { "cardid", "id" },
                { "name", "name" }, { "cardname", "name" },
                { "type", "type" }, { "cardtype", "type" },
                { "rarity", "rarity" },
                { "universe", "universe" }, { "series", "universe" },
                { "faction", "faction" },
                { "tags", "tags" }, { "tag", "tags" },
                { "cost", "cost" },
                { "attack", "attack" }, { "atk", "attack" },
                { "health", "health" }, { "hp", "health" },
                { "keywords", "keywords" }, { "keyword", "keywords" },
                { "abilities", "abilities" }, { "ability", "abilities" },
                { "actions", "abilities" }, { "script", "abilities" },
                { "auraattack", "auraattack" },
                { "aurahealth", "aurahealth" },
                { "evolvesinto", "evolvesinto" }, { "evolve", "evolvesinto" },
                { "evolveafterturns", "evolveafterturns" },
                { "evolveturns", "evolveafterturns" },
                { "source", "source" },
                { "description", "description" }, { "desc", "description" },
                { "text", "description" },
                { "version", "version" }
            };

        /// <summary>
        /// นำเข้าจากข้อความ CSV หลายไฟล์ (fileNames ใช้ในข้อความ Error เท่านั้น)
        /// externalLookup ใช้หาการ์ดที่อยู่นอกไฟล์ (เช่น Token จากโค้ด)
        /// </summary>
        public static List<CardData> Import(
            IList<string> csvTexts,
            IList<string> fileNames,
            Func<string, CardData> externalLookup,
            CardImportReport report)
        {
            List<Row> rows = new List<Row>();
            Dictionary<string, Row> byId = new Dictionary<string, Row>();

            for (int i = 0; i < csvTexts.Count; i++)
            {
                string fileName = fileNames != null && i < fileNames.Count
                    ? fileNames[i]
                    : "file" + (i + 1);

                report.FilesRead++;

                ReadFile(csvTexts[i], fileName, rows, byId, report);
            }

            // รอบ 2: ความสามารถและการอ้างอิงถึงการ์ดอื่น (ต้องรู้การ์ดทุกใบก่อน)
            Func<string, CardData> lookup = id =>
            {
                Row row;

                if (byId.TryGetValue(id, out row))
                    return row.Card;

                return externalLookup != null ? externalLookup(id) : null;
            };

            foreach (Row row in rows)
            {
                if (!row.Failed)
                    ResolveReferences(row, lookup, report);
            }

            List<CardData> cards = new List<CardData>();

            foreach (Row row in rows)
            {
                if (row.Failed)
                    continue;

                cards.Add(row.Card);
                report.CardsImported++;
            }

            return cards;
        }

        // ---------- รอบ 1: อ่านช่องธรรมดา ----------

        private static void ReadFile(
            string text,
            string fileName,
            List<Row> rows,
            Dictionary<string, Row> byId,
            CardImportReport report)
        {
            List<List<string>> table = CsvReader.Parse(text);

            if (table.Count == 0)
                return;

            // หัวตาราง
            List<string> header = new List<string>();

            foreach (string raw in table[0])
            {
                string key = CardScript.Normalize(raw);
                string canonical;

                header.Add(Aliases.TryGetValue(key, out canonical)
                    ? canonical
                    : key);

                if (key.Length > 0 && !Aliases.ContainsKey(key))
                    report.Warnings.Add(fileName + ": unknown column \"" + raw + "\" is ignored");
            }

            if (!header.Contains("id") || !header.Contains("name"))
            {
                report.Errors.Add(fileName + ": the header needs ID and Name columns");
                return;
            }

            for (int r = 1; r < table.Count; r++)
            {
                Row row = new Row();

                row.File = fileName;
                row.Line = r + 1;
                row.Fields = new Dictionary<string, string>();

                for (int c = 0; c < header.Count && c < table[r].Count; c++)
                    row.Fields[header[c]] = table[r][c];

                report.RowsRead++;

                string id = row.Get("id");

                if (id.Length == 0)
                {
                    Fail(row, report, "missing ID");
                    continue;
                }

                if (byId.ContainsKey(id))
                {
                    Fail(row, report, "duplicate ID (first used in " +
                        byId[id].File + " row " + byId[id].Line + ")");
                    continue;
                }

                // ลงทะเบียน ID ก่อน เพื่อให้ ID ซ้ำถูกจับได้แม้แถวนี้ผิด
                byId[id] = row;
                rows.Add(row);

                row.Card = BuildCard(row, report);

                if (row.Card == null)
                    row.Failed = true;
            }
        }

        private static CardData BuildCard(Row row, CardImportReport report)
        {
            string name = row.Get("name");

            if (name.Length == 0)
            {
                report.Errors.Add(row.Label + ": missing Name");
                return null;
            }

            CardType type = CardType.Unit;
            CardRarity rarity = CardRarity.Common;
            Universe universe = Universe.Original;
            Faction faction = Faction.Neutral;
            CardTag tags = CardTag.None;
            Keyword keywords = Keyword.None;
            int cost = 0;
            int attack = 0;
            int health = 0;
            int auraAttack = 0;
            int auraHealth = 0;
            int evolveTurns = 0;

            bool ok = true;

            ok &= ReadEnum(row, "type", ref type, report);
            ok &= ReadEnum(row, "rarity", ref rarity, report);
            ok &= ReadEnum(row, "universe", ref universe, report);
            ok &= ReadEnum(row, "faction", ref faction, report);
            ok &= ReadInt(row, "cost", ref cost, report);
            ok &= ReadInt(row, "attack", ref attack, report);
            ok &= ReadInt(row, "health", ref health, report);
            ok &= ReadInt(row, "auraattack", ref auraAttack, report);
            ok &= ReadInt(row, "aurahealth", ref auraHealth, report);
            ok &= ReadInt(row, "evolveafterturns", ref evolveTurns, report);

            if (!CardScript.TryTags(row.Get("tags"), out tags))
            {
                report.Errors.Add(row.Label + ": bad Tags \"" + row.Get("tags") + "\"");
                ok = false;
            }

            if (!CardScript.TryKeywords(row.Get("keywords"), out keywords))
            {
                report.Errors.Add(row.Label + ": bad Keywords \"" + row.Get("keywords") + "\"");
                ok = false;
            }

            // Source: ไม่ระบุ = Token ถ้ามี Tag Token ไม่งั้น Pack
            CardSource source =
                (tags & CardTag.Token) != 0 ? CardSource.Token : CardSource.Pack;

            ok &= ReadEnum(row, "source", ref source, report);

            if (!ok)
                return null;

            if (cost < 0 || attack < 0 || health < 0)
            {
                report.Errors.Add(row.Label + ": Cost, Attack and Health cannot be negative");
                return null;
            }

            if (type == CardType.Unit && health <= 0)
            {
                report.Errors.Add(row.Label + ": a Unit needs Health of at least 1");
                return null;
            }

            if (cost > 10)
            {
                report.Warnings.Add(row.Label + ": Cost above 10 can never be played");
            }

            if (type != CardType.Unit && (attack > 0 || health > 0))
            {
                report.Warnings.Add(row.Label + ": only Units use Attack and Health");
            }

            if (type != CardType.Unit && (auraAttack != 0 || auraHealth != 0))
            {
                report.Warnings.Add(row.Label + ": only Units can have an Aura");
            }

            CardData data = ScriptableObject.CreateInstance<CardData>();

            // ป้องกัน Unity เก็บกวาด asset ที่สร้างจากโค้ดตอนเปลี่ยนซีน
            data.hideFlags = HideFlags.DontUnloadUnusedAsset;

            string version = row.Get("version");

            data.name = row.Get("id");
            data.CardID = row.Get("id");
            data.CardName = name;
            data.Version = version.Length > 0 ? version : "1.0";
            data.Description = row.Get("description");

            data.Type = type;
            data.Rarity = rarity;
            data.Source = source;
            data.Universe = universe;
            data.Faction = faction;
            data.Tags = tags;
            data.Keywords = keywords;

            data.Cost = cost;
            data.Attack = attack;
            data.Health = health;

            data.AuraAttack = auraAttack;
            data.AuraHealth = auraHealth;
            data.EvolveAfterTurns = evolveTurns;

            return data;
        }

        private static bool ReadEnum<T>(
            Row row, string key, ref T value, CardImportReport report)
            where T : struct
        {
            string text = row.Get(key);

            if (text.Length == 0)
                return true;

            T parsed;

            if (!CardScript.TryEnum(text, out parsed))
            {
                report.Errors.Add(row.Label + ": unknown " + key + " \"" + text + "\"");
                return false;
            }

            value = parsed;

            return true;
        }

        private static bool ReadInt(
            Row row, string key, ref int value, CardImportReport report)
        {
            string text = row.Get(key);

            if (text.Length == 0)
                return true;

            int parsed;

            if (!CardScript.TryInt(text, out parsed))
            {
                report.Errors.Add(row.Label + ": " + key + " must be a whole number (got \"" + text + "\")");
                return false;
            }

            value = parsed;

            return true;
        }

        // ---------- รอบ 2: ความสามารถ / Evolve ----------

        private static void ResolveReferences(
            Row row,
            Func<string, CardData> lookup,
            CardImportReport report)
        {
            List<string> errors = new List<string>();

            row.Card.Actions = CardScript.ParseActions(
                row.Get("abilities"), lookup, errors);

            string evolve = row.Get("evolvesinto");

            if (evolve.Length > 0)
            {
                CardData target = lookup(evolve);

                if (target == null)
                    errors.Add("unknown EvolvesInto card \"" + evolve + "\"");
                else if (target == row.Card)
                    errors.Add("a card cannot evolve into itself");
                else
                    row.Card.EvolvesInto = target;

                if (row.Card.EvolveAfterTurns <= 0)
                {
                    report.Warnings.Add(row.Label +
                        ": EvolvesInto is set but EvolveAfterTurns is 0 (evolves after its first turn)");
                }
            }

            if (errors.Count == 0)
                return;

            foreach (string error in errors)
                report.Errors.Add(row.Label + ": " + error);

            row.Failed = true;
        }

        private static void Fail(Row row, CardImportReport report, string message)
        {
            report.Errors.Add(row.Label + ": " + message);

            row.Failed = true;
        }
    }
}
