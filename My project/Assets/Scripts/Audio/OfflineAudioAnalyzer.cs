using UnityEngine;
using System;
using System.Collections.Generic;

namespace AIBeat.Audio
{
    /// <summary>
    /// 오프라인 오디오 분석 시스템
    /// AudioClip의 PCM 데이터를 분석하여 BPM, 온셋, 에너지 프로필 추출
    /// </summary>
    public class OfflineAudioAnalyzer
    {
        // FFT 설정
        private const int FFT_SIZE = 2048;
        private const int HOP_SIZE = 512;

        // BPM 범위
        private const float MIN_BPM = 60f;
        private const float MAX_BPM = 200f;
        private const float PREFERRED_BPM_MIN = 120f;
        private const float PREFERRED_BPM_MAX = 160f;

        // 온셋 감지 설정
        private const float ONSET_THRESHOLD_ALPHA = 1.5f; // 적응형 임계값 배율
        private const int ONSET_MEDIAN_WINDOW = 7;         // 적응형 임계값 윈도우

        // 주파수 밴드 구간 (Hz)
        private static readonly float[] BAND_EDGES = { 0, 100, 200, 400, 800, 1600, 3200, 6400, 20000 };

        /// <summary>
        /// 분석 결과 데이터
        /// </summary>
        public class AnalysisResult
        {
            public float BPM;
            public float Duration;
            public List<OnsetData> Onsets;
            public List<SectionData> Sections;
            public float[] EnergyProfile;    // 프레임별 총 에너지
            public float[][] BandEnergies;   // [frame][band] 밴드별 에너지
        }

        public struct OnsetData
        {
            public float Time;       // 온셋 시간(초)
            public float Strength;   // 온셋 강도
            public int DominantBand; // 지배적 주파수 밴드 (0~7)
        }

        public struct SectionData
        {
            public float StartTime;
            public float EndTime;
            public SectionType Type;
            public float AverageEnergy;
        }

        public enum SectionType { Intro, Build, Drop, Outro, Calm }

        // FFT 작업 배열 (재사용)
        private float[] fftReal;
        private float[] fftImag;
        private float[] hannWindow;
        private float[] prevSpectrum;

        public OfflineAudioAnalyzer()
        {
            fftReal = new float[FFT_SIZE];
            fftImag = new float[FFT_SIZE];
            hannWindow = new float[FFT_SIZE];
            prevSpectrum = new float[FFT_SIZE / 2];

            // Hann 윈도우 미리 계산
            for (int i = 0; i < FFT_SIZE; i++)
                hannWindow[i] = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (FFT_SIZE - 1)));
        }

        /// <summary>
        /// AudioClip을 완전히 분석하여 결과 반환
        /// </summary>
        public AnalysisResult Analyze(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogError("[OfflineAudioAnalyzer] AudioClip is null!");
                return null;
            }

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // 모노 변환 (스테레오인 경우)
            float[] mono = ToMono(samples, clip.channels);
            float sampleRate = clip.frequency;
            float duration = clip.length;

            int totalFrames = (mono.Length - FFT_SIZE) / HOP_SIZE + 1;
            if (totalFrames <= 0)
            {
                Debug.LogError("[OfflineAudioAnalyzer] Audio too short for analysis");
                return null;
            }

#if UNITY_EDITOR
            Debug.Log($"[OfflineAudioAnalyzer] Analyzing: {clip.name}, duration={duration:F1}s, sampleRate={sampleRate}, frames={totalFrames}");
