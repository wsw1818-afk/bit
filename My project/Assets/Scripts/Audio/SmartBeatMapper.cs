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
        [SerializeField] private float longNoteThreshold = 0.55f;  // 에너지 비율 이상 → 롱노트 (이전 0.7)
        [SerializeField] private float scratchNoteThreshold = 0.65f; // 저주파 강세 + 고에너지 → 스크래치 (이전 0.8)
        [SerializeField] private float longNoteDuration = 0.5f;     // 롱노트 기본 지속 시간

        // 커스텀 매핑 파라미터 (AutoTuner에서 주입)
        private MappingParams? customMappingParams;

        // 주파수 밴드 → 4레인 매핑
        // Band 0(~100Hz): 스크래치L (킥)
        // Band 1(100-200): Key1 (베이스)
        // Band 2(200-400): Key1
        // Band 3(400-800): Key2 (중앙)
        // Band 4(800-1600): Key2
        // Band 5(1600-3200): Key1
        // Band 6(3200-6400): 스크래치R (하이햇)
        // Band 7(6400+): 스크래치R
        // 개선: 4레인 균등 분배 (이전: { 0, 1, 1, 2, 2, 1, 3, 3 } → Lane 1에 3밴드 편중)
        private static readonly int[] BAND_TO_LANE = { 0, 1, 2, 1, 2, 3, 0, 3 };

        /// <summary>
        /// 외부에서 난이도 설정 (SongData.Difficulty 연동)
        /// </summary>
        public void SetDifficulty(int diff)
        {
            difficulty = Mathf.Clamp(diff, 1, 10);
        }

        /// <summary>
        /// AutoTuner에서 최적화된 매핑 파라미터 주입
        /// 미호출 시 기존 SerializeField 값 사용
        /// </summary>
        public void SetMappingParams(MappingParams p)
        {
            customMappingParams = p;
#if UNITY_EDITOR
            Debug.Log($"[SmartBeatMapper] Custom mapping params set: interval={p.MinNoteInterval:F2}, " +
                      $"longThresh={p.LongNoteThreshold:F2}, dropDensity={p.DropDensity:F1}");
#endif
        }

        /// <summary>
        /// 커스텀 파라미터 초기화 (기존 SerializeField로 복귀)
        /// </summary>
        public void ClearMappingParams()
        {
            customMappingParams = null;
        }

        /// <summary>
        /// 분석 결과 → NoteData 리스트 변환
        /// </summary>
        public List<NoteData> GenerateNotes(OfflineAudioAnalyzer.AnalysisResult analysis)
        {
            if (analysis == null || analysis.Onsets == null || analysis.Onsets.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[SmartBeatMapper] No analysis data, generating fallback notes");
#endif
                return GenerateFallbackNotes(analysis?.Duration ?? 30f, analysis?.BPM ?? 120f);
            }

            var notes = new List<NoteData>();
            float lastNoteTime = -1f;

            // 레인 사용 카운터 초기화 (동적 분산)
            laneUsageCount = new int[4];

            // 에너지 통계 계산 (정규화용)
            float maxStrength = 0f;
            foreach (var onset in analysis.Onsets)
                if (onset.Strength > maxStrength) maxStrength = onset.Strength;

            if (maxStrength <= 0f) maxStrength = 1f;

            // 커스텀 파라미터 or SerializeField 값
            float effectiveMinInterval = customMappingParams?.MinNoteInterval ?? minNoteInterval;
            int effectiveMaxSameLaneRepeat = customMappingParams?.MaxSameLaneRepeat ?? 2;

            // Onset 노이즈 필터링: 연속 3개 이상이 0.15초 미만 간격이면 중간 것들 제거
            var filteredOnsets = FilterNoiseOnsets(analysis.Onsets);

            // 난이도에 따른 필터링 임계값
            float strengthThreshold = GetStrengthThreshold(difficulty, maxStrength);
            // 난이도가 높을수록 간격이 좁아져 더 많은 노트 허용
            float adjustedMinInterval = effectiveMinInterval / Mathf.Lerp(0.8f, 2.0f, (difficulty - 1f) / 9f);

#if UNITY_EDITOR
            Debug.Log($"[SmartBeatMapper] Generating notes: difficulty={difficulty}, onsets={filteredOnsets.Count}/{analysis.Onsets.Count} (filtered), threshold={strengthThreshold:F2}");
#endif

            // 이전 레인 기록 (같은 레인 연속 방지)
            int prevLane = -1;
            int sameCount = 0;

            for (int idx = 0; idx < filteredOnsets.Count; idx++)
            {
                var onset = filteredOnsets[idx];

                // 강도 필터링 (약한 온셋은 스킵)
                float normalizedStrength = onset.Strength / maxStrength;
                if (normalizedStrength < strengthThreshold) continue;

                // 최소 간격 체크
                if (onset.Time - lastNoteTime < adjustedMinInterval) continue;

                // 구간별 밀도 보정
                var section = FindSection(analysis.Sections, onset.Time);
                float densityMult = GetSectionDensityMultiplier(section);

                // 밀도가 낮은 구간: 임계값을 소폭 올려 노트 수 조절 (과도한 필터링 방지)
                if (densityMult < 1f)
                {
                    // 0.8배율 → threshold +5%, 0.7배율 → threshold +10% (이전: Lerp(t,1,1-d) → 과잉 제거)
                    float adjustedThreshold = strengthThreshold + (1f - densityMult) * 0.3f;
                    adjustedThreshold = Mathf.Min(adjustedThreshold, strengthThreshold * 2f); // 최대 2배
                    if (normalizedStrength < adjustedThreshold) continue;
                }

                // 레인 결정 (DominantBand 범위 검증)
                int bandIndex = Mathf.Clamp(onset.DominantBand, 0, 7);
                int lane = BAND_TO_LANE[bandIndex];

                // 같은 레인 연속 방지 (N회 이상이면 이웃 레인으로)
                if (lane == prevLane)
                {
                    sameCount++;
                    if (sameCount >= effectiveMaxSameLaneRepeat)
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
                float effectiveLongDuration = customMappingParams?.LongNoteDuration ?? longNoteDuration;
                float duration = (type == NoteType.Long) ? effectiveLongDuration : 0f;

                // 롱노트일 때 다음 온셋과의 간격으로 지속시간 조절
                if (type == NoteType.Long && idx < filteredOnsets.Count - 1)
                {
                    float gap = filteredOnsets[idx + 1].Time - onset.Time;
                    duration = Mathf.Min(gap * 0.8f, 2f); // 최대 2초
                    duration = Mathf.Max(duration, 0.3f);  // 최소 0.3초
                }

                // BPM 기반 beat snap: 구간에 따라 snap 강도 조절
                float sectionSnapTolerance = GetSectionSnapTolerance(section);
                float snappedTime = SnapToBeat(onset.Time, analysis.BPM, analysis.BeatPhaseOffset, sectionSnapTolerance);
                // snap 후 이전 노트와 너무 가까우면 스킵
                if (snappedTime - lastNoteTime < adjustedMinInterval) continue;

                notes.Add(new NoteData(snappedTime, lane, type, duration));
                lastNoteTime = snappedTime;
                prevLane = lane;
                laneUsageCount[lane]++;
            }

            // 구간별 최소 노트 보장: 빈 구간이 없도록 비트 그리드로 보충
            FillEmptySections(notes, analysis);

            // 비트 기반 보충 노트: onset 기반만으로 부족하면 BPM 그리드에 맞춰 추가
            int minNotesExpected = Mathf.RoundToInt(analysis.Duration * Mathf.Lerp(0.8f, 3.0f, (difficulty - 1f) / 9f));
            if (notes.Count < minNotesExpected)
            {
                AddBeatFillerNotes(notes, analysis, minNotesExpected - notes.Count);
            }

            // 난이도에 따른 동시 타격 노트 추가
            if (difficulty >= 6)
                AddChordNotes(notes, analysis, difficulty);

            // 밀도 균일화 패스: 같은 섹션 내에서 과밀→소밀 구간으로 노트 재배치
            SmoothSectionDensity(notes, analysis);

            // 타입 다양성 보장: 최소 Long/Scratch 비율 보정
            EnsureMinTypeRatios(notes, analysis);

            // 레인 밸런싱 패스: 과잉 레인 → 부족 레인으로 이동
            BalanceLaneDistribution(notes);

            // 시간순 정렬
            notes.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));

