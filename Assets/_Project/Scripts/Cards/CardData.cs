using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Effects;

namespace MultiverseCardBattle.Cards
{
    [CreateAssetMenu(
        fileName = "New Card",
        menuName = "Multiverse Card Battle/Card")]
    public class CardData : ScriptableObject
    {
        [Header("Basic Information")]

        public string CardID;
        public string CardName;
        public string Version;

        [TextArea]
        public string Description;

        public Sprite Artwork;

        [Header("Classification")]

        public CardType Type;
        public CardRarity Rarity;
        public CardSource Source;
        public Universe Universe;
        public Faction Faction;
        public CardTag Tags;

        [Header("Keywords")]

        public Keyword Keywords;

        // ความสามารถพิเศษที่ทำงานในโค้ด (เช่น การ์ดลับ)
        public CardPassive Passives;

        // ชุดการ์ด (ใช้แยกซอง) ค่าเริ่มต้น = Core
        public CardSet Set;

        [Header("Equipment")]

        // โบนัสที่อุปกรณ์ให้ (ใช้กับ Effect Equip) / Leader gear ใช้ GearDurability
        public int GearAttack;
        public int GearHealth;
        public Keyword GearKeywords;

        // Leader gear: จำนวนเทิร์นของเจ้าของที่อุปกรณ์อยู่ได้ (0 = ไม่ใช่ Leader gear)
        public int GearDurability;

        // Trident: เมื่อจบการต่อสู้ที่ยูนิตที่สวมโจมตี (ถ้ายังรอด) อุปกรณ์นี้และยูนิตนั้นกลับขึ้นมือ
        public bool GearReturnsOnAttack;

        [Header("Stats")]

        public int Cost;
        public int Attack;
        public int Health;

        [Header("Immune (ลดความเสียหายทุกครั้งที่โดน 0 = ใช้ค่าตั้งต้น)")]

        public const int DefaultImmuneAmount = 3;

        public int ImmuneAmount;

        public int EffectiveImmuneAmount
        {
            get { return ImmuneAmount > 0 ? ImmuneAmount : DefaultImmuneAmount; }
        }

        [Header("Overload (Energy ที่ถูกล็อกในเทิร์นหน้า 0 = ใช้ค่าตั้งต้น)")]

        public const int DefaultOverloadAmount = 1;

        public int OverloadAmount;

        public int EffectiveOverloadAmount
        {
            get { return OverloadAmount > 0 ? OverloadAmount : DefaultOverloadAmount; }
        }

        [Header("Aura (ให้ยูนิตพวกเดียวกันตัวอื่นขณะอยู่บนสนาม)")]

        public int AuraAttack;
        public int AuraHealth;

        // ลดความเสียหายที่ยูนิตพวกเดียวกันตัวอื่นได้รับ (เหมือน Defense Up) ขณะอยู่บนสนาม
        public int AuraDefense;

        // ใช้กับ AssistStrike: CardID ของยูนิตฝ่ายเราที่ต้องเป็นผู้โจมตี
        public string AssistTargetCardID;

        [Header("Transform on Kill (Kurosaki Ichigo)")]

        // ร่างที่แปลงเมื่อยูนิตนี้ทำลายการ์ดที่ Cost รวม > KillTransformCost หรือ ATK รวม > KillTransformAttack
        public CardData KillTransformForm;
        public int KillTransformCost;
        public int KillTransformAttack;

        // ร่างนี้อยู่ได้เฉพาะบนสนาม: เมื่อออกจากสนามจะกลับเป็นร่างเดิม (Bankai)
        public bool RevertsOnLeaveField;

        [Header("Evolve")]

        // เปลี่ยนเป็นร่างนี้เมื่ออยู่บนสนามครบ EvolveAfterTurns เทิร์นของเจ้าของ
        public CardData EvolvesInto;
        public int EvolveAfterTurns;

        [Header("Actions")]

        public List<CardActionData> Actions = new();
    }
}