using System.Collections.Generic;

namespace MultiverseCardBattle.Cards
{
    public class CardInstance
    {
        private static int nextInstanceId = 1;

        public int InstanceID { get; private set; }

        public CardData Data;

        // เจ้าของการ์ด (PlayerID) 0 = ยังไม่มีเจ้าของ
        public int OwnerID;

        public int CurrentCost;
        public int CurrentAttack;
        public int CurrentHealth;
        public int MaxHealth;

        public Keyword Keywords;
        public List<StatusEffect> Statuses = new List<StatusEffect>();

        public CardZone Zone;

        // เทิร์นรวม (TurnManager.TotalTurns) ที่ลงสนาม ใช้ตรวจ Summoning Sickness
        public int SummonedOnTurn = -1;

        public int AttacksThisTurn;

        public CardInstance(CardData data)
        {
            InstanceID = nextInstanceId++;

            Data = data;

            ApplyBaseStats(data);

            Zone = CardZone.Deck;
        }

        /// <summary>
        /// รีเซ็ตค่าสถานะทั้งหมดกลับเป็นค่าตั้งต้นของ CardData
        /// ใช้ตอนสร้างการ์ด, Transform, หรือการ์ดกลับเข้า Hand/Deck
        /// </summary>
        public void ApplyBaseStats(CardData data)
        {
            if (data == null)
                return;

            CurrentCost = data.Cost;
            CurrentAttack = data.Attack;
            CurrentHealth = data.Health;
            MaxHealth = data.Health;

            Keywords = data.Keywords;

            Statuses.Clear();

            AttacksThisTurn = 0;
        }

        public void TransformTo(CardData data)
        {
            if (data == null)
                return;

            Data = data;

            ApplyBaseStats(data);
        }

        // ---------- Keyword ----------

        public bool HasKeyword(Keyword keyword)
        {
            if (keyword == Keyword.None)
                return false;

            return (Keywords & keyword) == keyword;
        }

        public void AddKeyword(Keyword keyword)
        {
            Keywords |= keyword;
        }

        public void RemoveKeyword(Keyword keyword)
        {
            Keywords &= ~keyword;
        }

        // ---------- Status ----------

        public bool HasStatus(StatusType type)
        {
            for (int i = 0; i < Statuses.Count; i++)
            {
                if (Statuses[i].Type == type)
                    return true;
            }

            return false;
        }

        public bool IsSilenced
        {
            get { return HasStatus(StatusType.Silence); }
        }

        public bool IsStunned
        {
            get { return HasStatus(StatusType.Stun); }
        }

        public void AddStatus(StatusType type, int value, int duration)
        {
            if (type == StatusType.None)
                return;

            if (type == StatusType.Silence)
            {
                // Silence ล้าง Keyword ทั้งหมด
                Keywords = Keyword.None;
            }

            // Stun / Silence ไม่ซ้อนกัน แค่ต่ออายุ
            if (type == StatusType.Stun || type == StatusType.Silence)
            {
                for (int i = 0; i < Statuses.Count; i++)
                {
                    if (Statuses[i].Type != type)
                        continue;

                    if (Statuses[i].Duration != StatusEffect.Permanent &&
                        (duration == StatusEffect.Permanent ||
                         duration > Statuses[i].Duration))
                    {
                        Statuses[i].Duration = duration;
                    }

                    return;
                }
            }

            Statuses.Add(new StatusEffect(type, value, duration));
        }

        public void RemoveStatus(StatusType type)
        {
            for (int i = Statuses.Count - 1; i >= 0; i--)
            {
                if (Statuses[i].Type == type)
                    Statuses.RemoveAt(i);
            }
        }
    }
}