#if UNITY_EDITOR
            Debug.Log($"[SmartBeatMapper] Generated {notes.Count} notes (difficulty={difficulty}, BPM={analysis.BPM}, " +
                      $"minExpected={minNotesExpected}, onsets={analysis.Onsets.Count})");
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
        /// onset 시간을 가장 가까운 박자 그리드에 정렬
        /// 8분음표(1/2박) 기반 그리드 → 16분음표보다 안정적인 비트 정렬
        /// phase offset으로 비트 그리드 원점을 곡의 첫 박자에 맞춤
        /// sectionTolerance: 구간별 snap 강도 (0=원본유지, 1=완전 snap)
        /// </summary>
        private float SnapToBeat(float time, float bpm, float phaseOffset = 0f, float sectionTolerance = -1f)
        {
            if (bpm <= 0f) return time;

            float beatInterval = 60f / bpm;          // 1박 간격
            float subdivInterval = beatInterval / 2f; // 1/2박(8분음표) 그리드

            // phase offset 적용: 그리드 원점을 곡의 첫 박자 위치로 이동
            float relative = time - phaseOffset;

            // 가장 가까운 그리드 포인트 계산
            float gridIndex = Mathf.Round(relative / subdivInterval);
            float snapped = gridIndex * subdivInterval + phaseOffset;

            // snap tolerance: 구간별 오버라이드 or 기본값
            float snapTolerance = sectionTolerance >= 0f ? sectionTolerance
                : (customMappingParams?.BeatSnapTolerance ?? 0.8f);
            float maxSnapDistance = subdivInterval * snapTolerance;
            if (Mathf.Abs(snapped - time) > maxSnapDistance)
                return time;

            return Mathf.Max(0f, snapped); // 음수 방지
        }

        /// <summary>
        /// 구간 타입별 BeatSnap 강도 반환
        /// Drop: 타이트하게 snap (0.5) → 비트감 강조
        /// Build: 보통 (0.7)
        /// Intro/Outro/Calm: 여유롭게 snap (0.9) → 자연스러운 배치
        /// </summary>
        private float GetSectionSnapTolerance(OfflineAudioAnalyzer.SectionData section)
        {
            return section.Type switch
            {
                OfflineAudioAnalyzer.SectionType.Drop => 0.5f,
                OfflineAudioAnalyzer.SectionType.Build => 0.7f,
                OfflineAudioAnalyzer.SectionType.Intro => 0.9f,
                OfflineAudioAnalyzer.SectionType.Outro => 0.9f,
                OfflineAudioAnalyzer.SectionType.Calm => 0.9f,
                _ => 0.8f
            };
        }

        // 레인별 누적 사용 횟수 (동적 분산용)
        private int[] laneUsageCount = new int[4];

        /// <summary>
        /// 레인을 이웃으로 이동 (히스토리 기반 분산)
        /// 가장 적게 사용된 인접 레인을 우선 선택
        /// </summary>
        private int ShiftLane(int lane)
        {
            // 인접 레인 후보 결정
            int[] candidates;
            if (lane == 0) candidates = new[] { 1, 2 };
            else if (lane == 3) candidates = new[] { 2, 1 };
            else candidates = new[] { lane - 1, lane + 1 };

            // 가장 적게 사용된 레인 선택
            int best = candidates[0];
            for (int i = 1; i < candidates.Length; i++)
            {
                if (laneUsageCount[candidates[i]] < laneUsageCount[best])
                    best = candidates[i];
                else if (laneUsageCount[candidates[i]] == laneUsageCount[best] && Random.value > 0.5f)
                    best = candidates[i];
            }
            return best;
        }

        /// <summary>
        /// 노트 타입 결정 (에너지/밴드/구간 기반)
        /// 타입 다양성을 위해 Scratch/Long 조건을 확장
        /// </summary>
        private NoteType DetermineNoteType(float strength, int band, OfflineAudioAnalyzer.SectionData section)
        {
            float effectiveScratchThresh = customMappingParams?.ScratchNoteThreshold ?? scratchNoteThreshold;
            float effectiveLongThresh = customMappingParams?.LongNoteThreshold ?? longNoteThreshold;

            // 스크래치: 고주파 밴드(6,7) + 높은 에너지 (모든 활성 구간)
            if ((band >= 6) && strength > effectiveScratchThresh &&
                section.Type != OfflineAudioAnalyzer.SectionType.Outro)
                return NoteType.Scratch;

            // 저주파(0,1) + 높은 에너지 → 스크래치
            if (band <= 1 && strength > effectiveScratchThresh)
                return NoteType.Scratch;

            // 롱노트: 높은 에너지 (모든 구간에서 가능, Outro 제외)
            if (strength > effectiveLongThresh && section.Type != OfflineAudioAnalyzer.SectionType.Outro)
                return NoteType.Long;

            return NoteType.Tap;
        }

        /// <summary>
        /// 난이도별 강도 필터링 임계값
        /// 낮을수록 더 많은 onset이 노트로 변환됨
        /// </summary>
        private float GetStrengthThreshold(int diff, float maxStrength)
        {
            // 난이도 1: 상위 30%만 (0.35), 난이도 10: 거의 모든 온셋 (0.02)
            // 이전 값(0.5~0.05)에서 전체적으로 낮춰서 노트 수 증가
            float ratio = Mathf.Lerp(0.35f, 0.02f, (diff - 1f) / 9f);
            return ratio;
        }

        /// <summary>
        /// 구간별 밀도 배율
        /// </summary>
        private float GetSectionDensityMultiplier(OfflineAudioAnalyzer.SectionData section)
        {
            if (customMappingParams.HasValue)
            {
                var mp = customMappingParams.Value;
                return section.Type switch
                {
                    OfflineAudioAnalyzer.SectionType.Intro => mp.IntroDensity,
                    OfflineAudioAnalyzer.SectionType.Build => mp.BuildDensity,
                    OfflineAudioAnalyzer.SectionType.Drop => mp.DropDensity,
                    OfflineAudioAnalyzer.SectionType.Outro => mp.OutroDensity,
                    OfflineAudioAnalyzer.SectionType.Calm => mp.CalmDensity,
                    _ => 1f
                };
            }

            return section.Type switch
            {
                OfflineAudioAnalyzer.SectionType.Intro => 0.8f,
                OfflineAudioAnalyzer.SectionType.Build => 0.9f,
                OfflineAudioAnalyzer.SectionType.Drop => 1.5f,
                OfflineAudioAnalyzer.SectionType.Outro => 0.7f,
                OfflineAudioAnalyzer.SectionType.Calm => 0.8f,
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
            float chordMin = customMappingParams?.ChordChanceMin ?? 0.05f;
            float chordMax = customMappingParams?.ChordChanceMax ?? 0.30f;
            float chordChance = Mathf.Lerp(chordMin, chordMax, (diff - 6f) / 4f);
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
        /// 구간별 빈 구간을 감지하여 비트 그리드 노트로 보충
        /// 2초 이상 노트가 없는 구간에 최소한의 노트를 추가
        /// </summary>
        private void FillEmptySections(List<NoteData> notes, OfflineAudioAnalyzer.AnalysisResult analysis)
        {
            if (analysis.BPM <= 0f) return;

            float beatInterval = 60f / analysis.BPM;
            float phase = analysis.BeatPhaseOffset;
            // 난이도에 따른 빈 구간 허용 시간 (쉬움: 4박, 어려움: 2박)
            float maxGap = beatInterval * Mathf.Lerp(4f, 2f, (difficulty - 1f) / 9f);
            maxGap = Mathf.Max(maxGap, 1.5f); // 최소 1.5초는 보장

            // 시간순 정렬 (이미 정렬되어 있어야 하지만 안전장치)
            notes.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));

            var toAdd = new List<NoteData>();
            float prevTime = 0f;
            float endTime = analysis.Duration - 1f;

            // 노트 사이의 빈 구간 탐색
            for (int i = 0; i <= notes.Count; i++)
            {
                float currTime = (i < notes.Count) ? notes[i].HitTime : endTime;

                float gap = currTime - prevTime;
                if (gap > maxGap)
                {
                    // 빈 구간을 비트 그리드로 채움
                    float fillStart = prevTime + beatInterval;
                    float fillEnd = currTime - beatInterval * 0.5f;

                    int fillLaneIdx = 0;
                    int[] fillLanes = { 1, 2, 0, 3 }; // 4레인 라운드로빈
                    for (float t = phase + Mathf.Ceil((fillStart - phase) / beatInterval) * beatInterval;
                         t < fillEnd; t += beatInterval)
                    {
                        int lane = fillLanes[fillLaneIdx % fillLanes.Length];
                        fillLaneIdx++;
                        toAdd.Add(new NoteData(t, lane, NoteType.Tap));
                    }
                }

                if (i < notes.Count)
                    prevTime = notes[i].HitTime;
            }

            if (toAdd.Count > 0)
            {
                notes.AddRange(toAdd);
#if UNITY_EDITOR
                Debug.Log($"[SmartBeatMapper] Filled {toAdd.Count} notes in empty sections (maxGap={maxGap:F2}s)");
#endif
            }
        }

        /// <summary>
        /// BPM 그리드 기반 보충 노트 추가
        /// onset 기반 노트가 부족할 때, 비트 그리드의 빈 위치에 노트를 채움
        /// </summary>
        private void AddBeatFillerNotes(List<NoteData> notes, OfflineAudioAnalyzer.AnalysisResult analysis, int countNeeded)
        {
            if (analysis.BPM <= 0f || countNeeded <= 0) return;

            float beatInterval = 60f / analysis.BPM;
            float subdivInterval = beatInterval / 2f; // 8분음표 그리드
            float phase = analysis.BeatPhaseOffset;
            float startTime = 1f; // 첫 1초는 비움
            float endTime = analysis.Duration - 1f;

            // 기존 노트 시간을 HashSet으로 빠르게 검색
            var existingTimes = new HashSet<float>();
            foreach (var n in notes)
                existingTimes.Add(Mathf.Round(n.HitTime / 0.01f) * 0.01f); // 10ms 단위로 양자화

            // 비트 그리드의 빈 위치 수집
            var candidates = new List<float>();
            for (float t = phase + Mathf.Ceil((startTime - phase) / subdivInterval) * subdivInterval;
                 t < endTime; t += subdivInterval)
            {
                float rounded = Mathf.Round(t / 0.01f) * 0.01f;
                if (!existingTimes.Contains(rounded))
                    candidates.Add(t);
            }

            // 강한 비트(정박) 위치 우선 + 랜덤 셔플
            // 정박 = beatInterval 정수배 위치에 가까운 것
            candidates.Sort((a, b) =>
            {
                float relA = (a - phase) % beatInterval;
                float relB = (b - phase) % beatInterval;
                float distA = Mathf.Min(relA, beatInterval - relA);
                float distB = Mathf.Min(relB, beatInterval - relB);
                return distA.CompareTo(distB); // 정박에 가까운 것이 우선
            });

            int added = 0;
            float lastAddedTime = -1f;
            int fillerLaneIdx = 0;
            int[] fillerLanes = { 1, 2, 0, 3 }; // 4레인 라운드로빈
            foreach (float t in candidates)
            {
                if (added >= countNeeded) break;
                if (t - lastAddedTime < 0.15f) continue; // 최소 간격 보장

                int lane = fillerLanes[fillerLaneIdx % fillerLanes.Length];
                fillerLaneIdx++;
                notes.Add(new NoteData(t, lane, NoteType.Tap));
                lastAddedTime = t;
                added++;
            }

#if UNITY_EDITOR
            Debug.Log($"[SmartBeatMapper] Added {added} filler notes on beat grid (needed={countNeeded})");
#endif
        }

        /// <summary>
        /// 섹션 내 밀도 균일화: 8초 윈도우 기준으로 2단계 밸런싱
        /// Pass 1: 인접 윈도우 밀도 비율 밸런싱 (국소 연속성)
        /// Pass 2: 구간 전체 over/under 글로벌 매칭 (전체 균일도)
        /// </summary>
        private void SmoothSectionDensity(List<NoteData> notes, OfflineAudioAnalyzer.AnalysisResult analysis)
        {
            if (notes == null || notes.Count < 10 || analysis.Sections == null) return;

            notes.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));
            float windowSize = 8f;
            float stepSize = 4f;
            int totalMoved = 0;
            int maxMoves = notes.Count / 4; // 최대 25% 이동

            foreach (var section in analysis.Sections)
            {
                float secDur = section.EndTime - section.StartTime;
                if (secDur < windowSize * 2f) continue;

                // 이 구간 내 노트 인덱스 수집
                var indices = new List<int>();
                for (int i = 0; i < notes.Count; i++)
                    if (notes[i].HitTime >= section.StartTime && notes[i].HitTime < section.EndTime)
                        indices.Add(i);

                if (indices.Count < 4) continue;

                // 8초 윈도우별 노트 인덱스 수집
                var windows = new List<List<int>>();
                var windowStarts = new List<float>();
                for (float wStart = section.StartTime; wStart + windowSize <= section.EndTime; wStart += stepSize)
                {
                    var wNotes = new List<int>();
                    foreach (int idx in indices)
                        if (notes[idx].HitTime >= wStart && notes[idx].HitTime < wStart + windowSize)
                            wNotes.Add(idx);
                    windows.Add(wNotes);
                    windowStarts.Add(wStart);
                }

                if (windows.Count < 2) continue;

                // === Pass 1: 인접 윈도우 밀도 비율 밸런싱 (2회) ===
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int w = 1; w < windows.Count && totalMoved < maxMoves; w++)
                    {
                        float densA = windows[w - 1].Count / windowSize;
                        float densB = windows[w].Count / windowSize;
                        if (densA <= 0f && densB <= 0f) continue;

                        float maxD = Mathf.Max(densA, densB);
                        float minD = Mathf.Min(densA, densB);
                        if (maxD <= 0f) continue;

                        float ratio = maxD / Mathf.Max(minD, 0.1f);
                        if (ratio < 1.5f) continue;

                        int fromW = densA > densB ? w - 1 : w;
                        int toW = densA > densB ? w : w - 1;
                        int movesNeeded = Mathf.CeilToInt((windows[fromW].Count - windows[toW].Count) / 3f);

                        totalMoved += MoveNotesBetweenWindows(notes, windows, windowStarts, fromW, toW,
                            movesNeeded, maxMoves - totalMoved, analysis.BPM, analysis.BeatPhaseOffset, windowSize);
                    }
                }

                // === Pass 2: 글로벌 over/under 매칭 ===
                // 구간 전체 평균 밀도 대비 ±30% 이상 편차인 윈도우들을 매칭
                float globalAvgCount = 0f;
                foreach (var w in windows) globalAvgCount += w.Count;
                globalAvgCount /= windows.Count;

                if (globalAvgCount < 1f) continue;

                // over/under 윈도우 수집
                var overWindows = new List<int>();   // 평균의 1.3배 초과
                var underWindows = new List<int>();  // 평균의 0.7배 미만
                for (int w = 0; w < windows.Count; w++)
                {
                    if (windows[w].Count > globalAvgCount * 1.3f) overWindows.Add(w);
                    else if (windows[w].Count < globalAvgCount * 0.7f) underWindows.Add(w);
                }

                // over → under 매칭 (가장 과밀 → 가장 소밀 순)
                overWindows.Sort((a, b) => windows[b].Count.CompareTo(windows[a].Count));
                underWindows.Sort((a, b) => windows[a].Count.CompareTo(windows[b].Count));

                for (int oi = 0; oi < overWindows.Count && totalMoved < maxMoves; oi++)
                {
                    for (int ui = 0; ui < underWindows.Count && totalMoved < maxMoves; ui++)
                    {
                        int fromW = overWindows[oi];
                        int toW = underWindows[ui];
                        if (windows[fromW].Count <= globalAvgCount * 1.1f) break; // 충분히 줄었으면 중단
                        if (windows[toW].Count >= globalAvgCount * 0.9f) continue; // 충분히 채워졌으면 스킵

                        int movesNeeded = Mathf.CeilToInt((windows[fromW].Count - windows[toW].Count) / 4f);
                        movesNeeded = Mathf.Min(movesNeeded, 3); // 한 쌍당 최대 3개

                        totalMoved += MoveNotesBetweenWindows(notes, windows, windowStarts, fromW, toW,
                            movesNeeded, maxMoves - totalMoved, analysis.BPM, analysis.BeatPhaseOffset, windowSize);
                    }
                }
            }

