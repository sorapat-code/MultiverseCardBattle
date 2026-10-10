using System;

namespace MultiverseCardBattle.Cards
{
    public enum StatusType
    {
        None,

        // ---------- ควบคุมการกระทำ ----------

        // โจมตีไม่ได้ และความสามารถแบบ Active (TurnStart / TurnEnd / Attack) ไม่ทำงาน
        Stun,

        // ปิด Keyword และ Action ของการ์ด
        Silence,

        // ---------- ความเสียหายต่อเนื่อง ----------

        // โดนความเสียหายตอนเริ่มเทิร์นของเจ้าของ
        Burn,

        // ฟื้น HP ตอนเริ่มเทิร์นของเจ้าของ (Regeneration)
        Regen,

        // ---------- เพิ่มต่อท้าย เพื่อไม่ให้ค่า Enum ของการ์ดเดิมเลื่อน ----------

        // โจมตีไม่ได้ในช่วงเวลาที่กำหนด (โดน Burn แล้วละลาย)
        Freeze,

        // ถูกย้ายโซนไม่ได้ (Return / MoveToDeck / Banish)
        Root,

        // โจมตีไม่ได้ แต่ความสามารถอื่นยังทำงาน
        Disarm,

        // ตอนโจมตี เป้าหมายถูกสุ่มใหม่ (อาจโดนพวกเดียวกัน)
        Confusion,

        // พิษ: ความเสียหายจริง (ทะลุ Shield) ตอนเริ่มเทิร์น ซ้อน Value ได้
        Poison,

        // เลือดไหล: ความเสียหายต่อเนื่อง ซ้อน Value ได้
        Bleed,

        // คำสาป: รักษาไม่ได้
        Curse,

        // ถูกทำเครื่องหมาย: Stealth ไม่ช่วย และเป็นเงื่อนไขให้ความสามารถอื่น (TargetMarked)
        Marked,

        AttackUp,
        AttackDown,

        // ลดความเสียหายที่ได้รับ Value (อย่างน้อยเหลือ 1)
        DefenseUp,

        // ความเสียหายที่ได้รับเพิ่ม Value
        DefenseDown,

        // ความเสียหายที่ได้รับเพิ่ม 50%
        Vulnerable,

        // ความเสียหายที่สร้างลดลง 50%
        Weakness,

        // เกราะ: ดูดซับความเสียหายรวม Value แล้วหายไป
        Barrier,

        // Cost ของการ์ดในมือเปลี่ยน
        CostUp,
        CostDown,

        // Shield / Barrier / DefenseUp ใช้ไม่ได้ และ Stealth ไม่ช่วย
        Exposed,

        // กันสถานะเสียทุกชนิดที่จะมาติด
        Protected,

        // ตอนเริ่มเทิร์น ล้างสถานะเสีย 1 อย่างและฟื้น 1 HP
        Blessed,

        // ---------- Transformation ----------

        // อยู่ในร่างที่แปลงมา
        Transformed,

        // ร่างปัจจุบันมีเวลาจำกัด หมดเวลาแล้วกลับร่างเดิม
        TemporaryTransformation,

        // แปลงร่างซ้ำไม่ได้
        TransformationLock,

        // หลังกลับร่างเดิม Attack ลดลง Value
        TransformationFatigue
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

    /// <summary>
    /// ข้อมูลประกอบสถานะ: ชื่อที่แสดง, คำอธิบาย, เป็นสถานะเสียหรือไม่, ซ้อนค่าได้หรือไม่
    /// </summary>
    public static class StatusInfo
    {
        public static string DisplayName(StatusType type)
        {
            switch (type)
            {
                case StatusType.Regen: return "Regeneration";
                case StatusType.AttackUp: return "Attack Up";
                case StatusType.AttackDown: return "Attack Down";
                case StatusType.DefenseUp: return "Defense Up";
                case StatusType.DefenseDown: return "Defense Down";
                case StatusType.CostUp: return "Cost Up";
                case StatusType.CostDown: return "Cost Down";
                case StatusType.TemporaryTransformation: return "Temporary Transformation";
                case StatusType.TransformationLock: return "Transformation Lock";
                case StatusType.TransformationFatigue: return "Transformation Fatigue";
                default: return type.ToString();
            }
        }

