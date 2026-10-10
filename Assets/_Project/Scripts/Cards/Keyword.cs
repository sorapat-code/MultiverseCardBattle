using System;

namespace MultiverseCardBattle.Cards
{
    /// <summary>
    /// Keyword ของการ์ด (เก็บเป็น Flags เหมือน CardTag)
    /// Silence จะล้าง Keyword ทั้งหมดของการ์ดใบนั้น
    /// </summary>
    [Flags]
    public enum Keyword
    {
        None = 0,

        // ศัตรูต้องโจมตียูนิตที่มี Taunt ก่อน
        Taunt        = 1 << 0,

        // โจมตีได้ทันทีที่ลงสนาม (ทั้งยูนิตและ Leader)
        Charge       = 1 << 1,

        // โจมตีได้ทันทีที่ลงสนาม แต่โจมตีได้เฉพาะยูนิตศัตรู
        Rush         = 1 << 2,

        // กันความเสียหายครั้งถัดไป 1 ครั้ง (True Damage ทะลุ)
        Shield       = 1 << 3,

        // ความเสียหายที่ทำได้จะฟื้น HP ให้ Leader เจ้าของ
        Lifesteal    = 1 << 4,

        // ยูนิตที่โดนความเสียหายจากการ์ดนี้จะตายทันที
        Poison       = 1 << 5,

        // ศัตรูเลือกเป็นเป้าหมายไม่ได้ จนกว่าจะโจมตี
        Stealth      = 1 << 6,

        // โจมตีได้ 2 ครั้งต่อเทิร์น
        DoubleStrike = 1 << 7
    }
}
