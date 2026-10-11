using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Core
{
    /// <summary>
    /// เพลงต่อสู้และเสียงบรรยากาศ (ลม ฝน ไฟ คลื่น ฯลฯ) แยกตามธีมของแต่ละด่าน
    /// สร้างจากโค้ดทั้งหมดเหมือนเสียงอื่นในเกม จึงไม่ต้องมีไฟล์เสียง
    /// วางไฟล์ Resources/Music/battle_stage_05 (.mp3/.ogg/.wav) เพื่อใช้เพลงจริงแทนของด่านนั้นได้
    /// </summary>
    public static partial class Sfx
    {
        private const float AmbienceBase = 0.55f;

        private static string currentThemeKey = "";

        private static AudioSource ambientSource;
        private static MusicFader ambientFader;
        private static string ambientKey;
        private static bool ambientActive;

        private static readonly Dictionary<string, AudioClip> battleClips =
            new Dictionary<string, AudioClip>();

        private static readonly Dictionary<string, AudioClip> ambientClips =
            new Dictionary<string, AudioClip>();

        // คีย์ธีมของแมตช์ที่กำลังจะเล่น: ด่านใน Story = id ของด่าน, Tutorial = ด่านแรก,
        // Quick Battle = "" (ใช้เพลงต่อสู้ดั้งเดิม)
        private static string BattleMusicKey()
        {
            if (BattleSetup.Tutorial)
                return "stage_01";

            CampaignStage stage = BattleSetup.Stage;

            return stage != null && !string.IsNullOrEmpty(stage.Id) ? stage.Id : "";
        }

        // ---------- เพลงต่อสู้ตามธีม ----------

        private class BattleStyle
        {
            public float Step = 0.2f;           // ความยาวโน้ตหนึ่งช่อง (วินาที)
            public int[][] Chords;              // MIDI ของโน้ต 3 ตัวต่อคอร์ด
            public int[] Bass;                  // MIDI ของโน้ตเบสต่อคอร์ด
            public int[] Lead = { 0, 1, 2, 1, 2, 1, 0, 1 };   // ดัชนีโน้ตในคอร์ด (-1 = เงียบ)
            public float LeadGain = 0.13f;
            public float LeadDecay = 1.6f;
            public float Second = 0.5f;         // โอเวอร์โทนที่ 2 / 3 (ความ "สด" ของเสียง)
            public float Third = 0.3f;
            public float BassGain = 0.3f;
            public float PadGain = 0.06f;
            public float KickGain = 0.5f;
            public float Bar2Octave = 2f;       // ห้องที่สองของคอร์ดสูงขึ้นกี่เท่า
            public string Kick = "x.x.x.x.";
            public string Snare = "..x...x.";
            public string Hat = ".x.x.x.x";
            public int Seed = 7;
        }

        private static float Hz(int midi)
        {
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        private static BattleStyle GetBattleStyle(string key)
        {
            switch (key)
            {
                case "stage_01": // ป่ายามสาย: ฮีโรอิกสดใส
                    return new BattleStyle
                    {
                        Step = 0.21f,
                        Chords = new[]
                        {
                            new[] { 60, 64, 67 }, new[] { 59, 62, 67 },
                            new[] { 57, 60, 64 }, new[] { 57, 60, 65 }
                        },
                        Bass = new[] { 36, 31, 33, 29 },
                        Lead = new[] { 0, 1, 2, 1, 2, 1, 0, 1 },
                        Second = 0.3f, Third = 0.1f,
                        PadGain = 0.07f,
                        Kick = "x...x...", Snare = "....x...", Hat = "..x...x."
                    };

                case "stage_02": // Bandit Road: ควบม้าในทะเลทราย
                    return new BattleStyle
                    {
                        Step = 0.17f,
                        Chords = new[]
                        {
                            new[] { 62, 65, 69 }, new[] { 63, 67, 70 },
                            new[] { 62, 65, 69 }, new[] { 60, 64, 67 }
                        },
                        Bass = new[] { 38, 39, 38, 36 },
                        Lead = new[] { 2, -1, 1, 2, -1, 1, 0, 1 },
                        Second = 0.7f, Third = 0.4f,
                        Kick = "x..x..x.", Snare = "..x...x.", Hat = "x.x.x.xx"
                    };

                case "stage_03": // Shadow Alley: ซินธ์นัวร์ยามค่ำคืน
                    return new BattleStyle
                    {
                        Step = 0.22f,
                        Chords = new[]
                        {
                            new[] { 60, 63, 67 }, new[] { 56, 60, 63 },
                            new[] { 58, 62, 65 }, new[] { 55, 59, 62 }
                        },
                        Bass = new[] { 36, 32, 34, 31 },
                        Lead = new[] { 0, -1, 1, -1, 2, -1, 1, -1 },
                        Second = 0.8f, Third = 0.6f,
                        BassGain = 0.36f, PadGain = 0.1f,
                        Kick = "x...x...", Snare = "....x...", Hat = "x.x.x.x."
                    };

                case "stage_04": // Temple Siege: กลองศึกยิ่งใหญ่
                    return new BattleStyle
                    {
                        Step = 0.2f,
                        Chords = new[]
                        {
                            new[] { 64, 67, 71 }, new[] { 60, 64, 67 },
                            new[] { 62, 66, 69 }, new[] { 59, 63, 66 }
                        },
                        Bass = new[] { 40, 36, 38, 35 },
                        Lead = new[] { 0, 2, 1, 2, 0, 2, 1, 2 },
                        Second = 0.5f, Third = 0.35f,
                        KickGain = 0.7f, PadGain = 0.08f,
                        Kick = "x.x.x.x.", Snare = "..x...x.", Hat = "........"
                    };

                case "stage_05": // Mage Tower: อาร์เปจโจเวทมนตร์
                    return new BattleStyle
                    {
                        Step = 0.24f,
                        Chords = new[]
                        {
                            new[] { 57, 60, 64 }, new[] { 53, 57, 60 },
                            new[] { 50, 57, 62 }, new[] { 52, 56, 59 }
                        },
                        Bass = new[] { 33, 29, 26, 28 },
                        Lead = new[] { 0, 1, 2, 1, 2, 1, 0, 2 },
                        LeadGain = 0.15f, LeadDecay = 2.6f,
                        Second = 0.2f, Third = 0.05f,
                        PadGain = 0.11f, KickGain = 0.35f,
                        Kick = "x.......", Snare = "........", Hat = "..x...x."
                    };

                case "stage_06": // Titan Gate: เดินทัพหนักแน่น
                    return new BattleStyle
                    {
                        Step = 0.23f,
                        Chords = new[]
                        {
                            new[] { 55, 58, 62 }, new[] { 51, 55, 58 },
                            new[] { 53, 57, 60 }, new[] { 55, 58, 62 }
                        },
                        Bass = new[] { 31, 27, 29, 31 },
                        Lead = new[] { 0, -1, 0, -1, 2, -1, 1, -1 },
                        Second = 0.6f, Third = 0.1f,
                        BassGain = 0.45f, PadGain = 0.09f, KickGain = 0.65f,
                        Bar2Octave = 1f,
                        Kick = "x...x...", Snare = "..x...x.", Hat = "........"
                    };

                case "stage_07": // Dragon Peak: ดุดันรวดเร็ว
                    return new BattleStyle
                    {
                        Step = 0.16f,
                        Chords = new[]
                        {
                            new[] { 64, 67, 71 }, new[] { 65, 69, 72 },
                            new[] { 64, 67, 71 }, new[] { 62, 66, 69 }
                        },
                        Bass = new[] { 40, 41, 40, 38 },
                        Lead = new[] { 2, 1, 2, 0, 2, 1, 0, 1 },
                        Second = 0.9f, Third = 0.5f,
                        BassGain = 0.38f, KickGain = 0.6f,
                        Kick = "x.xxx.x.", Snare = "..x...x.", Hat = "x.x.x.x."
                    };

                case "stage_08": // Grand Line: เพลงลูกเรือสนุกสนาน
                    return new BattleStyle
                    {
                        Step = 0.2f,
                        Chords = new[]
                        {
                            new[] { 62, 66, 69 }, new[] { 59, 62, 67 },
                            new[] { 57, 61, 64 }, new[] { 62, 66, 69 }
                        },
                        Bass = new[] { 38, 31, 33, 38 },
                        Lead = new[] { 0, 2, 1, 2, 0, 1, 2, 1 },
                        Second = 0.4f, Third = 0.2f,
                        PadGain = 0.05f,
                        Kick = "x...x...", Snare = "..x...x.", Hat = "x.x.x.x."
                    };

                case "stage_09": // Soul Reaper's Trial: โนคเทิร์นเสียงพิณ
                    return new BattleStyle
                    {
                        Step = 0.26f,
                        Chords = new[]
                        {
                            new[] { 57, 60, 64 }, new[] { 55, 59, 62 },
                            new[] { 53, 57, 60 }, new[] { 52, 56, 59 }
                        },
                        Bass = new[] { 33, 31, 29, 28 },
                        Lead = new[] { 0, -1, 2, -1, 1, -1, 2, 1 },
                        LeadGain = 0.16f, LeadDecay = 3.2f,
                        Second = 0.15f, Third = 0.05f,
                        PadGain = 0.07f, KickGain = 0.4f,
                        Kick = "x.......", Snare = "........", Hat = "..x...x."
                    };

                case "stage_10": // Multiverse Sovereign: ศึกสุดท้าย
                    return new BattleStyle
                    {
                        Step = 0.17f,
                        Chords = new[]
                        {
                            new[] { 62, 65, 69 }, new[] { 58, 62, 65 },
                            new[] { 60, 64, 67 }, new[] { 57, 61, 64 }
                        },
                        Bass = new[] { 38, 34, 36, 33 },
                        Lead = new[] { 2, 1, 2, 0, 2, 1, 0, 1 },
                        LeadGain = 0.14f,
                        Second = 0.7f, Third = 0.4f,
                        BassGain = 0.36f, PadGain = 0.12f, KickGain = 0.6f,
                        Kick = "x.x.x.x.", Snare = "..x...x.", Hat = "xxxxxxxx"
                    };
            }

            return null;
        }

        private static AudioClip GetBattleMusicClip(string key)
        {
            if (key == null)
                key = "";

            AudioClip clip;

            if (battleClips.TryGetValue(key, out clip) && clip != null)
                return clip;

            // ไฟล์เพลงจริงของด่านนั้นก่อน แล้วค่อยไฟล์ battle ทั่วไป
            clip = key.Length > 0
                ? Resources.Load<AudioClip>("Music/battle_" + key)
                : null;

            if (clip == null)
                clip = Resources.Load<AudioClip>("Music/battle");

            if (clip == null)
            {
                BattleStyle style = GetBattleStyle(key);

                clip = style != null
                    ? BuildStyledMusic("battle_" + key, style)
                    : BuildBattleMusic();
            }

            battleClips[key] = clip;

            return clip;
        }

        private static AudioClip BuildStyledMusic(string name, BattleStyle s)
        {
            const int stepsPerBar = 8;
            const int barsPerChord = 2;

            int stepSamples = Mathf.RoundToInt(s.Step * SampleRate);
            int chordSteps = stepsPerBar * barsPerChord;
            int totalSteps = chordSteps * s.Chords.Length;

            float[] data = new float[stepSamples * totalSteps];

            System.Random random = new System.Random(s.Seed);

            // แพดเสียงยาวของคอร์ด (ค่อยๆ ดังและเบาลง)
            if (s.PadGain > 0f)
            {
                int chordSamples = stepSamples * chordSteps;

                for (int c = 0; c < s.Chords.Length; c++)
                {
                    for (int n = 0; n < s.Chords[c].Length; n++)
                    {
                        float freq = Hz(s.Chords[c][n]);
                        int begin = c * chordSamples;

                        for (int i = 0; i < chordSamples; i++)
                        {
                            float t = (float)i / chordSamples;

                            data[begin + i] +=
                                Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate) *
                                Mathf.Sin(Mathf.PI * t) * s.PadGain;
                        }
                    }
                }
            }

            for (int step = 0; step < totalSteps; step++)
            {
                int chord = step / chordSteps;
                int stepInBar = step % stepsPerBar;
                int start = step * stepSamples;

                float bassFrequency = Hz(s.Bass[chord]) * (stepInBar % 4 == 3 ? 2f : 1f);
                float bassGain = stepInBar == 0 ? s.BassGain * 1.4f : s.BassGain;

                AddTone(data, start, stepSamples, bassFrequency,
                    bassGain, 2.5f, 0.6f, 0.2f);

                int leadIndex = s.Lead[stepInBar];

                if (leadIndex >= 0)
                {
                    float leadFrequency = Hz(s.Chords[chord][leadIndex % 3]);

                    if (step % chordSteps >= stepsPerBar)
                        leadFrequency *= s.Bar2Octave;

                    AddTone(data, start, stepSamples, leadFrequency,
                        s.LeadGain, s.LeadDecay, s.Second, s.Third);
                }

                if (PatternHit(s.Kick, stepInBar))
                    AddKick(data, start, stepSamples, s.KickGain);

                if (PatternHit(s.Snare, stepInBar))
                    AddNoise(data, start, stepSamples, 0.11f, 0.28f, random, 190f);

                if (PatternHit(s.Hat, stepInBar))
                    AddNoise(data, start, stepSamples, 0.035f, 0.09f, random, 0f);
            }

            Normalize(data, 0.9f);

            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);

            clip.SetData(data, 0);

            return clip;
        }

        private static bool PatternHit(string pattern, int index)
        {
            return !string.IsNullOrEmpty(pattern) &&
                index < pattern.Length && pattern[index] == 'x';
        }

        // ปรับให้ค่าสูงสุดไม่เกินที่กำหนด (เฉพาะกรณีที่เกิน)
        private static void Normalize(float[] data, float limit)
        {
            float peak = 0f;

            for (int i = 0; i < data.Length; i++)
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));

            if (peak <= limit)
                return;

            float scale = limit / peak;

            for (int i = 0; i < data.Length; i++)
                data[i] *= scale;
        }

        // ---------- เสียงบรรยากาศ ----------

        private static void UpdateAmbience(MusicTrack track)
        {
            if (track != MusicTrack.Battle)
            {
                if (ambientFader != null && ambientActive)
                    ambientFader.FadeOut();

                ambientActive = false;
                ambientKey = null;

                return;
            }

            string key = AmbienceKey(currentThemeKey);

            if (ambientActive && ambientKey == key)
                return;

            EnsureAmbientSource();

            ambientSource.mute = Muted;

            ambientKey = key;
            ambientActive = true;

            ambientFader.FadeTo(GetAmbienceClip(key), AmbienceLevel());
        }

        // Quick Battle ("") ใช้บรรยากาศป่าเหมือนธีมภาพ
        private static string AmbienceKey(string themeKey)
        {
            return string.IsNullOrEmpty(themeKey) ? "stage_01" : themeKey;
        }

        private static float AmbienceLevel()
        {
            return AmbienceBase * 0.5f * GameSettings.Master * GameSettings.Sfx;
        }

        private static void RefreshAmbienceVolume()
        {
            if (ambientFader != null && ambientActive)
                ambientFader.SetVolume(AmbienceLevel());
        }

        private static void EnsureAmbientSource()
        {
            if (ambientSource != null && ambientFader != null)
                return;

            GameObject go = new GameObject("Ambience");

            Object.DontDestroyOnLoad(go);

            ambientSource = go.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
            ambientSource.volume = 0f;

            ambientFader = go.AddComponent<MusicFader>();
            ambientFader.Setup(ambientSource);
        }

        private static AudioClip GetAmbienceClip(string key)
        {
            AudioClip clip;

            if (ambientClips.TryGetValue(key, out clip) && clip != null)
                return clip;

            clip = Resources.Load<AudioClip>("Music/ambience_" + key);

            if (clip == null)
                clip = BuildAmbience(key);

            ambientClips[key] = clip;

            return clip;
        }

        // วนเล่นได้เนียน: ผสมปลายคลิปเข้ากับต้นคลิป
        private static AudioClip BuildAmbience(string key)
        {
            const float seconds = 8f;

            int loop = Mathf.RoundToInt(seconds * SampleRate);
            int fade = SampleRate / 2;

            float[] x = new float[loop + fade];

            System.Random r = new System.Random(key.Length * 131 + 17);

            switch (key)
            {
                case "stage_02": // ลมทะเลทราย + กองไฟ
                    AddWind(x, r, 0.16f, 0.03f, 0.13f, 0.6f);
                    AddWind(x, r, 0.07f, 0.09f, 0.31f, 0.8f);
                    AddCrackle(x, r, 3f, 0.05f);
                    break;

                case "stage_03": // ฝนตกในซอย + ฮัมนีออน
                    AddRain(x, r, 0.07f, 16);
                    AddRumble(x, 55f, 0.05f);
                    AddRumble(x, 120f, 0.012f);
                    break;

                case "stage_04": // วิหาร: เสียงประสานต่ำ + ระฆัง
                    AddDrone(x, new[] { 110f, 164.8f, 220f }, 0.05f, 0.15f);
                    AddWind(x, r, 0.05f, 0.015f, 0.1f, 0.5f);
                    AddPings(x, r, new[] { 523f, 659f, 784f }, 3, 0.07f, 2.2f);
                    break;

                case "stage_05": // หอคอยเวทมนตร์: ฮัมอาร์เคน + ประกายเวท
                    AddDrone(x, new[] { 220f, 221.6f, 330f, 331.4f }, 0.045f, 0.2f);
                    AddPings(x, r, new[] { 1318f, 1568f, 1976f, 2349f }, 16, 0.045f, 0.9f);
                    AddWind(x, r, 0.03f, 0.012f, 0.08f, 0.5f);
                    break;

                case "stage_06": // ลมหนาวหวีดหวิว + เสียงกึกก้อง
                    AddWind(x, r, 0.2f, 0.02f, 0.09f, 0.7f);
                    AddHowl(x, 380f, 0.035f);
                    AddRumble(x, 40f, 0.06f);
                    break;

                case "stage_07": // ภูเขาไฟ: ไฟปะทุ + เสียงคำราม
                    AddCrackle(x, r, 16f, 0.11f);
                    AddRumble(x, 45f, 0.09f);
                    AddWind(x, r, 0.06f, 0.05f, 0.17f, 0.6f);
                    break;

                case "stage_08": // ทะเล: คลื่น + นกนางนวล
                    AddWaves(x, r, 0.2f, 6.5f);
                    AddGulls(x, r, 4, 0.05f);
                    AddWind(x, r, 0.04f, 0.03f, 0.2f, 0.5f);
                    break;

                case "stage_09": // คืนจันทร์: ลมเย็น + ระฆังไกลๆ
                    AddWind(x, r, 0.09f, 0.02f, 0.11f, 0.6f);
                    AddBell(x, 2 * SampleRate, 196f, 0.1f, 4.5f);
                    AddBell(x, 5 * SampleRate + SampleRate / 2, 247f, 0.07f, 4f);
                    AddPings(x, r, new[] { 880f, 1175f }, 4, 0.025f, 1.4f);
                    break;

                case "stage_10": // จักรวาล: เสียงก้องลึก + ประกายดาว
                    AddDrone(x, new[] { 55f, 82.4f, 110f, 165f }, 0.06f, 0.1f);
                    AddHowl(x, 220f, 0.025f);
                    AddPings(x, r, new[] { 1046f, 1568f, 2093f }, 10, 0.04f, 1.6f);
                    break;

                default: // ป่า: ลมอ่อนๆ + นกร้อง
                    AddWind(x, r, 0.1f, 0.02f, 0.12f, 0.6f);
                    AddBirds(x, r, 7, 0.05f);
                    break;
            }

            float[] data = new float[loop];

            for (int i = 0; i < loop; i++)
                data[i] = x[i];

            for (int i = 0; i < fade; i++)
            {
                float t = (float)i / fade;

                data[i] = x[i] * t + x[loop + i] * (1f - t);
            }

            Normalize(data, 0.8f);

            AudioClip clip = AudioClip.Create(
                "ambience_" + key, data.Length, 1, SampleRate, false);

            clip.SetData(data, 0);

            return clip;
        }

        // ลม: เสียงซ่าผ่านตัวกรองต่ำ (cutoff ยิ่งน้อยยิ่งทุ้ม) พองขึ้นลงช้าๆ
        private static void AddWind(
            float[] x, System.Random r, float gain, float cutoff,
            float swellHz, float depth)
        {
            float y1 = 0f;
            float y2 = 0f;
            float phase = (float)r.NextDouble() * 6.28f;

            for (int i = 0; i < x.Length; i++)
            {
                float white = (float)(r.NextDouble() * 2.0 - 1.0);

                y1 += cutoff * (white - y1);
                y2 += cutoff * (y1 - y2);

                float t = (float)i / SampleRate;

                float swell = 1f - depth +
                    depth * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * swellHz * t + phase));

                x[i] += y2 * swell * gain * 9f;
            }
        }

        // เสียงหวีดของลม: ไซน์ที่ความถี่แกว่งช้าๆ
        private static void AddHowl(float[] x, float baseHz, float gain)
        {
            float phase = 0f;

            for (int i = 0; i < x.Length; i++)
            {
                float t = (float)i / SampleRate;

                float freq = baseHz *
                    (1f + 0.12f * Mathf.Sin(2f * Mathf.PI * 0.17f * t) +
                     0.05f * Mathf.Sin(2f * Mathf.PI * 0.41f * t));

                phase += 2f * Mathf.PI * freq / SampleRate;

                float amp = Mathf.Sin(Mathf.PI * 0.125f * t);

                x[i] += Mathf.Sin(phase) * amp * amp * gain;
            }
        }

        private static void AddRumble(float[] x, float hz, float gain)
        {
            for (int i = 0; i < x.Length; i++)
            {
                float t = (float)i / SampleRate;

                float mod = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.23f * t);

                x[i] += (Mathf.Sin(2f * Mathf.PI * hz * t) +
                         0.5f * Mathf.Sin(2f * Mathf.PI * hz * 1.5f * t)) * mod * gain;
            }
        }

        private static void AddDrone(float[] x, float[] freqs, float gain, float tremoloHz)
        {
            for (int i = 0; i < x.Length; i++)
            {
                float t = (float)i / SampleRate;

                float sum = 0f;

                for (int n = 0; n < freqs.Length; n++)
                    sum += Mathf.Sin(2f * Mathf.PI * freqs[n] * t);

                float trem = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * tremoloHz * t);

                x[i] += sum / freqs.Length * trem * gain * 2f;
            }
        }

        // ฝน: เสียงซ่าแหลม + หยดน้ำสุ่ม
        private static void AddRain(float[] x, System.Random r, float gain, int drips)
        {
            float previous = 0f;

            for (int i = 0; i < x.Length; i++)
            {
                float white = (float)(r.NextDouble() * 2.0 - 1.0);

                float high = white - previous * 0.85f;

                previous = white;

                x[i] += high * gain * 0.6f;
            }

            AddPings(x, r, new[] { 1800f, 2400f, 3100f }, drips, gain * 0.5f, 0.05f);
        }

        // เสียงเปรี๊ยะของไฟ: กลุ่มเสียงซ่าสั้นๆ สุ่มตำแหน่ง
        private static void AddCrackle(float[] x, System.Random r, float perSecond, float gain)
        {
            int count = Mathf.RoundToInt(perSecond * x.Length / SampleRate);

            for (int c = 0; c < count; c++)
            {
                int start = r.Next(0, x.Length);
                int length = Mathf.RoundToInt((0.006f + (float)r.NextDouble() * 0.04f) * SampleRate);
                float amp = gain * (0.3f + (float)r.NextDouble() * 0.7f);

                for (int i = 0; i < length && start + i < x.Length; i++)
                {
                    float t = (float)i / length;

                    x[start + i] +=
                        (float)(r.NextDouble() * 2.0 - 1.0) * amp * (1f - t) * (1f - t);
                }
            }
        }

        // คลื่น: เสียงซ่าทุ้มที่ดังขึ้นแล้วเบาลงเป็นจังหวะ พร้อมฟองคลื่น
        private static void AddWaves(float[] x, System.Random r, float gain, float period)
        {
            float y1 = 0f;
            float y2 = 0f;

            for (int i = 0; i < x.Length; i++)
            {
                float white = (float)(r.NextDouble() * 2.0 - 1.0);

                y1 += 0.06f * (white - y1);
                y2 += 0.5f * (white - y2);

                float t = (float)i / SampleRate;

                float wave = Mathf.Sin(Mathf.PI * t / period);

                wave = wave * wave;

                x[i] += (y1 * 9f * (0.25f + 0.75f * wave) +
                         (white - y2) * 0.5f * wave * wave) * gain;
            }
        }

        // ระฆัง: ไซน์พร้อมโอเวอร์โทนไม่เป็นจังหวะ ค่อยๆ เบาลง
        private static void AddBell(
            float[] x, int start, float freq, float gain, float decaySeconds)
        {
            int length = Mathf.RoundToInt(decaySeconds * SampleRate);

            for (int i = 0; i < length && start + i < x.Length; i++)
            {
                float t = (float)i / SampleRate;

                float env = Mathf.Exp(-t * 3f / decaySeconds);

                float tone =
                    Mathf.Sin(2f * Mathf.PI * freq * t) +
                    0.5f * Mathf.Sin(2f * Mathf.PI * freq * 2.76f * t) +
                    0.25f * Mathf.Sin(2f * Mathf.PI * freq * 5.4f * t);

                x[start + i] += tone * env * gain * Mathf.Min(1f, i / (SampleRate * 0.004f));
            }
        }

        // เสียง "ติ๊ง" สั้นๆ สุ่มตำแหน่งและความถี่ (หยดน้ำ ประกายเวท)
        private static void AddPings(
            float[] x, System.Random r, float[] freqs, int count,
            float gain, float decaySeconds)
        {
            for (int c = 0; c < count; c++)
            {
                int start = r.Next(0, x.Length - SampleRate / 2);
                float freq = freqs[r.Next(0, freqs.Length)];
                float amp = gain * (0.4f + (float)r.NextDouble() * 0.6f);

                int length = Mathf.RoundToInt(decaySeconds * SampleRate);

                for (int i = 0; i < length && start + i < x.Length; i++)
                {
                    float t = (float)i / SampleRate;

                    float env = Mathf.Exp(-t * 4.5f / decaySeconds);

                    x[start + i] += Mathf.Sin(2f * Mathf.PI * freq * t) * env * amp *
                        Mathf.Min(1f, i / (SampleRate * 0.002f));
                }
            }
        }

        // นกร้อง: เสียงร้องสั้นๆ 2-3 พยางค์ไล่ระดับขึ้น
        private static void AddBirds(float[] x, System.Random r, int count, float gain)
        {
            for (int c = 0; c < count; c++)
            {
                int start = r.Next(0, x.Length - SampleRate);
                float baseFreq = 2400f + (float)r.NextDouble() * 1400f;
                int notes = 2 + r.Next(0, 2);

                for (int n = 0; n < notes; n++)
                {
                    int noteStart = start + n * Mathf.RoundToInt(0.11f * SampleRate);
                    int length = Mathf.RoundToInt(0.07f * SampleRate);
                    float phase = 0f;

                    for (int i = 0; i < length && noteStart + i < x.Length; i++)
                    {
                        float t = (float)i / length;

                        float freq = baseFreq * (1f + 0.35f * t) * (1f + 0.04f * n);

                        phase += 2f * Mathf.PI * freq / SampleRate;

                        x[noteStart + i] += Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t) * gain;
                    }
                }
            }
        }

        // นกนางนวล: เสียงยาวไล่ระดับลงพร้อมสั่น
        private static void AddGulls(float[] x, System.Random r, int count, float gain)
        {
            for (int c = 0; c < count; c++)
            {
                int length = Mathf.RoundToInt(0.45f * SampleRate);
                int start = r.Next(0, x.Length - length);
                float phase = 0f;

                for (int i = 0; i < length; i++)
                {
                    float t = (float)i / length;

                    float freq = Mathf.Lerp(1900f, 1150f, t) *
                        (1f + 0.04f * Mathf.Sin(2f * Mathf.PI * 22f * t));

                    phase += 2f * Mathf.PI * freq / SampleRate;

                    x[start + i] += Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t) * gain;
                }
            }
        }
    }
}
