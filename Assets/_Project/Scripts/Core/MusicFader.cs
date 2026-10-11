using UnityEngine;

namespace MultiverseCardBattle.Core
{
    /// <summary>
    /// เปลี่ยนเพลงแบบค่อยๆ เบาลงแล้วดังขึ้น (ใช้เวลาแบบ unscaled)
    /// ใช้งานผ่าน Sfx.PlayMusic เท่านั้น
    /// </summary>
    public class MusicFader : MonoBehaviour
    {
        private const float FadeSeconds = 0.5f;

        private AudioSource source;
        private AudioClip pendingClip;
        private float targetVolume;

        public void Setup(AudioSource audioSource)
        {
            source = audioSource;
        }

        /// <summary>เปลี่ยนระดับเสียงเป้าหมายโดยไม่เปลี่ยนเพลง</summary>
        public void SetVolume(float volume)
        {
            targetVolume = volume;
        }

        public void FadeTo(AudioClip clip, float volume)
        {
            pendingClip = clip;
            targetVolume = volume;
        }

        private void Update()
        {
            if (source == null)
                return;

            float step = Time.unscaledDeltaTime / FadeSeconds * targetVolume;

            if (pendingClip != null)
            {
                // ยังไม่มีเพลงเล่นอยู่ ก็เปลี่ยนได้ทันที
                if (source.clip == null || !source.isPlaying)
                    source.volume = 0f;

                source.volume = Mathf.Max(0f, source.volume - step);

                if (source.volume <= 0.001f)
                {
                    source.clip = pendingClip;
                    source.volume = 0f;
                    source.Play();

                    pendingClip = null;
                }

                return;
            }

            if (source.volume < targetVolume)
                source.volume = Mathf.Min(targetVolume, source.volume + step);
            else if (source.volume > targetVolume)
                source.volume = targetVolume;
        }
    }
}
