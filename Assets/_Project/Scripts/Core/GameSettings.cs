using UnityEngine;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Core
{
    /// <summary>
    /// ตั้งค่าเกม (เสียง / จอภาพ / เกมเพลย์) เก็บในเซฟ แล้วนำไปใช้ผ่าน Apply()
    /// </summary>
    public static class GameSettings
    {
        public static readonly int[] FpsOptions = { 0, 30, 60, 120, 144 };
        public static readonly float[] AnimOptions = { 0.5f, 1f, 1.5f, 2f };
        public static readonly string[] AiSpeedNames = { "Slow", "Normal", "Fast" };
        private static readonly float[] AiDelays = { 0.6f * 1.8f, 0.6f, 0.6f * 0.4f };

        private static SaveData Data
        {
            get { return SaveManager.Profile.Data; }
        }

        public static float Master
        {
            get { return Mathf.Clamp01(Data.MasterVolume); }
            set { Data.MasterVolume = Mathf.Clamp01(value); }
        }

        public static float Music
        {
            get { return Mathf.Clamp01(Data.MusicVolume); }
            set { Data.MusicVolume = Mathf.Clamp01(value); }
        }

        public static float Sfx
        {
            get { return Mathf.Clamp01(Data.SfxVolume); }
            set { Data.SfxVolume = Mathf.Clamp01(value); }
        }

        public static bool Fullscreen
        {
            get { return Data.Fullscreen; }
            set { Data.Fullscreen = value; }
        }

        public static bool VSync
        {
            get { return Data.VSync; }
            set { Data.VSync = value; }
        }

        public static int FpsLimit
        {
            get { return Data.FpsLimit; }
            set { Data.FpsLimit = value; }
        }

        public static float AnimSpeed
        {
            get { return Mathf.Clamp(Data.AnimSpeed, 0.25f, 4f); }
            set { Data.AnimSpeed = value; }
        }

        public static int AiSpeed
        {
            get { return Mathf.Clamp(Data.AiSpeed, 0, AiDelays.Length - 1); }
            set { Data.AiSpeed = Mathf.Clamp(value, 0, AiDelays.Length - 1); }
        }

        /// <summary>เวลาหน่วงระหว่างแอ็กชันของ AI (วินาที)</summary>
        public static float AiActionDelay
        {
            get { return AiDelays[AiSpeed]; }
        }

        /// <summary>คัดลอกเฉพาะค่าตั้งค่าจากเซฟอื่น (ใช้ตอน Reset Progress เพื่อไม่ให้ตั้งค่าหาย)</summary>
        public static void CopyFrom(SaveData source)
        {
            if (source == null)
                return;

            SaveData d = Data;

            d.SoundMuted = source.SoundMuted;
            d.MasterVolume = source.MasterVolume;
            d.MusicVolume = source.MusicVolume;
            d.SfxVolume = source.SfxVolume;
            d.Fullscreen = source.Fullscreen;
            d.VSync = source.VSync;
            d.FpsLimit = source.FpsLimit;
            d.AnimSpeed = source.AnimSpeed;
            d.AiSpeed = source.AiSpeed;
        }

        public static void ResetToDefaults()
        {
            SaveData fresh = new SaveData();

            CopyFrom(fresh);
        }

        /// <summary>นำค่าที่ตั้งไว้ไปใช้กับจอภาพและเสียง</summary>
        public static void Apply()
        {
            if (Screen.fullScreen != Fullscreen)
                Screen.fullScreen = Fullscreen;

            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FpsLimit > 0 ? FpsLimit : -1;

            global::MultiverseCardBattle.Core.Sfx.RefreshVolumes();
        }
    }
}
