using UnityEngine;

namespace MultiverseCardBattle.Core
{
    /// <summary>
    /// เสียงเอฟเฟกต์ชุดเพิ่มเติม (โจมตี ตาย สเปลแต่ละธาตุ จั่วการ์ด เริ่มเทิร์น ฯลฯ)
    /// สังเคราะห์จากโค้ดเหมือนเสียงอื่น โดยซ้อนโทนเสียง + เสียงซ่าหลายชั้น
    /// </summary>
    public static partial class Sfx
    {
        // คืนคลิปของเสียงชุดใหม่ (null = ไม่ใช่เสียงในชุดนี้)
        private static AudioClip CreateExtra(SoundId id)
        {
            System.Random r = new System.Random((int)id * 7919 + 13);

            switch (id)
            {
                case SoundId.Attack:
                {
                    float[] d = NewBuffer(0.24f);

                    Hiss(d, 0f, 0.18f, 0.55f, 0.5f, 0.05f, 0.35f, 2f, r);
                    Tone(d, 0f, 0.14f, 900f, 250f, 0.28f, 2f, 0.3f, 0f);
                    Tone(d, 0.08f, 0.1f, 160f, 70f, 0.3f, 2f, 0.2f, 0f);

                    return Finish("attack", d);
                }

                case SoundId.Death:
                {
                    float[] d = NewBuffer(0.55f);

                    Hiss(d, 0f, 0.4f, 0.6f, 0.25f, 0f, 0.02f, 2.5f, r);
                    Tone(d, 0f, 0.45f, 230f, 45f, 0.5f, 2f, 0.4f, 0.1f);

                    for (int i = 0; i < 6; i++)
                        Hiss(d, 0.05f + (float)r.NextDouble() * 0.3f, 0.025f, 0.35f, 1f, 0.3f, 0f, 1.5f, r);

                    return Finish("death", d);
                }

                case SoundId.Spell:
                {
                    float[] d = NewBuffer(0.7f);

                    Tone(d, 0f, 0.4f, 400f, 1400f, 0.22f, 1.2f, 0.3f, 0f);
                    Tone(d, 0.03f, 0.4f, 408f, 1420f, 0.18f, 1.2f, 0.3f, 0f);
                    Tone(d, 0.18f, 0.25f, 1568f, 1568f, 0.14f, 3f, 0.2f, 0f);
                    Tone(d, 0.25f, 0.25f, 2093f, 2093f, 0.12f, 3f, 0.2f, 0f);
                    Tone(d, 0.32f, 0.3f, 2637f, 2637f, 0.1f, 3f, 0.2f, 0f);

                    return Finish("spell", d);
                }

                case SoundId.SpellFire:
                {
                    float[] d = NewBuffer(0.7f);

                    Hiss(d, 0f, 0.55f, 0.6f, 0.35f, 0f, 0.15f, 1.5f, r);
                    Tone(d, 0f, 0.45f, 190f, 80f, 0.3f, 1.5f, 0.5f, 0f);

                    for (int i = 0; i < 12; i++)
                        Hiss(d, 0.05f + (float)r.NextDouble() * 0.5f, 0.02f, 0.45f, 1f, 0.3f, 0f, 1.5f, r);

                    return Finish("spell_fire", d);
                }

                case SoundId.SpellIce:
                {
                    float[] d = NewBuffer(0.7f);

                    Tone(d, 0f, 0.45f, 2093f, 2093f, 0.2f, 3f, 0.3f, 0f);
                    Tone(d, 0.06f, 0.45f, 2637f, 2637f, 0.18f, 3f, 0.3f, 0f);
                    Tone(d, 0.12f, 0.45f, 3136f, 3136f, 0.16f, 3f, 0.3f, 0f);
                    Tone(d, 0f, 0.3f, 900f, 1500f, 0.1f, 2f, 0.5f, 0.2f);
                    Hiss(d, 0f, 0.25f, 0.2f, 1f, 0.25f, 0.1f, 2f, r);

                    return Finish("spell_ice", d);
                }

                case SoundId.SpellLightning:
                {
                    float[] d = NewBuffer(0.7f);

                    Hiss(d, 0f, 0.09f, 0.85f, 1f, 0.1f, 0f, 1f, r);
                    Tone(d, 0f, 0.28f, 1900f, 200f, 0.3f, 1.5f, 0.2f, 0.7f);
                    Hiss(d, 0.13f, 0.18f, 0.45f, 0.6f, 0.05f, 0f, 2f, r);
                    Tone(d, 0.05f, 0.5f, 95f, 45f, 0.45f, 1.8f, 0.4f, 0f);

                    return Finish("spell_lightning", d);
                }

                case SoundId.SpellDark:
                {
                    float[] d = NewBuffer(0.8f);

                    Tone(d, 0f, 0.7f, 160f, 70f, 0.35f, 1.5f, 0.6f, 0.2f);
                    Tone(d, 0f, 0.7f, 169f, 76f, 0.3f, 1.5f, 0.6f, 0.2f);
                    Tone(d, 0.1f, 0.5f, 620f, 300f, 0.08f, 2f, 0.5f, 0f);
                    Hiss(d, 0f, 0.6f, 0.22f, 0.1f, 0f, 0.4f, 1.5f, r);

                    return Finish("spell_dark", d);
                }

                case SoundId.SpellWater:
                {
                    float[] d = NewBuffer(0.7f);

                    Tone(d, 0f, 0.08f, 300f, 720f, 0.3f, 1.5f, 0.1f, 0f);
                    Tone(d, 0.09f, 0.08f, 380f, 820f, 0.28f, 1.5f, 0.1f, 0f);
                    Tone(d, 0.19f, 0.09f, 330f, 760f, 0.26f, 1.5f, 0.1f, 0f);
                    Hiss(d, 0f, 0.5f, 0.28f, 0.3f, 0.02f, 0.3f, 1.5f, r);

                    return Finish("spell_water", d);
                }

                case SoundId.SpellWind:
                {
                    float[] d = NewBuffer(0.8f);

                    Hiss(d, 0f, 0.7f, 0.55f, 0.3f, 0.02f, 0.45f, 1.3f, r);
                    Tone(d, 0f, 0.7f, 520f, 940f, 0.05f, 1.2f, 0f, 0f);

                    return Finish("spell_wind", d);
                }

                case SoundId.SpellEarth:
                {
                    float[] d = NewBuffer(0.6f);

                    Tone(d, 0f, 0.4f, 95f, 40f, 0.6f, 2f, 0.5f, 0f);
                    Hiss(d, 0f, 0.35f, 0.5f, 0.12f, 0f, 0.02f, 2f, r);

                    for (int i = 0; i < 8; i++)
                        Hiss(d, 0.1f + (float)r.NextDouble() * 0.35f, 0.02f, 0.3f, 0.5f, 0.1f, 0f, 1.5f, r);

                    return Finish("spell_earth", d);
                }

                case SoundId.SpellLight:
                {
                    float[] d = NewBuffer(0.9f);

                    Tone(d, 0f, 0.6f, 880f, 1760f, 0.18f, 1.2f, 0.3f, 0f);
                    Tone(d, 0f, 0.8f, 523f, 523f, 0.12f, 0.8f, 0.2f, 0f);
                    Tone(d, 0.1f, 0.3f, 1760f, 1760f, 0.14f, 3f, 0.2f, 0f);
                    Tone(d, 0.18f, 0.3f, 2217f, 2217f, 0.12f, 3f, 0.2f, 0f);
                    Tone(d, 0.26f, 0.4f, 2637f, 2637f, 0.12f, 3f, 0.2f, 0f);

                    return Finish("spell_light", d);
                }

                case SoundId.Draw:
                {
                    float[] d = NewBuffer(0.18f);

                    Hiss(d, 0f, 0.11f, 0.45f, 1f, 0.15f, 0.3f, 1.5f, r);
                    Tone(d, 0.07f, 0.06f, 1500f, 1000f, 0.12f, 2f, 0f, 0f);

                    return Finish("draw", d);
                }

                case SoundId.TurnStart:
                {
                    float[] d = NewBuffer(0.75f);

                    Tone(d, 0f, 0.5f, 659f, 659f, 0.28f, 2.5f, 0.3f, 0.1f);
                    Tone(d, 0.13f, 0.55f, 880f, 880f, 0.28f, 2.5f, 0.3f, 0.1f);

                    return Finish("turn_start", d);
                }

                case SoundId.EndTurn:
                {
                    float[] d = NewBuffer(0.2f);

                    Tone(d, 0f, 0.16f, 140f, 55f, 0.6f, 2f, 0.3f, 0f);
                    Tone(d, 0f, 0.04f, 1800f, 1200f, 0.2f, 2f, 0f, 0f);
                    Hiss(d, 0f, 0.05f, 0.3f, 1f, 0.2f, 0f, 2f, r);

                    return Finish("end_turn", d);
                }

                case SoundId.Buff:
                {
                    float[] d = NewBuffer(0.55f);

                    Tone(d, 0f, 0.2f, 523f, 523f, 0.25f, 2.5f, 0.3f, 0f);
                    Tone(d, 0.07f, 0.2f, 659f, 659f, 0.25f, 2.5f, 0.3f, 0f);
                    Tone(d, 0.14f, 0.2f, 784f, 784f, 0.25f, 2.5f, 0.3f, 0f);
                    Tone(d, 0.21f, 0.3f, 1046f, 1046f, 0.25f, 2.5f, 0.3f, 0f);

                    return Finish("buff", d);
                }

                case SoundId.Error:
                {
                    float[] d = NewBuffer(0.3f);

                    Tone(d, 0f, 0.1f, 190f, 175f, 0.3f, 1f, 0f, 0.8f);
                    Tone(d, 0.12f, 0.13f, 150f, 138f, 0.3f, 1f, 0f, 0.8f);

                    return Finish("error", d);
                }

                case SoundId.Coin:
                {
                    float[] d = NewBuffer(0.55f);

                    Tone(d, 0f, 0.4f, 1318f, 1318f, 0.3f, 3f, 0.4f, 0f);
                    Tone(d, 0.06f, 0.45f, 1760f, 1760f, 0.3f, 3f, 0.4f, 0f);
                    Hiss(d, 0f, 0.03f, 0.3f, 1f, 0.3f, 0f, 2f, r);

                    return Finish("coin", d);
                }

                case SoundId.Skill:
                {
                    float[] d = NewBuffer(0.8f);

                    Tone(d, 0f, 0.55f, 200f, 900f, 0.3f, 0.8f, 0.4f, 0f);
                    Tone(d, 0f, 0.55f, 207f, 915f, 0.25f, 0.8f, 0.4f, 0f);
                    Tone(d, 0.35f, 0.4f, 1318f, 1318f, 0.2f, 3f, 0.3f, 0f);
                    Tone(d, 0.42f, 0.4f, 1760f, 1760f, 0.2f, 3f, 0.3f, 0f);
                    Hiss(d, 0f, 0.45f, 0.2f, 0.4f, 0f, 0.8f, 1.5f, r);

                    return Finish("skill", d);
                }

                case SoundId.Hover:
                {
                    float[] d = NewBuffer(0.04f);

                    Tone(d, 0f, 0.035f, 1400f, 1200f, 0.12f, 1.5f, 0f, 0f);

                    return Finish("hover", d);
                }

                case SoundId.Clang:
                {
                    float[] d = NewBuffer(0.5f);

                    Tone(d, 0f, 0.45f, 820f, 820f, 0.25f, 3f, 0.6f, 0.35f);
                    Tone(d, 0f, 0.4f, 1260f, 1260f, 0.15f, 3f, 0f, 0f);
                    Tone(d, 0f, 0.3f, 2150f, 2150f, 0.1f, 3f, 0f, 0f);
                    Hiss(d, 0f, 0.03f, 0.4f, 1f, 0.3f, 0f, 2f, r);

                    return Finish("clang", d);
                }
            }

            return null;
        }

