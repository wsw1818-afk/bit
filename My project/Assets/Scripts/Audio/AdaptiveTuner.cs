using UnityEngine;
using System.Collections.Generic;
using AIBeat.Data;

namespace AIBeat.Audio
{
    /// <summary>
    /// 플레이 데이터 기반 적응형 파라미터 보정
    /// 과거 플레이 히스토리를 분석하여 매핑 파라미터를 점진적으로 조정
    ///
    /// 규칙:
    /// - 최소 2세션 데이터 필요
    /// - 최대 변동폭 20%
    /// - Lerp factor 0.3 (급격한 변화 방지)
    /// - rawDiff 평균 |>15ms| → 타이밍 오프셋 보정
    /// - 구간 Miss >30% → 해당 구간 밀도 20% 감소
    /// - 구간 Perfect >70% + Miss <5% → 밀도 15% 증가
    /// - 정확도 >90% → 노트 간격 10% 축소
    /// </summary>
    public static class AdaptiveTuner
    {
        private const int MIN_SESSIONS = 2;
        private const float MAX_CHANGE_RATIO = 0.2f;   // 최대 20% 변동
        private const float LERP_FACTOR = 0.3f;         // 점진적 적용
        private const float TIMING_THRESHOLD_MS = 15f;   // rawDiff 보정 임계값
        private const float MISS_RATE_HIGH = 0.30f;      // Miss 30% 이상 → 밀도 감소
        private const float PERFECT_RATE_HIGH = 0.70f;   // Perfect 70% 이상
        private const float MISS_RATE_LOW = 0.05f;       // Miss 5% 미만 → 밀도 증가 가능
        private const float HIGH_ACCURACY = 90f;         // 정확도 90% 이상 → 어렵게

        /// <summary>
        /// 적응형 보정 결과
        /// </summary>
        public struct AdaptResult
        {
            public MappingParams AdjustedParams;
            public float SuggestedTimingOffset; // ms 단위 타이밍 보정값
            public bool WasAdapted;             // 실제로 보정이 적용되었는지
            public string Summary;              // 보정 내역 요약
        }

        /// <summary>
        /// 플레이 히스토리 기반 매핑 파라미터 보정
        /// </summary>
        public static AdaptResult AdaptParams(MappingParams baseParams, SongPlayHistory history)
        {
            var result = new AdaptResult
            {
                AdjustedParams = baseParams,
                SuggestedTimingOffset = 0f,
                WasAdapted = false,
                Summary = ""
            };

            if (history == null || history.Sessions == null || history.Sessions.Count < MIN_SESSIONS)
            {
                result.Summary = $"[Adaptive] 데이터 부족 ({history?.Sessions?.Count ?? 0}/{MIN_SESSIONS} 세션)";
                return result;
            }

            var adjustments = new List<string>();
            var p = baseParams;

            // 최근 세션들의 통계 집계
            var recentSessions = GetRecentSessions(history, 5); // 최근 5세션

            // 1. 타이밍 오프셋 보정
            float avgRawDiff = CalculateAverageRawDiff(recentSessions);
            if (Mathf.Abs(avgRawDiff) > TIMING_THRESHOLD_MS)
            {
                // 50% lerp로 점진 보정
                result.SuggestedTimingOffset = avgRawDiff * 0.5f;
                adjustments.Add($"timing offset {result.SuggestedTimingOffset:+0.0;-0.0}ms");
            }

            // 2. 구간별 밀도 조정
            var sectionAdjustments = AnalyzeSectionPerformance(recentSessions);
            foreach (var adj in sectionAdjustments)
            {
                switch (adj.SectionType)
                {
                    case "Intro":
                        p.IntroDensity = ApplyBoundedChange(p.IntroDensity, adj.DensityMultiplier);
                        break;
                    case "Build":
                        p.BuildDensity = ApplyBoundedChange(p.BuildDensity, adj.DensityMultiplier);
                        break;
                    case "Drop":
                        p.DropDensity = ApplyBoundedChange(p.DropDensity, adj.DensityMultiplier);
                        break;
                    case "Outro":
                        p.OutroDensity = ApplyBoundedChange(p.OutroDensity, adj.DensityMultiplier);
                        break;
                    case "Calm":
                        p.CalmDensity = ApplyBoundedChange(p.CalmDensity, adj.DensityMultiplier);
                        break;
                }
                adjustments.Add($"{adj.SectionType} density ×{adj.DensityMultiplier:F2}");
            }

            // 3. 전체 정확도 기반 노트 간격 조정
            float avgAccuracy = CalculateAverageAccuracy(recentSessions);
            if (avgAccuracy > HIGH_ACCURACY)
            {
                // 잘하는 유저 → 노트 간격 10% 축소 (더 어렵게)
                float newInterval = p.MinNoteInterval * 0.9f;
                p.MinNoteInterval = ApplyBoundedChange(p.MinNoteInterval, 0.9f);
                adjustments.Add($"interval {baseParams.MinNoteInterval:F2}→{p.MinNoteInterval:F2}");
            }

            result.AdjustedParams = p;
            result.WasAdapted = adjustments.Count > 0;
            result.Summary = adjustments.Count > 0
                ? $"[Adaptive] {string.Join(", ", adjustments)}"
                : "[Adaptive] 보정 불필요 (현재 파라미터 적합)";

#if UNITY_EDITOR
            Debug.Log(result.Summary);
#endif
            return result;
        }

