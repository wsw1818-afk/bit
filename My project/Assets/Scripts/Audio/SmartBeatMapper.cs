using UnityEngine;
using System.Collections.Generic;
using AIBeat.Data;

namespace AIBeat.Audio
{
    /// <summary>
    /// OfflineAudioAnalyzer 결과를 기반으로 노트 데이터 생성
    /// 온셋 → 레인 매핑, 에너지 → 노트 타입, 구간별 밀도 조절
    /// </summary>
    public class SmartBeatMapper : MonoBehaviour
    {
        [Header("Difficulty Settings")]
        [SerializeField, Range(1, 10)] private int difficulty = 5;
        [SerializeField] private float minNoteInterval = 0.15f;  // 최소 노트 간격(초)

        [Header("Note Type Ratios")]
        [SerializeField] private float longNoteThreshold = 0.7f;   // 에너지 비율 이상 → 롱노트
        [SerializeField] private float scratchNoteThreshold = 0.8f; // 저주파 강세 + 고에너지 → 스크래치
        [SerializeField] private float longNoteDuration = 0.5f;     // 롱노트 기본 지속 시간

        // 주파수 밴드 → 4레인 매핑
        // Band 0(~100Hz): 스크래치L (킥)
        // Band 1(100-200): Key1 (베이스)
        // Band 2(200-400): Key1
        // Band 3(400-800): Key2 (중앙)
        // Band 4(800-1600): Key2
        // Band 5(1600-3200): Key1
        // Band 6(3200-6400): 스크래치R (하이햇)
        // Band 7(6400+): 스크래치R
        private static readonly int[] BAND_TO_LANE = { 0, 1, 1, 2, 2, 1, 3, 3 };