        private static float[] NewBuffer(float seconds)
        {
            return new float[Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate))];
        }

        private static AudioClip Finish(string name, float[] data)
        {
            Normalize(data, 0.85f);

            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);

            clip.SetData(data, 0);

            return clip;
        }

        // โทนเสียง: เลื่อนความถี่ f0 -> f1 พร้อมโอเวอร์โทน h2/h3 ค่อยๆ เบาลงตาม decay
        private static void Tone(
            float[] d, float at, float seconds, float f0, float f1,
            float gain, float decay, float h2, float h3)
        {
            int start = Mathf.RoundToInt(at * SampleRate);
            int length = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));

            float phase = 0f;

            for (int i = 0; i < length && start + i < d.Length; i++)
            {
                float t = (float)i / length;

                float freq = Mathf.Lerp(f0, f1, t);

                phase += 2f * Mathf.PI * freq / SampleRate;

                float env = Mathf.Pow(1f - t, decay) *
                    Mathf.Min(1f, i / (SampleRate * 0.003f));

                float tone = Mathf.Sin(phase) +
                    h2 * Mathf.Sin(2f * phase) +
                    h3 * Mathf.Sin(3f * phase);

                d[start + i] += tone * env * gain;
            }
        }

        // เสียงซ่า: lowpass (1 = ไม่กรอง ยิ่งน้อยยิ่งทุ้ม) + highpass (0 = ไม่กรอง)
        // peak = ตำแหน่งที่ดังสุดของเอนเวโลป (0 = เริ่มดังทันที)
        private static void Hiss(
            float[] d, float at, float seconds, float gain, float lowpass,
            float highpass, float peak, float decay, System.Random r)
        {
            int start = Mathf.RoundToInt(at * SampleRate);
            int length = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));

            float low = 0f;
            float high = 0f;

            for (int i = 0; i < length && start + i < d.Length; i++)
            {
                float t = (float)i / length;

                float white = (float)(r.NextDouble() * 2.0 - 1.0);

                low += lowpass * (white - low);
                high += highpass * (low - high);

                float sample = low - high;

                float env = peak > 0.001f && t < peak
                    ? t / peak
                    : Mathf.Pow(Mathf.Clamp01(1f - (t - peak) / Mathf.Max(0.001f, 1f - peak)), decay);

                d[start + i] += sample * env * gain * (lowpass < 0.5f ? 3f : 1f);
            }
        }

        /// <summary>เสียงสเปลตามธาตุของการ์ด</summary>
        public static SoundId SpellSoundFor(MultiverseCardBattle.Cards.CardTag tags)
        {
            if ((tags & MultiverseCardBattle.Cards.CardTag.Fire) != 0) return SoundId.SpellFire;
            if ((tags & MultiverseCardBattle.Cards.CardTag.Ice) != 0) return SoundId.SpellIce;
            if ((tags & MultiverseCardBattle.Cards.CardTag.Lightning) != 0) return SoundId.SpellLightning;
            if ((tags & MultiverseCardBattle.Cards.CardTag.Dark) != 0) return SoundId.SpellDark;
            if ((tags & MultiverseCardBattle.Cards.CardTag.Water) != 0) return SoundId.SpellWater;
            if ((tags & MultiverseCardBattle.Cards.CardTag.Wind) != 0) return SoundId.SpellWind;
            if ((tags & MultiverseCardBattle.Cards.CardTag.Earth) != 0) return SoundId.SpellEarth;
            if ((tags & MultiverseCardBattle.Cards.CardTag.Light) != 0) return SoundId.SpellLight;

            return SoundId.Spell;
        }
    }
}
