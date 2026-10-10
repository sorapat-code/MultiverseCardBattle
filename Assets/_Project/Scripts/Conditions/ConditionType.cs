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
        TargetMarked
    }
}
