using System;
using System.Collections.Generic;

namespace MultiverseCardBattle.Cards
{
    public class CardInstance
    {
        private static int nextInstanceId = 1;

        public int InstanceID { get; private set; }

        public CardData Data;

        // ร่างเดิมก่อนแปลงร่าง (null = ยังไม่เคยแปลง) ใช้กลับร่างเดิม / ตอนกลับเข้ามือ
        public CardData OriginalData;

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

        // จำนวนเทิร์นของเจ้าของที่ยูนิตนี้อยู่บนสนามจนจบเทิร์น (ใช้กับ Evolve)
        public int TurnsOnField;

        // ค่าที่ Aura / สถานะบวกลบไว้อยู่ตอนนี้ (BattleManager.RefreshModifiers คำนวณใหม่เอง)
        public int AppliedAttackMod;
        public int AppliedHealthMod;
        public int AppliedCostMod;

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
            TurnsOnField = 0;

            AppliedAttackMod = 0;
            AppliedHealthMod = 0;
            AppliedCostMod = 0;
        }

        public void TransformTo(CardData data)
        {
            if (data == null)
                return;

            Data = data;

            ApplyBaseStats(data);
        }

        /// <summary>
        /// Evolve: เปลี่ยนเป็นร่างใหม่ แต่คงความเสียหายที่สะสมไว้และสถานะเดิม
        /// </summary>
        public void EvolveTo(CardData data)
        {
            if (data == null)
                return;

            int damage = MaxHealth - CurrentHealth;
            int attacks = AttacksThisTurn;
            int turns = TurnsOnField;

            List<StatusEffect> kept = new List<StatusEffect>(Statuses);

            Data = data;

            ApplyBaseStats(data);

            Statuses.AddRange(kept);

            AttacksThisTurn = attacks;
            TurnsOnField = turns;

            if (IsSilenced)
                Keywords = Keyword.None;

            CurrentHealth = Math.Max(1, MaxHealth - damage);
        }

