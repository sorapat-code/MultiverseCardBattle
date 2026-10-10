namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// ทุกการกระทำของผู้เล่น/AI ต้องผ่าน Command เพื่อให้กติกาอยู่ที่เดียว
    /// ส่งผ่าน BattleManager.Submit(command)
    /// </summary>
    public interface IBattleCommand
    {
        ActionResult Execute(BattleManager battle);
    }
}
