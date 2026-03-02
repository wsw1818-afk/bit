using UnityEngine;
using System.Collections.Generic;
using AIBeat.Data;

namespace AIBeat.Audio
{
    /// <summary>
    /// 노트 매핑 품질을 수치로 평가하는 지표 계산기
    /// A/B 비교 및 자동 튜닝 검증에 사용
    /// </summary>
    public static class BeatSyncMetrics
    {
        /// <summary>
        /// 종합 품질 결과
        /// </summary>
        public struct MetricsResult
        {
            public float BeatAlignment;       // 비트 그리드 정렬도 (0-100)
            public float DensityUniformity;   // 밀도 균일도 (0-100)
            public float Diversity;           // 레인/타입 다양성 (0-100)
            public float OverallScore;        // 가중 평균 종합점수
            public int TotalNotes;
            public float BPM;
            public float[] DensityHistogram;  // 시간 구간별 노트 밀도
            public int[] LaneDistribution;    // 레인별 노트 수 [4]
            public int[] TypeDistribution;    // 타입별 노트 수 [Tap, Long, Scratch]
        }

        /// <summary>
        /// 비트 그리드 정렬도 계산 (0-100)
        /// 노트가 BPM 그리드(1/4박)에 얼마나 정확히 배치되었는지 측정
        /// </summary>
        public static float CalculateBeatAlignment(List<NoteData> notes, float bpm, float phaseOffset)
        {
            if (notes == null || notes.Count == 0 || bpm <= 0f) return 0f;

            float beatInterval = 60f / bpm;
            float subdivInterval = beatInterval / 2f; // 8분음표 그리드 (SnapToBeat과 동일)

            float totalDeviation = 0f;
            int count = 0;

            foreach (var note in notes)
            {
                float relative = note.HitTime - phaseOffset;
                float gridIndex = Mathf.Round(relative / subdivInterval);
                float snapped = gridIndex * subdivInterval + phaseOffset;
                float deviation = Mathf.Abs(note.HitTime - snapped);

                // subdivInterval 대비 정규화 (0=완벽, 0.5=최악)
                float normalizedDev = deviation / subdivInterval;
                totalDeviation += Mathf.Clamp01(normalizedDev);
                count++;
            }

            if (count == 0) return 0f;
            float avgDeviation = totalDeviation / count;
            // 0=완벽(100), 0.5=최악(0)
            return Mathf.Clamp(100f * (1f - avgDeviation * 2f), 0f, 100f);
        }

        /// <summary>
        /// 밀도 균일도 계산 (0-100)
        /// 같은 구간 타입 내에서 밀도 균일도를 측정 (Drop/Calm 간 의도적 차이는 감점하지 않음)
        /// </summary>
        public static float CalculateDensityUniformity(List<NoteData> notes,
            List<OfflineAudioAnalyzer.SectionData> sections, float duration)
        {
            if (notes == null || notes.Count < 2 || duration <= 0f) return 0f;

            // 섹션 정보가 있으면 섹션 내부 균일도로 계산
            if (sections != null && sections.Count > 1)
                return CalculateSectionAwareDensityUniformity(notes, sections, duration);

            // 섹션 정보 없으면 전체 히스토그램 기반 계산
            return CalculateGlobalDensityUniformity(notes, duration);
        }

        /// <summary>
        /// 구간 타입별 내부 균일도 계산 후 가중 평균
        /// 긴 구간은 자체적으로 서브구간으로 분할하여 "국소적 균일도"로 측정
        /// </summary>
        private static float CalculateSectionAwareDensityUniformity(List<NoteData> notes,
            List<OfflineAudioAnalyzer.SectionData> sections, float duration)
        {
            float bucketSize = 4f; // 4초 버킷 (2초→4초: 자연스러운 밀도 변동 흡수)
            float totalWeight = 0f;
            float weightedScore = 0f;

            foreach (var section in sections)
            {
                float sectionDuration = section.EndTime - section.StartTime;
                if (sectionDuration < bucketSize * 2f) continue;

                // 이 구간 내의 노트만 수집
                var sectionNotes = new List<float>();
                foreach (var note in notes)
                {
                    if (note.HitTime >= section.StartTime && note.HitTime < section.EndTime)
                        sectionNotes.Add(note.HitTime);
                }

                if (sectionNotes.Count < 2) continue;

                // 긴 구간(30초+)은 서브구간(16초)으로 분할하여 국소 균일도 측정
                float score;
                if (sectionDuration > 30f)
                {
                    score = CalculateLocalUniformityScore(sectionNotes, section.StartTime, section.EndTime, bucketSize);
                }
                else
                {
                    score = CalculateBucketCVScore(sectionNotes, section.StartTime, section.EndTime, bucketSize);
                }

                totalWeight += sectionDuration;
                weightedScore += score * sectionDuration;
            }

            if (totalWeight <= 0f) return CalculateGlobalDensityUniformity(notes, duration);
            return weightedScore / totalWeight;
        }

