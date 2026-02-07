using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using AIBeat.Data;
using System.Collections.Generic;

namespace AIBeat.Editor
{
    /// <summary>
    /// 테스트용 샘플 곡 데이터 생성 (다양한 프리셋 패턴 포함)
    /// 4레인 구조: 0=ScratchL, 1=Key1, 2=Key2, 3=ScratchR
    /// </summary>
#if UNITY_EDITOR
    public class TestSongCreator : UnityEditor.Editor
    {
        private const string SAVE_DIR = "Assets/Resources/Songs";

        [MenuItem("Tools/A.I. BEAT/Create Test Song Data")]
        public static void CreateTestSongData()
        {
            EnsureDirectory();
            var songData = CreateSong("Test Beat", "A.I. Composer", 120f, 60f, 3);
            songData.Notes = GenerateMixedPattern(120f, 60f).ToArray();
            songData.Sections = Audio.BeatMapper.CreateDefaultSections(60f);
            SaveAsset(songData, "TestBeat");
        }

        [MenuItem("Tools/A.I. BEAT/Create Simple Test Pattern")]
        public static void CreateSimpleTestPattern()
        {
            EnsureDirectory();
            var songData = CreateSong("Simple Test", "Debug", 100f, 30f, 1);
            songData.Notes = GenerateSimplePattern(100f, 30f).ToArray();
            songData.Sections = Audio.BeatMapper.CreateDefaultSections(30f);
            SaveAsset(songData, "SimpleTest");
        }

        [MenuItem("Tools/A.I. BEAT/Test Songs/Tap Only (Easy)")]
        public static void CreateTapOnly()
        {
            EnsureDirectory();
            var songData = CreateSong("Tap Basics", "Debug", 100f, 30f, 1);
            songData.Notes = GenerateTapOnlyPattern(100f, 30f).ToArray();
            songData.Sections = Audio.BeatMapper.CreateDefaultSections(30f);
            SaveAsset(songData, "TapOnly");
        }

        [MenuItem("Tools/A.I. BEAT/Test Songs/Long Notes")]
        public static void CreateLongNotes()
        {
            EnsureDirectory();
            var songData = CreateSong("Hold It!", "Debug", 110f, 30f, 3);
            songData.Notes = GenerateLongNotePattern(110f, 30f).ToArray();
            songData.Sections = Audio.BeatMapper.CreateDefaultSections(30f);
            SaveAsset(songData, "LongNotes");
        }

        [MenuItem("Tools/A.I. BEAT/Test Songs/Scratch Focused")]
        public static void CreateScratchFocused()
        {
            EnsureDirectory();
            var songData = CreateSong("Scratch Master", "Debug", 130f, 30f, 5);
            songData.Notes = GenerateScratchPattern(130f, 30f).ToArray();
            songData.Sections = Audio.BeatMapper.CreateDefaultSections(30f);
            SaveAsset(songData, "ScratchFocused");
        }

        [MenuItem("Tools/A.I. BEAT/Test Songs/Simultaneous Notes")]
        public static void CreateSimultaneous()
        {
            EnsureDirectory();
            var songData = CreateSong("Double Tap", "Debug", 120f, 30f, 4);
            songData.Notes = GenerateSimultaneousPattern(120f, 30f).ToArray();
            songData.Sections = Audio.BeatMapper.CreateDefaultSections(30f);
            SaveAsset(songData, "Simultaneous");
        }

        [MenuItem("Tools/A.I. BEAT/Test Songs/Speed Test (Fast)")]
        public static void CreateSpeedTest()
        {
            EnsureDirectory();
            var songData = CreateSong("Speed Demon", "Debug", 180f, 30f, 8);
            songData.Notes = GenerateSpeedPattern(180f, 30f).ToArray();
            songData.Sections = Audio.BeatMapper.CreateDefaultSections(30f);
            SaveAsset(songData, "SpeedTest");
        }

        [MenuItem("Tools/A.I. BEAT/Test Songs/Full Mix (All Types)")]
        public static void CreateFullMix()
        {
            EnsureDirectory();
            var songData = CreateSong("Full Mix", "Debug", 140f, 45f, 6);
            songData.Notes = GenerateFullMixPattern(140f, 45f).ToArray();
            songData.Sections = new SongSection[]
            {
                new SongSection("intro", 0f, 8f, 0.3f),
                new SongSection("build", 8f, 18f, 0.6f),
                new SongSection("drop", 18f, 34f, 1.0f),
                new SongSection("outro", 34f, 45f, 0.4f)
            };
            SaveAsset(songData, "FullMix");
        }