#if UNITY_EDITOR
            if (totalMoved > 0)
                Debug.Log($"[SmartBeatMapper] Density smoothing: moved {totalMoved} notes (adjacent+global)");
#endif
        }

        /// <summary>
        /// 윈도우 간 Tap 노트 이동 헬퍼
        /// </summary>
        private int MoveNotesBetweenWindows(List<NoteData> notes, List<List<int>> windows,
            List<float> windowStarts, int fromW, int toW, int movesNeeded, int movesLeft,
            float bpm, float phaseOffset, float windowSize)
        {
            int moved = 0;
            for (int m = 0; m < movesNeeded && moved < movesLeft; m++)
            {
                // fromW에서 Tap 노트 찾기
                int moveLocalIdx = -1;
                for (int j = windows[fromW].Count - 1; j >= 0; j--)
                {
                    if (notes[windows[fromW][j]].Type == NoteType.Tap)
                    { moveLocalIdx = j; break; }
                }
                if (moveLocalIdx < 0) break;

                int noteIdx = windows[fromW][moveLocalIdx];
                var note = notes[noteIdx];

                // 타겟 윈도우의 빈 비트 그리드 위치 찾기
                float targetCenter = windowStarts[toW] + windowSize * 0.5f;
                // 기존 노트와 겹치지 않는 위치 탐색
                float snappedTarget = FindNonOverlappingBeatPosition(notes, targetCenter, bpm, phaseOffset, windowStarts[toW], windowStarts[toW] + windowSize);

                notes[noteIdx] = new NoteData(snappedTarget, note.LaneIndex, note.Type, note.Duration);
                windows[fromW].RemoveAt(moveLocalIdx);
                windows[toW].Add(noteIdx);
                moved++;
            }
            return moved;
        }

        /// <summary>
        /// 기존 노트와 겹치지 않는 비트 그리드 위치 탐색
        /// </summary>
        private float FindNonOverlappingBeatPosition(List<NoteData> notes, float idealTime,
            float bpm, float phaseOffset, float rangeStart, float rangeEnd)
        {
            float beatInterval = 60f / Mathf.Max(bpm, 60f);
            float subdivInterval = beatInterval / 2f;

            // idealTime에서 가장 가까운 비트 그리드 위치
            float snapped = SnapToBeat(idealTime, bpm, phaseOffset, 0.9f);

            // 이미 겹치는지 확인
            bool isOccupied(float t)
            {
                foreach (var n in notes)
                    if (Mathf.Abs(n.HitTime - t) < 0.08f) return true;
                return false;
            }

            if (!isOccupied(snapped) && snapped >= rangeStart && snapped < rangeEnd)
                return snapped;

            // 주변 비트 그리드에서 빈 자리 탐색 (±4박)
            for (int offset = 1; offset <= 8; offset++)
            {
                float tPlus = snapped + offset * subdivInterval;
                if (tPlus >= rangeStart && tPlus < rangeEnd && !isOccupied(tPlus))
                    return tPlus;

                float tMinus = snapped - offset * subdivInterval;
                if (tMinus >= rangeStart && tMinus < rangeEnd && !isOccupied(tMinus))
                    return tMinus;
            }

            return snapped; // 폴백: 원래 위치
        }

        /// <summary>
        /// 타입 다양성 보장: Long/Scratch 비율이 최소치 미달 시 Tap을 변환
        /// 난이도 5+에서 Long 최소 8%, Scratch 최소 6% 보장
        /// Outro 구간의 노트는 변환하지 않음
        /// </summary>
        private void EnsureMinTypeRatios(List<NoteData> notes, OfflineAudioAnalyzer.AnalysisResult analysis)
        {
            if (notes == null || notes.Count < 20 || difficulty < 3) return;

            // 현재 타입 분포 계산
            int tapCount = 0, longCount = 0, scratchCount = 0;
            foreach (var n in notes)
            {
                switch (n.Type)
                {
                    case NoteType.Tap: tapCount++; break;
                    case NoteType.Long: longCount++; break;
                    case NoteType.Scratch: scratchCount++; break;
                }
            }

            int total = notes.Count;
            // 난이도에 따른 최소 비율 (높을수록 다양성 요구)
            float minLongRatio = Mathf.Lerp(0.05f, 0.12f, (difficulty - 1f) / 9f);
            float minScratchRatio = Mathf.Lerp(0.03f, 0.10f, (difficulty - 1f) / 9f);

            int longNeeded = Mathf.Max(0, Mathf.CeilToInt(total * minLongRatio) - longCount);
            int scratchNeeded = Mathf.Max(0, Mathf.CeilToInt(total * minScratchRatio) - scratchCount);

            if (longNeeded == 0 && scratchNeeded == 0) return;

            // 변환 가능한 Tap 노트 수집 (Outro 제외, 에너지 높은 순)
            var tapIndices = new List<(int idx, float strength)>();
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i].Type != NoteType.Tap) continue;
                var section = FindSection(analysis?.Sections, notes[i].HitTime);
                if (section.Type == OfflineAudioAnalyzer.SectionType.Outro) continue;
                // 에너지 대용: 구간 에너지
                tapIndices.Add((i, section.AverageEnergy));
            }

            // 에너지 높은 Tap부터 변환 (에너지 높을수록 Long/Scratch에 적합)
            tapIndices.Sort((a, b) => b.strength.CompareTo(a.strength));

            int converted = 0;
            float effectiveLongDur = customMappingParams?.LongNoteDuration ?? longNoteDuration;

            // 스크래치 변환 (최상위 에너지 Tap → Scratch)
            for (int t = 0; t < tapIndices.Count && scratchNeeded > 0; t++)
            {
                int idx = tapIndices[t].idx;
                var note = notes[idx];
                notes[idx] = new NoteData(note.HitTime, note.LaneIndex, NoteType.Scratch, 0f);
                scratchNeeded--;
                converted++;
                tapIndices.RemoveAt(t);
                t--;
            }

            // 롱노트 변환 (그다음 에너지 Tap → Long)
            for (int t = 0; t < tapIndices.Count && longNeeded > 0; t++)
            {
                int idx = tapIndices[t].idx;
                var note = notes[idx];
                // 다음 노트까지 간격으로 롱노트 지속시간 결정
                float dur = effectiveLongDur;
                for (int j = idx + 1; j < notes.Count; j++)
                {
                    if (notes[j].HitTime > note.HitTime + 0.1f)
                    {
                        dur = Mathf.Min((notes[j].HitTime - note.HitTime) * 0.8f, 2f);
                        dur = Mathf.Max(dur, 0.3f);
                        break;
                    }
                }
                notes[idx] = new NoteData(note.HitTime, note.LaneIndex, NoteType.Long, dur);
                longNeeded--;
                converted++;
            }

