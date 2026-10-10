using System.Collections.Generic;
using System.Text;

namespace MultiverseCardBattle.Data.Import
{
    /// <summary>
    /// อ่านไฟล์ CSV (RFC 4180): รองรับเครื่องหมายคำพูด, ลูกน้ำและขึ้นบรรทัดใหม่ในช่อง,
    /// BOM ของ Excel / Google Sheets  ข้ามบรรทัดว่างและบรรทัดที่ขึ้นต้นด้วย #
    /// </summary>
    public static class CsvReader
    {
        public static List<List<string>> Parse(string text)
        {
            List<List<string>> rows = new List<List<string>>();

            if (string.IsNullOrEmpty(text))
                return rows;

            if (text[0] == '﻿')
                text = text.Substring(1);

            List<string> row = new List<string>();
            StringBuilder cell = new StringBuilder();

            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }

                    continue;
                }

                if (c == '"' && cell.Length == 0)
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    row.Add(cell.ToString().Trim());
                    cell.Length = 0;
                }
                else if (c == '\r')
                {
                    // ข้าม (ใช้ \n เป็นตัวจบบรรทัด)
                }
                else if (c == '\n')
                {
                    row.Add(cell.ToString().Trim());
                    cell.Length = 0;

                    AddRow(rows, row);

                    row = new List<string>();
                }
                else
                {
                    cell.Append(c);
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString().Trim());

                AddRow(rows, row);
            }

            return rows;
        }

        private static void AddRow(List<List<string>> rows, List<string> row)
        {
            bool empty = true;

            foreach (string value in row)
            {
                if (value.Length > 0)
                {
                    empty = false;
                    break;
                }
            }

            if (empty)
                return;

            // บรรทัดคอมเมนต์
            if (row[0].StartsWith("#"))
                return;

            rows.Add(row);
        }

        /// <summary>ใส่เครื่องหมายคำพูดครอบถ้าช่องมี , " หรือขึ้นบรรทัดใหม่</summary>
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 &&
                !value.StartsWith("#"))
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