        /// <summary>
        /// 긴 구간을 슬라이딩 윈도우로 국소 균일도 측정
        /// 8초 윈도우를 4초씩 슬라이딩하며 인접 윈도우 간 밀도 비율 체크
        /// 인접 구간끼리 밀도가 비슷하면 높은 점수 (전체 CV가 아닌 국소 연속성 측정)
        /// </summary>
        private static float CalculateLocalUniformityScore(List<float> noteTimes,
            float start, float end, float bucketSize)
        {
            float windowSize = 8f;
            float stepSize = 4f;

            // 윈도우별 밀도 계산
            var windowDensities = new List<float>();
            for (float wStart = start; wStart + windowSize <= end; wStart += stepSize)
            {
                int count = 0;
                foreach (float t in noteTimes)
                    if (t >= wStart && t < wStart + windowSize) count++;
                windowDensities.Add(count / windowSize); // 초당 밀도
            }

            if (windowDensities.Count < 2) return 70f;

            // 인접 윈도우 밀도 비율의 안정성 측정
            float totalRatioScore = 0f;
            int pairs = 0;
            for (int i = 1; i < windowDensities.Count; i++)
            {
                float a = windowDensities[i - 1];
                float b = windowDensities[i];
                if (a <= 0f && b <= 0f) { totalRatioScore += 100f; pairs++; continue; }

                float maxD = Mathf.Max(a, b);
                float minD = Mathf.Min(a, b);
                if (maxD <= 0f) { totalRatioScore += 50f; pairs++; continue; }

                // 밀도 비율: 1.0이면 완벽 동일, 2.0이면 2배 차이
                float ratio = maxD / Mathf.Max(minD, 0.01f);
                // ratio 1.0→100, 1.5→75, 2.0→50, 3.0→0
                float pairScore = Mathf.Clamp(100f * (1f - (ratio - 1f) / 2f), 0f, 100f);
                totalRatioScore += pairScore;
                pairs++;
            }

            return pairs > 0 ? totalRatioScore / pairs : 50f;
        }

        /// <summary>
        /// 버킷 기반 CV 점수 계산 (짧은 구간용)
        /// </summary>
        private static float CalculateBucketCVScore(List<float> noteTimes,
            float start, float end, float bucketSize)
        {
            float sectionDuration = end - start;
            int bucketCount = Mathf.Max(1, Mathf.CeilToInt(sectionDuration / bucketSize));
            float[] buckets = new float[bucketCount];

            foreach (float t in noteTimes)
            {
                int idx = Mathf.Clamp(Mathf.FloorToInt((t - start) / bucketSize), 0, bucketCount - 1);
                buckets[idx]++;
            }

            float mean = 0f;
            for (int i = 0; i < bucketCount; i++) mean += buckets[i];
            mean /= bucketCount;

            if (mean <= 0f) return 0f;

            float variance = 0f;
            for (int i = 0; i < bucketCount; i++)
                variance += (buckets[i] - mean) * (buckets[i] - mean);
            variance /= bucketCount;

            float cv = Mathf.Sqrt(variance) / mean;
            // 비선형 변환: cv^0.5으로 낮은 CV에 더 관대
            float adjustedCV = Mathf.Pow(Mathf.Min(cv, 1f), 0.5f);
            return Mathf.Clamp(100f * (1f - adjustedCV), 0f, 100f);
        }

        /// <summary>
        /// 전체 히스토그램 기반 균일도 (폴백)
        /// </summary>
        private static float CalculateGlobalDensityUniformity(List<NoteData> notes, float duration)
        {
            float bucketSize = 2f;
            int bucketCount = Mathf.Max(1, Mathf.CeilToInt(duration / bucketSize));
            float[] buckets = new float[bucketCount];

            foreach (var note in notes)
            {
                int idx = Mathf.Clamp(Mathf.FloorToInt(note.HitTime / bucketSize), 0, bucketCount - 1);
                buckets[idx]++;
            }

            int firstNonZero = 0, lastNonZero = bucketCount - 1;
            for (int i = 0; i < bucketCount; i++)
                if (buckets[i] > 0) { firstNonZero = i; break; }
            for (int i = bucketCount - 1; i >= 0; i--)
                if (buckets[i] > 0) { lastNonZero = i; break; }

            int activeBuckets = lastNonZero - firstNonZero + 1;
            if (activeBuckets < 2) return 50f;

            float mean = 0f;
            for (int i = firstNonZero; i <= lastNonZero; i++)
                mean += buckets[i];
            mean /= activeBuckets;

            if (mean <= 0f) return 0f;

            float variance = 0f;
            for (int i = firstNonZero; i <= lastNonZero; i++)
                variance += (buckets[i] - mean) * (buckets[i] - mean);
            variance /= activeBuckets;

            float cv = Mathf.Sqrt(variance) / mean;
            return Mathf.Clamp(100f * (1f - cv), 0f, 100f);
        }

