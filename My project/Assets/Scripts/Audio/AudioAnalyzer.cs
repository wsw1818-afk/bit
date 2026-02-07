using UnityEngine;
using System;

namespace AIBeat.Audio
{
    /// <summary>
    /// 오디오 스펙트럼 분석 및 BPM 동기화
    /// </summary>
    public class AudioAnalyzer : MonoBehaviour
    {
        [Header("Analysis Settings")]
        [SerializeField] private int spectrumSize = 1024;
        [SerializeField] private FFTWindow fftWindow = FFTWindow.Blackman;

        [Header("Beat Detection")]
        [SerializeField] private float beatThreshold = 0.5f;
        [SerializeField] private float beatCooldown = 0.1f;

        private AudioSource audioSource;
        private float[] spectrumData;
        private float[] frequencyBands;
        private float[] bandBuffer;
        private float[] bufferDecrease;

        private float lastBeatTime;
        private bool isAnalyzing;

        // 8개의 주파수 밴드
        private const int BAND_COUNT = 8;

        public float[] FrequencyBands => frequencyBands;
        public float BeatStrength => GetBeatStrength();

        public event Action OnBeatDetected;

        private void Awake()
        {
            spectrumData = new float[spectrumSize];
            frequencyBands = new float[BAND_COUNT];
            bandBuffer = new float[BAND_COUNT];
            bufferDecrease = new float[BAND_COUNT];
        }

        public void Initialize(AudioSource source)
        {
            audioSource = source;
            isAnalyzing = true;
        }

        public void StopAnalysis()
        {
            isAnalyzing = false;
        }

        private void Update()
        {
            if (!isAnalyzing || audioSource == null || !audioSource.isPlaying)
                return;

            AnalyzeSpectrum();
            DetectBeat();
        }

        private void AnalyzeSpectrum()
        {
            audioSource.GetSpectrumData(spectrumData, 0, fftWindow);

            // 스펙트럼 데이터를 8개 주파수 밴드로 분류
            // Band 0: Sub Bass (20-60 Hz)
            // Band 1: Bass (60-250 Hz)
            // Band 2: Low Mid (250-500 Hz)
            // Band 3: Mid (500-2k Hz)
            // Band 4: High Mid (2k-4k Hz)
            // Band 5: Presence (4k-6k Hz)
            // Band 6: Brilliance (6k-12k Hz)
            // Band 7: Air (12k-20k Hz)

            int sampleRate = AudioSettings.outputSampleRate;
            float freqPerBin = (float)sampleRate / 2 / spectrumSize;

            for (int i = 0; i < BAND_COUNT; i++)
            {
                float sum = 0;
                int count = 0;

                float minFreq = GetBandMinFrequency(i);
                float maxFreq = GetBandMaxFrequency(i);

                int minBin = Mathf.Max(0, Mathf.FloorToInt(minFreq / freqPerBin));
                int maxBin = Mathf.Min(spectrumSize - 1, Mathf.CeilToInt(maxFreq / freqPerBin));

                for (int j = minBin; j <= maxBin; j++)
                {
                    sum += spectrumData[j];
                    count++;
                }

                float average = count > 0 ? sum / count : 0;
                frequencyBands[i] = average * 100; // 스케일 조정

                // 버퍼 처리 (부드러운 감소)
                if (frequencyBands[i] > bandBuffer[i])
                {
                    bandBuffer[i] = frequencyBands[i];
                    bufferDecrease[i] = 0.005f;
                }
                else
                {
                    bandBuffer[i] -= bufferDecrease[i];
                    bufferDecrease[i] *= 1.2f;
                    bandBuffer[i] = Mathf.Max(0, bandBuffer[i]);
                }
            }
        }

        private float GetBandMinFrequency(int band)
        {
            return band switch
            {
                0 => 20,
                1 => 60,
                2 => 250,
                3 => 500,
                4 => 2000,
                5 => 4000,
                6 => 6000,
                7 => 12000,
                _ => 0
            };
        }

        private float GetBandMaxFrequency(int band)
        {
            return band switch
            {
                0 => 60,
                1 => 250,
                2 => 500,
                3 => 2000,
                4 => 4000,
                5 => 6000,
                6 => 12000,
                7 => 20000,
                _ => 0
            };
        }

        private float GetBeatStrength()
        {
            // Bass + Sub Bass의 평균 강도
            return (frequencyBands[0] + frequencyBands[1]) / 2f;
        }

        private void DetectBeat()
        {
            float currentTime = Time.time;
            if (currentTime - lastBeatTime < beatCooldown)
                return;

            float strength = GetBeatStrength();
            if (strength > beatThreshold)
            {
                lastBeatTime = currentTime;
                OnBeatDetected?.Invoke();
            }
        }

        /// <summary>
        /// 현재 시간에 가장 강한 주파수 밴드 인덱스 반환
        /// </summary>
        public int GetDominantBand()
        {
            int maxIndex = 0;
            float maxValue = 0;

            for (int i = 0; i < BAND_COUNT; i++)
            {
                if (frequencyBands[i] > maxValue)
                {
                    maxValue = frequencyBands[i];
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// 특정 밴드의 버퍼링된 값 (비주얼라이저용)
        /// </summary>
        public float GetBufferedBand(int index)
        {
            if (index < 0 || index >= BAND_COUNT) return 0;
            return bandBuffer[index];
        }

        /// <summary>
        /// 주파수 밴드를 레인 인덱스로 매핑
        /// </summary>
        public int MapBandToLane(int band)
        {
            // 4개 레인: 0=ScratchL, 1=Key1, 2=Key2, 3=ScratchR
            return band switch
            {
                0 => UnityEngine.Random.Range(0, 2) == 0 ? 0 : 3, // Sub Bass → 스크래치
                7 => UnityEngine.Random.Range(0, 2) == 0 ? 0 : 3, // Air → 스크래치
                1 => 1, // Bass → Key1
                5 => 1, // Presence → Key1
                2 => UnityEngine.Random.Range(1, 3), // Low Mid → Key1 or Key2
                4 => UnityEngine.Random.Range(1, 3), // High Mid → Key1 or Key2
                3 => 2, // Mid → Key2
                6 => UnityEngine.Random.Range(1, 3), // Brilliance → Key1 or Key2
                _ => UnityEngine.Random.Range(1, 3)
            };
        }
    }
}
