using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using AIBeat.Data;
using AIBeat.Gameplay;
using AIBeat.Core;
using AIBeat.Audio;
using System.Collections.Generic;

namespace AIBeat.Tests.Editor
{
    /// <summary>
    /// A.I. BEAT EditMode unit tests
    /// Covers: NoteData, SongData, BeatMapper, JudgementResult, asset existence, compilation
    /// </summary>
    public class AIBeatEditorTests
    {
        #region NoteData Tests

        [Test]
        public void NoteData_Constructor_SetsValuesCorrectly()
        {
            var noteData = new NoteData(5.5f, 2, NoteType.Tap, 0f);

            Assert.AreEqual(5.5f, noteData.HitTime);
            Assert.AreEqual(2, noteData.LaneIndex);
            Assert.AreEqual(NoteType.Tap, noteData.Type);
            Assert.AreEqual(0f, noteData.Duration);
        }

        [Test]
        public void NoteData_LongNote_HasDuration()
        {
            var longNote = new NoteData(10f, 2, NoteType.Long, 2.5f);

            Assert.AreEqual(NoteType.Long, longNote.Type);
            Assert.AreEqual(2.5f, longNote.Duration);
        }

        [Test]
        public void NoteData_DefaultValues_AreZero()
        {
            var note = new NoteData();

            Assert.AreEqual(0f, note.HitTime);
            Assert.AreEqual(0, note.LaneIndex);
            Assert.AreEqual(NoteType.Tap, note.Type);
            Assert.AreEqual(0f, note.Duration);
        }

        [Test]
        public void NoteData_ScratchNote_UsesEdgeLanes()
        {
            var scratchL = new NoteData(1f, 0, NoteType.Scratch);
            var scratchR = new NoteData(2f, 3, NoteType.Scratch);

            Assert.AreEqual(0, scratchL.LaneIndex, "Left scratch should be lane 0");
            Assert.AreEqual(3, scratchR.LaneIndex, "Right scratch should be lane 3");
            Assert.AreEqual(NoteType.Scratch, scratchL.Type);
            Assert.AreEqual(NoteType.Scratch, scratchR.Type);
        }

