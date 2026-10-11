using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// การกระทำ 1 อย่างที่รอทำงานบนเชน (ลงการ์ดหรือโจมตี)
    /// ทำงานเมื่อไม่มีใครตอบโต้เพิ่ม โดยทำจากใบบนสุด (ใบหลังสุด) ลงมา
    /// </summary>
    public class ChainLink
    {
        public BattlePlayer Player;

        // ใบที่ลง หรือยูนิตที่โจมตี
        public CardInstance Card;

        public PlayCardCommand Play;
        public AttackCommand Attack;

        // เป็นการตอบโต้ใบก่อนหน้าบนเชน (ไม่ใช่การกระทำต้นเรื่อง)
        public bool IsResponse;

        // ถูก Negate: ไม่ทำงานเมื่อถึงคิว
        public bool Cancelled;

        public bool IsAttack
        {
            get { return Attack != null; }
        }

        public static ChainLink ForPlay(PlayCardCommand play, bool isResponse)
        {
            return new ChainLink
            {
                Player = play.Player,
                Card = play.Card,
                Play = play,
                IsResponse = isResponse
            };
        }

        public static ChainLink ForAttack(AttackCommand attack)
        {
            return new ChainLink
            {
                Player = attack.Player,
                Card = attack.Attacker,
                Attack = attack
            };
        }

        public string Describe()
        {
            string name = Card != null && Card.Data != null
                ? Card.Data.CardName
                : "?";

            return IsAttack ? name + " attacks" : name;
        }
    }
}
