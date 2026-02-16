using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using AIBeat.Core;
using AIBeat.Data;

public class ClickPlayButton
{
    [MenuItem("Tools/A.I. BEAT/Click Play Button")]
    public static void Click()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ClickPlayButton] Play 모드에서만 실행 가능합니다");
            return;
        }

        var btn = GameObject.Find("PlayButton")?.GetComponent<Button>();
        if (btn != null)
        {
            Debug.Log("[ClickPlayButton] PlayButton 클릭 실행");
            btn.onClick.Invoke();
        }
        else
        {
            Debug.LogError("[ClickPlayButton] PlayButton을 찾을 수 없습니다");
        }
    }

    [MenuItem("Tools/A.I. BEAT/Start Test Game")]
    public static void StartTestGame()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ClickPlayButton] Play 모드에서만 실행 가능합니다");
            return;
        }

        // 테스트 곡 로드 (Resources에서)
        var songData = Resources.Load<SongData>("Songs/SimpleTest");
        if (songData == null)
        {
            songData = Resources.Load<SongData>("Songs/TestBeat");
        }

        // Resources에 없으면 테스트용 곡 데이터 생성
        if (songData == null)
        {
            songData = ScriptableObject.CreateInstance<SongData>();
            songData.Title = "Test Song";
            songData.Artist = "Debug";
            songData.BPM = 120f;
            songData.Duration = 30f;
            songData.Difficulty = 3;

            // 간단한 노트 패턴 생성
            var notes = new System.Collections.Generic.List<NoteData>();
            float beat = 0.5f; // 120 BPM
            for (float t = 2f; t < 28f; t += beat)
            {
                int lane = ((int)(t / beat)) % 4;
                notes.Add(new NoteData(t, lane, NoteType.Tap));
            }
            songData.Notes = notes.ToArray();
        }

        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            Debug.Log($"[ClickPlayButton] 테스트 게임 시작: {songData.Title}");
            gameManager.StartGame(songData);
        }
        else
        {
            Debug.LogError("[ClickPlayButton] GameManager를 찾을 수 없습니다");
        }
    }
}
