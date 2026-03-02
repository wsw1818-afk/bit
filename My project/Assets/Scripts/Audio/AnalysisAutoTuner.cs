using UnityEngine;
using System;
using System.Collections.Generic;

namespace AIBeat.Audio
{
    /// <summary>
    /// 오디오 분석 파라미터 (OfflineAudioAnalyzer용)
    /// </summary>
    [Serializable]
    public struct AnalysisParams
    {
        // 온셋 감지
        public float OnsetThresholdAlpha;    // mean + α×std (기본 1.5)
        public int OnsetMedianWindow;        // 적응형 임계값 윈도우 (기본 7)
        public float MinOnsetInterval;       // 최소 onset 간격(초) (기본 0.05)

        // 저주파 보정
        public float LowRatioThreshold;      // 저주파 flux 비율 임계값 (기본 0.3)
        public float LowBoostFactor;         // 임계값 감소율 (기본 0.8)
        public float LowBandWeight;          // onset 강도에 저주파 가중치 (기본 0.5)

        // BPM 탐색
        public float MinBPM;                 // BPM 탐색 최소 (기본 60)
        public float MaxBPM;                 // BPM 탐색 최대 (기본 200)
        public float PreferredBPMMin;        // 선호 BPM 최소 (기본 120)
        public float PreferredBPMMax;        // 선호 BPM 최대 (기본 160)

        // Phase 탐색
        public float PhaseSearchStep;        // 탐색 해상도(초) (기본 0.001)
        public float PhaseStrengthFilter;    // 상위 N% onset만 사용 (기본 0.3 = 상위 30%)

        // 구간 감지
        public float SectionSize;            // 구간 크기(초) (기본 4.0)
        public float DropThreshold;          // Drop 분류 임계값 (globalAvg×이 값) (기본 1.3)
        public float BuildThreshold;         // Build 분류 임계값 (globalAvg×이 값) (기본 0.8)

        public static AnalysisParams Default => new AnalysisParams
        {
            OnsetThresholdAlpha = 1.5f,
            OnsetMedianWindow = 7,
            MinOnsetInterval = 0.05f,
            LowRatioThreshold = 0.3f,
            LowBoostFactor = 0.8f,
            LowBandWeight = 0.5f,
            MinBPM = 60f,
            MaxBPM = 200f,
            PreferredBPMMin = 120f,
            PreferredBPMMax = 160f,
            PhaseSearchStep = 0.001f,
            PhaseStrengthFilter = 0.3f,
            SectionSize = 4f,
            DropThreshold = 1.3f,
            BuildThreshold = 0.8f
        };
    }

    /// <summary>
    /// 노트 매핑 파라미터 (SmartBeatMapper용)
    /// </summary>
    [Serializable]
    public struct MappingParams
    {
        // 노트 간격
        public float MinNoteInterval;        // 최소 노트 간격(초) (기본 0.15)

        // 노트 타입 임계값
        public float LongNoteThreshold;      // 롱노트 에너지 비율 (기본 0.7)
        public float ScratchNoteThreshold;   // 스크래치 에너지 비율 (기본 0.8)
        public float LongNoteDuration;       // 롱노트 기본 지속시간 (기본 0.5)

        // Beat snap
        public float BeatSnapTolerance;      // subdivInterval × 이 값 (기본 0.5)

        // 구간별 밀도
        public float IntroDensity;           // Intro 밀도 배율 (기본 0.7)
        public float BuildDensity;           // Build 밀도 배율 (기본 0.8)
        public float DropDensity;            // Drop 밀도 배율 (기본 1.5)
        public float OutroDensity;           // Outro 밀도 배율 (기본 0.5)
        public float CalmDensity;            // Calm 밀도 배율 (기본 0.6)

        // 동시타격
        public float ChordChanceMin;         // 최소 동시타격 확률 (기본 0.05)
        public float ChordChanceMax;         // 최대 동시타격 확률 (기본 0.30)

        // 레인 연속 방지
        public int MaxSameLaneRepeat;        // 연속 허용 횟수 (기본 3)

        public static MappingParams Default => new MappingParams
        {
            MinNoteInterval = 0.15f,
            LongNoteThreshold = 0.55f,   // SmartBeatMapper SerializeField와 일치
            ScratchNoteThreshold = 0.65f, // SmartBeatMapper SerializeField와 일치
            LongNoteDuration = 0.5f,
            BeatSnapTolerance = 0.8f,
            IntroDensity = 0.8f,
            BuildDensity = 0.9f,
            DropDensity = 1.5f,
            OutroDensity = 0.7f,
            CalmDensity = 0.8f,
            ChordChanceMin = 0.05f,
            ChordChanceMax = 0.30f,
            MaxSameLaneRepeat = 2         // SmartBeatMapper SerializeField와 일치
        };
    }