        [Test]
        public void NoteType_Enum_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)NoteType.Tap);
            Assert.AreEqual(1, (int)NoteType.Long);
            Assert.AreEqual(2, (int)NoteType.Scratch);
        }

        #endregion

        #region SongData Tests

        [Test]
        public void SongData_CreateInstance_IsNotNull()
        {
            var songData = ScriptableObject.CreateInstance<SongData>();
            Assert.IsNotNull(songData);
            Object.DestroyImmediate(songData);
        }

        [Test]
        public void SongData_SetProperties_Persists()
        {
            var songData = ScriptableObject.CreateInstance<SongData>();
            songData.Title = "Test";
            songData.Artist = "Artist";
            songData.BPM = 140f;
            songData.Duration = 60f;
            songData.Difficulty = 5;

            Assert.AreEqual("Test", songData.Title);
            Assert.AreEqual("Artist", songData.Artist);
            Assert.AreEqual(140f, songData.BPM);
            Assert.AreEqual(60f, songData.Duration);
            Assert.AreEqual(5, songData.Difficulty);

            Object.DestroyImmediate(songData);
        }

        [Test]
        public void SongSection_Constructor_SetsValues()
        {
            var section = new SongSection("drop", 10f, 25f, 1.0f);

            Assert.AreEqual("drop", section.Name);
            Assert.AreEqual(10f, section.StartTime);
            Assert.AreEqual(25f, section.EndTime);
            Assert.AreEqual(1.0f, section.DensityMultiplier);
        }

        [Test]
        public void SongSection_DefaultDensity_IsOne()
        {
            var section = new SongSection("intro", 0f, 5f);
            Assert.AreEqual(1f, section.DensityMultiplier);
        }

        #endregion

        #region BeatMapper Tests

        [Test]
        public void BeatMapper_CreateDefaultSections_ReturnsFourSections()
        {
            var sections = BeatMapper.CreateDefaultSections(60f);

            Assert.IsNotNull(sections);
            Assert.AreEqual(4, sections.Length);
        }

        [Test]
        public void BeatMapper_CreateDefaultSections_CoversDuration()
        {
            float duration = 60f;
            var sections = BeatMapper.CreateDefaultSections(duration);

            // First section starts at 0
            Assert.AreEqual(0f, sections[0].StartTime, 0.001f);

            // Last section ends at duration
            Assert.AreEqual(duration, sections[sections.Length - 1].EndTime, 0.001f);
        }

        [Test]
        public void BeatMapper_CreateDefaultSections_HasCorrectNames()
        {
            var sections = BeatMapper.CreateDefaultSections(60f);

            Assert.AreEqual("intro", sections[0].Name);
            Assert.AreEqual("build", sections[1].Name);
            Assert.AreEqual("drop", sections[2].Name);
            Assert.AreEqual("outro", sections[3].Name);
        }

        [Test]
        public void BeatMapper_CreateDefaultSections_SectionsAreContiguous()
        {
            var sections = BeatMapper.CreateDefaultSections(60f);

            for (int i = 1; i < sections.Length; i++)
            {
                Assert.AreEqual(sections[i - 1].EndTime, sections[i].StartTime, 0.001f,
                    $"Section {sections[i].Name} should start where {sections[i - 1].Name} ends");
            }
        }

        [Test]
        public void BeatMapper_CreateDefaultSections_DropHasHighestDensity()
        {
            var sections = BeatMapper.CreateDefaultSections(60f);

            float maxDensity = 0f;
            string maxSection = "";
            foreach (var s in sections)
            {
                if (s.DensityMultiplier > maxDensity)
                {
                    maxDensity = s.DensityMultiplier;
                    maxSection = s.Name;
                }
            }

            Assert.AreEqual("drop", maxSection, "Drop section should have highest density");
        }

        [Test]
        public void BeatMapper_GenerateNotesFromBPM_ProducesNotes()
        {
            var go = new GameObject("TestBeatMapper");
            var mapper = go.AddComponent<BeatMapper>();

            var sections = BeatMapper.CreateDefaultSections(30f);
            var notes = mapper.GenerateNotesFromBPM(120f, 30f, sections, 42);

            Assert.IsNotNull(notes);
            Assert.Greater(notes.Count, 0, "Should generate at least one note");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void BeatMapper_GenerateNotesFromBPM_DeterministicWithSeed()
        {
            var go = new GameObject("TestBeatMapper");
            var mapper = go.AddComponent<BeatMapper>();

            var sections = BeatMapper.CreateDefaultSections(30f);
            var notes1 = mapper.GenerateNotesFromBPM(120f, 30f, sections, 42);
            var notes2 = mapper.GenerateNotesFromBPM(120f, 30f, sections, 42);

            Assert.AreEqual(notes1.Count, notes2.Count, "Same seed should produce same count");
            for (int i = 0; i < notes1.Count; i++)
            {
                Assert.AreEqual(notes1[i].HitTime, notes2[i].HitTime, 0.001f,
                    $"Note {i} HitTime should match with same seed");
                Assert.AreEqual(notes1[i].LaneIndex, notes2[i].LaneIndex,
                    $"Note {i} Lane should match with same seed");
            }

            Object.DestroyImmediate(go);
        }

        [Test]
        public void BeatMapper_GenerateNotesFromBPM_NotesWithinDuration()
        {
            var go = new GameObject("TestBeatMapper");
            var mapper = go.AddComponent<BeatMapper>();

            float duration = 30f;
            var sections = BeatMapper.CreateDefaultSections(duration);
            var notes = mapper.GenerateNotesFromBPM(120f, duration, sections, 42);

            foreach (var note in notes)
            {
                Assert.GreaterOrEqual(note.HitTime, 0f, "Note time should be >= 0");
                Assert.LessOrEqual(note.HitTime, duration, "Note time should be <= duration");
            }

            Object.DestroyImmediate(go);
        }

        [Test]
        public void BeatMapper_GenerateNotesFromBPM_LanesInRange()
        {
            var go = new GameObject("TestBeatMapper");
            var mapper = go.AddComponent<BeatMapper>();

            var sections = BeatMapper.CreateDefaultSections(30f);
            var notes = mapper.GenerateNotesFromBPM(120f, 30f, sections, 42);

            foreach (var note in notes)
            {
                Assert.GreaterOrEqual(note.LaneIndex, 0, "Lane should be >= 0");
                Assert.LessOrEqual(note.LaneIndex, 3, "Lane should be <= 3");
            }

            Object.DestroyImmediate(go);
        }

        [Test]
        public void BeatMapper_GenerateNotesFromBPM_LongNotesHaveDuration()
        {
            var go = new GameObject("TestBeatMapper");
            var mapper = go.AddComponent<BeatMapper>();

            var sections = BeatMapper.CreateDefaultSections(60f);
            var notes = mapper.GenerateNotesFromBPM(120f, 60f, sections, 42);

            foreach (var note in notes)
            {
                if (note.Type == NoteType.Long)
                {
                    Assert.Greater(note.Duration, 0f, "Long notes must have duration > 0");
                }
                else
                {
                    Assert.AreEqual(0f, note.Duration, "Non-long notes should have duration 0");
                }
            }

            Object.DestroyImmediate(go);
        }

        #endregion

        #region JudgementResult Tests

        [Test]
        public void JudgementResult_Enum_HasCorrectValues()
        {
            Assert.AreEqual(0, (int)JudgementResult.Perfect);
            Assert.AreEqual(1, (int)JudgementResult.Great);
            Assert.AreEqual(2, (int)JudgementResult.Good);
            Assert.AreEqual(3, (int)JudgementResult.Bad);
            Assert.AreEqual(4, (int)JudgementResult.Miss);
        }

        [Test]
        public void JudgementResult_Enum_HasFiveValues()
        {
            var values = System.Enum.GetValues(typeof(JudgementResult));
            Assert.AreEqual(5, values.Length);
        }

        #endregion

        #region GameResult Tests

        [Test]
        public void GameResult_Struct_DefaultValues()
        {
            var result = new GameResult();

            Assert.AreEqual(0, result.Score);
            Assert.AreEqual(0, result.MaxCombo);
            Assert.AreEqual(0f, result.Accuracy);
            Assert.AreEqual(0, result.PerfectCount);
            Assert.AreEqual(0, result.MissCount);
            Assert.AreEqual(0, result.TotalNotes);
            Assert.IsNull(result.Rank);
        }

        [Test]
        public void GameResult_Struct_SetValues()
        {
            var result = new GameResult
            {
                Score = 95000,
                MaxCombo = 120,
                Accuracy = 95.5f,
                PerfectCount = 80,
                GreatCount = 30,
                GoodCount = 5,
                BadCount = 3,
                MissCount = 2,
                TotalNotes = 120,
                Rank = "S"
            };

            Assert.AreEqual(95000, result.Score);
            Assert.AreEqual(120, result.MaxCombo);
            Assert.AreEqual(95.5f, result.Accuracy, 0.01f);
            Assert.AreEqual("S", result.Rank);
            Assert.AreEqual(120, result.PerfectCount + result.GreatCount +
                           result.GoodCount + result.BadCount + result.MissCount);
        }

        #endregion

        #region PromptOptions Tests

        [Test]
        public void PromptOptions_Genres_NotEmpty()
        {
            Assert.IsNotNull(PromptOptions.Genres);
            Assert.Greater(PromptOptions.Genres.Length, 0);
        }

        [Test]
        public void PromptOptions_Moods_NotEmpty()
        {
            Assert.IsNotNull(PromptOptions.Moods);
            Assert.Greater(PromptOptions.Moods.Length, 0);
        }

        [Test]
        public void PromptOptions_BPMOptions_InReasonableRange()
        {
            foreach (var bpm in PromptOptions.BPMOptions)
            {
                Assert.GreaterOrEqual(bpm, 60, "BPM should be >= 60");
                Assert.LessOrEqual(bpm, 300, "BPM should be <= 300");
            }
        }

        #endregion

        #region Scene Assets Tests

        [Test]
        public void Scene_Gameplay_Exists()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Gameplay.unity");
            Assert.IsNotNull(sceneAsset, "Gameplay.unity should exist");
        }

        [Test]
        public void Scene_MainMenu_Exists()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
            Assert.IsNotNull(sceneAsset, "MainMenu.unity should exist");
        }

        [Test]
        public void Scene_SongSelect_Exists()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SongSelect.unity");
            Assert.IsNotNull(sceneAsset, "SongSelect.unity should exist");
        }

        #endregion

        #region Material Tests

        [Test]
        public void Material_NormalNoteMat_Exists()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/NormalNoteMat.mat");
            Assert.IsNotNull(mat, "NormalNoteMat.mat should exist");
        }

        [Test]
        public void Material_LongNoteMat_Exists()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LongNoteMat.mat");
            Assert.IsNotNull(mat, "LongNoteMat.mat should exist");
        }

        [Test]
        public void Material_ScratchNoteMat_Exists()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ScratchNoteMat.mat");
            Assert.IsNotNull(mat, "ScratchNoteMat.mat should exist");
        }

        [Test]
        public void Material_JudgementLineMat_Exists()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/JudgementLineMat.mat");
            Assert.IsNotNull(mat, "JudgementLineMat.mat should exist");
        }

        #endregion

        #region Script Compilation Tests

        [Test]
        public void Scripts_GameplayController_Compiles()
        {
            Assert.IsNotNull(typeof(GameplayController));
        }

        [Test]
        public void Scripts_NoteSpawner_Compiles()
        {
            Assert.IsNotNull(typeof(NoteSpawner));
        }

        [Test]
        public void Scripts_JudgementSystem_Compiles()
        {
            Assert.IsNotNull(typeof(JudgementSystem));
        }

        [Test]
        public void Scripts_InputHandler_Compiles()
        {
            Assert.IsNotNull(typeof(InputHandler));
        }

        [Test]
        public void Scripts_Note_Compiles()
        {
            Assert.IsNotNull(typeof(Note));
        }

        [Test]
        public void Scripts_BeatMapper_Compiles()
        {
            Assert.IsNotNull(typeof(BeatMapper));
        }

        [Test]
        public void Scripts_SmartBeatMapper_Compiles()
        {
            Assert.IsNotNull(typeof(SmartBeatMapper));
        }

        [Test]
        public void Scripts_AudioAnalyzer_Compiles()
        {
            Assert.IsNotNull(typeof(AudioAnalyzer));
        }

        [Test]
        public void Scripts_SongData_Compiles()
        {
            Assert.IsNotNull(typeof(SongData));
        }

        [Test]
        public void Scripts_NoteData_Compiles()
        {
            Assert.IsNotNull(typeof(NoteData));
        }

        #endregion

        #region 4-Lane Structure Validation

        [Test]
        public void FourLane_ScratchLeft_IsLane0()
        {
            var note = new NoteData(1f, 0, NoteType.Scratch);
            Assert.AreEqual(0, note.LaneIndex, "ScratchL should be lane 0");
        }

        [Test]
        public void FourLane_Key1_IsLane1()
        {
            var note = new NoteData(1f, 1, NoteType.Tap);
            Assert.AreEqual(1, note.LaneIndex, "Key1 should be lane 1");
        }

        [Test]
        public void FourLane_Key2_IsLane2()
        {
            var note = new NoteData(1f, 2, NoteType.Tap);
            Assert.AreEqual(2, note.LaneIndex, "Key2 should be lane 2");
        }

        [Test]
        public void FourLane_ScratchRight_IsLane3()
        {
            var note = new NoteData(1f, 3, NoteType.Scratch);
            Assert.AreEqual(3, note.LaneIndex, "ScratchR should be lane 3");
        }

        #endregion

        #region Note Sorting Tests

        [Test]
        public void Notes_SortByHitTime_Works()
        {
            var notes = new List<NoteData>
            {
                new NoteData(5f, 1, NoteType.Tap),
                new NoteData(2f, 2, NoteType.Tap),
                new NoteData(8f, 1, NoteType.Long, 1f),
                new NoteData(1f, 0, NoteType.Scratch)
            };

            notes.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));

            Assert.AreEqual(1f, notes[0].HitTime);
            Assert.AreEqual(2f, notes[1].HitTime);
            Assert.AreEqual(5f, notes[2].HitTime);
            Assert.AreEqual(8f, notes[3].HitTime);
        }

        [Test]
        public void Notes_MixedTypes_CanCoexist()
        {
            var notes = new List<NoteData>
            {
                new NoteData(1f, 0, NoteType.Scratch),
                new NoteData(1f, 1, NoteType.Tap),
                new NoteData(1f, 2, NoteType.Long, 2f)
            };

            // Three notes at the same time, different lanes/types
            Assert.AreEqual(3, notes.Count);
            Assert.AreEqual(NoteType.Scratch, notes[0].Type);
            Assert.AreEqual(NoteType.Tap, notes[1].Type);
            Assert.AreEqual(NoteType.Long, notes[2].Type);
        }

        #endregion
    }
}
