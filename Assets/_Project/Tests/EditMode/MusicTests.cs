using NUnit.Framework;
using UnityEngine;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Tests
{
    public class MusicTests
    {
        [Test]
        public void MenuAndBattleTracks_AreDifferentClips()
        {
            AudioClip menu = Sfx.GetMusicClip(MusicTrack.Menu);
            AudioClip battle = Sfx.GetMusicClip(MusicTrack.Battle);

            Assert.IsNotNull(menu);
            Assert.IsNotNull(battle);
            Assert.AreNotSame(menu, battle);
        }

        [Test]
        public void BattleTrack_IsAudibleAndNotClipping()
        {
            AudioClip battle = Sfx.GetMusicClip(MusicTrack.Battle);

            // 4 คอร์ด x 16 สเต็ป x 0.2 วินาที
            Assert.AreEqual(12.8f, battle.length, 0.1f);

            float[] data = new float[battle.samples];

            Assert.IsTrue(battle.GetData(data, 0));

            float peak = 0f;

            foreach (float sample in data)
                peak = Mathf.Max(peak, Mathf.Abs(sample));

            Assert.Greater(peak, 0.2f);
            Assert.LessOrEqual(peak, 0.95f);
        }
    }
}
