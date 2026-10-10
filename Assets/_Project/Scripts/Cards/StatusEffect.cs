using System;

namespace MultiverseCardBattle.Cards
{
    public enum StatusType
    {
        None,

        // โจมตีไม่ได้
        Stun,

        // ปิด Keyword และ Action ของการ์ด
        Silence,

        // โดนความเสียหายตอนเริ่มเทิร์นของเจ้าของ
        Burn,

        // ฟื้น HP ตอนเริ่มเทิร์นของเจ้าของ
        Regen
    }

    /// <summary>
    /// สถานะที่ติดอยู่กับ CardInstance
    /// Duration = จำนวนเทิร์น (นับลดตอนจบเทิร์นของเจ้าของการ์ด), -1 = ถาวร
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        public StatusType Type;
        public int Value;
        public int Duration;

        public const int Permanent = -1;

        public StatusEffect(StatusType type, int value, int duration)
        {
            Type = type;
            Value = value;
            Duration = duration;
        }
    }
}
