using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AIBeat.Audio;
using AIBeat.Data;

namespace AIBeat.Editor
{
    /// <summary>
    /// A/B 파라미터 비교 에디터 윈도우
    /// Set A(기본 파라미터) vs Set B(AutoTuner 최적화) 품질 비교
    /// </summary>
    public class BeatSyncAnalyzerWindow : EditorWindow
    {
        private AudioClip testClip;
        private int testDifficulty = 5;

        // 분석 결과
        private OfflineAudioAnalyzer.AnalysisResult analysisResult;
        private BeatSyncMetrics.MetricsResult metricsA; // Set A: 기본
        private BeatSyncMetrics.MetricsResult metricsB; // Set B: AutoTuned
        private AnalysisParams tunedParams;
        private MappingParams tunedMappingParams;
        private bool hasResults;
        private bool isAnalyzing;
        private float analyzeProgress;
        private string statusMessage = "";

        private Vector2 scrollPos;

        [MenuItem("Tools/A.I. BEAT/Beat Sync Analyzer")]
        static void ShowWindow()
        {
            var window = GetWindow<BeatSyncAnalyzerWindow>("Beat Sync Analyzer");
            window.minSize = new Vector2(500, 600);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawHeader();
            DrawInputSection();

            if (isAnalyzing)
                DrawProgressBar();

            if (hasResults)
            {
                DrawComparisonTable();
                DrawParamsDiff();
                DrawDensityHistogram();
                DrawDistributions();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Beat Sync A/B Analyzer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "AudioClip을 분석하여 기본 파라미터(Set A)와 AutoTuner 최적화 파라미터(Set B)의 품질을 비교합니다.",
                MessageType.Info);
            EditorGUILayout.Space(5);
        }

        private void DrawInputSection()
        {
            EditorGUILayout.BeginVertical("box");
            testClip = (AudioClip)EditorGUILayout.ObjectField("AudioClip", testClip, typeof(AudioClip), false);
            testDifficulty = EditorGUILayout.IntSlider("Difficulty", testDifficulty, 1, 10);

            EditorGUI.BeginDisabledGroup(testClip == null || isAnalyzing);
            if (GUILayout.Button("Analyze & Compare", GUILayout.Height(30)))
                RunAnalysis();
            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrEmpty(statusMessage))
                EditorGUILayout.LabelField(statusMessage, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawProgressBar()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(rect, analyzeProgress, $"Analyzing... {analyzeProgress * 100:F0}%");
        }

        private void DrawComparisonTable()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Quality Comparison", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // 헤더
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Metric", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("Set A (Default)", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label("Set B (AutoTuned)", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label("Delta", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            DrawMetricRow("Beat Alignment", metricsA.BeatAlignment, metricsB.BeatAlignment);
            DrawMetricRow("Density Uniformity", metricsA.DensityUniformity, metricsB.DensityUniformity);
            DrawMetricRow("Diversity", metricsA.Diversity, metricsB.Diversity);

