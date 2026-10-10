namespace MultiverseCardBattle.Battle
{
    public class EndTurnCommand : IBattleCommand
    {
        public BattlePlayer Player;

        public EndTurnCommand(BattlePlayer player)
        {
            Player = player;
        }

        public ActionResult Execute(BattleManager battle)
        {
            if (battle.IsOver)
                return ActionResult.Fail("The battle is over");

            if (battle.HasPendingDiscover)
                return ActionResult.Fail("Choose a Discover option first");

            if (battle.CurrentPlayer != Player)
                return ActionResult.Fail("It is not your turn");

            battle.TurnManager.EndTurn();

            return ActionResult.Ok();
        }
    }
}
