using System.Collections.Generic;
using NUnit.Framework;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Conditions;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Data.Import;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Tests
{
    /// <summary>
    /// ทดสอบตัวนำเข้าการ์ดจาก CSV และภาษาความสามารถ
    /// </summary>
    public class CardImportTests
    {
        private const string Header =
            "ID,Name,Type,Rarity,Universe,Faction,Tags,Cost,Attack,Health,Keywords,Abilities\n";

        private static List<CardData> Import(string csv, out CardImportReport report)
        {
            report = new CardImportReport();

            return CardImporter.Import(
                new[] { csv }, new[] { "test.csv" }, null, report);
        }

        // ---------- CSV ----------

        [Test]
        public void Csv_HandlesQuotesCommasAndNewlines()
        {
            List<List<string>> rows = CsvReader.Parse(
                "a,b,c\n\"x,y\",\"say \"\"hi\"\"\",\"line1\nline2\"\r\n1,2,3");

            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual("x,y", rows[1][0]);
            Assert.AreEqual("say \"hi\"", rows[1][1]);
            Assert.AreEqual("line1\nline2", rows[1][2]);
            Assert.AreEqual("3", rows[2][2]);
        }

        [Test]
        public void Csv_SkipsBlankLinesCommentsAndBom()
        {
            List<List<string>> rows = CsvReader.Parse(
                "﻿a,b\n\n# a comment\n,\n1,2\n");

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("a", rows[0][0]);
        }

        [Test]
        public void Csv_EscapeRoundTrips()
        {
            string cell = "a,\"b\"\nc";

            List<List<string>> rows =
                CsvReader.Parse(CsvReader.Escape(cell) + ",x");

            Assert.AreEqual(cell, rows[0][0]);
        }

        // ---------- ภาษาความสามารถ ----------

        [Test]
        public void Script_ParsesTriggerTargetSelectionAndEffects()
        {
            List<string> errors = new List<string>();

            List<CardActionData> actions = CardScript.ParseActions(
                "Play @EnemyUnit:Chosen -> DealDamage(2); ApplyStatus(Burn, 1, 3)",
                null, errors);

            Assert.IsEmpty(errors);
            Assert.AreEqual(1, actions.Count);

            CardActionData action = actions[0];

            Assert.AreEqual(TriggerType.Play, action.Trigger);
            Assert.AreEqual(TargetRule.EnemyUnit, action.Target);
            Assert.AreEqual(TargetSelection.Chosen, action.Selection);
            Assert.AreEqual(2, action.Effects.Count);
            Assert.AreEqual(EffectType.DealDamage, action.Effects[0].Type);
            Assert.AreEqual(2, action.Effects[0].Value);
            Assert.AreEqual(EffectType.ApplyStatus, action.Effects[1].Type);
            Assert.AreEqual(StatusType.Burn, action.Effects[1].Status);
            Assert.AreEqual(1, action.Effects[1].Value);
            Assert.AreEqual(3, action.Effects[1].Duration);
        }

        [Test]
        public void Script_ParsesConditionsAndMultipleActions()
        {
            List<string> errors = new List<string>();

            List<CardActionData> actions = CardScript.ParseActions(
                "Play [Combo, SelfHealthLessThan:15] @AllyLeader -> DrawCard(1) | " +
                "Death @EnemyLeader -> DealDamage(3)",
                null, errors);

            Assert.IsEmpty(errors);
            Assert.AreEqual(2, actions.Count);
            Assert.AreEqual(2, actions[0].Conditions.Count);
            Assert.AreEqual(ConditionType.Combo, actions[0].Conditions[0].Condition);
            Assert.AreEqual(15, actions[0].Conditions[1].Value);
            Assert.AreEqual(TriggerType.Death, actions[1].Trigger);
        }

        [Test]
        public void Script_NamesIgnoreCaseSpacesAndSeparators()
        {
            List<string> errors = new List<string>();

            List<CardActionData> actions = CardScript.ParseActions(
                "play @enemy_unit:chosen -> add keyword(quick attack + Taunt)",
                null, errors);

            Assert.IsEmpty(errors);
            Assert.AreEqual(
                Keyword.QuickAttack | Keyword.Taunt,
                actions[0].Effects[0].Keyword);
        }

        [Test]
        public void Script_ReportsMistakes()
        {
            string[] bad =
            {
                "Play @EnemyUnit DealDamage(2)",
                "Wrong @EnemyUnit -> DealDamage(2)",
                "Play @Nobody -> DealDamage(2)",
                "Play @EnemyUnit -> Explode(2)",
                "Play @EnemyUnit -> DealDamage(two)",
                "Play @EnemyUnit -> DealDamage(1,2)",
                "Play @EnemyUnit -> ApplyStatus(Nonsense)",
                "Play @Self -> Summon(MISSING_CARD)",
                "Play [Combo @Self -> DrawCard(1)"
            };

            foreach (string script in bad)
            {
                List<string> errors = new List<string>();

                CardScript.ParseActions(script, null, errors);

                Assert.IsNotEmpty(errors, "no error for: " + script);
            }
        }

        // ---------- นำเข้าการ์ด ----------

        [Test]
        public void Import_BuildsCardsWithAllFields()
        {
            CardImportReport report;

            List<CardData> cards = Import(
                Header +
                "T_ONE,Test One,Unit,Rare,Naruto,LeafVillage,Human|Melee,3,2,4," +
                "Taunt|Shield,\"Play @EnemyUnit:Chosen -> DealDamage(2)\"\n",
                out report);

            Assert.IsFalse(report.HasErrors, string.Join("\n", report.Errors.ToArray()));
            Assert.AreEqual(1, cards.Count);

            CardData card = cards[0];

            Assert.AreEqual("T_ONE", card.CardID);
            Assert.AreEqual("Test One", card.CardName);
            Assert.AreEqual(CardType.Unit, card.Type);
            Assert.AreEqual(CardRarity.Rare, card.Rarity);
            Assert.AreEqual(Universe.Naruto, card.Universe);
            Assert.AreEqual(Faction.LeafVillage, card.Faction);
            Assert.AreEqual(CardTag.Human | CardTag.Melee, card.Tags);
            Assert.AreEqual(3, card.Cost);
            Assert.AreEqual(2, card.Attack);
            Assert.AreEqual(4, card.Health);
            Assert.AreEqual(Keyword.Taunt | Keyword.Shield, card.Keywords);
            Assert.AreEqual(1, card.Actions.Count);
        }

        [Test]
        public void Import_ColumnOrderAndAliasesDoNotMatter()
        {
            CardImportReport report;

            List<CardData> cards = Import(
                "HP,Name,ATK,ID,Cost,Type\n5,Alias Card,3,T_ALIAS,2,Unit\n",
                out report);

            Assert.IsFalse(report.HasErrors);
            Assert.AreEqual(5, cards[0].Health);
            Assert.AreEqual(3, cards[0].Attack);
            Assert.AreEqual("T_ALIAS", cards[0].CardID);
        }

        [Test]
        public void Import_ResolvesCardReferencesInAnyOrder()
        {
            CardImportReport report;

            List<CardData> cards = Import(
                "ID,Name,Type,Cost,Attack,Health,Tags,Abilities,EvolvesInto,EvolveAfterTurns\n" +
                "T_MAKER,Maker,Unit,3,2,2,,\"Play @AllyLeader -> Summon(T_TOKEN)\",T_BIG,2\n" +
                "T_TOKEN,Token,Unit,1,1,1,Token,,,\n" +
                "T_BIG,Big,Unit,6,6,6,,,,\n",
                out report);

            Assert.IsFalse(report.HasErrors, string.Join("\n", report.Errors.ToArray()));
            Assert.AreEqual(3, cards.Count);

            CardData maker = cards[0];

            Assert.AreSame(cards[1], maker.Actions[0].Effects[0].Card);
            Assert.AreSame(cards[2], maker.EvolvesInto);
            Assert.AreEqual(2, maker.EvolveAfterTurns);

            // Tag Token ทำให้ Source เป็น Token อัตโนมัติ
            Assert.AreEqual(CardSource.Token, cards[1].Source);
            Assert.IsFalse(CardRegistry.IsCollectible(cards[1]));
        }

        [Test]
        public void Import_SkipsBadRowsButKeepsGoodOnes()
        {
            CardImportReport report;

            List<CardData> cards = Import(
                Header +
                "T_OK,Fine,Unit,Common,Original,Neutral,,1,1,1,,\n" +
                "T_OK,Duplicate,Unit,Common,Original,Neutral,,1,1,1,,\n" +
                "T_BADENUM,Bad,Unit,Mythic,Original,Neutral,,1,1,1,,\n" +
                "T_BADNUM,Bad,Unit,Common,Original,Neutral,,one,1,1,,\n" +
                "T_NOHP,Bad,Unit,Common,Original,Neutral,,1,1,0,,\n" +
                "T_BADKW,Bad,Unit,Common,Original,Neutral,,1,1,1,Flying,\n" +
                "T_BADSCRIPT,Bad,Unit,Common,Original,Neutral,,1,1,1,,Play @Nobody -> Heal(1)\n" +
                ",Nameless,Unit,Common,Original,Neutral,,1,1,1,,\n",
                out report);

            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual("T_OK", cards[0].CardID);
            Assert.AreEqual(7, report.Errors.Count,
                string.Join("\n", report.Errors.ToArray()));
        }

        [Test]
        public void Import_RequiresIdAndNameColumns()
        {
            CardImportReport report;

            List<CardData> cards = Import("Cost,Attack\n1,1\n", out report);

            Assert.IsEmpty(cards);
            Assert.IsTrue(report.HasErrors);
        }

        [Test]
        public void Import_UnknownColumnIsOnlyAWarning()
        {
            CardImportReport report;

            List<CardData> cards = Import(
                "ID,Name,Type,Health,Artist\nT_COL,Col,Unit,2,Someone\n",
                out report);

            Assert.AreEqual(1, cards.Count);
            Assert.IsFalse(report.HasErrors);
            Assert.IsNotEmpty(report.Warnings);
        }

        [Test]
        public void Import_UsesExternalLookupForCodeCards()
        {
            CardImportReport report = new CardImportReport();

            List<CardData> cards = CardImporter.Import(
                new[]
                {
                    "ID,Name,Type,Health,Abilities\n" +
                    "T_EXT,Ext,Unit,2,\"Play @AllyLeader -> Summon(TKN_001)\"\n"
                },
                new[] { "ext.csv" },
                StarterCardLibrary.Get,
                report);

            Assert.IsFalse(report.HasErrors, string.Join("\n", report.Errors.ToArray()));
            Assert.AreSame(
                StarterCardLibrary.Get("TKN_001"),
                cards[0].Actions[0].Effects[0].Card);
        }

        // ---------- ไป-กลับกับการ์ดจากโค้ด ----------

        [Test]
        public void EveryCodeCard_SurvivesExportAndImport()
        {
            string csv = CardCsvWriter.Write(StarterCardLibrary.All);

            CardImportReport report = new CardImportReport();

            List<CardData> imported = CardImporter.Import(
                new[] { csv }, new[] { "export.csv" }, null, report);

            Assert.IsEmpty(report.Errors, string.Join("\n", report.Errors.ToArray()));
            Assert.AreEqual(StarterCardLibrary.All.Count, imported.Count);

            Dictionary<string, CardData> byId = new Dictionary<string, CardData>();

            foreach (CardData card in imported)
                byId[card.CardID] = card;

            foreach (CardData original in StarterCardLibrary.All)
            {
                CardData copy = byId[original.CardID];
                string id = original.CardID;

                Assert.AreEqual(original.CardName, copy.CardName, id);
                Assert.AreEqual(original.Type, copy.Type, id);
                Assert.AreEqual(original.Rarity, copy.Rarity, id);
                Assert.AreEqual(original.Universe, copy.Universe, id);
                Assert.AreEqual(original.Faction, copy.Faction, id);
                Assert.AreEqual(original.Tags, copy.Tags, id);
                Assert.AreEqual(original.Cost, copy.Cost, id);
                Assert.AreEqual(original.Attack, copy.Attack, id);
                Assert.AreEqual(original.Health, copy.Health, id);
                Assert.AreEqual(original.Keywords, copy.Keywords, id);
                Assert.AreEqual(original.Source, copy.Source, id);
                Assert.AreEqual(original.AuraAttack, copy.AuraAttack, id);
                Assert.AreEqual(original.AuraHealth, copy.AuraHealth, id);
                Assert.AreEqual(original.EvolveAfterTurns, copy.EvolveAfterTurns, id);
                Assert.AreEqual(
                    original.EvolvesInto == null ? null : original.EvolvesInto.CardID,
                    copy.EvolvesInto == null ? null : copy.EvolvesInto.CardID, id);

                AssertSameActions(original.Actions, copy.Actions, id);
            }
        }

        private static void AssertSameActions(
            List<CardActionData> expected, List<CardActionData> actual, string id)
        {
            Assert.AreEqual(expected.Count, actual.Count, id + " action count");

            for (int a = 0; a < expected.Count; a++)
            {
                CardActionData x = expected[a];
                CardActionData y = actual[a];

                Assert.AreEqual(x.Trigger, y.Trigger, id);
                Assert.AreEqual(x.Target, y.Target, id);
                Assert.AreEqual(x.Selection, y.Selection, id);

                Assert.AreEqual(x.Conditions.Count, y.Conditions.Count, id);

                for (int c = 0; c < x.Conditions.Count; c++)
                {
                    Assert.AreEqual(x.Conditions[c].Condition, y.Conditions[c].Condition, id);
                    Assert.AreEqual(x.Conditions[c].Value, y.Conditions[c].Value, id);
                }

                Assert.AreEqual(x.Effects.Count, y.Effects.Count, id);

                for (int e = 0; e < x.Effects.Count; e++)
                {
                    EffectData p = x.Effects[e];
                    EffectData q = y.Effects[e];

                    Assert.AreEqual(p.Type, q.Type, id);
                    Assert.AreEqual(p.Value, q.Value, id + " " + p.Type);
                    Assert.AreEqual(p.Duration, q.Duration, id + " " + p.Type);
                    Assert.AreEqual(p.Status, q.Status, id);
                    Assert.AreEqual(p.Keyword, q.Keyword, id);
                    Assert.AreEqual(
                        p.Card == null ? null : p.Card.CardID,
                        q.Card == null ? null : q.Card.CardID, id);
                }
            }
        }

        [Test]
        public void DocsTemplate_ImportsWithoutErrors()
        {
            string path = System.IO.Path.Combine(
                UnityEngine.Application.dataPath, "..", "docs", "cards_template.csv");

            if (!System.IO.File.Exists(path))
                Assert.Ignore("docs/cards_template.csv not found");

            CardImportReport report;

            List<CardData> cards = Import(
                System.IO.File.ReadAllText(path), out report);

            Assert.IsFalse(report.HasErrors, string.Join("\n", report.Errors));
            Assert.AreEqual(18, cards.Count);
        }
    }
}
