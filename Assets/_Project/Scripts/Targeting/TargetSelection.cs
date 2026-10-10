namespace MultiverseCardBattle.Targeting
{
    /// <summary>
    /// วิธีเลือกเป้าหมายจาก TargetRule
    /// All    = ทุกเป้าหมายที่เข้าเงื่อนไข (พฤติกรรมเดิม)
    /// Chosen = ผู้เล่นเลือก 1 เป้าหมาย (ถ้าไม่มี/ไม่ถูกต้อง จะสุ่มแทน)
    /// Random = สุ่ม 1 เป้าหมาย
    /// </summary>
    public enum TargetSelection
    {
        All,
        Chosen,
        Random
    }
}
