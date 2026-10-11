namespace MultiverseCardBattle.Data
{
    /// <summary>ระดับความยาก AI (ผู้เล่นเลือกได้จากเมนูหลัก)</summary>
    public enum AiDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    public static class AiDifficultyInfo
    {
        public static readonly string[] Names = { "Easy", "Normal", "Hard" };

        public static string NameOf(AiDifficulty difficulty)
        {
            int index = (int)difficulty;

            return index >= 0 && index < Names.Length ? Names[index] : "Normal";
        }

        public static AiDifficulty FromInt(int value)
        {
            if (value <= 0)
                return AiDifficulty.Easy;

            return value >= 2 ? AiDifficulty.Hard : AiDifficulty.Normal;
        }

        /// <summary>ตัวคูณ HP ของ Leader ศัตรู</summary>
        public static float EnemyHealthScale(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy: return 0.8f;
                case AiDifficulty.Hard: return 1.25f;
                default: return 1f;
            }
        }

        /// <summary>ตัวคูณ Gold ที่ได้ตอนชนะ (ยากขึ้นได้มากขึ้น)</summary>
        public static float GoldScale(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy: return 0.7f;
                case AiDifficulty.Hard: return 1.5f;
                default: return 1f;
            }
        }

        public static string Describe(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy:
                    return "Opponent plays randomly and makes mistakes. Enemy HP x0.8, Gold x0.7.";
                case AiDifficulty.Hard:
                    return "Opponent plans lethal and trades smartly. Enemy HP x1.25, Gold x1.5.";
                default:
                    return "Balanced opponent.";
            }
        }
    }
}
