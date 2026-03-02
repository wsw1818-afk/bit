using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AIBeat.Audio;
using AIBeat.Data;

namespace AIBeat.Editor
{
    /// <summary>
    /// MCP로 자동 실행 가능한 비트 매핑 테스트베드
    /// 메뉴 실행 → Resources/Songs 전체 곡 자동 탐색 → 노트 생성 → 품질 지표 콘솔 출력
    /// </summary>
    public static class BeatMappingTestBed
    {
        [MenuItem("Tools/A.I. BEAT/Run Beat Mapping Test")]
        public static void RunTest()
        {
            Debug.Log("=== [TestBed] Beat Mapping Quality Test START ===");

            // Resources/Songs 폴더에서 모든 AudioClip 자동 탐색
            var clips = Resources.LoadAll<AudioClip>("Songs");
            if (clips == null || clips.Length == 0)
            {
                Debug.LogError("[TestBed] No AudioClips found in Resources/Songs/");
                return;
            }

            Debug.Log($"[TestBed] Found {clips.Length} song(s) in Resources/Songs/");

            float totalOverall = 0f;
            int testCount = 0;

            foreach (var clip in clips)
            {
                Debug.Log($"\n=== [TestBed] Song: {clip.name} ===");
                Debug.Log($"[TestBed] Duration={clip.length:F1}s, Channels={clip.channels}, Freq={clip.frequency}Hz");

                // 분석
                var analyzer = new OfflineAudioAnalyzer();
                var analysis = analyzer.Analyze(clip);

                if (analysis == null)
                {
                    Debug.LogError($"[TestBed] Analysis failed for {clip.name}!");
                    continue;
                }

                Debug.Log($"[TestBed] Analysis: BPM={analysis.BPM:F1}, Onsets={analysis.Onsets.Count}, " +
                          $"Sections={analysis.Sections.Count}, Phase={analysis.BeatPhaseOffset:F3}");

                // 섹션 정보 출력
                foreach (var s in analysis.Sections)
                {
                    Debug.Log($"[TestBed]   Section: {s.Type} [{s.StartTime:F1}~{s.EndTime:F1}s] Energy={s.AverageEnergy:F2}");
                }

                // 난이도별 테스트 (5, 7, 10)
                int[] difficulties = { 5, 7, 10 };
                foreach (int diff in difficulties)
                {
                    float overall = RunDifficultyTest(clip.name, diff, analysis);
                    totalOverall += overall;
                    testCount++;
                }
            }

            // 전체 요약
            if (testCount > 0)
            {
                Debug.Log($"\n=== [TestBed] SUMMARY ===");
                Debug.Log($"[TestBed] Songs={clips.Length}, Tests={testCount}, " +
                          $"AvgOverall={totalOverall / testCount:F1}");
            }

            Debug.Log("=== [TestBed] Beat Mapping Quality Test END ===");
        }

        /// <summary>
        /// 특정 곡 + 난이도 테스트 실행, Overall 점수 반환
        /// </summary>
        private static float RunDifficultyTest(string songName, int difficulty, OfflineAudioAnalyzer.AnalysisResult analysis)
        {
            Debug.Log($"--- [TestBed] {songName} Diff={difficulty} ---");

            // 임시 SmartBeatMapper
            var tempGo = new GameObject("_TestMapper");
            var mapper = tempGo.AddComponent<SmartBeatMapper>();
            mapper.SetDifficulty(difficulty);

            // 기본 파라미터로 노트 생성
            var notes = mapper.GenerateNotes(analysis);

            // 품질 지표 계산
            var metrics = BeatSyncMetrics.CalculateAll(notes, analysis);

            // 결과 출력
            Debug.Log($"[TestBed] {songName} Diff={difficulty} | Notes={metrics.TotalNotes} | " +
                      $"BeatAlign={metrics.BeatAlignment:F1} | DensityUniform={metrics.DensityUniformity:F1} | " +
                      $"Diversity={metrics.Diversity:F1} | Overall={metrics.OverallScore:F1}");

            // 레인 분포
            Debug.Log($"[TestBed] {songName} Diff={difficulty} | " +
                      $"Lane[ScrL={metrics.LaneDistribution[0]}, Key1={metrics.LaneDistribution[1]}, " +
                      $"Key2={metrics.LaneDistribution[2]}, ScrR={metrics.LaneDistribution[3]}]");

            // 타입 분포
            Debug.Log($"[TestBed] {songName} Diff={difficulty} | " +
                      $"Type[Tap={metrics.TypeDistribution[0]}, Long={metrics.TypeDistribution[1]}, " +
                      $"Scratch={metrics.TypeDistribution[2]}]");

            // 빈 구간 체크
            int emptyGaps = CountEmptyGaps(notes, 2f);
            Debug.Log($"[TestBed] {songName} Diff={difficulty} | EmptyGaps(>2s)={emptyGaps}");

            // AutoTuner 적용 비교
            var tunedMapping = AnalysisAutoTuner.TuneMappingParams(analysis, difficulty);
            mapper.SetMappingParams(tunedMapping);
            var notesB = mapper.GenerateNotes(analysis);
            var metricsB = BeatSyncMetrics.CalculateAll(notesB, analysis);

            float delta = metricsB.OverallScore - metrics.OverallScore;
            Debug.Log($"[TestBed] {songName} Diff={difficulty} | AutoTuned: Notes={metricsB.TotalNotes}, " +
                      $"Overall={metricsB.OverallScore:F1} (delta={delta:+0.0;-0.0;0.0})");

            Object.DestroyImmediate(tempGo);
            return metrics.OverallScore;
        }

        private static int CountEmptyGaps(List<NoteData> notes, float threshold)
        {
            if (notes == null || notes.Count < 2) return 0;

            var sorted = new List<NoteData>(notes);
            sorted.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));

            int gaps = 0;
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].HitTime - sorted[i - 1].HitTime > threshold)
                    gaps++;
            }
            return gaps;
        }
    }
}
