using System.Collections.Generic;

namespace MultiverseCardBattle.Data.Import
{
    /// <summary>
    /// ผลการนำเข้าการ์ดจาก CSV: Error = การ์ดนั้นไม่ถูกนำเข้า, Warning = นำเข้าแล้วแต่น่าสงสัย
    /// </summary>
    public class CardImportReport
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();

        public int FilesRead;
        public int RowsRead;
        public int CardsImported;

        public bool HasErrors
        {
            get { return Errors.Count > 0; }
        }

        public string Summary()
        {
            return "Card database: " + CardsImported + " cards from " +
                   FilesRead + " file(s), " + Errors.Count + " error(s), " +
                   Warnings.Count + " warning(s)";
        }
    }
}
