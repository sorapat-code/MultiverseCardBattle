using System.Collections.Generic;
using UnityEngine;

namespace MultiverseCardBattle.Core
{
    public enum SoundId
    {
        Click,
        Play,
        Hit,
        Heal,
        Win,
        Lose,
        Pack
    }

    /// <summary>
    /// เสียงเอฟเฟกต์สร้างจากโค้ด (คลื่นเสียงสังเคราะห์) จึงไม่ต้องมีไฟล์เสียง
    /// เปลี่ยนเป็นไฟล์เสียงจริงทีหลังได้โดยแก้เฉพาะคลาสนี้
    /// </summary>
    public static class Sfx
    {
        private const int SampleRate = 22050;
        private const float Volume = 0.5f;

        private static readonly Dictionary<SoundId, AudioClip> clips =
            new Dictionary<SoundId, AudioClip>();

        private static AudioSource source;
        private static AudioSource musicSource;

        private const float MusicVolume = 0.16f;

        public static bool Muted
        {
            get { return SaveManager.Profile.Data.SoundMuted; }
            set { SaveManager.Profile.Data.SoundMuted = value; }
        }

        public static void Play(SoundId id)
        {
            if (Muted)
                return;

            if (!EnsureSource())
                return;

            AudioClip clip = GetClip(id);

            if (clip != null)
                source.PlayOneShot(clip, Volume);
        }

        /// <summary>
        /// เพลงพื้นหลังเรียบง่าย (อาร์เปจโจคอร์ด Am-F-C-G วน) สร้างจากโค้ด เล่นต่อเนื่องข้ามซีน
        /// </summary>
        public static void StartMusic()
        {
            if (musicSource == null)
            {
                GameObject go = new GameObject("Music");

                Object.DontDestroyOnLoad(go);

                musicSource = go.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.spatialBlend = 0f;
                musicSource.volume = MusicVolume;

                // วางไฟล์เพลงไว้ที่ Assets/_Project/Resources/Music/bgm (.mp3/.ogg/.wav)
                // เกมจะใช้ไฟล์นั้นแทนเพลงที่สร้างจากโค้ด
                AudioClip custom = Resources.Load<AudioClip>("Music/bgm");

                musicSource.clip = custom != null ? custom : BuildMusic();
            }

            musicSource.mute = Muted;

            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        /// <summary>
        /// เรียกหลังเปลี่ยนการเปิด/ปิดเสียง
        /// </summary>
        public static void RefreshMusicMute()
        {
            if (musicSource != null)
                musicSource.mute = Muted;
        }

        private static AudioClip BuildMusic()
        {
            // คอร์ดละ 4 วินาที: ตัวโน้ต 4 ตัวของคอร์ด (ต่ำ -> สูง)
            float[][] chords =
            {
                new[] { 220.00f, 261.63f, 329.63f, 440.00f },   // Am
                new[] { 174.61f, 220.00f, 261.63f, 349.23f },   // F
                new[] { 261.63f, 329.63f, 392.00f, 523.25f },   // C
                new[] { 196.00f, 246.94f, 293.66f, 392.00f }    // G
            };

            int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 3 };

            const float noteSeconds = 0.5f;

            int noteSamples = Mathf.RoundToInt(noteSeconds * SampleRate);
            int chordSamples = noteSamples * pattern.Length;

            float[] data = new float[chordSamples * chords.Length];

            for (int c = 0; c < chords.Length; c++)
            {
                int chordStart = c * chordSamples;

                // เสียงรองพื้นต่ำๆ ตลอดคอร์ด (ค่อยๆ ดังและเบาลง)
                float padFrequency = chords[c][0] * 0.5f;

                for (int i = 0; i < chordSamples; i++)
                {
                    float t = (float)i / chordSamples;

                    float padEnvelope = Mathf.Sin(Mathf.PI * t);

                    float pad =
                        Mathf.Sin(2f * Mathf.PI * padFrequency * i / SampleRate) *
                        padEnvelope * 0.25f;

                    data[chordStart + i] += pad;
                }

                // โน้ตอาร์เปจโจ
                for (int n = 0; n < pattern.Length; n++)
                {
                    float frequency = chords[c][pattern[n]];

                    int noteStart = chordStart + n * noteSamples;

                    for (int i = 0; i < noteSamples; i++)
                    {
                        float t = (float)i / noteSamples;

                        float envelope = Mathf.Pow(1f - t, 2f) *
                            Mathf.Min(1f, i / (SampleRate * 0.01f));

                        float tone =
                            Mathf.Sin(2f * Mathf.PI * frequency * i / SampleRate) +
                            0.3f * Mathf.Sin(4f * Mathf.PI * frequency * i / SampleRate);

                        data[noteStart + i] += tone * envelope * 0.28f;
                    }
                }
            }

            AudioClip clip =
                AudioClip.Create("music", data.Length, 1, SampleRate, false);

            clip.SetData(data, 0);

            return clip;
        }