#endif

            // 1단계: FFT + 스펙트럼 분석 → 밴드 에너지 + Spectral Flux
            float[] spectralFlux = new float[totalFrames];
            float[] totalEnergy = new float[totalFrames];
            float[][] bandEnergies = new float[totalFrames][];
            int[] dominantBands = new int[totalFrames];

            Array.Clear(prevSpectrum, 0, prevSpectrum.Length);

            for (int frame = 0; frame < totalFrames; frame++)
            {
                int offset = frame * HOP_SIZE;

                // 윈도우 적용 + FFT 입력 준비
                for (int i = 0; i < FFT_SIZE; i++)
                {
                    fftReal[i] = (offset + i < mono.Length) ? mono[offset + i] * hannWindow[i] : 0f;
                    fftImag[i] = 0f;
                }

                // FFT 실행
                FFT(fftReal, fftImag, false);

                // 스펙트럼 계산 (magnitude)
                float[] spectrum = new float[FFT_SIZE / 2];
                for (int i = 0; i < FFT_SIZE / 2; i++)
                    spectrum[i] = Mathf.Sqrt(fftReal[i] * fftReal[i] + fftImag[i] * fftImag[i]);

                // Spectral Flux (Half-Wave Rectified)
                float flux = 0f;
                for (int i = 0; i < FFT_SIZE / 2; i++)
                {
                    float diff = spectrum[i] - prevSpectrum[i];
                    if (diff > 0) flux += diff;
                }
                spectralFlux[frame] = flux;

                // 밴드별 에너지
                bandEnergies[frame] = ComputeBandEnergies(spectrum, sampleRate);
                totalEnergy[frame] = 0f;
                float maxBandEnergy = 0f;
                int maxBand = 0;
                for (int b = 0; b < 8; b++)
                {
                    totalEnergy[frame] += bandEnergies[frame][b];
                    if (bandEnergies[frame][b] > maxBandEnergy)
                    {
                        maxBandEnergy = bandEnergies[frame][b];
                        maxBand = b;
                    }
                }
                dominantBands[frame] = maxBand;

                // 현재 스펙트럼을 이전으로 복사
                Array.Copy(spectrum, prevSpectrum, FFT_SIZE / 2);
            }

            // 2단계: 온셋 감지 (적응형 임계값)
            List<OnsetData> onsets = DetectOnsets(spectralFlux, dominantBands, sampleRate, totalFrames);
#if UNITY_EDITOR
            Debug.Log($"[OfflineAudioAnalyzer] Detected {onsets.Count} onsets");
#endif

            // 3단계: BPM 추출
            float bpm = EstimateBPM(onsets, duration);
#if UNITY_EDITOR
            Debug.Log($"[OfflineAudioAnalyzer] Estimated BPM: {bpm:F1}");
#endif

            // 4단계: 구간 감지
            List<SectionData> sections = DetectSections(totalEnergy, sampleRate, totalFrames, duration);
#if UNITY_EDITOR
            Debug.Log($"[OfflineAudioAnalyzer] Detected {sections.Count} sections");
