using UnityEngine;

namespace AIBeat.Audio
{
    /// <summary>
    /// 프로시저럴 방식으로 히트 사운드를 생성하는 유틸리티
    /// 외부 에셋 없이 코드만으로 사운드 생성
    /// </summary>
    public static class ProceduralSoundGenerator
    {
        private const int SAMPLE_RATE = 44100;
        private const int SAMPLE_COUNT = 22050; // 0.5초

        /// <summary>
        /// 사인파 + 하모닉으로打撃音 생성
        /// </summary>
        public static AudioClip CreateHitSound(float frequency, float duration, float decay, float harmonics)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("HitSound", samples, 1, SAMPLE_RATE, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float envelope = Mathf.Exp(-t * decay);
                
                // 기본 주파수 + 하모닉
                float sample = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope;
                sample += Mathf.Sin(2 * Mathf.PI * frequency * 2 * t) * envelope * harmonics * 0.5f;
                sample += Mathf.Sin(2 * Mathf.PI * frequency * 3 * t) * envelope * harmonics * 0.25f;
                
                // 노이즈 추가 (초반에만)
                if (t < 0.05f)
                {
                    sample += (Random.value * 2 - 1) * envelope * 0.3f;
                }
                
                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Perfect 판정용 사운드 (높은 음, 빠른 디케이)
        /// </summary>
        public static AudioClip CreatePerfectSound()
        {
            return CreateHitSound(880f, 0.3f, 15f, 0.8f); // A5
        }

        /// <summary>
        /// Great 판정용 사운드 (중간 높이)
        /// </summary>
        public static AudioClip CreateGreatSound()
        {
            return CreateHitSound(659f, 0.25f, 12f, 0.6f); // E5
        }

        /// <summary>
        /// Good 판정용 사운드 (중간 높이, 느린 디케이)
        /// </summary>
        public static AudioClip CreateGoodSound()
        {
            return CreateHitSound(523f, 0.2f, 10f, 0.5f); // C5
        }

        /// <summary>
        /// Bad 판정용 사운드 (낮은 음)
        /// </summary>
        public static AudioClip CreateBadSound()
        {
            return CreateHitSound(330f, 0.15f, 8f, 0.3f); // E4
        }

        /// <summary>
        /// 스크래치 사운드 (노이즈 기반)
        /// </summary>
        public static AudioClip CreateScratchSound()
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * 0.4f);
            AudioClip clip = AudioClip.Create("ScratchSound", samples, 1, SAMPLE_RATE, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float envelope = Mathf.Exp(-t * 8f);
                
                // 백색 잡음 + 저역 통과 필터 효과
                float noise = (Random.value * 2 - 1);
                float filtered = noise * 0.7f + (i > 0 ? data[i - 1] * 0.3f : 0);
                
                data[i] = filtered * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 카운트다운 비프음
        /// </summary>
        public static AudioClip CreateBeepSound(float frequency, float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("Beep", samples, 1, SAMPLE_RATE, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                // 짧은 attack, 짧은 decay
                float envelope = t < 0.01f ? t / 0.01f : Mathf.Exp(-(t - 0.01f) * 10f);
                data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * 0.8f;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}