        /// <summary>
        /// กลับเป็นร่างเดิม (ถ้าเคยแปลงร่าง) คืน true ถ้ามีการเปลี่ยน
        /// ไม่รีเซ็ตค่า ให้เรียก ApplyBaseStats ต่อเอง
        /// </summary>
        public bool RestoreOriginalForm()
        {
            if (OriginalData == null)
                return false;

            Data = OriginalData;
            OriginalData = null;

            return true;
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

        public StatusEffect GetStatus(StatusType type)
        {
            for (int i = 0; i < Statuses.Count; i++)
            {
                if (Statuses[i].Type == type)
                    return Statuses[i];
            }

            return null;
        }

        /// <summary>ผลรวมค่า (Value) ของสถานะชนิดนี้ทั้งหมด</summary>
        public int StatusValue(StatusType type)
        {
            int total = 0;

            for (int i = 0; i < Statuses.Count; i++)
            {
                if (Statuses[i].Type == type)
                    total += Statuses[i].Value;
            }

            return total;
        }

        public bool IsSilenced
        {
            get { return HasStatus(StatusType.Silence); }
        }

        // Unstoppable ไม่ถูกหยุดด้วย Stun / Freeze / Disarm / Confusion
        public bool IsStunned
        {
            get
            {
                return HasStatus(StatusType.Stun) &&
                       !HasKeyword(Keyword.Unstoppable);
            }
        }

        public bool IsFrozen
        {
            get
            {
                return HasStatus(StatusType.Freeze) &&
                       !HasKeyword(Keyword.Unstoppable);
            }
        }

        public bool IsDisarmed
        {
            get
            {
                return HasStatus(StatusType.Disarm) &&
                       !HasKeyword(Keyword.Unstoppable);
            }
        }

        public bool IsConfused
        {
            get
            {
                return HasStatus(StatusType.Confusion) &&
                       !HasKeyword(Keyword.Unstoppable);
            }
        }

        /// <summary>ซ่อนตัวด้วย Stealth อยู่หรือไม่ (Marked / Exposed ทำให้ซ่อนไม่ได้)</summary>
        /// <summary>
        /// Immune: ลดความเสียหายทุกครั้งที่โดน (0 = ไม่มี Immune)
        /// </summary>
        public int ImmuneAmount
        {
            get
            {
                if (!HasKeyword(Keyword.Immune))
                    return 0;

                return Data != null
                    ? Data.EffectiveImmuneAmount
                    : CardData.DefaultImmuneAmount;
            }
        }

        // ATK โบนัสชั่วคราวจนจบเทิร์น (ล้างทุกครั้งที่เทิร์นจบ)
        public int TurnAttackBonus;

        // Last Guardian ใช้ไปแล้วในแมตช์นี้
        public bool LastGuardianUsed;

        // เทิร์น (TotalTurns) ที่ต้องคืน Taunt ให้ -1 = ไม่มี
        public int TauntRestoreTurn = -1;

        // โจมตีเพิ่มได้อีกกี่ครั้งในเทิร์นนี้ (ล้างตอนจบเทิร์น)
        public int ExtraAttacks;

        // ใช้ Devil Trigger ไปแล้วในแมตช์นี้
        public bool DevilTriggerUsed;

        // ได้รับความเสียหายตั้งแต่ต้นเทิร์นของเจ้าของครั้งก่อน (Concentration)
        public bool DamagedSinceTurnStart;

        // จำนวนครั้งที่ยูนิตนี้โจมตีทั้งหมด (ตัวนับเริ่มใหม่เมื่อเป็นการ์ดใบใหม่)
        public int TotalAttacks;

        // The Undead Overlord ใช้ไปแล้วในแมตช์นี้
        public bool UndeadOverlordUsed;

        // Unstoppable ชั่วคราวจาก The Undead Overlord หมดหลังจบเทิร์นนี้ (TotalTurns) -1 = ไม่มี
        public int TempUnstoppableUntil = -1;
        public bool TempUnstoppableGranted;

        // ผู้ที่ทำความเสียหาย/ทำลายครั้งล่าสุดด้วย "เอฟเฟกต์" (ไม่ใช่การต่อสู้)
        // PlayerID = 0 แปลว่าไม่ใช่เอฟเฟกต์ ใช้ตัดสินสาเหตุการตาย
        public int LastEffectPlayerID;
        public CardInstance LastEffectSource;

        public bool HasPassive(CardPassive passive)
        {
            return Data != null &&
                   !IsSilenced &&
                   (Data.Passives & passive) != 0;
        }

        // Shadowbound ที่กำลังทำงานอยู่ (BattleManager.RefreshModifiers เป็นผู้ตั้งค่า)
        public bool ShadowActive;

        // Unstoppable ที่ได้จาก Shadowbound (เอาออกได้เมื่อเงื่อนไขหาย)
        public bool GrantedUnstoppable;

        public bool IsHidden
        {
            get
            {
                return HasKeyword(Keyword.Stealth) &&
                       !HasStatus(StatusType.Marked) &&
                       !HasStatus(StatusType.Exposed);
            }
        }

        /// <summary>
        /// ติดสถานะ คืน false ถ้าถูกกัน (Immune / Protected / Unstoppable)
        /// </summary>
        public bool AddStatus(StatusType type, int value, int duration)
        {
            if (type == StatusType.None)
                return false;

            if (StatusInfo.IsHarmful(type))
            {
                if (HasKeyword(Keyword.Immune) ||
                    HasStatus(StatusType.Protected))
                    return false;

                if (HasKeyword(Keyword.Unstoppable) &&
                    StatusInfo.StopsAttack(type))
                    return false;
            }

            if (type == StatusType.Silence)
            {
                // Silence ล้าง Keyword ทั้งหมด
                Keywords = Keyword.None;
            }

            // Burn นับเป็นเทิร์น: ติดซ้ำแล้วต่อจำนวนเทิร์น (ความแรงใช้ค่าสูงสุด ไม่รวมกัน)
            // เช่น Burn 1 สองครั้ง = 1 ดาเมจต่อเทิร์น นาน 2 เทิร์น
            if (type == StatusType.Burn)
            {
                for (int i = 0; i < Statuses.Count; i++)
                {
                    StatusEffect existing = Statuses[i];

                    if (existing.Type != StatusType.Burn)
                        continue;

                    if (value > existing.Value)
                        existing.Value = value;

                    if (existing.Duration != StatusEffect.Permanent)
                    {
                        existing.Duration =
                            duration == StatusEffect.Permanent
                                ? StatusEffect.Permanent
                                : existing.Duration + duration;
                    }

                    return true;
                }
            }

            if (StatusInfo.Stacks(type) || StatusInfo.Refreshes(type))
            {
                for (int i = 0; i < Statuses.Count; i++)
                {
                    StatusEffect existing = Statuses[i];

                    if (existing.Type != type)
                        continue;

                    if (StatusInfo.Stacks(type))
                        existing.Value += value;
                    else if (value > existing.Value)
                        existing.Value = value;

                    if (existing.Duration != StatusEffect.Permanent &&
                        (duration == StatusEffect.Permanent ||
                         duration > existing.Duration))
                    {
                        existing.Duration = duration;
                    }

                    return true;
                }
            }

            Statuses.Add(new StatusEffect(type, value, duration));

            return true;
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
