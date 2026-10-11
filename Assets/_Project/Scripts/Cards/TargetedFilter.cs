namespace MultiverseCardBattle.Cards
{
    /// <summary>ใช้กับ Trigger OnTarget: ถูกเล็งเป้าด้วยอะไร</summary>
    public enum TargetedSource
    {
        Any,
        Attack,
        SpellOrAbility
    }

    /// <summary>ใช้กับ Trigger OnTarget: ผู้เล็งเป้าเป็นฝ่ายไหน (เทียบกับเจ้าของการ์ด)</summary>
    public enum TargetedSide
    {
        Any,
        Enemy,
        Ally
    }
}
