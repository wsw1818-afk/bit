using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using AIBeat.Core;
using AIBeat.Data;

public class ClickPlayButton
{
    [MenuItem("Tools/A.I. BEAT/Click Settings Button")]
    public static void ClickSettings()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ClickSettings] Play 모드에서만 실행 가능합니다");
            return;
        }

        Application.runInBackground = true;

        var btn = GameObject.Find("SettingsButton")?.GetComponent<Button>();
        if (btn != null)
        {
            Debug.Log("[ClickSettings] SettingsButton 클릭 실행");
            btn.onClick.Invoke();
        }
        else
        {
            Debug.LogError("[ClickSettings] SettingsButton을 찾을 수 없습니다");
        }
    }

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

    [MenuItem("Tools/A.I. BEAT/Force Show Result")]
    public static void ForceShowResult()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ForceShowResult] Play 모드에서만 실행 가능합니다");
            return;
        }

        Application.runInBackground = true;

        var gameplayUI = Object.FindFirstObjectByType<AIBeat.UI.GameplayUI>();
        if (gameplayUI == null)
        {
            Debug.LogError("[ForceShowResult] GameplayUI를 찾을 수 없습니다");
            return;
        }

        var js = Object.FindFirstObjectByType<AIBeat.Gameplay.JudgementSystem>();
        var result = new AIBeat.Gameplay.GameResult
        {
            Score = js != null ? js.TotalScore : 63759,
            MaxCombo = js != null ? js.MaxCombo : 37,
            Accuracy = js != null ? js.Accuracy : 84.5f,
            Rank = "S",
            PerfectCount = js != null ? js.PerfectCount : 39,
            GreatCount = js != null ? js.GreatCount : 13,
            GoodCount = js != null ? js.GoodCount : 12,
            BadCount = js != null ? js.BadCount : 2,
            MissCount = js != null ? js.MissCount : 0,
            BonusScore = js != null ? js.BonusScore : 3350,
        };

        gameplayUI.ShowResult(result);
        Debug.Log("[ForceShowResult] Result 화면 강제 표시 완료");
    }

    [MenuItem("Tools/A.I. BEAT/Test Analysis Overlay")]
    public static void TestAnalysisOverlay()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[TestAnalysisOverlay] Play 모드에서만 실행 가능합니다");
            return;
        }

        Application.runInBackground = true;

        var gameplayUI = Object.FindFirstObjectByType<AIBeat.UI.GameplayUI>();
        if (gameplayUI == null)
        {
            Debug.LogError("[TestAnalysisOverlay] GameplayUI를 찾을 수 없습니다");
            return;
        }

        gameplayUI.SetAnalysisSongTitle("Test Song - AI Analysis");
        gameplayUI.ShowAnalysisOverlay(true);
        // 코루틴으로 프로그레스 시뮬레이션
        var mono = Object.FindFirstObjectByType<MonoBehaviour>();
        mono?.StartCoroutine(SimulateAnalysisProgress(gameplayUI));
        Debug.Log("[TestAnalysisOverlay] 분석 오버레이 테스트 시작");
    }

    private static System.Collections.IEnumerator SimulateAnalysisProgress(AIBeat.UI.GameplayUI ui)
    {
        float progress = 0f;
        while (progress < 1f)
        {
            progress += UnityEngine.Random.Range(0.005f, 0.02f);
            if (progress > 1f) progress = 1f;
            ui.UpdateAnalysisProgress(progress);
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(1f);
        ui.ShowAnalysisOverlay(false);
        Debug.Log("[TestAnalysisOverlay] 분석 오버레이 테스트 완료");
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
