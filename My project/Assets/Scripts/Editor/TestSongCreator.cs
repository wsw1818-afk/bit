using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using AIBeat.Data;
using System.Collections.Generic;

namespace AIBeat.Editor
{
    /// <summary>
    /// 테스트용 샘플 곡 데이터 생성
    /// </summary>
#if UNITY_EDITOR
    public class TestSongCreator : UnityEditor.Editor
    {
        [MenuItem("Tools/A.I. BEAT/Create Test Song Data")]
        public static void CreateTestSongData()
        {
            // SongData ScriptableObject 생성
            var songData = ScriptableObject.CreateInstance<SongData>();

            // 기본 정보 설정
            songData.Title = "Test Beat";
            songData.Artist = "A.I. Composer";
            songData.BPM = 120f;
            songData.Duration = 60f;
            songData.Difficulty = 3;

            // 테스트 노트 생성 (30초 동안 다양한 패턴)
            var notes = new List<NoteData>();
            float bpm = 120f;
            float beatInterval = 60f / bpm; // 0.5초

            // 4비트마다 노트 생성
            for (int i = 0; i < 60; i++)
            {
                float hitTime = 2f + (i * beatInterval); // 2초 후부터 시작

                // 레인 패턴 (0-6)
                int lane = i % 7;

                // 노트 타입 결정
                NoteType noteType = NoteType.Tap;
                float duration = 0f;

                // 매 8번째 노트는 롱노트
                if (i % 8 == 0 && i > 0)
                {
                    noteType = NoteType.Long;
                    duration = beatInterval * 2; // 2비트 길이
                }
                // 매 16번째 노트는 스크래치
                else if (i % 16 == 0)
                {
                    noteType = NoteType.Scratch;
                    lane = Random.Range(0, 2) == 0 ? 0 : 6; // 스크래치는 양 끝 레인
                }

                notes.Add(new NoteData
                {
                    Type = noteType,
                    LaneIndex = lane,
                    HitTime = hitTime,
                    Duration = duration
                });
            }

            songData.Notes = notes.ToArray();

            // 에셋으로 저장
            string path = "Assets/Resources/Songs/TestBeat.asset";
            AssetDatabase.CreateAsset(songData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TestSongCreator] 테스트 곡 데이터 생성 완료: {path}");
            Debug.Log($"[TestSongCreator] 총 {notes.Count}개 노트, BPM: {bpm}");

            // 선택
            Selection.activeObject = songData;
        }

        [MenuItem("Tools/A.I. BEAT/Create Simple Test Pattern")]
        public static void CreateSimpleTestPattern()
        {
            var songData = ScriptableObject.CreateInstance<SongData>();

            songData.Title = "Simple Test";
            songData.Artist = "Debug";
            songData.BPM = 100f;
            songData.Duration = 30f;
            songData.Difficulty = 1;

            var notes = new List<NoteData>();

            // 간단한 테스트: 1초마다 순서대로 레인에 노트
            for (int i = 0; i < 20; i++)
            {
                notes.Add(new NoteData
                {
                    Type = NoteType.Tap,
                    LaneIndex = i % 5 + 1, // 레인 1-5만 사용
                    HitTime = 3f + i,      // 3초부터 시작
                    Duration = 0f
                });
            }

            songData.Notes = notes.ToArray();

            string path = "Assets/Resources/Songs/SimpleTest.asset";
            AssetDatabase.CreateAsset(songData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TestSongCreator] 간단한 테스트 패턴 생성 완료: {path}");
            Selection.activeObject = songData;
        }
    }
#endif
}