        /// <summary>สถานะเสียที่ Immune / Protected กันได้</summary>
        public static bool IsHarmful(StatusType type)
        {
            switch (type)
            {
                case StatusType.Stun:
                case StatusType.Silence:
                case StatusType.Burn:
                case StatusType.Freeze:
                case StatusType.Root:
                case StatusType.Disarm:
                case StatusType.Confusion:
                case StatusType.Poison:
                case StatusType.Bleed:
                case StatusType.Curse:
                case StatusType.Marked:
                case StatusType.AttackDown:
                case StatusType.DefenseDown:
                case StatusType.Vulnerable:
                case StatusType.Weakness:
                case StatusType.CostUp:
                case StatusType.Exposed:
                case StatusType.TransformationLock:
                case StatusType.TransformationFatigue:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>สถานะที่ Unstoppable กัน (สถานะที่หยุดการโจมตี)</summary>
        public static bool StopsAttack(StatusType type)
        {
            return type == StatusType.Stun ||
                   type == StatusType.Freeze ||
                   type == StatusType.Disarm ||
                   type == StatusType.Confusion;
        }

        /// <summary>ติดซ้ำแล้วรวมค่า (Value) เข้าด้วยกัน</summary>
        public static bool Stacks(StatusType type)
        {
            switch (type)
            {
                case StatusType.Poison:
                case StatusType.Bleed:
                case StatusType.Barrier:
                case StatusType.AttackUp:
                case StatusType.AttackDown:
                case StatusType.DefenseUp:
                case StatusType.DefenseDown:
                case StatusType.CostUp:
                case StatusType.CostDown:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>ติดซ้ำแล้วแค่ต่ออายุ (ไม่มีสองอัน)</summary>
        public static bool Refreshes(StatusType type)
        {
            switch (type)
            {
                case StatusType.Burn:
                case StatusType.Regen:
                case StatusType.None:
                    return false;

                default:
                    return !Stacks(type);
            }
        }

        /// <summary>จำนวนเทิร์นเริ่มต้นเมื่อ Effect ไม่ได้ระบุ Duration</summary>
        public static int DefaultDuration(StatusType type)
        {
            switch (type)
            {
                case StatusType.Silence:
                case StatusType.Transformed:
                case StatusType.Barrier:
                    return StatusEffect.Permanent;

                case StatusType.Stun:
                case StatusType.Freeze:
                case StatusType.Disarm:
                    return 1;

                case StatusType.Root:
                case StatusType.Confusion:
                case StatusType.Marked:
                case StatusType.Exposed:
                case StatusType.TransformationLock:
                case StatusType.TransformationFatigue:
                case StatusType.TemporaryTransformation:
                    return 2;

                default:
                    return 3;
            }
        }

        public static string Description(StatusType type)
        {
            switch (type)
            {
                case StatusType.Stun: return "Cannot attack or use turn/attack abilities.";
                case StatusType.Silence: return "Loses keywords and abilities.";
                case StatusType.Freeze: return "Cannot attack for a time. Burn thaws it.";
                case StatusType.Root: return "Cannot be returned, shuffled or banished.";
                case StatusType.Disarm: return "Cannot attack, but other abilities still work.";
                case StatusType.Confusion: return "Attacks hit a random target, even allies.";
                case StatusType.Burn: return "Takes damage at the start of its owner's turn. Burning again adds turns, not damage.";
                case StatusType.Poison: return "Takes true damage each turn. Stacks.";
                case StatusType.Bleed: return "Takes damage each turn. Stacks.";
                case StatusType.Curse: return "Cannot be healed.";
                case StatusType.Marked: return "Cannot hide in Stealth; triggers bonus effects.";
                case StatusType.AttackUp: return "Gains Attack.";
                case StatusType.AttackDown: return "Loses Attack.";
                case StatusType.DefenseUp: return "Takes less damage (minimum 1).";
                case StatusType.DefenseDown: return "Takes more damage.";
                case StatusType.Vulnerable: return "Takes 50% more damage.";
                case StatusType.Weakness: return "Deals 50% less damage.";
                case StatusType.Regen: return "Heals at the start of its owner's turn.";
                case StatusType.Barrier: return "Absorbs a set amount of damage.";
                case StatusType.CostUp: return "Card costs more Energy.";
                case StatusType.CostDown: return "Card costs less Energy.";
                case StatusType.Exposed: return "Shield, Barrier and Defense Up stop working; Stealth is lost.";
                case StatusType.Protected: return "Blocks every harmful status.";
                case StatusType.Blessed: return "Each turn cleanses one harmful status and heals 1.";
                case StatusType.Transformed: return "Currently in a transformed form.";
                case StatusType.TemporaryTransformation: return "Reverts to its original form when time runs out.";
                case StatusType.TransformationLock: return "Cannot transform.";
                case StatusType.TransformationFatigue: return "Weakened after transforming back (less Attack).";
                default: return "";
            }
        }
    }
}
