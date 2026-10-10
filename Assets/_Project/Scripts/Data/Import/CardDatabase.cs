using System;
using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data.Import
{
    /// <summary>
    /// โหลดการ์ดจากไฟล์ CSV ทุกไฟล์ในโฟลเดอร์ Resources/CardDatabase
    /// (วางกี่ไฟล์ก็ได้ เช่น naruto.csv, onepiece.csv  ต้องมีหัวตารางเหมือนกัน)
    /// </summary>
    public static class CardDatabase
    {
        public const string ResourceFolder = "CardDatabase";

        // ผลการนำเข้าครั้งล่าสุด (ดูจากเมนู Editor หรือ Console)
        public static CardImportReport LastReport { get; private set; }

        public static List<CardData> LoadFromResources(
            Func<string, CardData> externalLookup)
        {
            TextAsset[] files = Resources.LoadAll<TextAsset>(ResourceFolder);

            Array.Sort(files, (a, b) =>
                string.CompareOrdinal(a.name, b.name));

            List<string> texts = new List<string>();
            List<string> names = new List<string>();

            foreach (TextAsset file in files)
            {
                texts.Add(file.text);
                names.Add(file.name + ".csv");
            }

            return LoadFromTexts(texts, names, externalLookup);
        }

        public static List<CardData> LoadFromTexts(
            IList<string> texts,
            IList<string> names,
            Func<string, CardData> externalLookup)
        {
            CardImportReport report = new CardImportReport();

            List<CardData> cards =
                CardImporter.Import(texts, names, externalLookup, report);

            LastReport = report;

            if (report.FilesRead == 0)
                return cards;

            foreach (string error in report.Errors)
                Debug.LogError("Card database: " + error);

            foreach (string warning in report.Warnings)
                Debug.LogWarning("Card database: " + warning);

            Debug.Log(report.Summary());

            return cards;
        }
    }
}
