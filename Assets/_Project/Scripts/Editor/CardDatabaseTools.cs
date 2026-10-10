using System.IO;
using UnityEditor;
using UnityEngine;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Data.Import;

namespace MultiverseCardBattle.Editor
{
    /// <summary>
    /// เมนูจัดการฐานข้อมูลการ์ดแบบ CSV (Resources/CardDatabase)
    /// </summary>
    public static class CardDatabaseTools
    {
        private const string Folder = "Assets/_Project/Resources/CardDatabase";
        private const string StarterFile = Folder + "/starter_cards.csv";

        [MenuItem("Multiverse Card Battle/Card Database/Export Code Cards To CSV")]
        public static void ExportCodeCards()
        {
            if (File.Exists(StarterFile) &&
                !EditorUtility.DisplayDialog(
                    "Export Code Cards To CSV",
                    StarterFile + " already exists. Overwrite it?\n" +
                    "Any edits you made in that file will be lost.",
                    "Overwrite", "Cancel"))
            {
                return;
            }

            Directory.CreateDirectory(Folder);

            File.WriteAllText(
                StarterFile,
                CardCsvWriter.Write(StarterCardLibrary.All),
                new System.Text.UTF8Encoding(true));

            AssetDatabase.Refresh();

            CardRegistry.Reload();

            Debug.Log(
                "Exported " + StarterCardLibrary.All.Count + " cards to " +
                StarterFile + ". Edit it in Excel / Google Sheets.");
        }

        [MenuItem("Multiverse Card Battle/Card Database/Validate And Reload")]
        public static void ValidateAndReload()
        {
            CardRegistry.Reload();

            CardImportReport report = CardDatabase.LastReport;

            if (report == null || report.FilesRead == 0)
            {
                Debug.Log(
                    "Card database: no CSV files found in " + Folder +
                    ". Cards come from code and Resources/Cards only.");
                return;
            }

            // Error / Warning ถูกพิมพ์ตอนโหลดแล้ว  ตรงนี้สรุปให้เห็นง่าย
            string message = report.Summary();

            if (report.HasErrors)
                Debug.LogError(message + "  - fix the errors above; those cards were skipped.");
            else
                Debug.Log(message + "  - OK");
        }

        [MenuItem("Multiverse Card Battle/Card Database/Open Folder")]
        public static void OpenFolder()
        {
            Directory.CreateDirectory(Folder);

            AssetDatabase.Refresh();

            EditorUtility.RevealInFinder(Folder);
        }
    }
}