        /// <summary>
        /// 분석 결과 → NoteData 리스트 변환
        /// </summary>
        public List<NoteData> GenerateNotes(OfflineAudioAnalyzer.AnalysisResult analysis)
        {
            if (analysis == null || analysis.Onsets == null || analysis.Onsets.Count == 0)
            {
                Debug.LogWarning("[SmartBeatMapper] No analysis data, generating fallback notes");
                return GenerateFallbackNotes(analysis?.Duration ?? 30f, analysis?.BPM ?? 120f);
            }

            var notes = new List<NoteData>();
            float lastNoteTime = -1f;

            // 에너지 통계 계산 (정규화용)
            float maxStrength = 0f;
            foreach (var onset in analysis.Onsets)
                if (onset.Strength > maxStrength) maxStrength = onset.Strength;

            if (maxStrength <= 0f) maxStrength = 1f;

            // 난이도에 따른 필터링 임계값
            float strengthThreshold = GetStrengthThreshold(difficulty, maxStrength);
            float adjustedMinInterval = minNoteInterval / Mathf.Lerp(0.5f, 1.5f, (difficulty - 1f) / 9f);

#if UNITY_EDITOR
            Debug.Log($"[SmartBeatMapper] Generating notes: difficulty={difficulty}, onsets={analysis.Onsets.Count}, threshold={strengthThreshold:F2}");
#endif

            // 이전 레인 기록 (같은 레인 연속 방지)
            int prevLane = -1;
            int sameCount = 0;

            for (int idx = 0; idx < analysis.Onsets.Count; idx++)
            {
                var onset = analysis.Onsets[idx];

                // 강도 필터링 (약한 온셋은 스킵)
                float normalizedStrength = onset.Strength / maxStrength;
                if (normalizedStrength < strengthThreshold) continue;

                // 최소 간격 체크
                if (onset.Time - lastNoteTime < adjustedMinInterval) continue;

                // 구간별 밀도 보정
                var section = FindSection(analysis.Sections, onset.Time);
                float densityMult = GetSectionDensityMultiplier(section);

                // 밀도가 낮은 구간에서는 강한 온셋만 통과
                if (densityMult < 1f && normalizedStrength < strengthThreshold / densityMult)
                    continue;

                // 레인 결정 (DominantBand 범위 검증)
                int bandIndex = Mathf.Clamp(onset.DominantBand, 0, 7);
                int lane = BAND_TO_LANE[bandIndex];

                // 같은 레인 연속 방지 (3회 이상이면 이웃 레인으로)
                if (lane == prevLane)
                {
                    sameCount++;
                    if (sameCount >= 3)
                    {
                        lane = ShiftLane(lane);
                        sameCount = 0;
                    }
                }
                else
                {
                    sameCount = 0;
                }

                // 노트 타입 결정
                NoteType type = DetermineNoteType(normalizedStrength, onset.DominantBand, section);
                float duration = (type == NoteType.Long) ? longNoteDuration : 0f;

                // 롱노트일 때 다음 온셋과의 간격으로 지속시간 조절
                if (type == NoteType.Long && idx < analysis.Onsets.Count - 1)
                {
                    float gap = analysis.Onsets[idx + 1].Time - onset.Time;
                    duration = Mathf.Min(gap * 0.8f, 2f); // 최대 2초
                    duration = Mathf.Max(duration, 0.3f);  // 최소 0.3초
                }

                notes.Add(new NoteData(onset.Time, lane, type, duration));
                lastNoteTime = onset.Time;
                prevLane = lane;
            }

            // 난이도에 따른 동시 타격 노트 추가
            if (difficulty >= 6)
                AddChordNotes(notes, analysis, difficulty);

            // 시간순 정렬
            notes.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));

#if UNITY_EDITOR
            Debug.Log($"[SmartBeatMapper] Generated {notes.Count} notes (difficulty={difficulty})");
#endif
            return notes;
        }

        /// <summary>
        /// 분석 결과를 SongSection 배열로 변환 (SongData용)
        /// </summary>
        public SongSection[] ConvertSections(OfflineAudioAnalyzer.AnalysisResult analysis)
        {
            if (analysis?.Sections == null) return null;

            var sections = new SongSection[analysis.Sections.Count];
            for (int i = 0; i < analysis.Sections.Count; i++)
            {
                var s = analysis.Sections[i];
                sections[i] = new SongSection(
                    s.Type.ToString().ToLower(),
                    s.StartTime,
                    s.EndTime,
                    GetSectionDensityMultiplier(s)
                );
            }
            return sections;
        }

        /// <summary>
        /// 레인을 이웃으로 이동 (같은 레인 연속 방지)
        /// </summary>
        private int ShiftLane(int lane)
        {
            if (lane == 0) return 1;
            if (lane == 3) return 2;
            return Mathf.Clamp(lane + (Random.value > 0.5f ? 1 : -1), 0, 3);
        }

        /// <summary>
        /// 노트 타입 결정 (에너지/밴드/구간 기반)
        /// </summary>
        private NoteType DetermineNoteType(float strength, int band, OfflineAudioAnalyzer.SectionData section)
        {
            // 스크래치: 고주파 밴드(6,7) + 높은 에너지 + Drop 구간
            if ((band >= 6) && strength > scratchNoteThreshold && section.Type == OfflineAudioAnalyzer.SectionType.Drop)
                return NoteType.Scratch;

            // 저주파(0,1) + 높은 에너지 → 스크래치
            if (band <= 1 && strength > scratchNoteThreshold)
                return NoteType.Scratch;

            // 롱노트: Build/Drop 구간 + 중간 에너지
            if (strength > longNoteThreshold && (section.Type == OfflineAudioAnalyzer.SectionType.Build || section.Type == OfflineAudioAnalyzer.SectionType.Drop))
                return NoteType.Long;

            return NoteType.Tap;
        }

        /// <summary>
        /// 난이도별 강도 필터링 임계값
        /// </summary>
        private float GetStrengthThreshold(int diff, float maxStrength)
        {
            // 난이도 1: 상위 20%만, 난이도 10: 거의 모든 온셋
            float ratio = Mathf.Lerp(0.5f, 0.05f, (diff - 1f) / 9f);
            return ratio;
        }

        /// <summary>
        /// 구간별 밀도 배율
        /// </summary>
        private float GetSectionDensityMultiplier(OfflineAudioAnalyzer.SectionData section)
        {
            return section.Type switch
            {
                OfflineAudioAnalyzer.SectionType.Intro => 0.4f,
                OfflineAudioAnalyzer.SectionType.Build => 0.8f,
                OfflineAudioAnalyzer.SectionType.Drop => 1.5f,
                OfflineAudioAnalyzer.SectionType.Outro => 0.3f,
                OfflineAudioAnalyzer.SectionType.Calm => 0.5f,
                _ => 1f
            };
        }

        /// <summary>
        /// 현재 시간이 속한 구간 찾기
        /// </summary>
        private OfflineAudioAnalyzer.SectionData FindSection(List<OfflineAudioAnalyzer.SectionData> sections, float time)
        {
            if (sections == null || sections.Count == 0)
                return new OfflineAudioAnalyzer.SectionData
                {
                    StartTime = 0, EndTime = float.MaxValue,
                    Type = OfflineAudioAnalyzer.SectionType.Build, AverageEnergy = 0.5f
                };

            foreach (var s in sections)
                if (time >= s.StartTime && time < s.EndTime) return s;

            return sections[sections.Count - 1];
        }

        /// <summary>
        /// 동시 타격 노트 추가 (고난이도)
        /// </summary>
        private void AddChordNotes(List<NoteData> notes, OfflineAudioAnalyzer.AnalysisResult analysis, int diff)
        {
            float chordChance = Mathf.Lerp(0.05f, 0.3f, (diff - 6f) / 4f);
            int originalCount = notes.Count;
            var toAdd = new List<NoteData>();

            for (int i = 0; i < originalCount; i++)
            {
                if (Random.value > chordChance) continue;

                var note = notes[i];
                // 스크래치에는 동시타격 안 넣음
                if (note.Type == NoteType.Scratch) continue;
                if (note.LaneIndex == 0 || note.LaneIndex == 3) continue;

                // 인접 레인에 동시타격 (2키만이므로 1↔2)
                int chordLane = note.LaneIndex == 1 ? 2 : 1;

                // 모바일 안전 제약: 같은 엄지로 스크래치+키 동시 불가
                // 같은 타이밍에 Scratch(Lane 0)+Lane 1 또는 Scratch(Lane 3)+Lane 2 방지
                if (chordLane == 1 || chordLane == 2)
                {
                    int adjacentScratchLane = (chordLane == 1) ? 0 : 3;
                    bool hasScratchAtSameTime = false;
                    foreach (var n in notes)
                        if (Mathf.Abs(n.HitTime - note.HitTime) < 0.01f && n.LaneIndex == adjacentScratchLane)
                        { hasScratchAtSameTime = true; break; }
                    if (hasScratchAtSameTime) continue;
                }

                // 같은 시간에 같은 레인이 이미 있는지 확인
                bool exists = false;
                foreach (var n in notes)
                    if (Mathf.Abs(n.HitTime - note.HitTime) < 0.01f && n.LaneIndex == chordLane) { exists = true; break; }
                foreach (var n in toAdd)
                    if (Mathf.Abs(n.HitTime - note.HitTime) < 0.01f && n.LaneIndex == chordLane) { exists = true; break; }

                if (!exists)
                    toAdd.Add(new NoteData(note.HitTime, chordLane, NoteType.Tap));
            }

            notes.AddRange(toAdd);
        }

        /// <summary>
        /// 분석 실패 시 BPM 기반 폴백 노트 생성
        /// </summary>
        private List<NoteData> GenerateFallbackNotes(float duration, float bpm)
        {
            var notes = new List<NoteData>();
            float beatInterval = 60f / bpm;
            float startTime = 2f; // 2초 후 시작

            for (float t = startTime; t < duration - 1f; t += beatInterval)
            {
                int lane = Random.Range(1, 3); // 1~2 레인
                notes.Add(new NoteData(t, lane, NoteType.Tap));
            }

#if UNITY_EDITOR
            Debug.Log($"[SmartBeatMapper] Fallback: Generated {notes.Count} notes (BPM={bpm})");
#endif
            return notes;
        }
    }
}