    /// <summary>
    /// 곡 분석 결과 통계를 기반으로 파라미터를 자동 조정하는 셀프 튜닝 시스템
    /// </summary>
    public static class AnalysisAutoTuner
    {
        /// <summary>
        /// 1차 분석 결과를 기반으로 최적 분석 파라미터 계산
        /// </summary>
        public static AnalysisParams TuneAnalysisParams(OfflineAudioAnalyzer.AnalysisResult result)
        {
            var p = AnalysisParams.Default;
            if (result == null || result.Onsets == null || result.Onsets.Count < 4)
                return p;

            // --- onset 강도 분포 분석 ---
            float meanStr = 0f, maxStr = 0f;
            foreach (var o in result.Onsets)
            {
                meanStr += o.Strength;
                if (o.Strength > maxStr) maxStr = o.Strength;
            }
            meanStr /= result.Onsets.Count;

            float varianceStr = 0f;
            foreach (var o in result.Onsets)
                varianceStr += (o.Strength - meanStr) * (o.Strength - meanStr);
            varianceStr /= result.Onsets.Count;
            float stdStr = Mathf.Sqrt(varianceStr);
            float cv = (meanStr > 0f) ? stdStr / meanStr : 0f; // 변동 계수

            // 변동 계수가 크면(에너지 차이 큼) → 임계값 상향 (noise 제거)
            if (cv > 2f)
                p.OnsetThresholdAlpha = 2.0f;
            else if (cv > 1.5f)
                p.OnsetThresholdAlpha = 1.8f;
            // 변동 계수가 작으면(균일) → 임계값 하향 (더 민감하게)
            else if (cv < 0.5f)
                p.OnsetThresholdAlpha = 1.0f;
            else if (cv < 0.8f)
                p.OnsetThresholdAlpha = 1.2f;

            // --- 저주파 에너지 비율 분석 ---
            float totalLowEnergy = 0f, totalAllEnergy = 0f;
            if (result.BandEnergies != null)
            {
                for (int f = 0; f < result.BandEnergies.Length; f++)
                {
                    if (result.BandEnergies[f] == null) continue;
                    for (int b = 0; b <= 2 && b < result.BandEnergies[f].Length; b++)
                        totalLowEnergy += result.BandEnergies[f][b];
                    for (int b = 0; b < result.BandEnergies[f].Length; b++)
                        totalAllEnergy += result.BandEnergies[f][b];
                }
            }
            float lowEnergyRatio = (totalAllEnergy > 0f) ? totalLowEnergy / totalAllEnergy : 0f;

            // 베이스 중심 곡 → 저주파 가중치 증가
            if (lowEnergyRatio > 0.6f)
            {
                p.LowBoostFactor = 0.6f;
                p.LowBandWeight = 0.8f;
            }
            else if (lowEnergyRatio > 0.4f)
            {
                p.LowBoostFactor = 0.7f;
                p.LowBandWeight = 0.6f;
            }
            // 고주파 중심 곡 → 저주파 보정 약화
            else if (lowEnergyRatio < 0.2f)
            {
                p.LowBoostFactor = 1.0f; // 보정 없음
                p.LowBandWeight = 0.2f;
            }

            // --- BPM 기반 onset 간격 조정 ---
            float bpm = result.BPM;
            if (bpm > 160f)
                p.MinOnsetInterval = 0.03f;
            else if (bpm > 140f)
                p.MinOnsetInterval = 0.04f;
            else if (bpm < 90f)
                p.MinOnsetInterval = 0.08f;
            else if (bpm < 100f)
                p.MinOnsetInterval = 0.06f;

            // BPM 기반 선호 범위 동적 설정 (추정 BPM 주변 ±30)
            if (bpm > 0f)
            {
                p.PreferredBPMMin = Mathf.Max(60f, bpm - 30f);
                p.PreferredBPMMax = Mathf.Min(200f, bpm + 30f);
            }

            // --- onset 밀도 분석 ---
            float onsetDensity = result.Onsets.Count / Mathf.Max(1f, result.Duration);
            if (onsetDensity > 20f) // 초당 20개 이상 → 과밀 (noise 많음)
                p.OnsetThresholdAlpha += 0.5f;
            else if (onsetDensity > 10f)
                p.OnsetThresholdAlpha += 0.2f;

            // --- 곡 길이 기반 구간 크기 ---
            if (result.Duration < 30f)
                p.SectionSize = 2f;
            else if (result.Duration < 60f)
                p.SectionSize = 3f;
            else if (result.Duration > 240f)
                p.SectionSize = 6f;

#if UNITY_EDITOR
            Debug.Log($"[AutoTuner] Analysis params tuned: α={p.OnsetThresholdAlpha:F1}, lowRatio={lowEnergyRatio:F2}, " +
                      $"lowBoost={p.LowBoostFactor:F1}, lowWeight={p.LowBandWeight:F1}, " +
                      $"minInterval={p.MinOnsetInterval:F3}, density={onsetDensity:F1}/s, sectionSize={p.SectionSize:F0}s");
#endif
            return p;
        }

