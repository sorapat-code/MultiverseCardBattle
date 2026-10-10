using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Battle
{
    public class PlayCardCommand : IBattleCommand
    {
        public BattlePlayer Player;
        public CardInstance Card;
        public TargetCandidate Target;

        public PlayCardCommand(
            BattlePlayer player,
            CardInstance card,
            TargetCandidate target = null)
        {
            Player = player;
            Card = card;
            Target = target;
        }

        public ActionResult Execute(BattleManager battle)
        {
            ActionResult check =
                BattleRules.ValidatePlay(battle, Player, Card, Target);

            if (!check.Success)
                return check;

            // จ่าย Energy และเอาการ์ดออกจากมือ
            Player.CurrentEnergy -= Card.CurrentCost;
            Player.Hand.Remove(Card);

            battle.Log(Player.Name + " plays " + Card.Data.CardName);

            bool isUnit = Card.Data.Type == CardType.Unit;

            if (isUnit)
            {
                // ลงสนามก่อน แล้วค่อยทำ Effect ตอน Play (เหมือน Battlecry)
                battle.SummonUnit(Card, Player);
            }

            // แจ้งการ์ดที่ถูกเลือกเป็นเป้าหมาย
            if (Target != null &&
                Target.TargetObject is CardInstance targetCard)
            {
                battle.Publish(
                    TriggerType.OnTarget,
                    card: targetCard,
                    player: battle.GetPlayer(targetCard.OwnerID),
                    target: Card,
                    targetPlayer: Player);
            }

            battle.Publish(
                TriggerType.Play,
                card: Card,
                player: Player,
                chosen: Target);

            // การ์ดที่ไม่ใช่ยูนิต (Spell/Equipment) ลงสุสานหลังใช้เสร็จ
            if (!isUnit)
            {
                Player.AddToZone(Card, CardZone.Graveyard);
            }

            // Spellburst: ยูนิตของผู้เล่นที่มี Keyword นี้ทำงานเมื่อใช้ Spell
            if (Card.Data.Type == CardType.Spell)
            {
                battle.Stats.AddSpellPlayed(Player.PlayerID);
                Player.SpellsPlayedThisTurn++;

                battle.Publish(
                    TriggerType.Spellburst,
                    card: Card,
                    player: Player);
            }

            // นับหลังจบ Effect เพื่อให้ Combo ดูเฉพาะการ์ดที่ใช้ "ก่อนหน้า"
            Player.CardsPlayedThisTurn++;

            return ActionResult.Ok();
        }
    }
}