        /// <summary>
        /// 다양성 점수 계산 (0-100)
        /// 레인 분포 엔트로피 + 노트 타입 엔트로피의 평균
        /// </summary>
        public static float CalculateDiversity(List<NoteData> notes)
        {
            if (notes == null || notes.Count == 0) return 0f;

            // 레인 분포 (4레인)
            int[] laneCounts = new int[4];
            // 타입 분포 (Tap=0, Long=1, Scratch=2)
            int[] typeCounts = new int[3];

            foreach (var note in notes)
            {
                int laneIdx = Mathf.Clamp(note.LaneIndex, 0, 3);
                laneCounts[laneIdx]++;

                int typeIdx = note.Type switch
                {
                    NoteType.Tap => 0,
                    NoteType.Long => 1,
                    NoteType.Scratch => 2,
                    _ => 0
                };
                typeCounts[typeIdx]++;
            }

            float laneEntropy = CalculateNormalizedEntropy(laneCounts, notes.Count);
            float typeEntropy = CalculateNormalizedEntropy(typeCounts, notes.Count);

            // 두 엔트로피의 가중 평균 (레인 60%, 타입 40%)
            return (laneEntropy * 60f + typeEntropy * 40f);
        }

        /// <summary>
        /// 종합 품질 점수 계산
        /// </summary>
        public static MetricsResult CalculateAll(List<NoteData> notes,
            OfflineAudioAnalyzer.AnalysisResult analysis)
        {
            var result = new MetricsResult();
            if (notes == null || analysis == null) return result;

            result.TotalNotes = notes.Count;
            result.BPM = analysis.BPM;

            result.BeatAlignment = CalculateBeatAlignment(notes, analysis.BPM, analysis.BeatPhaseOffset);
            result.DensityUniformity = CalculateDensityUniformity(notes, analysis.Sections, analysis.Duration);
            result.Diversity = CalculateDiversity(notes);

            // 가중 평균: 정렬도 40%, 균일도 30%, 다양성 30%
            result.OverallScore = result.BeatAlignment * 0.4f
                                + result.DensityUniformity * 0.3f
                                + result.Diversity * 0.3f;

            result.DensityHistogram = CalculateDensityHistogram(notes, analysis.Duration);
            result.LaneDistribution = CalculateLaneDistribution(notes);
            result.TypeDistribution = CalculateTypeDistribution(notes);

            return result;
        }

        /// <summary>
        /// 시간 구간별 밀도 히스토그램 (2초 단위)
        /// </summary>
        public static float[] CalculateDensityHistogram(List<NoteData> notes, float duration)
        {
            if (notes == null || duration <= 0f) return new float[0];

            float bucketSize = 2f;
            int bucketCount = Mathf.Max(1, Mathf.CeilToInt(duration / bucketSize));
            float[] histogram = new float[bucketCount];

            foreach (var note in notes)
            {
                int idx = Mathf.Clamp(Mathf.FloorToInt(note.HitTime / bucketSize), 0, bucketCount - 1);
                histogram[idx]++;
            }

            // 초당 밀도로 변환
            for (int i = 0; i < bucketCount; i++)
                histogram[i] /= bucketSize;

            return histogram;
        }

        /// <summary>
        /// 레인별 노트 수 분포 [4]
        /// </summary>
        public static int[] CalculateLaneDistribution(List<NoteData> notes)
        {
            int[] dist = new int[4];
            if (notes == null) return dist;

            foreach (var note in notes)
                dist[Mathf.Clamp(note.LaneIndex, 0, 3)]++;

            return dist;
        }

        /// <summary>
        /// 타입별 노트 수 분포 [Tap, Long, Scratch]
        /// </summary>
        public static int[] CalculateTypeDistribution(List<NoteData> notes)
        {
            int[] dist = new int[3];
            if (notes == null) return dist;

            foreach (var note in notes)
            {
                int idx = note.Type switch
                {
                    NoteType.Tap => 0,
                    NoteType.Long => 1,
                    NoteType.Scratch => 2,
                    _ => 0
                };
                dist[idx]++;
            }

            return dist;
        }

        /// <summary>
        /// 정규화된 엔트로피 계산 (0-1)
        /// 0 = 모든 값이 하나에 집중, 1 = 완벽 균등 분포
        /// </summary>
        private static float CalculateNormalizedEntropy(int[] counts, int total)
        {
            if (total <= 0) return 0f;

            int nonZeroCategories = 0;
            float entropy = 0f;

            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] <= 0) continue;
                nonZeroCategories++;
                float p = (float)counts[i] / total;
                entropy -= p * Mathf.Log(p, 2);
            }

            if (nonZeroCategories <= 1) return 0f;

            // 최대 엔트로피로 정규화 (log2(카테고리수))
            float maxEntropy = Mathf.Log(counts.Length, 2);
            return Mathf.Clamp01(entropy / maxEntropy);
        }
    }
}
