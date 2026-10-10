using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// Vergil: แปลงร่างเป็น Devil Trigger ไม่เสีย Cost ใช้ได้ครั้งเดียวต่อแมตช์
    /// คงตำแหน่งบนสนาม ความเสียหายสะสม และจำนวนครั้งที่โจมตีไปแล้ว
    /// อยู่ในร่างนี้ 2 เทิร์นของเรา แล้วกลับร่างปกติ (StatusProcessor.RevertForm)
    /// </summary>
    public class DevilTriggerCommand : IBattleCommand
    {
        public const int FormTurns = 2;

        public BattlePlayer Player;
        public CardInstance Card;

        public DevilTriggerCommand(BattlePlayer player, CardInstance card)
        {
            Player = player;
            Card = card;
        }

        /// <summary>ยูนิตนี้แปลงร่างได้ตอนนี้หรือไม่ (ใช้กับปุ่มใน UI ด้วย)</summary>
        public static ActionResult Validate(
            BattleManager battle, BattlePlayer player, CardInstance card)
        {
            if (battle == null || player == null || card == null)
                return ActionResult.Fail("Invalid transformation");

            if (battle.IsOver)
                return ActionResult.Fail("The battle is over");

            if (battle.HasPendingChoice)
                return ActionResult.Fail("Make your choice first");

            if (battle.CurrentPlayer != player)
                return ActionResult.Fail("It is not your turn");

            if (card.Zone != CardZone.Field || card.OwnerID != player.PlayerID)
                return ActionResult.Fail("That unit is not on your field");

            if (!card.HasPassive(CardPassive.DevilTrigger))
                return ActionResult.Fail("That unit cannot transform");

            if (card.DevilTriggerUsed)
                return ActionResult.Fail("Devil Trigger was already used this match");

            if (card.SummonedOnTurn == battle.TurnManager.TotalTurns)
                return ActionResult.Fail("Devil Trigger is available from your next turn");

            if (card.HasStatus(StatusType.TransformationLock))
                return ActionResult.Fail("That unit cannot transform right now");

            if (StarterCardLibrary.Get(StarterCardLibrary.DevilTriggerId) == null)
                return ActionResult.Fail("Missing Devil Trigger form");

            return ActionResult.Ok();
        }

        public ActionResult Execute(BattleManager battle)
        {
            ActionResult check = Validate(battle, Player, Card);

            if (!check.Success)
                return check;

            CardData form = StarterCardLibrary.Get(StarterCardLibrary.DevilTriggerId);
            CardData original = Card.Data;

            string from = original.CardName;

            Card.DevilTriggerUsed = true;

            // คงความเสียหายสะสมและสถานะ (EvolveTo) จากนั้นจำร่างเดิมไว้ให้กลับร่าง
            Card.EvolveTo(form);

            Card.OriginalData = original;

            Card.AddStatus(StatusType.Transformed, 0, StatusEffect.Permanent);
            Card.AddStatus(StatusType.TemporaryTransformation, 0, FormTurns);

            battle.Log(from + " activates Devil Trigger!");

            return ActionResult.Ok();
        }
    }
}
