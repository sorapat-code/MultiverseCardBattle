using System.Collections.Generic;

namespace MultiverseCardBattle.Targeting
{
    public static class TargetResolver
    {
        public static List<TargetCandidate> Resolve(
            TargetRule rule,
            int ownerPlayerIndex,
            List<TargetCandidate> candidates)
        {
            List<TargetCandidate> results = new List<TargetCandidate>();

            if (candidates == null)
                return results;

            foreach (TargetCandidate candidate in candidates)
            {
                bool isAlly =
                    candidate.PlayerIndex == ownerPlayerIndex;

                bool isEnemy =
                    candidate.PlayerIndex != ownerPlayerIndex;

                switch (rule)
                {
                    case TargetRule.None:
                        break;

                    case TargetRule.Self:
                        if (isAlly && candidate.IsUnit)
                            results.Add(candidate);
                        break;

                    case TargetRule.AllyUnit:
                        if (isAlly && candidate.IsUnit)
                            results.Add(candidate);
                        break;

                    case TargetRule.AllyLeader:
                        if (isAlly && candidate.IsLeader)
                            results.Add(candidate);
                        break;

                    case TargetRule.EnemyUnit:
                        if (isEnemy && candidate.IsUnit)
                            results.Add(candidate);
                        break;

                    case TargetRule.EnemyLeader:
                        if (isEnemy && candidate.IsLeader)
                            results.Add(candidate);
                        break;

                    case TargetRule.AnyUnit:
                        if (candidate.IsUnit)
                            results.Add(candidate);
                        break;

                    case TargetRule.AnyLeader:
                        if (candidate.IsLeader)
                            results.Add(candidate);
                        break;

                    case TargetRule.AllAllies:
                        if (isAlly && candidate.IsUnit)
                            results.Add(candidate);
                        break;

                    case TargetRule.AllEnemies:
                        if (isEnemy && candidate.IsUnit)
                            results.Add(candidate);
                        break;

                    case TargetRule.AllUnits:
                        if (candidate.IsUnit)
                            results.Add(candidate);
                        break;
                }
            }

            return results;
        }

        /// <summary>
        /// ตัดยูนิตศัตรูที่มี Stealth ออก (ใช้กับการเลือกเป้าหมายแบบ Chosen/Random)
        /// </summary>
        public static List<TargetCandidate> RemoveHiddenTargets(
            List<TargetCandidate> candidates,
            int ownerPlayerIndex)
        {
            List<TargetCandidate> results = new List<TargetCandidate>();

            if (candidates == null)
                return results;

            foreach (TargetCandidate candidate in candidates)
            {
                if (candidate.IsUnit &&
                    candidate.PlayerIndex != ownerPlayerIndex)
                {
                    MultiverseCardBattle.Cards.CardInstance card =
                        candidate.TargetObject as MultiverseCardBattle.Cards.CardInstance;

                    if (card != null &&
                        card.HasKeyword(MultiverseCardBattle.Cards.Keyword.Stealth))
                    {
                        continue;
                    }
                }

                results.Add(candidate);
            }

            return results;
        }
    }
}