#endif

            return new AnalysisResult
            {
                BPM = bpm,
                Duration = duration,
                Onsets = onsets,
                Sections = sections,
                EnergyProfile = totalEnergy,
                BandEnergies = bandEnergies
            };
        }

        /// <summary>
        /// 스테레오 → 모노 변환
        /// </summary>
        private float[] ToMono(float[] samples, int channels)
        {
            if (channels == 1) return samples;

            float[] mono = new float[samples.Length / channels];
            for (int i = 0; i < mono.Length; i++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++)
                    sum += samples[i * channels + c];
                mono[i] = sum / channels;
            }
            return mono;
        }

        /// <summary>
        /// 밴드별 에너지 계산 (8밴드)
        /// </summary>
        private float[] ComputeBandEnergies(float[] spectrum, float sampleRate)
        {
            float[] energies = new float[8];
            float freqResolution = sampleRate / FFT_SIZE;

            for (int band = 0; band < 8; band++)
            {
                int startBin = Mathf.Max(1, Mathf.FloorToInt(BAND_EDGES[band] / freqResolution));
                int endBin = Mathf.Min(FFT_SIZE / 2 - 1, Mathf.FloorToInt(BAND_EDGES[band + 1] / freqResolution));

                float energy = 0f;
                for (int bin = startBin; bin <= endBin; bin++)
                    energy += spectrum[bin] * spectrum[bin];

                energies[band] = energy;
            }
            return energies;
        }

        /// <summary>
        /// 적응형 임계값으로 온셋 감지
        /// </summary>
        private List<OnsetData> DetectOnsets(float[] flux, int[] bands, float sampleRate, int totalFrames)
        {
            var onsets = new List<OnsetData>();
            float frameTime = (float)HOP_SIZE / sampleRate;
            int halfWindow = ONSET_MEDIAN_WINDOW / 2;

            // 최소 간격 (연속 온셋 방지, 약 50ms)
            float minOnsetInterval = 0.05f;
            float lastOnsetTime = -1f;

            for (int i = halfWindow; i < totalFrames - halfWindow; i++)
            {
                // 로컬 윈도우에서 평균 + 표준편차 계산
                float mean = 0f;
                for (int j = i - halfWindow; j <= i + halfWindow; j++)
                    mean += flux[j];
                mean /= ONSET_MEDIAN_WINDOW;

                float variance = 0f;
                for (int j = i - halfWindow; j <= i + halfWindow; j++)
                    variance += (flux[j] - mean) * (flux[j] - mean);
                variance /= ONSET_MEDIAN_WINDOW;
                float std = Mathf.Sqrt(variance);

                float threshold = mean + ONSET_THRESHOLD_ALPHA * std;

                // 임계값 초과 + 로컬 피크 확인
                if (flux[i] > threshold && flux[i] > flux[i - 1] && flux[i] >= flux[i + 1])
                {
                    float time = i * frameTime;
                    if (time - lastOnsetTime >= minOnsetInterval)
                    {
                        onsets.Add(new OnsetData
                        {
                            Time = time,
                            Strength = flux[i],
                            DominantBand = bands[i]
                        });
                        lastOnsetTime = time;
                    }
                }
            }

            return onsets;
        }

        /// <summary>
        /// Autocorrelation 기반 BPM 추출
        /// </summary>
        private float EstimateBPM(List<OnsetData> onsets, float duration)
        {
            if (onsets.Count < 4) return 120f; // 기본값

            // IOI (Inter-Onset Interval) 히스토그램
            float[] iois = new float[onsets.Count - 1];
            for (int i = 0; i < iois.Length; i++)
                iois[i] = onsets[i + 1].Time - onsets[i].Time;

            // BPM 후보 스코어 (히스토그램 기반)
            float bestBPM = 120f;
            float bestScore = 0f;
            float bpmResolution = 0.5f;

            for (float candidateBPM = MIN_BPM; candidateBPM <= MAX_BPM; candidateBPM += bpmResolution)
            {
                float beatInterval = 60f / candidateBPM;
                float score = 0f;

                foreach (float ioi in iois)
                {
                    // IOI가 비트 간격의 정수배에 가까운지 확인
                    for (int mult = 1; mult <= 4; mult++)
                    {
                        float expectedInterval = beatInterval * mult;
                        float diff = Mathf.Abs(ioi - expectedInterval);
                        float tolerance = beatInterval * 0.1f; // 10% 허용
                        if (diff < tolerance)
                        {
                            float weight = 1f / mult; // 1배 > 2배 > 4배
                            score += weight * (1f - diff / tolerance);
                        }
                    }
                }

                // 선호 BPM 범위 보너스
                if (candidateBPM >= PREFERRED_BPM_MIN && candidateBPM <= PREFERRED_BPM_MAX)
                    score *= 1.2f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestBPM = candidateBPM;
                }
            }

            // 옥타브 에러 보정 (너무 느리면 2배, 너무 빠르면 1/2)
            while (bestBPM < 80f) bestBPM *= 2f;
            while (bestBPM > 180f) bestBPM /= 2f;

            return Mathf.Round(bestBPM);
        }

        /// <summary>
        /// 에너지 프로필 기반 구간 감지
        /// </summary>
        private List<SectionData> DetectSections(float[] energy, float sampleRate, int totalFrames, float duration)
        {
            var sections = new List<SectionData>();
            float frameTime = (float)HOP_SIZE / sampleRate;

            // 에너지 정규화
            float maxEnergy = 0f;
            for (int i = 0; i < totalFrames; i++)
                if (energy[i] > maxEnergy) maxEnergy = energy[i];

            if (maxEnergy <= 0f)
            {
                sections.Add(new SectionData { StartTime = 0, EndTime = duration, Type = SectionType.Calm, AverageEnergy = 0 });
                return sections;
            }

            float[] normalized = new float[totalFrames];
            for (int i = 0; i < totalFrames; i++)
                normalized[i] = energy[i] / maxEnergy;

            // 구간 크기: 약 4초씩
            int sectionFrameSize = Mathf.Max(1, Mathf.RoundToInt(4f / frameTime));
            int numSections = Mathf.Max(1, totalFrames / sectionFrameSize);

            float[] sectionEnergies = new float[numSections];
            for (int s = 0; s < numSections; s++)
            {
                int start = s * sectionFrameSize;
                int end = Mathf.Min(start + sectionFrameSize, totalFrames);
                float sum = 0f;
                for (int i = start; i < end; i++)
                    sum += normalized[i];
                sectionEnergies[s] = sum / (end - start);
            }

            // 전체 평균 에너지
            float globalAvg = 0f;
            for (int s = 0; s < numSections; s++)
                globalAvg += sectionEnergies[s];
            globalAvg /= numSections;

            // 구간 분류
            SectionType prevType = SectionType.Intro;

            for (int s = 0; s < numSections; s++)
            {
                float e = sectionEnergies[s];
                SectionType type;

                if (s < 2)
                    type = SectionType.Intro;
                else if (s >= numSections - 2)
                    type = SectionType.Outro;
                else if (e > globalAvg * 1.3f)
                    type = SectionType.Drop;
                else if (e > globalAvg * 0.8f)
                    type = SectionType.Build;
                else
                    type = SectionType.Calm;

                float endTime = Mathf.Min((s + 1) * sectionFrameSize * frameTime, duration);

                // 같은 타입이면 병합
                if (s > 0 && type == prevType && sections.Count > 0)
                {
                    var last = sections[sections.Count - 1];
                    last.EndTime = endTime;
                    last.AverageEnergy = (last.AverageEnergy + e) / 2f;
                    sections[sections.Count - 1] = last;
                }
                else
                {
                    if (sections.Count > 0)
                    {
                        var last = sections[sections.Count - 1];
                        last.EndTime = s * sectionFrameSize * frameTime;
                        sections[sections.Count - 1] = last;
                    }

                    sections.Add(new SectionData
                    {
                        StartTime = s * sectionFrameSize * frameTime,
                        EndTime = endTime,
                        Type = type,
                        AverageEnergy = e
                    });
                }

                prevType = type;
            }

            // 마지막 구간 종료 시간 보정
            if (sections.Count > 0)
            {
                var last = sections[sections.Count - 1];
                last.EndTime = duration;
                sections[sections.Count - 1] = last;
            }

            return sections;
        }

        /// <summary>
        /// Cooley-Tukey Radix-2 DIT FFT (in-place)
        /// </summary>
        private void FFT(float[] real, float[] imag, bool inverse)
        {
            int n = real.Length;
            int bits = (int)Mathf.Log(n, 2);

            // Bit-reversal permutation
            for (int i = 0; i < n; i++)
            {
                int j = BitReverse(i, bits);
                if (j > i)
                {
                    float tr = real[i]; real[i] = real[j]; real[j] = tr;
                    float ti = imag[i]; imag[i] = imag[j]; imag[j] = ti;
                }
            }

            // Butterfly computation
            for (int size = 2; size <= n; size *= 2)
            {
                int halfSize = size / 2;
                float angle = (inverse ? 1 : -1) * 2f * Mathf.PI / size;

                float wRealStep = Mathf.Cos(angle);
                float wImagStep = Mathf.Sin(angle);

                for (int i = 0; i < n; i += size)
                {
                    float wReal = 1f;
                    float wImag = 0f;

                    for (int k = 0; k < halfSize; k++)
                    {
                        int even = i + k;
                        int odd = i + k + halfSize;

                        float tReal = wReal * real[odd] - wImag * imag[odd];
                        float tImag = wReal * imag[odd] + wImag * real[odd];

                        real[odd] = real[even] - tReal;
                        imag[odd] = imag[even] - tImag;
                        real[even] += tReal;
                        imag[even] += tImag;

                        float newWReal = wReal * wRealStep - wImag * wImagStep;
                        wImag = wReal * wImagStep + wImag * wRealStep;
                        wReal = newWReal;
                    }
                }
            }

            if (inverse)
            {
                for (int i = 0; i < n; i++)
                {
                    real[i] /= n;
                    imag[i] /= n;
                }
            }
        }

        private int BitReverse(int n, int bits)
        {
            int reversed = 0;
            for (int i = 0; i < bits; i++)
            {
                reversed = (reversed << 1) | (n & 1);
                n >>= 1;
            }
            return reversed;
        }
    }
}
