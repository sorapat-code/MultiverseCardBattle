using System;

namespace MultiverseCardBattle.Conditions
{
    [Serializable]
    public class ConditionData
    {
        public ConditionType Condition;

        public int Value;

        // ใช้กับ AllyCardOnField
        public string CardID;
    }
}