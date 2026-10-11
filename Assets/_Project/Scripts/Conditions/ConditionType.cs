namespace MultiverseCardBattle.Conditions
{
    public enum ConditionType
    {
        None,

        // Turn
        TurnNumber,
        IsFirstTurn,

        // HP
        SelfHealthLessThan,
        SelfHealthGreaterThan,

        // Energy
        EnergyGreaterThan,
        EnergyLessThan,

        // Cards
        HandCount,
        DeckCount,
        GraveyardCount,

        // Board
        AllyUnitCount,
        EnemyUnitCount,

        // Tags
        HasTag,
        HasFaction,
        HasUniverse,

        // Random
        Chance,

        // Combo: เจ้าของใช้การ์ดอื่นมาก่อนแล้วในเทิร์นนี้
        Combo,

        // เป้าหมาย (ของ Trigger) ติดสถานะ Marked อยู่
        TargetMarked,

        // เป้าหมาย (ของ Trigger) เป็นยูนิตที่มี HP เต็ม
        TargetFullHealth,

        // เจ้าของมียูนิตที่มี CardID ตาม ConditionData.CardID อยู่บนสนาม
        AllyCardOnField,

        // ยูนิตต้นเหตุกำลังโจมตีครั้งแรกของเทิร์นนี้ (ใช้กับ Trigger Attack / AfterAttack)
        FirstAttackThisTurn,

        // เป้าหมาย (ของ Trigger) เป็นยูนิตของฝ่ายตรงข้ามเจ้าของการ์ด
        TargetIsEnemyUnit,

        // เป้าหมาย (ของ Trigger) คือการ์ดที่มี CardID ตาม ConditionData.CardID
        TargetIsCard,

        // ยูนิตต้นเหตุกำลังโจมตียูนิตศัตรูครั้งแรกของเทิร์นนี้
        FirstUnitAttackThisTurn,

        // เจ้าของมียูนิต CardID นี้บนสนาม และมันยังไม่ได้โจมตียูนิตศัตรูในเทิร์นนี้
        AllyCardNotUnitAttackedYet,

        // เป้าหมาย (ของ Trigger) เป็นการ์ดที่มี Cost ตั้งแต่ Value ขึ้นไป (เช่น Spell ที่ใช้ตอน Spellburst)
        TargetCostAtLeast
    }
}
