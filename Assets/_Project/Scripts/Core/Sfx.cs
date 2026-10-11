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

    public enum MusicTrack
    {
        Menu,
        Battle
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

        private const float MenuVolume = 0.16f;
        private const float BattleVolume = 0.14f;

        private static MusicFader musicFader;
        private static bool musicStarted;
        private static MusicTrack currentTrack;

        private static readonly Dictionary<MusicTrack, AudioClip> musicClips =
            new Dictionary<MusicTrack, AudioClip>();

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
                source.PlayOneShot(clip, Volume * GameSettings.Master * GameSettings.Sfx);
        }

        /// <summary>
        /// เล่นเพลงเมนู (เก็บไว้เพื่อความเข้ากันได้กับโค้ดเดิม)
        /// </summary>
        public static void StartMusic()
        {
            PlayMusic(MusicTrack.Menu);
        }

        /// <summary>
        /// เปลี่ยนเพลงพื้นหลังตามหน้าจอ: เมนูเป็นเพลงสงบ ตอนต่อสู้เป็นเพลงจังหวะเร็ว
        /// เพลงทั้งสองสร้างจากโค้ด เล่นต่อเนื่องข้ามซีน และไม่เริ่มใหม่ถ้าเพลงเดิมอยู่แล้ว
        /// วางไฟล์เพลงจริงไว้ที่ Assets/_Project/Resources/Music/ ชื่อ menu และ battle
        /// (.mp3/.ogg/.wav) เกมจะใช้ไฟล์นั้นแทน (menu ใช้ bgm เป็นชื่อสำรอง)
        /// </summary>
        public static void PlayMusic(MusicTrack track)
        {
            EnsureMusicSource();

            musicSource.mute = Muted;

            if (musicStarted && currentTrack == track)
            {
                if (!musicSource.isPlaying)
                    musicSource.Play();

                return;
            }

            musicStarted = true;
            currentTrack = track;

            musicFader.FadeTo(
                GetMusicClip(track),
                MusicLevel(track));
        }

        private static float MusicLevel(MusicTrack track)
        {
            float baseVolume = track == MusicTrack.Battle ? BattleVolume : MenuVolume;

            return baseVolume * GameSettings.Master * GameSettings.Music;
        }

        /// <summary>เรียกหลังเปลี่ยนสไลเดอร์เสียง ให้เพลงปรับระดับตามทันที</summary>
        public static void RefreshVolumes()
        {
            if (musicFader != null && musicStarted)
                musicFader.SetVolume(MusicLevel(currentTrack));
        }

        private static void EnsureMusicSource()
        {
            if (musicSource != null && musicFader != null)
                return;

            GameObject go = new GameObject("Music");

            Object.DontDestroyOnLoad(go);

            musicSource = go.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0f;

            musicFader = go.AddComponent<MusicFader>();
            musicFader.Setup(musicSource);

            musicStarted = false;
        }

        public static AudioClip GetMusicClip(MusicTrack track)
        {
            AudioClip clip;

            if (musicClips.TryGetValue(track, out clip) && clip != null)
                return clip;

            if (track == MusicTrack.Battle)
            {
                clip = Resources.Load<AudioClip>("Music/battle");

                if (clip == null)
                    clip = BuildBattleMusic();
            }
            else
            {
                clip = Resources.Load<AudioClip>("Music/menu");

                if (clip == null)
                    clip = Resources.Load<AudioClip>("Music/bgm");

                if (clip == null)
                    clip = BuildMenuMusic();
            }

            musicClips[track] = clip;

            return clip;
        }

        /// <summary>
        /// เรียกหลังเปลี่ยนการเปิด/ปิดเสียง
        /// </summary>
        public static void RefreshMusicMute()
        {
            if (musicSource != null)
                musicSource.mute = Muted;
        }

        // เพลงเมนู: อาร์เปจโจสงบๆ คอร์ด Am-F-C-G วน
        private static AudioClip BuildMenuMusic()
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

        // เพลงต่อสู้: จังหวะเร็ว 150 BPM คีย์ Dm มีเบส กลอง เบสไลน์ และอาร์เปจโจ
        private static AudioClip BuildBattleMusic()
        {
            // 3 โน้ตต่อคอร์ด + โน้ตเบส: Dm - Bb - C - A
            float[][] chords =
            {
                new[] { 293.66f, 349.23f, 440.00f },
                new[] { 233.08f, 293.66f, 349.23f },
                new[] { 261.63f, 329.63f, 392.00f },
                new[] { 220.00f, 277.18f, 329.63f }
            };

            float[] bass = { 73.42f, 58.27f, 65.41f, 55.00f };

            int[] lead = { 2, 1, 2, 0, 2, 1, 0, 1 };

            const float stepSeconds = 0.2f;   // โน้ตเขบ็ต 1 ชั้น ที่ 150 BPM
            const int stepsPerBar = 8;
            const int barsPerChord = 2;

            int stepSamples = Mathf.RoundToInt(stepSeconds * SampleRate);
            int chordSteps = stepsPerBar * barsPerChord;
            int totalSteps = chordSteps * chords.Length;

            float[] data = new float[stepSamples * totalSteps];

            System.Random random = new System.Random(7);

            for (int step = 0; step < totalSteps; step++)
            {
                int chord = step / chordSteps;
                int stepInBar = step % stepsPerBar;
                int start = step * stepSamples;

                // เบส: ทุกจังหวะย่อย เน้นจังหวะแรกของห้อง
                float bassFrequency = bass[chord] * (stepInBar % 4 == 3 ? 2f : 1f);
                float bassGain = stepInBar == 0 ? 0.42f : 0.28f;

                AddTone(data, start, stepSamples, bassFrequency,
                    bassGain, 2.5f, 0.6f, 0.2f);

                // ทำนองอาร์เปจโจ
                float leadFrequency = chords[chord][lead[stepInBar]];

                if (step % chordSteps >= stepsPerBar)
                    leadFrequency *= 2f;   // ห้องที่สองสูงขึ้นหนึ่งออกเตฟ

                AddTone(data, start, stepSamples, leadFrequency,
                    0.13f, 1.6f, 0.5f, 0.33f);

                // กลองใหญ่ทุกจังหวะ
                if (stepInBar % 2 == 0)
                    AddKick(data, start, stepSamples);

                // สแนร์ที่จังหวะ 2 และ 4
                if (stepInBar == 2 || stepInBar == 6)
                    AddNoise(data, start, stepSamples, 0.11f, 0.3f, random, 190f);

                // ไฮแฮตที่จังหวะย่อย
                if (stepInBar % 2 == 1)
                    AddNoise(data, start, stepSamples, 0.035f, 0.1f, random, 0f);
            }

            // ปรับระดับเสียงไม่ให้แตก
            float peak = 0f;

            for (int i = 0; i < data.Length; i++)
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));

            if (peak > 0.9f)
            {
                float scale = 0.9f / peak;

                for (int i = 0; i < data.Length; i++)
                    data[i] *= scale;
            }

            AudioClip clip =
                AudioClip.Create("battle_music", data.Length, 1, SampleRate, false);

            clip.SetData(data, 0);

            return clip;
        }

        // โน้ตเสียงเดียวที่ค่อยๆ เบาลง พร้อมโอเวอร์โทนที่สอง/สาม
        private static void AddTone(
            float[] data, int start, int length, float frequency,
            float gain, float decay, float second, float third)
        {
            for (int i = 0; i < length && start + i < data.Length; i++)
            {
                float t = (float)i / length;

                float envelope = Mathf.Pow(1f - t, decay) *
                    Mathf.Min(1f, i / (SampleRate * 0.005f));

                float phase = 2f * Mathf.PI * frequency * i / SampleRate;

                float tone =
                    Mathf.Sin(phase) +
                    second * Mathf.Sin(2f * phase) +
                    third * Mathf.Sin(3f * phase);

                data[start + i] += tone * envelope * gain;
            }
        }

        // กลองใหญ่: ความถี่ลดจากสูงไปต่ำอย่างรวดเร็ว
        private static void AddKick(float[] data, int start, int stepSamples)
        {
            int length = Mathf.Min(stepSamples, Mathf.RoundToInt(0.16f * SampleRate));

            float phase = 0f;

            for (int i = 0; i < length && start + i < data.Length; i++)
            {
                float t = (float)i / length;

                float frequency = Mathf.Lerp(130f, 42f, Mathf.Sqrt(t));

                phase += 2f * Mathf.PI * frequency / SampleRate;

                float envelope = Mathf.Pow(1f - t, 2f);

                data[start + i] += Mathf.Sin(phase) * envelope * 0.55f;
            }
        }

        // เสียงซ่า (สแนร์/ไฮแฮต) ผสมโทนต่ำเล็กน้อยถ้าระบุความถี่
        private static void AddNoise(
            float[] data, int start, int stepSamples, float seconds,
            float gain, System.Random random, float toneFrequency)
        {
            int length = Mathf.Min(stepSamples, Mathf.RoundToInt(seconds * SampleRate));

            float previous = 0f;

            for (int i = 0; i < length && start + i < data.Length; i++)
            {
                float t = (float)i / length;

                float white = (float)(random.NextDouble() * 2.0 - 1.0);

                // ลบค่าก่อนหน้าออกให้เหลือเสียงแหลม (ไฮพาสอย่างง่าย)
                float high = white - previous * 0.6f;

                previous = white;

                float sample = high;

                if (toneFrequency > 0f)
                {
                    sample = high * 0.7f +
                        Mathf.Sin(2f * Mathf.PI * toneFrequency * i / SampleRate) * 0.5f;
                }

                data[start + i] += sample * Mathf.Pow(1f - t, 2f) * gain;
            }
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
