namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// ไม่ตอบโต้ในหน้าต่างตอบโต้ ทำให้ใบบนสุดของเชนทำงาน
    /// </summary>
    public class PassChainCommand : IBattleCommand
    {
        public BattlePlayer Player;

        public PassChainCommand(BattlePlayer player)
        {
            Player = player;
        }

        public ActionResult Execute(BattleManager battle)
        {
            if (battle.ChainResponder != Player)
                return ActionResult.Fail("You have nothing to respond to");

            battle.PassChain(Player);

            return ActionResult.Ok();
        }
    }
}