        /// <summary>
        /// 분석 결과를 기반으로 매핑 파라미터 최적화
        /// </summary>
        public static MappingParams TuneMappingParams(OfflineAudioAnalyzer.AnalysisResult result, int difficulty)
        {
            var p = MappingParams.Default;
            if (result == null) return p;

            float bpm = result.BPM;

            // --- BPM 기반 노트 간격 ---
            if (bpm > 160f)
                p.MinNoteInterval = 0.10f;
            else if (bpm > 140f)
                p.MinNoteInterval = 0.12f;
            else if (bpm < 90f)
                p.MinNoteInterval = 0.25f;
            else if (bpm < 100f)
                p.MinNoteInterval = 0.20f;

            // --- 구간 비율 분석 → 밀도 조정 ---
            if (result.Sections != null && result.Sections.Count > 0)
            {
                float totalDuration = Mathf.Max(1f, result.Duration);
                float dropDuration = 0f, introOutroDuration = 0f;

                foreach (var s in result.Sections)
                {
                    float sLen = s.EndTime - s.StartTime;
                    if (s.Type == OfflineAudioAnalyzer.SectionType.Drop)
                        dropDuration += sLen;
                    if (s.Type == OfflineAudioAnalyzer.SectionType.Intro ||
                        s.Type == OfflineAudioAnalyzer.SectionType.Outro)
                        introOutroDuration += sLen;
                }

                float dropRatio = dropDuration / totalDuration;
                // Drop이 길면 밀도 낮춤 (과부하 방지), 짧으면 집중 밀도
                if (dropRatio > 0.5f)
                    p.DropDensity = 1.2f;
                else if (dropRatio > 0.3f)
                    p.DropDensity = 1.4f;
                else if (dropRatio < 0.1f)
                    p.DropDensity = 1.8f;

                // Intro/Outro 비율이 큰 곡은 해당 구간 밀도 높임 (빈 구간 방지)
                float ioRatio = introOutroDuration / totalDuration;
                if (ioRatio > 0.4f)
                {
                    p.IntroDensity = 0.9f;
                    p.OutroDensity = 0.8f;
                }
            }

            // --- 에너지 분포 기반 노트 타입 임계값 ---
            if (result.Onsets != null && result.Onsets.Count > 0)
            {
                // 상위 에너지 비율 계산
                float maxStr = 0f;
                foreach (var o in result.Onsets)
                    if (o.Strength > maxStr) maxStr = o.Strength;

                // 전체 에너지가 높은 곡 → 롱노트/스크래치 기준 상향
                float avgRatio = 0f;
                foreach (var o in result.Onsets)
                    avgRatio += o.Strength / Mathf.Max(maxStr, 0.001f);
                avgRatio /= result.Onsets.Count;

                // 에너지가 높은 곡: 임계값을 소폭 올리되 기본값(0.55/0.65)을 넘지 않음
                // → 다양성 보존하면서 노이즈 타입 분류만 방지
                if (avgRatio > 0.6f)
                {
                    p.LongNoteThreshold = 0.58f;   // 기본 0.55 대비 소폭 상향
                    p.ScratchNoteThreshold = 0.68f; // 기본 0.65 대비 소폭 상향
                }
                else if (avgRatio < 0.3f) // 에너지 차이가 큼 → 더 낮춰서 다양성 극대화
                {
                    p.LongNoteThreshold = 0.45f;
                    p.ScratchNoteThreshold = 0.55f;
                }

                // Onset이 적으면 beat snap 더 관대하게
                float density = result.Onsets.Count / Mathf.Max(1f, result.Duration);
                if (density < 1f)
                    p.BeatSnapTolerance = 0.95f; // 매우 관대
                else if (density < 3f)
                    p.BeatSnapTolerance = 0.85f;
            }

            // --- BPM 기반 롱노트 지속시간 ---
            float beatInterval = 60f / Mathf.Max(bpm, 60f);
            p.LongNoteDuration = Mathf.Clamp(beatInterval * 2f, 0.3f, 2f);

#if UNITY_EDITOR
            Debug.Log($"[AutoTuner] Mapping params tuned: interval={p.MinNoteInterval:F2}, " +
                      $"longThresh={p.LongNoteThreshold:F2}, dropDensity={p.DropDensity:F1}, " +
                      $"snapTol={p.BeatSnapTolerance:F2}, longDur={p.LongNoteDuration:F2}");
#endif
            return p;
        }

        /// <summary>
        /// 파라미터 차이가 유의미하여 재분석이 필요한지 판단
        /// </summary>
        public static bool NeedsReanalysis(AnalysisParams original, AnalysisParams tuned)
        {
            // onset 임계값이 20% 이상 변경되면 재분석
            float alphaDiff = Mathf.Abs(original.OnsetThresholdAlpha - tuned.OnsetThresholdAlpha);
            if (alphaDiff / Mathf.Max(original.OnsetThresholdAlpha, 0.1f) > 0.2f)
                return true;

            // 저주파 가중치가 크게 변경되면 재분석
            float lowDiff = Mathf.Abs(original.LowBandWeight - tuned.LowBandWeight);
            if (lowDiff > 0.2f)
                return true;

            // 최소 onset 간격이 크게 변경되면 재분석
            float intervalDiff = Mathf.Abs(original.MinOnsetInterval - tuned.MinOnsetInterval);
            if (intervalDiff > 0.02f)
                return true;

            return false;
        }
    }
}