        /// <summary>
        /// 최근 N세션 가져오기
        /// </summary>
        private static List<PlaySessionData> GetRecentSessions(SongPlayHistory history, int count)
        {
            var sessions = history.Sessions;
            int start = Mathf.Max(0, sessions.Count - count);
            return sessions.GetRange(start, sessions.Count - start);
        }

        /// <summary>
        /// 평균 rawDiff 계산 (ms)
        /// </summary>
        private static float CalculateAverageRawDiff(List<PlaySessionData> sessions)
        {
            float totalDiff = 0f;
            int count = 0;

            foreach (var session in sessions)
            {
                if (session.Judgements == null) continue;
                foreach (var j in session.Judgements)
                {
                    if (j.Result == "Miss") continue; // Miss는 rawDiff가 의미없음
                    totalDiff += j.RawDiff;
                    count++;
                }
            }

            return count > 0 ? totalDiff / count : 0f;
        }

        /// <summary>
        /// 평균 정확도 계산
        /// </summary>
        private static float CalculateAverageAccuracy(List<PlaySessionData> sessions)
        {
            float totalAcc = 0f;
            int count = 0;

            foreach (var session in sessions)
            {
                totalAcc += session.FinalAccuracy;
                count++;
            }

            return count > 0 ? totalAcc / count : 0f;
        }

        /// <summary>
        /// 구간별 성과 분석 → 밀도 조정 제안
        /// </summary>
        private static List<DensityAdjustment> AnalyzeSectionPerformance(List<PlaySessionData> sessions)
        {
            // 구간 타입별 누적 통계
            var sectionTotals = new Dictionary<string, AggregatedSectionStat>();

            foreach (var session in sessions)
            {
                if (session.SectionStats == null) continue;
                foreach (var stat in session.SectionStats)
                {
                    if (stat.TotalCount == 0) continue;

                    if (!sectionTotals.ContainsKey(stat.SectionType))
                    {
                        sectionTotals[stat.SectionType] = new AggregatedSectionStat();
                    }

                    var agg = sectionTotals[stat.SectionType];
                    agg.PerfectCount += stat.PerfectCount;
                    agg.MissCount += stat.MissCount;
                    agg.TotalCount += stat.TotalCount;
                    sectionTotals[stat.SectionType] = agg;
                }
            }

            var adjustments = new List<DensityAdjustment>();

            foreach (var kvp in sectionTotals)
            {
                var agg = kvp.Value;
                if (agg.TotalCount < 5) continue; // 데이터 부족

                float missRate = (float)agg.MissCount / agg.TotalCount;
                float perfectRate = (float)agg.PerfectCount / agg.TotalCount;

                if (missRate > MISS_RATE_HIGH)
                {
                    // Miss 30% 이상 → 밀도 20% 감소
                    adjustments.Add(new DensityAdjustment
                    {
                        SectionType = kvp.Key,
                        DensityMultiplier = 0.8f
                    });
                }
                else if (perfectRate > PERFECT_RATE_HIGH && missRate < MISS_RATE_LOW)
                {
                    // Perfect 70%+ & Miss 5%- → 밀도 15% 증가
                    adjustments.Add(new DensityAdjustment
                    {
                        SectionType = kvp.Key,
                        DensityMultiplier = 1.15f
                    });
                }
            }

            return adjustments;
        }

        /// <summary>
        /// 변동폭을 MAX_CHANGE_RATIO 이내로 제한하고 lerp 적용
        /// </summary>
        private static float ApplyBoundedChange(float original, float multiplier)
        {
            float target = original * multiplier;
            float maxDelta = original * MAX_CHANGE_RATIO;
            float clamped = Mathf.Clamp(target, original - maxDelta, original + maxDelta);
            return Mathf.Lerp(original, clamped, LERP_FACTOR);
        }

        private struct AggregatedSectionStat
        {
            public int PerfectCount;
            public int MissCount;
            public int TotalCount;
        }

        private struct DensityAdjustment
        {
            public string SectionType;
            public float DensityMultiplier;
        }
    }
}
