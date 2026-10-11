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
        GraspHeart = 1 << 5,

        // Vergil: ต้นเทิร์น ถ้าไม่โดนความเสียหายตั้งแต่ต้นเทิร์นก่อน ATK +2 จนจบเทิร์น
        Concentration = 1 << 6,

        // Vergil: แปลงร่างเป็น Devil Trigger ได้ครั้งเดียวต่อแมตช์ (ปุ่มกดในหน้าต่อสู้)
        DevilTrigger = 1 << 7,

        // ร่าง Devil Trigger: ต้นเทิร์นฟื้น HP ให้ตัวเอง 3
        SinDevilRegeneration = 1 << 8,

        // ร่าง Devil Trigger: ความเสียหายจากการโจมตีไม่ถูกลดโดย Immune / Defense Up / Weakness
        OverwhelmingPower = 1 << 9,

        // เป็นร่าง Devil Trigger: กลับร่างแล้วคง HP (ลดเหลือเพดานของร่างปกติ) และไม่ติด Fatigue
        DevilTriggerForm = 1 << 10,

        // Lelouch (Geass): ต้นเทิร์นเลือกยูนิตศัตรู 1 ใบ ให้ Stun (โจมตี/ใช้ความสามารถไม่ได้)
        Geass = 1 << 11,

        // Lelouch (Chessmaster): ครั้งแรกในแต่ละเทิร์นที่เจ้าของเล่นการ์ดใบที่สอง จั่วการ์ด 1 ใบ
        Chessmaster = 1 << 12,

        // Guts: ถูกความเสียหายแล้ว HP เหลือ 2 หรือน้อยกว่า ได้ ATK +3 และ Unstoppable จนจบเทิร์น
        Berserker = 1 << 13,

        // Zhongli: เมื่อ Shield ของยูนิตฝ่ายเราถูกทำลายครั้งแรกในแต่ละเทิร์น ฟื้นฟู Leader 1
        JadeShield = 1 << 14,

        // Mini Herta: เมื่อยูนิตฝ่ายเราที่เป็น AssistTargetCardID (The Herta) โจมตียูนิต ยิงใส่เป้าหมายก่อนด้วย ATK ของตัวเอง
        // (ไม่นับเป็นการโจมตี ไม่โดนสวนกลับ)
        AssistStrike = 1 << 15
    }
}
