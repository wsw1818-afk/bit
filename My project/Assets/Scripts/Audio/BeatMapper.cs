using UnityEngine;
using System.Collections.Generic;
using AIBeat.Data;

namespace AIBeat.Audio
{
    /// <summary>
    /// BPM 기반 노트 패턴 생성
    /// </summary>
    public class BeatMapper : MonoBehaviour
    {
        [Header("Mapping Settings")]
#pragma warning disable 0414
        [SerializeField] private float noteThreshold = 0.3f;  // 노트 생성 최소 강도
#pragma warning restore 0414
        [SerializeField] private float minNoteGap = 0.1f;     // 노트 간 최소 간격 (초)

        [Header("Difficulty Scaling")]
        [SerializeField] private AnimationCurve densityCurve;  // 구간별 밀도 곡선

        private AudioAnalyzer audioAnalyzer;
        private System.Random rng; // 시드 기반 결정론적 랜덤

        /// <summary>
        /// 곡 데이터에서 노트 패턴 생성 (오프라인 분석)
        /// seed가 0이면 비결정적 (호환성 유지)
        /// </summary>
        public List<NoteData> GenerateNotes(SongData songData, int seed = 0)
        {
            rng = seed != 0 ? new System.Random(seed) : new System.Random();

            var notes = new List<NoteData>();
            float bpm = songData.BPM;
            float beatInterval = 60f / bpm;

            float currentTime = 0f;
            float lastNoteTime = -1f;

            while (currentTime < songData.Duration)
            {
                SongSection currentSection = GetSectionAt(songData.Sections, currentTime);
                float density = currentSection.DensityMultiplier;

                if (ShouldSpawnNote(density, currentTime, songData))
                {
                    if (currentTime - lastNoteTime >= minNoteGap)
                    {
                        NoteData note = CreateNote(currentTime, currentSection);
                        notes.Add(note);
                        lastNoteTime = currentTime;
                    }
                }

                currentTime += beatInterval;
            }

            return notes;
        }

        /// <summary>
        /// BPM과 구간 정보만으로 노트 생성 (AI 응답 기반)
        /// </summary>
        public List<NoteData> GenerateNotesFromBPM(float bpm, float duration, SongSection[] sections, int seed = 0)
        {
            var notes = new List<NoteData>();

            // BPM 유효성 검증
            if (bpm <= 0f || bpm > 300f)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[BeatMapper] Invalid BPM: {bpm}, using default 120");
#endif
                bpm = 120f;
            }

            float beatInterval = 60f / bpm;
            float currentTime = 0f;

            System.Random random = seed != 0 ? new System.Random(seed) : new System.Random();

            while (currentTime < duration)
            {
                SongSection section = GetSectionAt(sections, currentTime);
                float density = section.DensityMultiplier;

                // 밀도에 따른 노트 생성
                if (random.NextDouble() < density)
                {
                    int lane = GetRandomLane(section.Name, random);
                    NoteType type = GetRandomNoteType(section.Name, random);
                    float noteDuration = type == NoteType.Long
                        ? beatInterval * (1 + random.Next(3))
                        : 0f;

                    notes.Add(new NoteData(currentTime, lane, type, noteDuration));
                }

                // 동시타 생성 (Drop 구간에서) - notes가 비어있지 않은 경우만
                if (section.Name == "drop" && notes.Count > 0 && random.NextDouble() < 0.3)
                {
                    int secondLane = GetRandomLane("drop", random);
                    if (secondLane != notes[notes.Count - 1].LaneIndex)
                    {
                        notes.Add(new NoteData(currentTime, secondLane, NoteType.Tap));
                    }
                }

                currentTime += beatInterval;
            }

            return notes;
        }

        private SongSection GetSectionAt(SongSection[] sections, float time)
        {
            if (sections == null || sections.Length == 0)
            {
                return new SongSection("default", 0, float.MaxValue, 0.5f);
            }

            foreach (var section in sections)
            {
                if (time >= section.StartTime && time < section.EndTime)
                {
                    return section;
                }
            }

            return sections[sections.Length - 1];
        }

        private bool ShouldSpawnNote(float density, float time, SongData songData)
        {
            float baseChance = density * (songData.Difficulty / 10f);
            return (float)rng.NextDouble() < baseChance;
        }

        private NoteData CreateNote(float time, SongSection section)
        {
            int lane;
            NoteType type = NoteType.Tap;
            float duration = 0f;

            // 구간에 따른 레인/타입 결정 (4레인: 0=ScratchL, 1=Key1, 2=Key2, 3=ScratchR)
            switch (section.Name.ToLower())
            {
                case "intro":
                case "outro":
                    lane = rng.Next(1, 3);
                    break;

                case "build":
                    lane = rng.Next(1, 3);
                    if ((float)rng.NextDouble() < 0.2f)
                    {
                        type = NoteType.Long;
                        duration = 0.5f + (float)rng.NextDouble() * 1f;
                    }
                    break;

                case "drop":
                    if ((float)rng.NextDouble() < 0.15f)
                    {
                        lane = rng.Next(0, 2) == 0 ? 0 : 3;
                        type = NoteType.Scratch;
                    }
                    else
                    {
                        lane = rng.Next(1, 3);
                        if ((float)rng.NextDouble() < 0.25f)
                        {
                            type = NoteType.Long;
                            duration = 0.3f + (float)rng.NextDouble() * 0.7f;
                        }
                    }
                    break;

                default:
                    lane = rng.Next(1, 3);
                    break;
            }

            return new NoteData(time, lane, type, duration);
        }

        private int GetRandomLane(string sectionName, System.Random random)
        {
            return sectionName.ToLower() switch
            {
                "intro" => random.Next(1, 3),
                "outro" => random.Next(1, 3),
                "build" => random.Next(1, 3),
                "drop" => random.Next(0, 4),
                _ => random.Next(1, 3)
            };
        }

        private NoteType GetRandomNoteType(string sectionName, System.Random random)
        {
            float roll = (float)random.NextDouble();

            return sectionName.ToLower() switch
            {
                "intro" => NoteType.Tap,
                "outro" => NoteType.Tap,
                "build" => roll < 0.15f ? NoteType.Long : NoteType.Tap,
                "drop" => roll < 0.1f ? NoteType.Scratch
                       : roll < 0.25f ? NoteType.Long
                       : NoteType.Tap,
                _ => NoteType.Tap
            };
        }

        /// <summary>
        /// 기본 구간 생성 (AI 응답에 구간 정보가 없을 때)
        /// </summary>
        public static SongSection[] CreateDefaultSections(float duration)
        {
            return new SongSection[]
            {
                new SongSection("intro", 0, duration * 0.125f, 0.3f),
                new SongSection("build", duration * 0.125f, duration * 0.375f, 0.5f),
                new SongSection("drop", duration * 0.375f, duration * 0.75f, 1.0f),
                new SongSection("outro", duration * 0.75f, duration, 0.4f)
            };
        }
    }
}
