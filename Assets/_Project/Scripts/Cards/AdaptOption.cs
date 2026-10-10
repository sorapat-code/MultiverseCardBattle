using System.Collections.Generic;

namespace MultiverseCardBattle.Cards
{
    /// <summary>
    /// ตัวเลือกของ Adapt: เพิ่ม Attack / Health หรือ Keyword ให้การ์ด
    /// </summary>
    public class AdaptOption
    {
        public string Label;
        public int Attack;
        public int Health;
        public Keyword Keyword;

        public AdaptOption(string label, int attack, int health, Keyword keyword)
        {
            Label = label;
            Attack = attack;
            Health = health;
            Keyword = keyword;
        }

        public string Describe()
        {
            return Label;
        }

        /// <summary>ตัวเลือกทั้งหมดที่ Adapt สุ่มมาให้เลือก</summary>
        public static List<AdaptOption> CreatePool()
        {
            List<AdaptOption> pool = new List<AdaptOption>();

            pool.Add(new AdaptOption("+3 Attack", 3, 0, Keyword.None));
            pool.Add(new AdaptOption("+3 Health", 0, 3, Keyword.None));
            pool.Add(new AdaptOption("Shield", 0, 0, Keyword.Shield));
            pool.Add(new AdaptOption("Taunt", 0, 0, Keyword.Taunt));
            pool.Add(new AdaptOption("Lifesteal", 0, 0, Keyword.Lifesteal));
            pool.Add(new AdaptOption("Poison", 0, 0, Keyword.Poison));
            pool.Add(new AdaptOption("Quick Attack", 0, 0, Keyword.QuickAttack));
            pool.Add(new AdaptOption("Pierce", 0, 0, Keyword.Pierce));
            pool.Add(new AdaptOption("Reborn", 0, 0, Keyword.Reborn));
            pool.Add(new AdaptOption("Double Strike", 0, 0, Keyword.DoubleStrike));

            return pool;
        }

        /// <summary>สุ่มตัวเลือกที่ไม่ซ้ำกัน count ตัว</summary>
        public static List<AdaptOption> Roll(int count, System.Random rng)
        {
            List<AdaptOption> pool = CreatePool();
            List<AdaptOption> picked = new List<AdaptOption>();

            if (count < 1)
                count = 1;

            while (picked.Count < count && pool.Count > 0)
            {
                int index = rng.Next(pool.Count);

                picked.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return picked;
        }
    }
}
