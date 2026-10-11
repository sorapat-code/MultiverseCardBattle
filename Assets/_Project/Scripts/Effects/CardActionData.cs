using System;
using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Targeting;
using MultiverseCardBattle.Conditions;

namespace MultiverseCardBattle.Effects
{
    [Serializable]
    public class CardActionData
    {
        [Header("Trigger")]
        public TriggerType Trigger;

        [Header("When targeted (Trigger = OnTarget)")]
        public TargetedSource TargetedBy;
        public TargetedSide TargetedFrom;

        [Header("Conditions")]
        public List<ConditionData> Conditions = new();

        [Header("Target")]
        public TargetRule Target;

        // วิธีเลือกเป้าหมาย (ค่าเริ่มต้น All = พฤติกรรมเดิม)
        public TargetSelection Selection;

        // ใช้กับ Target = AllyByCardID
        public string TargetCardID;

        [Header("Effects")]
        public List<EffectData> Effects = new();
    }
}