            EditorGUILayout.Space(3);
            // 종합 점수 (굵게)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Overall Score", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label($"{metricsA.OverallScore:F1}", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label($"{metricsB.OverallScore:F1}", EditorStyles.boldLabel, GUILayout.Width(120));
            float delta = metricsB.OverallScore - metricsA.OverallScore;
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = delta > 0 ? new Color(0.2f, 0.8f, 0.2f) : (delta < 0 ? Color.red : Color.white);
            GUILayout.Label($"{(delta > 0 ? "+" : "")}{delta:F1}", style, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            // 노트 수
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Total Notes", GUILayout.Width(150));
            GUILayout.Label($"{metricsA.TotalNotes}", GUILayout.Width(120));
            GUILayout.Label($"{metricsB.TotalNotes}", GUILayout.Width(120));
            int noteDelta = metricsB.TotalNotes - metricsA.TotalNotes;
            GUILayout.Label($"{(noteDelta > 0 ? "+" : "")}{noteDelta}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("BPM", GUILayout.Width(150));
            GUILayout.Label($"{metricsA.BPM:F1}", GUILayout.Width(120));
            GUILayout.Label($"{metricsB.BPM:F1}", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawMetricRow(string label, float valueA, float valueB)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150));
            GUILayout.Label($"{valueA:F1}", GUILayout.Width(120));
            GUILayout.Label($"{valueB:F1}", GUILayout.Width(120));
            float d = valueB - valueA;
            var color = d > 1f ? new Color(0.2f, 0.8f, 0.2f) : (d < -1f ? Color.red : Color.gray);
            var s = new GUIStyle(EditorStyles.label) { normal = { textColor = color } };
            GUILayout.Label($"{(d > 0 ? "+" : "")}{d:F1}", s, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawParamsDiff()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("AutoTuner Parameter Changes", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            var def = AnalysisParams.Default;
            DrawParamChange("Onset Alpha", def.OnsetThresholdAlpha, tunedParams.OnsetThresholdAlpha);
            DrawParamChange("Min Onset Interval", def.MinOnsetInterval, tunedParams.MinOnsetInterval);
            DrawParamChange("Low Band Weight", def.LowBandWeight, tunedParams.LowBandWeight);
            DrawParamChange("Low Boost Factor", def.LowBoostFactor, tunedParams.LowBoostFactor);
            DrawParamChange("Section Size", def.SectionSize, tunedParams.SectionSize);
            DrawParamChange("Preferred BPM Min", def.PreferredBPMMin, tunedParams.PreferredBPMMin);
            DrawParamChange("Preferred BPM Max", def.PreferredBPMMax, tunedParams.PreferredBPMMax);

            EditorGUILayout.Space(3);
            var mDef = MappingParams.Default;
            DrawParamChange("Note Interval", mDef.MinNoteInterval, tunedMappingParams.MinNoteInterval);
            DrawParamChange("Long Note Thresh", mDef.LongNoteThreshold, tunedMappingParams.LongNoteThreshold);
            DrawParamChange("Scratch Thresh", mDef.ScratchNoteThreshold, tunedMappingParams.ScratchNoteThreshold);
            DrawParamChange("Drop Density", mDef.DropDensity, tunedMappingParams.DropDensity);
            DrawParamChange("Snap Tolerance", mDef.BeatSnapTolerance, tunedMappingParams.BeatSnapTolerance);
            DrawParamChange("Long Note Dur", mDef.LongNoteDuration, tunedMappingParams.LongNoteDuration);

            EditorGUILayout.EndVertical();
        }

        private void DrawParamChange(string label, float original, float tuned)
        {
            bool changed = Mathf.Abs(original - tuned) > 0.001f;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150));
            GUILayout.Label($"{original:F3}", GUILayout.Width(80));
            GUILayout.Label("→", GUILayout.Width(20));
            var s = new GUIStyle(EditorStyles.label);
            if (changed)
                s.normal.textColor = new Color(1f, 0.8f, 0.2f);
            GUILayout.Label($"{tuned:F3}", s, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDensityHistogram()
        {
            if (metricsA.DensityHistogram == null || metricsB.DensityHistogram == null) return;

            EditorGUILayout.Space(10);
            GUILayout.Label("Density Histogram (notes/sec)", EditorStyles.boldLabel);

            float maxDensity = 1f;
            foreach (var v in metricsA.DensityHistogram)
                if (v > maxDensity) maxDensity = v;
            foreach (var v in metricsB.DensityHistogram)
                if (v > maxDensity) maxDensity = v;

            int count = Mathf.Max(metricsA.DensityHistogram.Length, metricsB.DensityHistogram.Length);
            float histHeight = 80f;
            var rect = EditorGUILayout.GetControlRect(false, histHeight + 20);

            // 배경
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            float barWidth = rect.width / Mathf.Max(count, 1);
            for (int i = 0; i < count; i++)
            {
                float valA = (i < metricsA.DensityHistogram.Length) ? metricsA.DensityHistogram[i] : 0f;
                float valB = (i < metricsB.DensityHistogram.Length) ? metricsB.DensityHistogram[i] : 0f;

                float hA = (valA / maxDensity) * histHeight;
                float hB = (valB / maxDensity) * histHeight;

                float x = rect.x + i * barWidth;
                float halfBar = barWidth * 0.45f;

                // Set A = 파란색 (왼쪽)
                var barA = new Rect(x, rect.y + histHeight - hA, halfBar, hA);
                EditorGUI.DrawRect(barA, new Color(0.3f, 0.5f, 0.9f, 0.8f));

                // Set B = 초록색 (오른쪽)
                var barB = new Rect(x + halfBar, rect.y + histHeight - hB, halfBar, hB);
                EditorGUI.DrawRect(barB, new Color(0.3f, 0.9f, 0.4f, 0.8f));
            }

            // 범례
            EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 10, GUILayout.Width(10)), new Color(0.3f, 0.5f, 0.9f));
            GUILayout.Label("Set A (Default)", GUILayout.Width(100));
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 10, GUILayout.Width(10)), new Color(0.3f, 0.9f, 0.4f));
            GUILayout.Label("Set B (AutoTuned)", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDistributions()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Distributions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // 레인 분포
            GUILayout.Label("Lane Distribution:");
            EditorGUILayout.BeginHorizontal();
            string[] laneNames = { "ScrL(0)", "Key1(1)", "Key2(2)", "ScrR(3)" };
            for (int i = 0; i < 4; i++)
            {
                int a = (metricsA.LaneDistribution != null && i < metricsA.LaneDistribution.Length) ? metricsA.LaneDistribution[i] : 0;
                int b = (metricsB.LaneDistribution != null && i < metricsB.LaneDistribution.Length) ? metricsB.LaneDistribution[i] : 0;
                GUILayout.Label($"{laneNames[i]}: {a}→{b}", GUILayout.Width(110));
            }
            EditorGUILayout.EndHorizontal();

            // 타입 분포
            GUILayout.Label("Type Distribution:");
            EditorGUILayout.BeginHorizontal();
            string[] typeNames = { "Tap", "Long", "Scratch" };
            for (int i = 0; i < 3; i++)
            {
                int a = (metricsA.TypeDistribution != null && i < metricsA.TypeDistribution.Length) ? metricsA.TypeDistribution[i] : 0;
                int b = (metricsB.TypeDistribution != null && i < metricsB.TypeDistribution.Length) ? metricsB.TypeDistribution[i] : 0;
                GUILayout.Label($"{typeNames[i]}: {a}→{b}", GUILayout.Width(110));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void RunAnalysis()
        {
            if (testClip == null) return;

            isAnalyzing = true;
            hasResults = false;
            statusMessage = "Analyzing...";
            analyzeProgress = 0f;

            // 임시 GameObject (SmartBeatMapper는 MonoBehaviour)
            var tempGo = new GameObject("_TempMapper");
            var mapper = tempGo.AddComponent<SmartBeatMapper>();
            mapper.SetDifficulty(testDifficulty);

            // Set A: 기본 파라미터로 동기 분석
            var analyzer = new OfflineAudioAnalyzer();
            analysisResult = analyzer.Analyze(testClip);

            if (analysisResult == null)
            {
                statusMessage = "Analysis failed!";
                DestroyImmediate(tempGo);
                isAnalyzing = false;
                Repaint();
                return;
            }

            // Set A 노트 생성 + 품질 측정
            var notesA = mapper.GenerateNotes(analysisResult);
            metricsA = BeatSyncMetrics.CalculateAll(notesA, analysisResult);

            // AutoTuner로 파라미터 튜닝
            tunedParams = AnalysisAutoTuner.TuneAnalysisParams(analysisResult);
            tunedMappingParams = AnalysisAutoTuner.TuneMappingParams(analysisResult, testDifficulty);

            bool needsReanalysis = AnalysisAutoTuner.NeedsReanalysis(AnalysisParams.Default, tunedParams);

            OfflineAudioAnalyzer.AnalysisResult resultForB = analysisResult;
            if (needsReanalysis)
            {
                // Set B: 튜닝된 파라미터로 재분석
                statusMessage = "Re-analyzing with tuned params...";
                var analyzer2 = new OfflineAudioAnalyzer();
                resultForB = analyzer2.Analyze(testClip, tunedParams) ?? analysisResult;
                tunedMappingParams = AnalysisAutoTuner.TuneMappingParams(resultForB, testDifficulty);
            }

            // Set B 노트 생성 + 품질 측정
            mapper.SetMappingParams(tunedMappingParams);
            var notesB = mapper.GenerateNotes(resultForB);
            metricsB = BeatSyncMetrics.CalculateAll(notesB, resultForB);

            // 정리
            DestroyImmediate(tempGo);
            isAnalyzing = false;
            hasResults = true;
            analyzeProgress = 1f;

            float delta = metricsB.OverallScore - metricsA.OverallScore;
            statusMessage = delta > 0
                ? $"AutoTuner improved score by +{delta:F1} points"
                : $"AutoTuner score delta: {delta:F1} points";

            Repaint();
        }
    }
}
