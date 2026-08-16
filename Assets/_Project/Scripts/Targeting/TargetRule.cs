namespace MultiverseCardBattle.Targeting
{
    public enum TargetRule
    {
        None,

        Self,

        AllyUnit,
        AllyLeader,

        EnemyUnit,
        EnemyLeader,

        AnyUnit,
        AnyLeader,

        AllAllies,
        AllEnemies,

        AllUnits
    }
}