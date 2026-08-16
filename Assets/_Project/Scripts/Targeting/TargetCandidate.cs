namespace MultiverseCardBattle.Targeting
{
    public class TargetCandidate
    {
        public int PlayerIndex;

        public bool IsLeader;
        public bool IsUnit;

        public object TargetObject;

        public TargetCandidate(
            int playerIndex,
            bool isLeader,
            bool isUnit,
            object targetObject)
        {
            PlayerIndex = playerIndex;
            IsLeader = isLeader;
            IsUnit = isUnit;
            TargetObject = targetObject;
        }
    }
}