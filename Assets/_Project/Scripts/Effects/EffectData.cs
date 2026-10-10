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

        // ใช้กับ AddKeyword / RemoveKeyword
        public Keyword Keyword;

        // จำนวนเทิร์นของสถานะ (Stun / Burn / Regen) 0 = ใช้ค่าเริ่มต้นของสถานะนั้น
        public int Duration;
    }
}