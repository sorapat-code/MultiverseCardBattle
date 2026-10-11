namespace MultiverseCardBattle.Cards
{
    /// <summary>
    /// ชุดการ์ด (แต่ละชุดมีซองของตัวเอง) ใส่ต่อท้ายเพื่อไม่ให้ค่า Enum ของการ์ดเดิมเลื่อน
    /// </summary>
    public enum CardSet
    {
        // ชุดเริ่มต้น
        Core = 0,

        // ชุดใหม่: Rift Pack
        Rift = 1
    }

    public static class CardSetInfo
    {
        public static string Name(CardSet set)
        {
            switch (set)
            {
                case CardSet.Rift: return "Rift Pack";
                default: return "Core Pack";
            }
        }

        public static int PackPrice(CardSet set)
        {
            switch (set)
            {
                case CardSet.Rift: return 150;
                default: return 100;
            }
        }

        public static string Description(CardSet set)
        {
            switch (set)
            {
                case CardSet.Rift:
                    return "Heroes and monsters torn through the Rift from new universes.";
                default:
                    return "The original multiverse roster.";
            }
        }
    }
}