        private static bool EnsureSource()
        {
            if (source != null)
                return true;

            GameObject go = new GameObject("Sfx");

            Object.DontDestroyOnLoad(go);

            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            return true;
        }

        private static AudioClip GetClip(SoundId id)
        {
            AudioClip clip;

            if (clips.TryGetValue(id, out clip) && clip != null)
                return clip;

            clip = Create(id);

            clips[id] = clip;

            return clip;
        }

        // (ความถี่เริ่ม, ความถี่จบ, ความยาววินาที, สัดส่วนเสียงซ่า 0-1)
        private static AudioClip Create(SoundId id)
        {
            switch (id)
            {
                case SoundId.Click:
                    return Build("click", (900f, 700f, 0.05f, 0f));

                case SoundId.Play:
                    return Build("play",
                        (300f, 600f, 0.12f, 0f),
                        (600f, 900f, 0.08f, 0f));

                case SoundId.Hit:
                    return Build("hit", (180f, 60f, 0.18f, 0.5f));

                case SoundId.Heal:
                    return Build("heal",
                        (500f, 900f, 0.15f, 0f),
                        (900f, 1200f, 0.1f, 0f));

                case SoundId.Win:
                    return Build("win",
                        (523f, 523f, 0.12f, 0f),
                        (659f, 659f, 0.12f, 0f),
                        (784f, 784f, 0.12f, 0f),
                        (1046f, 1046f, 0.3f, 0f));

                case SoundId.Lose:
                    return Build("lose",
                        (400f, 200f, 0.3f, 0f),
                        (200f, 100f, 0.4f, 0f));

                default:
                    return Build("pack",
                        (700f, 1400f, 0.2f, 0f),
                        (1400f, 1400f, 0.1f, 0f));
            }
        }

        private static AudioClip Build(
            string name,
            params (float start, float end, float seconds, float noise)[] parts)
        {
            int total = 0;

            foreach (var part in parts)
                total += Mathf.Max(1, Mathf.RoundToInt(part.seconds * SampleRate));

            float[] data = new float[total];

            int index = 0;
            float phase = 0f;

            foreach (var part in parts)
            {
                int length = Mathf.Max(1, Mathf.RoundToInt(part.seconds * SampleRate));

                for (int i = 0; i < length; i++)
                {
                    float t = (float)i / length;

                    float frequency = Mathf.Lerp(part.start, part.end, t);

                    phase += 2f * Mathf.PI * frequency / SampleRate;

                    float tone = Mathf.Sin(phase);
                    float noise = Random.value * 2f - 1f;

                    float sample = tone * (1f - part.noise) + noise * part.noise;

                    // ค่อยๆ เบาลง และไม่ให้เกิดเสียงแตกตอนเริ่ม
                    float envelope = Mathf.Pow(1f - t, 1.5f);

                    float attack = Mathf.Min(1f, i / (SampleRate * 0.005f));

                    data[index++] = sample * envelope * attack * 0.6f;
                }
            }

            AudioClip clip = AudioClip.Create(name, total, 1, SampleRate, false);

            clip.SetData(data, 0);

            return clip;
        }
    }
}
