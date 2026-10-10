using System;

namespace MultiverseCardBattle.Cards
{
    /// <summary>
    /// ความสามารถพิเศษประจำการ์ดที่ไม่ใช่ Keyword ทั่วไป (ทำงานในโค้ดของ Battle)
    /// ถูกปิดเมื่อการ์ดโดน Silence
    /// </summary>
    [Flags]
    public enum CardPassive
    {
        None = 0,

        // ได้รับความเสียหายจากการโจมตี: ATK +1 จนจบเทิร์น
        Abysswalker = 1 << 0,

        // ครั้งแรกในแมตช์ที่กำลังจะตาย: เหลือ HP 1 แต่เสีย Taunt จนจบเทิร์นถัดไป
        LastGuardian = 1 << 1
    }
}