        // --- Pattern Generators ---

        /// <summary>
        /// 간단한 패턴: Key1/Key2 교대 탭 (입문용)
        /// </summary>
        private static List<NoteData> GenerateSimplePattern(float bpm, float duration)
        {
            var notes = new List<NoteData>();
            float beat = 60f / bpm;
            float t = 2f;

            while (t < duration - 1f)
            {
                int lane = ((int)(t / beat)) % 2 == 0 ? 1 : 2;
                notes.Add(new NoteData(t, lane, NoteType.Tap));
                t += beat;
            }

            return notes;
        }

        /// <summary>
        /// 탭 전용: 4레인 전체 사용, 다양한 간격
        /// </summary>
        private static List<NoteData> GenerateTapOnlyPattern(float bpm, float duration)
        {
            var notes = new List<NoteData>();
            float beat = 60f / bpm;
            float eighth = beat / 2f;
            var rng = new System.Random(100);
            float t = 2f;

            while (t < duration - 1f)
            {
                // Key lanes mainly (1, 2), occasional scratch lanes (0, 3)
                int lane;
                if (rng.NextDouble() < 0.15)
                    lane = rng.Next(0, 2) == 0 ? 0 : 3;
                else
                    lane = rng.Next(1, 3);

                notes.Add(new NoteData(t, lane, NoteType.Tap));

                // Alternate between quarter and eighth note spacing
                t += rng.NextDouble() < 0.3 ? eighth : beat;
            }

            return notes;
        }

        /// <summary>
        /// 롱노트 중심: 다양한 길이의 홀드 노트
        /// </summary>
        private static List<NoteData> GenerateLongNotePattern(float bpm, float duration)
        {
            var notes = new List<NoteData>();
            float beat = 60f / bpm;
            var rng = new System.Random(200);
            float t = 2f;

            while (t < duration - 2f)
            {
                int lane = rng.Next(1, 3);
                float roll = (float)rng.NextDouble();

                if (roll < 0.5f)
                {
                    // Long note (1~3 beats)
                    float holdDuration = beat * (1 + rng.Next(0, 3));
                    // Clamp to not exceed song duration
                    holdDuration = Mathf.Min(holdDuration, duration - t - 1f);
                    if (holdDuration > 0.2f)
                    {
                        notes.Add(new NoteData(t, lane, NoteType.Long, holdDuration));
                        t += holdDuration + beat * 0.5f; // Gap after long note
                    }
                    else
                    {
                        notes.Add(new NoteData(t, lane, NoteType.Tap));
                        t += beat;
                    }
                }
                else
                {
                    // Tap note between long notes
                    notes.Add(new NoteData(t, lane, NoteType.Tap));
                    t += beat;
                }
            }

            return notes;
        }

