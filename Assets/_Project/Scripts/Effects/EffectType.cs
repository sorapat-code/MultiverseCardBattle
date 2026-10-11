namespace MultiverseCardBattle.Effects
{
    public enum EffectType
    {
        None,

        // Damage
        DealDamage,
        DealTrueDamage,

        // Heal
        Heal,

        // Stats
        GainAttack,
        GainHealth,
        SetAttack,
        SetHealth,

        // Cost
        ReduceCost,
        IncreaseCost,
        SetCost,

        // Card
        DrawCard,
        DiscardCard,
        CreateCard,
        DestroyCard,

        // Board
        Summon,
        ReturnToHand,
        MoveToDeck,
        Banish,

        // Keywords
        AddKeyword,
        RemoveKeyword,

        // Status
        Silence,
        Stun,

        // Resource
        GainEnergy,
        SpendEnergy,

        // Other
        Discover,
        Transform,

        // Status (เพิ่มต่อท้ายเพื่อไม่ให้ค่า Enum ของการ์ดเดิมเลื่อน)
        Burn,
        Regen,

        // สถานะทั่วไป: ใช้ EffectData.Status + Value + Duration
        ApplyStatus,
        RemoveStatus,

        // เลือกความสามารถ 1 อย่างจากตัวเลือกเพื่อเพิ่มให้การ์ด (Value = จำนวนตัวเลือก)
        Adapt,

        // ให้ยูนิตโจมตีเพิ่มได้ Value ครั้งในเทิร์นนี้ (Judgment Cut)
        GrantExtraAttack,

        // อุปกรณ์: Equip = สวมใส่ยูนิตเป้าหมาย (ใช้ค่า Gear* ของการ์ดต้นทาง)
        Equip,

        // อุปกรณ์ Leader: วางในช่อง Leader ของผู้เล่น มี Durability (เทิร์น)
        EquipLeader,
        // Negate: ยกเลิกการกระทำที่ถูกตอบโต้ (ใบถัดลงไปบนเชน) ใช้ได้เฉพาะตอนตอบโต้
        Negate
    }
}