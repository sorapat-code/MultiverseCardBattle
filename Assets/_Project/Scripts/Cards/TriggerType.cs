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
    OnTarget
}