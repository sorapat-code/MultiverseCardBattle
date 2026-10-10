using System.Collections.Generic;
using UnityEngine;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ช่องสำหรับรูปภาพ UI จริง วางไฟล์รูปไว้ที่ Assets/_Project/Resources/UI/
    /// (ตั้ง Texture Type = Sprite) เกมจะใช้รูปนั้นแทนภาพที่สร้างจากโค้ดโดยอัตโนมัติ
    /// ไม่มีไฟล์ก็ใช้ภาพจากโค้ดตามเดิม
    ///
    /// ชื่อไฟล์ที่รองรับ:
    ///   bg_menu         พื้นหลังเมนู ร้านค้า คอลเลกชัน เด็ค แคมเปญ
    ///   bg_battle       พื้นหลังหน้าต่อสู้
    ///   logo            โลโก้เกมบนหน้าเมนู
    ///   portrait_player รูป Leader ของผู้เล่น
    ///   portrait_enemy  รูป Leader ของศัตรู (ทุกด่าน)
    ///   portrait_ชื่อ    รูป Leader ศัตรูเฉพาะด่าน ชื่อไฟล์ = ชื่อศัตรูตัวพิมพ์เล็ก ไม่มีช่องว่าง
    ///                   เช่น portrait_banditchief
    ///   card_back       หลังการ์ด (ใช้แสดงกองการ์ด)
    /// </summary>
    public static class UIArt
    {
        private const string Folder = "UI/";

        private static readonly Dictionary<string, Sprite> cache =
            new Dictionary<string, Sprite>();

        /// <summary>คืนรูปตามชื่อ หรือ null ถ้าไม่มีไฟล์</summary>
        public static Sprite Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            Sprite sprite;

            if (cache.TryGetValue(name, out sprite) && sprite != null)
                return sprite;

            sprite = Resources.Load<Sprite>(Folder + name);

            // เก็บ null ไว้ด้วย จะได้ไม่ค้นหาไฟล์ซ้ำทุกครั้ง
            cache[name] = sprite;

            return sprite;
        }

        /// <summary>ชื่อไฟล์รูป Leader ของศัตรูเฉพาะตัว</summary>
        public static string PortraitName(string leaderName)
        {
            if (string.IsNullOrEmpty(leaderName))
                return "";

            System.Text.StringBuilder key = new System.Text.StringBuilder();

            foreach (char c in leaderName)
            {
                if (char.IsLetterOrDigit(c))
                    key.Append(char.ToLowerInvariant(c));
            }

            return "portrait_" + key;
        }
    }
}
