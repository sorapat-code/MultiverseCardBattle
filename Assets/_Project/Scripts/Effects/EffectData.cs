using System;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Effects
{
    [Serializable]
    public class EffectData
    {
        public EffectType Type;

        // ค่าตัวเลข เช่น Damage, Heal, Cost
        public int Value;

        // ใช้กับ CreateCard / Transform
        public CardData Card;
    }
}