#if UNITY_EDITOR
            if (converted > 0)
                Debug.Log($"[SmartBeatMapper] Type ratio fix: converted {converted} Tap → Long/Scratch " +
                          $"(minLong={minLongRatio:P0}, minScratch={minScratchRatio:P0})");
#endif
        }

        /// <summary>
        /// 레인 분포 밸런싱: stddev 기반 반복 재분배
        /// 표준편차가 평균의 15% 이내가 될 때까지 반복 (최대 5회)
        /// 과잉 레인의 Tap을 부족 레인으로 이동, 인접 레인 우선
        /// </summary>
        private void BalanceLaneDistribution(List<NoteData> notes)
        {
            if (notes == null || notes.Count < 10) return;

            int totalMoved = 0;
            int maxTotalMoves = notes.Count / 4; // 최대 25% 이동

            for (int pass = 0; pass < 5; pass++)
            {
                int[] laneCounts = new int[4];
                foreach (var n in notes)
                    laneCounts[Mathf.Clamp(n.LaneIndex, 0, 3)]++;

                float avg = notes.Count / 4f;
                if (avg <= 0f) break;

                // 표준편차 계산
                float variance = 0f;
                for (int j = 0; j < 4; j++)
                    variance += (laneCounts[j] - avg) * (laneCounts[j] - avg);
                float stddev = Mathf.Sqrt(variance / 4f);

                // 목표: stddev < avg * 0.15 (충분히 균일)
                if (stddev < avg * 0.15f) break;

                // 과잉 레인(avg+stddev 초과)에서 부족 레인(avg-stddev 미만)으로 이동
                int movedThisPass = 0;
                for (int i = 0; i < notes.Count && totalMoved < maxTotalMoves; i++)
                {
                    var note = notes[i];
                    int lane = Mathf.Clamp(note.LaneIndex, 0, 3);
                    if (note.Type != NoteType.Tap) continue;
                    if (laneCounts[lane] <= avg + stddev * 0.5f) continue;

                    // 가장 부족한 인접 레인 → 전체 최소 레인 순으로 선택
                    int targetLane = FindBestTargetLane(lane, laneCounts, avg);
                    if (targetLane < 0 || laneCounts[targetLane] >= laneCounts[lane] - 1) continue;

                    notes[i] = new NoteData(note.HitTime, targetLane, note.Type, note.Duration);
                    laneCounts[lane]--;
                    laneCounts[targetLane]++;
                    totalMoved++;
                    movedThisPass++;
                }

                if (movedThisPass == 0) break; // 더 이상 이동 불가
            }

#if UNITY_EDITOR
            if (totalMoved > 0)
            {
                int[] finalCounts = new int[4];
                foreach (var n in notes)
                    finalCounts[Mathf.Clamp(n.LaneIndex, 0, 3)]++;
                Debug.Log($"[SmartBeatMapper] Lane balancing: moved {totalMoved} notes (5-pass). " +
                          $"Distribution: [{finalCounts[0]},{finalCounts[1]},{finalCounts[2]},{finalCounts[3]}]");
            }
#endif
        }

        /// <summary>
        /// 밸런싱 타겟 레인 선택: 인접 레인 중 가장 부족한 것 우선, 없으면 전체 최소
        /// </summary>
        private int FindBestTargetLane(int fromLane, int[] laneCounts, float avg)
        {
            // 인접 레인 후보
            int[] adjacent;
            if (fromLane == 0) adjacent = new[] { 1 };
            else if (fromLane == 3) adjacent = new[] { 2 };
            else adjacent = new[] { fromLane - 1, fromLane + 1 };

            // 인접 레인 중 평균 이하인 가장 부족한 레인
            int best = -1;
            for (int i = 0; i < adjacent.Length; i++)
            {
                if (laneCounts[adjacent[i]] >= avg) continue;
                if (best < 0 || laneCounts[adjacent[i]] < laneCounts[best])
                    best = adjacent[i];
            }
            if (best >= 0) return best;

            // 인접에 적합한 곳이 없으면 전체에서 최소 레인
            int globalMin = 0;
            for (int j = 1; j < 4; j++)
                if (laneCounts[j] < laneCounts[globalMin]) globalMin = j;

            return laneCounts[globalMin] < avg ? globalMin : -1;
        }

        /// <summary>
        /// Onset 노이즈 필터링: 연속 3개 이상이 극단적으로 짧은 간격(0.15초 미만)이면
        /// 가장 강한 것만 남기고 나머지 제거 (타악기 잔울림/더블트리거 방지)
        /// </summary>
        private List<OfflineAudioAnalyzer.OnsetData> FilterNoiseOnsets(List<OfflineAudioAnalyzer.OnsetData> onsets)
        {
            if (onsets == null || onsets.Count < 3) return onsets;

            const float noiseThreshold = 0.08f; // 80ms 미만 간격만 노이즈 (이전 0.15 → 91% 과잉 제거)
            var result = new List<OfflineAudioAnalyzer.OnsetData>(onsets.Count);
            var cluster = new List<OfflineAudioAnalyzer.OnsetData> { onsets[0] };

            for (int i = 1; i < onsets.Count; i++)
            {
                if (onsets[i].Time - cluster[cluster.Count - 1].Time < noiseThreshold)
                {
                    cluster.Add(onsets[i]);
                }
                else
                {
                    // 클러스터 처리: 3개 이상이면 가장 강한 것만, 아니면 전부 유지
                    if (cluster.Count >= 3)
                    {
                        var strongest = cluster[0];
                        for (int j = 1; j < cluster.Count; j++)
                            if (cluster[j].Strength > strongest.Strength) strongest = cluster[j];
                        result.Add(strongest);
                    }
                    else
                    {
                        result.AddRange(cluster);
                    }
                    cluster.Clear();
                    cluster.Add(onsets[i]);
                }
            }

            // 마지막 클러스터 처리
            if (cluster.Count >= 3)
            {
                var strongest = cluster[0];
                for (int j = 1; j < cluster.Count; j++)
                    if (cluster[j].Strength > strongest.Strength) strongest = cluster[j];
                result.Add(strongest);
            }
            else
            {
                result.AddRange(cluster);
            }

#if UNITY_EDITOR
            if (result.Count < onsets.Count)
                Debug.Log($"[SmartBeatMapper] Noise filter: {onsets.Count} → {result.Count} onsets ({onsets.Count - result.Count} removed)");
#endif
            return result;
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
