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
        Adapt
    }
}