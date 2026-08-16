using System;

namespace MultiverseCardBattle.Cards
{
    [Flags]
    public enum CardTag
    {
        None = 0,

        // Race
        Human      = 1 << 0,
        Beast      = 1 << 1,
        Machine    = 1 << 2,
        Spirit     = 1 << 3,
        Undead     = 1 << 4,
        God        = 1 << 5,
        Demon      = 1 << 6,
        Dragon     = 1 << 7,

        // Combat
        Melee      = 1 << 8,
        Ranged     = 1 << 9,
        Magic      = 1 << 10,
        Support    = 1 << 11,
        Tank       = 1 << 12,
        Assassin   = 1 << 13,

        // Special
        Leader     = 1 << 14,
        Elite      = 1 << 15,
        Boss       = 1 << 16,
        Summon     = 1 << 17,
        Token      = 1 << 18,

        // Energy
        Fire       = 1 << 19,
        Water      = 1 << 20,
        Wind       = 1 << 21,
        Earth      = 1 << 22,
        Lightning  = 1 << 23,
        Ice        = 1 << 24,
        Dark       = 1 << 25,
        Light      = 1 << 26
    }
}