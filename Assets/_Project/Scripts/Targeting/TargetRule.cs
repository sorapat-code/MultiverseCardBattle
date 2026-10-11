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

        AllUnits,

        // การ์ดต้นเหตุของเหตุการณ์ (ใช้กับ OnTarget: ผู้โจมตี / การ์ดที่เล็งเป้ามา)
        EventSource,

        // ยูนิตฝ่ายเราที่มี CardID ตาม CardActionData.TargetCardID (ไม่รวมตัวเอง ต้องยังอยู่บนสนาม)
        AllyByCardID
    }
}