        /// <summary>
        /// 스크래치 중심: 양 끝 레인 스크래치 + 중앙 탭
        /// </summary>
        private static List<NoteData> GenerateScratchPattern(float bpm, float duration)
        {
            var notes = new List<NoteData>();
            float beat = 60f / bpm;
            float eighth = beat / 2f;
            var rng = new System.Random(300);
            float t = 2f;

            while (t < duration - 1f)
            {
                float roll = (float)rng.NextDouble();

                if (roll < 0.4f)
                {
                    // Scratch on left or right
                    int scratchLane = rng.Next(0, 2) == 0 ? 0 : 3;
                    notes.Add(new NoteData(t, scratchLane, NoteType.Scratch));
                }
                else if (roll < 0.6f)
                {
                    // Scratch + tap simultaneously
                    int scratchLane = rng.Next(0, 2) == 0 ? 0 : 3;
                    int tapLane = rng.Next(1, 3);
                    notes.Add(new NoteData(t, scratchLane, NoteType.Scratch));
                    notes.Add(new NoteData(t, tapLane, NoteType.Tap));
                }
                else
                {
                    // Regular tap
                    int tapLane = rng.Next(1, 3);
                    notes.Add(new NoteData(t, tapLane, NoteType.Tap));
                }

                t += rng.NextDouble() < 0.3 ? eighth : beat;
            }

            notes.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));
            return notes;
        }

        /// <summary>
        /// 동시타 패턴: 2개 레인 동시 입력
        /// </summary>
        private static List<NoteData> GenerateSimultaneousPattern(float bpm, float duration)
        {
            var notes = new List<NoteData>();
            float beat = 60f / bpm;
            var rng = new System.Random(400);
            float t = 2f;

            while (t < duration - 1f)
            {
                float roll = (float)rng.NextDouble();

                if (roll < 0.4f)
                {
                    // Double tap: Key1 + Key2
                    notes.Add(new NoteData(t, 1, NoteType.Tap));
                    notes.Add(new NoteData(t, 2, NoteType.Tap));
                }
                else if (roll < 0.55f)
                {
                    // Scratch + Key
                    int scratchLane = rng.Next(0, 2) == 0 ? 0 : 3;
                    int keyLane = rng.Next(1, 3);
                    notes.Add(new NoteData(t, scratchLane, NoteType.Scratch));
                    notes.Add(new NoteData(t, keyLane, NoteType.Tap));
                }
                else
                {
                    // Single tap
                    int lane = rng.Next(1, 3);
                    notes.Add(new NoteData(t, lane, NoteType.Tap));
                }

                t += beat;
            }

            notes.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));
            return notes;
        }

        /// <summary>
        /// 고속 패턴: 16분음표 연타, BPM 180
        /// </summary>
        private static List<NoteData> GenerateSpeedPattern(float bpm, float duration)
        {
            var notes = new List<NoteData>();
            float beat = 60f / bpm;
            float sixteenth = beat / 4f;
            float eighth = beat / 2f;
            var rng = new System.Random(500);
            float t = 2f;

            while (t < duration - 1f)
            {
                // Burst sections (16th note runs)
                if (rng.NextDouble() < 0.3 && t < duration - 3f)
                {
                    // 4-8 note burst
                    int burstLen = 4 + rng.Next(0, 5);
                    for (int i = 0; i < burstLen && t < duration - 1f; i++)
                    {
                        int lane = (i % 2 == 0) ? 1 : 2;
                        notes.Add(new NoteData(t, lane, NoteType.Tap));
                        t += sixteenth;
                    }
                    t += eighth; // Rest after burst
                }
                else
                {
                    int lane = rng.Next(1, 3);
                    notes.Add(new NoteData(t, lane, NoteType.Tap));
                    t += eighth;
                }
            }

            return notes;
        }

        /// <summary>
        /// 혼합 패턴: BPM 기반 구간별 밀도 변화
        /// </summary>
        private static List<NoteData> GenerateMixedPattern(float bpm, float duration)
        {
            var notes = new List<NoteData>();
            float beat = 60f / bpm;
            float eighth = beat / 2f;
            var rng = new System.Random(42);
            float t = 2f;

            while (t < duration - 1f)
            {
                // Section-based density
                float progress = t / duration;
                float density = progress < 0.15f ? 0.4f    // Intro
                              : progress < 0.4f ? 0.6f     // Build
                              : progress < 0.75f ? 0.9f    // Drop
                              : 0.4f;                       // Outro

                if ((float)rng.NextDouble() < density)
                {
                    int lane = rng.Next(1, 3);
                    float typeRoll = (float)rng.NextDouble();

                    if (typeRoll < 0.1f && progress > 0.3f)
                    {
                        // Scratch (only after intro)
                        int scratchLane = rng.Next(0, 2) == 0 ? 0 : 3;
                        notes.Add(new NoteData(t, scratchLane, NoteType.Scratch));
                    }
                    else if (typeRoll < 0.25f && progress > 0.15f)
                    {
                        // Long note
                        float holdDur = beat * (1 + rng.Next(0, 3));
                        holdDur = Mathf.Min(holdDur, duration - t - 1f);
                        if (holdDur > 0.3f)
                        {
                            notes.Add(new NoteData(t, lane, NoteType.Long, holdDur));
                            t += holdDur * 0.5f;
                        }
                        else
                        {
                            notes.Add(new NoteData(t, lane, NoteType.Tap));
                        }
                    }
                    else
                    {
                        notes.Add(new NoteData(t, lane, NoteType.Tap));
                    }
                }

                t += (float)rng.NextDouble() < 0.3 ? eighth : beat;
            }

            notes.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));
            return notes;
        }

        /// <summary>
        /// 풀 믹스: 구간별 특화 패턴 (Intro:tap, Build:long, Drop:scratch+simultaneous, Outro:sparse)
        /// </summary>
        private static List<NoteData> GenerateFullMixPattern(float bpm, float duration)
        {
            var notes = new List<NoteData>();
            float beat = 60f / bpm;
            float eighth = beat / 2f;
            float sixteenth = beat / 4f;
            var rng = new System.Random(999);
            float t = 2f;

            while (t < duration - 1f)
            {
                if (t < 8f)
                {
                    // Intro: simple alternating taps
                    int lane = ((int)(t / beat)) % 2 == 0 ? 1 : 2;
                    notes.Add(new NoteData(t, lane, NoteType.Tap));
                    t += beat;
                }
                else if (t < 18f)
                {
                    // Build: long notes + taps
                    float roll = (float)rng.NextDouble();
                    int lane = rng.Next(1, 3);

                    if (roll < 0.35f && t < 16f)
                    {
                        float holdDur = beat * (1 + rng.Next(0, 2));
                        notes.Add(new NoteData(t, lane, NoteType.Long, holdDur));
                        t += holdDur + eighth;
                    }
                    else
                    {
                        notes.Add(new NoteData(t, lane, NoteType.Tap));
                        t += rng.NextDouble() < 0.3 ? eighth : beat;
                    }
                }
                else if (t < 34f)
                {
                    // Drop: everything + higher density
                    float roll = (float)rng.NextDouble();

                    if (roll < 0.15f)
                    {
                        // Scratch + tap
                        int scratchLane = rng.Next(0, 2) == 0 ? 0 : 3;
                        notes.Add(new NoteData(t, scratchLane, NoteType.Scratch));
                        notes.Add(new NoteData(t, rng.Next(1, 3), NoteType.Tap));
                    }
                    else if (roll < 0.3f)
                    {
                        // Double tap
                        notes.Add(new NoteData(t, 1, NoteType.Tap));
                        notes.Add(new NoteData(t, 2, NoteType.Tap));
                    }
                    else if (roll < 0.45f)
                    {
                        // Long note
                        float holdDur = beat * (1 + rng.Next(0, 2));
                        holdDur = Mathf.Min(holdDur, 33f - t);
                        if (holdDur > 0.3f)
                        {
                            notes.Add(new NoteData(t, rng.Next(1, 3), NoteType.Long, holdDur));
                            t += holdDur * 0.3f;
                        }
                        else
                        {
                            notes.Add(new NoteData(t, rng.Next(1, 3), NoteType.Tap));
                        }
                    }
                    else
                    {
                        notes.Add(new NoteData(t, rng.Next(1, 3), NoteType.Tap));
                    }

                    t += rng.NextDouble() < 0.4 ? sixteenth : eighth;
                }
                else
                {
                    // Outro: sparse taps
                    notes.Add(new NoteData(t, rng.Next(1, 3), NoteType.Tap));
                    t += beat + (float)(rng.NextDouble() * 0.2);
                }
            }

            notes.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));
            return notes;
        }

        // --- Utility ---

        private static SongData CreateSong(string title, string artist, float bpm, float duration, int difficulty)
        {
            var song = ScriptableObject.CreateInstance<SongData>();
            song.Title = title;
            song.Artist = artist;
            song.BPM = bpm;
            song.Duration = duration;
            song.Difficulty = difficulty;
            return song;
        }

        private static void EnsureDirectory()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(SAVE_DIR))
                AssetDatabase.CreateFolder("Assets/Resources", "Songs");
        }

        private static void SaveAsset(SongData songData, string fileName)
        {
            string path = $"{SAVE_DIR}/{fileName}.asset";

            // Overwrite existing
            var existing = AssetDatabase.LoadAssetAtPath<SongData>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(songData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int noteCount = songData.Notes != null ? songData.Notes.Length : 0;
            Debug.Log($"[TestSongCreator] Created: {songData.Title} ({noteCount} notes, BPM={songData.BPM}, {songData.Duration}s) -> {path}");
            Selection.activeObject = songData;
        }
    }
#endif
}
