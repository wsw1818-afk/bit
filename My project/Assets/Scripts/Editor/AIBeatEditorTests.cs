using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using AIBeat.Data;
using AIBeat.Gameplay;
using AIBeat.Core;

namespace AIBeat.Tests.Editor
{
    /// <summary>
    /// A.I. BEAT 에디터 모드 테스트
    /// </summary>
    public class AIBeatEditorTests
    {
        #region SongData Tests

        [Test]
        public void SongData_SimpleTestAsset_Exists()
        {
            var songData = Resources.Load<SongData>("Songs/SimpleTest");
            Assert.IsNotNull(songData, "SimpleTest.asset이 Resources/Songs 폴더에 존재해야 합니다.");
        }

        [Test]
        public void SongData_SimpleTestAsset_HasValidData()
        {
            var songData = Resources.Load<SongData>("Songs/SimpleTest");
            Assert.IsNotNull(songData);

            Assert.AreEqual("Simple Test", songData.Title, "곡 제목이 'Simple Test'여야 합니다.");
            Assert.AreEqual("Debug", songData.Artist, "아티스트가 'Debug'여야 합니다.");
            Assert.AreEqual(100f, songData.BPM, "BPM이 100이어야 합니다.");
            Assert.AreEqual(30f, songData.Duration, "Duration이 30초여야 합니다.");
        }

        [Test]
        public void SongData_SimpleTestAsset_HasNotes()
        {
            var songData = Resources.Load<SongData>("Songs/SimpleTest");
            Assert.IsNotNull(songData);

            Assert.IsNotNull(songData.Notes, "Notes 배열이 null이 아니어야 합니다.");
            Assert.AreEqual(20, songData.Notes.Length, "SimpleTest에는 20개의 노트가 있어야 합니다.");
        }

        [Test]
        public void SongData_Notes_HaveValidLaneIndex()
        {
            var songData = Resources.Load<SongData>("Songs/SimpleTest");
            Assert.IsNotNull(songData);

            foreach (var note in songData.Notes)
            {
                Assert.GreaterOrEqual(note.LaneIndex, 0, "레인 인덱스는 0 이상이어야 합니다.");
                Assert.LessOrEqual(note.LaneIndex, 6, "레인 인덱스는 6 이하여야 합니다.");
            }
        }

        [Test]
        public void SongData_Notes_HaveValidHitTime()
        {
            var songData = Resources.Load<SongData>("Songs/SimpleTest");
            Assert.IsNotNull(songData);

            float previousTime = 0f;
            foreach (var note in songData.Notes)
            {
                Assert.GreaterOrEqual(note.HitTime, previousTime, "노트는 시간순으로 정렬되어야 합니다.");
                Assert.LessOrEqual(note.HitTime, songData.Duration, "노트 시간은 곡 길이 이내여야 합니다.");
                previousTime = note.HitTime;
            }
        }

        #endregion

        #region NoteData Tests

        [Test]
        public void NoteData_Constructor_SetsValuesCorrectly()
        {
            var noteData = new NoteData(5.5f, 3, NoteType.Tap, 0f);

            Assert.AreEqual(5.5f, noteData.HitTime);
            Assert.AreEqual(3, noteData.LaneIndex);
            Assert.AreEqual(NoteType.Tap, noteData.Type);
            Assert.AreEqual(0f, noteData.Duration);
        }

        [Test]
        public void NoteData_LongNote_HasDuration()
        {
            var longNote = new NoteData(10f, 2, NoteType.Long, 2.5f);

            Assert.AreEqual(NoteType.Long, longNote.Type);
            Assert.AreEqual(2.5f, longNote.Duration, "롱노트는 Duration 값이 있어야 합니다.");
        }

        #endregion

        #region Scene Assets Tests

        [Test]
        public void Scene_Gameplay_Exists()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Gameplay.unity");
            Assert.IsNotNull(sceneAsset, "Gameplay.unity 씬이 존재해야 합니다.");
        }

        [Test]
        public void Scene_MainMenu_Exists()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
            Assert.IsNotNull(sceneAsset, "MainMenu.unity 씬이 존재해야 합니다.");
        }

        [Test]
        public void Scene_SongSelect_Exists()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SongSelect.unity");
            Assert.IsNotNull(sceneAsset, "SongSelect.unity 씬이 존재해야 합니다.");
        }

        #endregion

        #region Material Tests

        [Test]
        public void Material_NormalNoteMat_Exists()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/NormalNoteMat.mat");
            Assert.IsNotNull(mat, "NormalNoteMat.mat이 존재해야 합니다.");
        }

        [Test]
        public void Material_LongNoteMat_Exists()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LongNoteMat.mat");
            Assert.IsNotNull(mat, "LongNoteMat.mat이 존재해야 합니다.");
        }

        [Test]
        public void Material_ScratchNoteMat_Exists()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ScratchNoteMat.mat");
            Assert.IsNotNull(mat, "ScratchNoteMat.mat이 존재해야 합니다.");
        }

        [Test]
        public void Material_JudgementLineMat_Exists()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/JudgementLineMat.mat");
            Assert.IsNotNull(mat, "JudgementLineMat.mat이 존재해야 합니다.");
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

        #endregion

        #region Script Compilation Tests

        [Test]
        public void Scripts_GameplayController_Compiles()
        {
            var type = typeof(GameplayController);
            Assert.IsNotNull(type, "GameplayController 클래스가 존재해야 합니다.");
        }

        [Test]
        public void Scripts_NoteSpawner_Compiles()
        {
            var type = typeof(NoteSpawner);
            Assert.IsNotNull(type, "NoteSpawner 클래스가 존재해야 합니다.");
        }

        [Test]
        public void Scripts_JudgementSystem_Compiles()
        {
            var type = typeof(JudgementSystem);
            Assert.IsNotNull(type, "JudgementSystem 클래스가 존재해야 합니다.");
        }

        [Test]
        public void Scripts_InputHandler_Compiles()
        {
            var type = typeof(InputHandler);
            Assert.IsNotNull(type, "InputHandler 클래스가 존재해야 합니다.");
        }

        [Test]
        public void Scripts_Note_Compiles()
        {
            var type = typeof(Note);
            Assert.IsNotNull(type, "Note 클래스가 존재해야 합니다.");
        }

        #endregion
    }
}
