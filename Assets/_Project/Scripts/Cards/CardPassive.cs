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
        LastGuardian = 1 << 1,

        // Ainz: ยูนิตศัตรูตายจาก Spell หรือความสามารถของ Ainz ได้ Energy 1 (สูงสุด 3 ต่อเทิร์น)
        DarkWisdom = 1 << 2,

        // Ainz: Spell ใบแรกของเทิร์นลด Cost 2 ใบที่สองลด 1
        SupremeMagicCaster = 1 << 3,

        // Ainz: ครั้งแรกในแมตช์ที่จะตายจากความเสียหาย เหลือ HP 1 และได้ Unstoppable ชั่วคราว
        UndeadOverlord = 1 << 4,

        // Ainz: ต้นเทิร์นเลือกยูนิตศัตรู 1 ใบ ดาเมจ 4 (ตายแล้วกระจาย 3 ดาเมจให้ศัตรูสุ่ม 2 ใบ)
        GraspHeart = 1 << 5
    }
}
