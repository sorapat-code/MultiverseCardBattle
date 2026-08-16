namespace MultiverseCardBattle.Targeting
{
    public enum TargetType
    {
        None,

        Self,

        AllyLeader,
        EnemyLeader,

        AllyUnit,
        EnemyUnit,

        AnyUnit,

        AllyCard,
        EnemyCard,
        AnyCard,

        AllyHand,
        EnemyHand,

        AllyDeck,
        EnemyDeck,

        AllyGraveyard,
        EnemyGraveyard,

        AllAllies,
        AllEnemies,
        Everyone
    }
}