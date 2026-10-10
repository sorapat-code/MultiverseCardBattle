using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    public class AttackCommand : IBattleCommand
    {
        public BattlePlayer Player;
        public CardInstance Attacker;

        // ตี Unit: ใส่ TargetUnit / ตี Leader: ใส่ TargetLeader
        public CardInstance TargetUnit;
        public BattlePlayer TargetLeader;

        public AttackCommand(
            BattlePlayer player,
            CardInstance attacker,
            CardInstance targetUnit,
            BattlePlayer targetLeader)
        {
            Player = player;
            Attacker = attacker;
            TargetUnit = targetUnit;
            TargetLeader = targetLeader;
        }

        public ActionResult Execute(BattleManager battle)
        {
            ActionResult check =
                BattleRules.ValidateAttack(
                    battle, Player, Attacker, TargetUnit, TargetLeader);

            if (!check.Success)
                return check;

            BattlePlayer defender = battle.GetOpponent(Player);

            Attacker.AttacksThisTurn++;

            // โจมตีแล้วหลุด Stealth
            Attacker.RemoveKeyword(Keyword.Stealth);

            if (TargetUnit != null)
            {
                battle.Log(
                    Attacker.Data.CardName + " attacks " +
                    TargetUnit.Data.CardName);

                battle.Publish(
                    TriggerType.Attack,
                    card: Attacker,
                    player: Player,
                    target: TargetUnit,
                    targetPlayer: defender);

                // Trigger อาจทำให้ใครตายหรือถูกย้ายไปแล้ว
                if (Attacker.Zone == CardZone.Field &&
                    TargetUnit.Zone == CardZone.Field)
                {
                    int attackerDamage = Attacker.CurrentAttack;
                    int counterDamage = TargetUnit.CurrentAttack;

                    battle.DamageUnit(Attacker, TargetUnit, attackerDamage);
                    battle.DamageUnit(TargetUnit, Attacker, counterDamage);
                }
            }
            else
            {
                battle.Log(
                    Attacker.Data.CardName + " attacks " + defender.Name);

                battle.Publish(
                    TriggerType.Attack,
                    card: Attacker,
                    player: Player,
                    targetPlayer: defender);

                battle.Publish(
                    TriggerType.LeaderAttack,
                    card: Attacker,
                    player: Player,
                    targetPlayer: defender);

                if (Attacker.Zone == CardZone.Field)
                {
                    battle.DamageLeader(
                        Attacker, defender, Attacker.CurrentAttack, false);
                }
            }

            return ActionResult.Ok();
        }
    }
}
