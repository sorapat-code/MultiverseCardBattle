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

        [Header("Conditions")]
        public List<ConditionData> Conditions = new();

        [Header("Target")]
        public TargetRule Target;

        [Header("Effects")]
        public List<EffectData> Effects = new();
    }
}