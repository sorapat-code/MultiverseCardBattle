public enum TriggerType
{
    None,

    // Battle
    BattleStart,
    BattleEnd,

    // Turn
    TurnStart,
    TurnEnd,

    // Card
    Draw,
    Play,
    Summon,
    Discard,
    Death,

    // Combat
    Attack,
    Hit,
    DamageTaken,
    Heal,
    LeaderAttack,

    // Target
    OnTarget,

    // เจ้าของยูนิตใช้การ์ด Spell (ใช้กับ Keyword Spellburst)
    // เพิ่มต่อท้ายเพื่อไม่ให้ค่า Enum ของการ์ดเดิมเลื่อน
    Spellburst,

    // การ์ดถูกส่งลงสุสานจากทางใดก็ได้ (ตาย / ทิ้ง / ใช้ Spell เสร็จ / ถูกทำลายจากมือหรือกอง)
    Graveyard,

    // หลังยูนิตโจมตีเสร็จ (หลังสู้/ตี Leader แล้ว และยังอยู่บนสนาม)
    AfterAttack,

    // ยูนิตอื่นฝั่งเดียวกันถูก Summon ลงสนาม (Card = ยูนิตที่คอยฟัง, Target = ยูนิตที่เพิ่งลงสนาม)
    AllySummoned,

    // การ์ดนี้เป็นผู้ทำลายยูนิตศัตรู (Card = ผู้ทำลาย, Target = ยูนิตที่ตาย)
    Kill,

    // ยูนิตฝ่ายเดียวกันตัวใดตัวหนึ่งทำลายยูนิตศัตรู (Card = ผู้ฟัง, Target = ยูนิตที่ตาย)
    AllyKill
}