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

        /// <summary>
        /// จ่ายค่าใช้จ่ายและวางการ์ดลงเชน ผลของการ์ดจะทำงานเมื่อคู่ต่อสู้ไม่ตอบโต้เพิ่ม
        /// (ถ้าไม่มีใครตอบโต้ได้ จะทำงานทันที)
        /// </summary>
        public ActionResult Execute(BattleManager battle)
        {
            ActionResult check =
                BattleRules.ValidatePlay(battle, Player, Card, Target);

            if (!check.Success)
                return check;

            bool responding = battle.ChainResponder == Player;

            // จ่าย Energy และเอาการ์ดออกจากมือ
            Player.CurrentEnergy -= Card.CurrentCost;
            Player.Hand.Remove(Card);

            // Overload: Energy ของเทิร์นหน้าบางส่วนถูกล็อก
            if (Card.HasKeyword(Keyword.Overload))
                Player.OverloadedEnergy += Card.Data.EffectiveOverloadAmount;

            battle.Log(Player.Name + (responding ? " responds with " : " plays ") +
                Card.Data.CardName);

            battle.Stats.AddCardPlayed(Player.PlayerID, Card.Data.CardID);

            battle.DeclareToChain(ChainLink.ForPlay(this, responding));

            return ActionResult.Ok();
        }

        /// <summary>ถูก Negate: การ์ดลงสุสานโดยไม่ทำงาน</summary>
        public void Negated(BattleManager battle)
        {
            Player.AddToZone(Card, CardZone.Graveyard);
        }

        /// <summary>ทำผลของการ์ดจริง (เรียกเมื่อถึงคิวบนเชน)</summary>
        public void ResolvePlay(BattleManager battle)
        {
            bool isUnit = Card.Data.Type == CardType.Unit;

            // เป้าหมายหายไปก่อนถึงคิว (ถูกตอบโต้ฆ่า / ย้าย)
            TargetCandidate target = Target;

            if (target != null &&
                target.TargetObject is CardInstance targetCard &&
                (targetCard.Zone != CardZone.Field || targetCard.CurrentHealth <= 0))
            {
                if (!isUnit)
                {
                    battle.Log(Card.Data.CardName + " fizzles (its target is gone)");

                    Player.AddToZone(Card, CardZone.Graveyard);

                    return;
                }

                target = null;
            }

            if (isUnit)
            {
                // ลงสนามก่อน แล้วค่อยทำ Effect ตอน Play (เหมือน Battlecry)
                if (!battle.SummonUnit(Card, Player))
                {
                    // สนามเต็มก่อนถึงคิว: การ์ดลงสุสาน
                    Player.AddToZone(Card, CardZone.Graveyard);

                    return;
                }
            }

            // แจ้งการ์ดที่ถูกเลือกเป็นเป้าหมาย
            if (target != null &&
                target.TargetObject is CardInstance chosenCard)
            {
                battle.Publish(
                    TriggerType.OnTarget,
                    card: chosenCard,
                    player: battle.GetPlayer(chosenCard.OwnerID),
                    target: Card,
                    targetPlayer: Player);
            }

            battle.Publish(
                TriggerType.Play,
                card: Card,
                player: Player,
                chosen: target);

            // การ์ดที่ไม่ใช่ยูนิต (Spell/Equipment) ลงสุสานหลังใช้เสร็จ
            // (Leader gear ที่สวมอยู่จะอยู่ในช่องอุปกรณ์จนหมดอายุ)
            if (!isUnit && Player.LeaderGear != Card)
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

            // Echo: ได้สำเนา (ไม่มี Echo) กลับเข้ามือ 1 ใบ
            if (Card.HasKeyword(Keyword.Echo) && !Card.IsEchoCopy)
            {
                CardInstance copy = new CardInstance(Card.Data);

                copy.IsEchoCopy = true;
                copy.RemoveKeyword(Keyword.Echo);

                Player.AddToZone(copy, CardZone.Hand);

                battle.Log("Echo: a copy of " + Card.Data.CardName +
                    " returns to " + Player.Name + "'s hand");
            }

            // นับหลังจบ Effect เพื่อให้ Combo ดูเฉพาะการ์ดที่ใช้ "ก่อนหน้า"
            Player.CardsPlayedThisTurn++;
        }
    }
}
