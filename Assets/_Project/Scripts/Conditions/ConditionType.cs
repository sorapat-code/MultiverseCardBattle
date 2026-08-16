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
        Chance
    }
}
