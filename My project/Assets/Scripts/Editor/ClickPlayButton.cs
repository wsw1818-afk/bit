using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using AIBeat.Core;
using AIBeat.Data;

/// <summary>
/// Domain Reload 후에도 캡처 예약을 유지하기 위한 InitializeOnLoad 핸들러.
/// SessionState에 플래그를 저장하여 Play 모드 진입 완료 시 자동 캡처 실행.
/// </summary>
[InitializeOnLoad]
public class PlayModeCaptureBridge
{
    private const string CAPTURE_PENDING_KEY = "AIBeat_CapturePending";

    static PlayModeCaptureBridge()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(CAPTURE_PENDING_KEY, false))
        {
            SessionState.SetBool(CAPTURE_PENDING_KEY, false);
            Debug.Log("[PlayModeCaptureBridge] Play 모드 진입 감지 → 캡처 실행 예약");
            EditorApplication.delayCall += () =>
            EditorApplication.delayCall += () =>
            EditorApplication.delayCall += () =>
            {
                ClickPlayButton.CapturePopupSafe();
            };
        }
    }

    public static void RequestCaptureOnPlay()
    {
        SessionState.SetBool(CAPTURE_PENDING_KEY, true);
    }
}

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

        // GameplayUI를 통해 직접 ShowPauseMenu 호출
        var gameplayUI = GameObject.FindFirstObjectByType<AIBeat.UI.GameplayUI>();
        if (gameplayUI != null)
        {
            Debug.Log("[ClickSettings] GameplayUI.ShowPauseMenu(true) 직접 호출");
            gameplayUI.ShowPauseMenu(true);
            UnityEngine.Time.timeScale = 0f;
        }
        else
        {
            Debug.LogError("[ClickSettings] GameplayUI를 찾을 수 없습니다");
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

    [MenuItem("Tools/A.I. BEAT/Click First Song Card")]
    public static void ClickFirstSongCard()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ClickSongCard] Play 모드에서만 실행 가능합니다");
            return;
        }

        Application.runInBackground = true;

        var card = GameObject.Find("SongCard_0");
        if (card != null)
        {
            var btn = card.GetComponent<Button>();
            if (btn != null)
            {
                Debug.Log("[ClickSongCard] SongCard_0 클릭 실행");
                btn.onClick.Invoke();
            }
            else
            {
                Debug.LogError("[ClickSongCard] SongCard_0에 Button 컴포넌트가 없습니다");
            }
        }
        else
        {
            Debug.LogError("[ClickSongCard] SongCard_0을 찾을 수 없습니다");
        }
    }

    [MenuItem("Tools/A.I. BEAT/Diagnose Font Material")]
    public static void DiagnoseFontMaterial()
    {
        if (!Application.isPlaying) return;
        var font = AIBeat.Core.KoreanFontManager.KoreanFont;
        if (font == null) { Debug.LogError("[Diag] KoreanFont is null"); return; }
        var mat = font.material;
        var mainTex = mat != null ? mat.GetTexture(TMPro.ShaderUtilities.ID_MainTex) : null;
        var atlasTex = font.atlasTextures?.Length > 0 ? font.atlasTextures[0] : null;
        Debug.Log($"[Diag] font={font.name}, mat={mat?.name}, _MainTex={mainTex?.name}({mainTex?.width}x{mainTex?.height}), atlas={atlasTex?.name}({atlasTex?.width}x{atlasTex?.height}), charCount={font.characterTable?.Count}, popMode={font.atlasPopulationMode}");

        // 모든 SongCard 상태
        for (int i = 0; i < 3; i++)
        {
            var cardGo = GameObject.Find($"SongCard_{i}");
            if (cardGo == null) { Debug.Log($"[Diag] Card{i}: not found"); continue; }
            var cg = cardGo.GetComponent<UnityEngine.CanvasGroup>();
            var rt = cardGo.GetComponent<UnityEngine.RectTransform>();
            Debug.Log($"[Diag] Card{i}: active={cardGo.activeInHierarchy}, scale={rt?.localScale}, pos={rt?.position}, size={rt?.rect.size}, cgAlpha={cg?.alpha}");

            var titleGo = GameObject.Find($"SongCard_{i}/InfoPanel/Title");
            if (titleGo == null) continue;
            var tmp = titleGo.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp == null) continue;
            var trt = tmp.rectTransform;
            var tMat = tmp.fontMaterial;
            var tTex = tMat != null ? tMat.GetTexture(TMPro.ShaderUtilities.ID_MainTex) : null;
            var le = titleGo.GetComponent<UnityEngine.UI.LayoutElement>();
            var canvas = tmp.canvas;
            Debug.Log($"[Diag] Card{i} Title: text='{tmp.text}', rectSize={trt?.rect.size}, _MainTex={tTex?.name}, truncated={tmp.isTextTruncated}, overflow={tmp.firstOverflowCharacterIndex}, bounds={tmp.bounds.size}, prefW={tmp.preferredWidth}, ppu={tmp.pixelsPerUnit:F3}, fontSize={tmp.fontSize}, le={le?.preferredHeight}, canvas={canvas?.name}");
            // 부모 VerticalLayoutGroup 정보
            var infoPanel = titleGo.transform.parent?.gameObject;
            if (infoPanel != null)
            {
                var ipRt = infoPanel.GetComponent<UnityEngine.RectTransform>();
                var vl = infoPanel.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                Debug.Log($"[Diag] InfoPanel: size={ipRt?.rect.size}, vlg={vl != null}, ctrlW={vl?.childControlWidth}, ctrlH={vl?.childControlHeight}");
            }
        }
    }

    [MenuItem("Tools/A.I. BEAT/Capture Song Select Screen")]
    public static void CaptureSongSelectScreen()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CaptureSongSelect] Play 모드에서만 실행 가능합니다");
            return;
        }

        Application.runInBackground = true;

        // delayCall 5단계 체인 - ForceRebuildAfterFrame 코루틴 완료 후 캡처
        EditorApplication.delayCall += () =>
        EditorApplication.delayCall += () =>
        EditorApplication.delayCall += () =>
        EditorApplication.delayCall += () =>
        EditorApplication.delayCall += () =>
        {
            string dir = System.IO.Path.Combine(Application.dataPath, "..", "Screenshots");
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            string fileName = $"SongSelect_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = System.IO.Path.Combine(dir, fileName);
            UnityEngine.ScreenCapture.CaptureScreenshot(fullPath, 2);
            Debug.Log($"[CaptureSongSelect] 캡처 완료: {fullPath}");
        };
    }

    [MenuItem("Tools/A.I. BEAT/Click Song Card + Capture")]
    public static void ClickSongCardAndCapture()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ClickSongCard+Capture] Play 모드에서만 실행 가능합니다");
            return;
        }

        Application.runInBackground = true;

        var songLibUI = Object.FindFirstObjectByType<AIBeat.UI.SongLibraryUI>();
        if (songLibUI != null)
        {
            songLibUI.ForceShowDifficultyPopup();
            Debug.Log("[ClickSongCard+Capture] ForceShowDifficultyPopup 호출 완료");
        }
        else
        {
            Debug.LogError("[ClickSongCard+Capture] SongLibraryUI를 찾을 수 없습니다");
            return;
        }

        // delayCall 체인으로 안전한 캡처 (Step() 사용 금지)
        EditorApplication.delayCall += () =>
        EditorApplication.delayCall += () =>
        {
            string dir = System.IO.Path.Combine(Application.dataPath, "..", "Screenshots");
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            string fileName = $"DifficultyPopup_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = System.IO.Path.Combine(dir, fileName);

            UnityEngine.ScreenCapture.CaptureScreenshot(fullPath, 2);
            Debug.Log($"[ClickSongCard+Capture] 캡처 완료: {fullPath}");
        };
    }

    [MenuItem("Tools/A.I. BEAT/Click Easy Difficulty")]
    public static void ClickEasyDifficulty()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ClickDifficulty] Play 모드에서만 실행 가능합니다");
            return;
        }

        Application.runInBackground = true;

        var songLibUI = Object.FindFirstObjectByType<AIBeat.UI.SongLibraryUI>();
        if (songLibUI != null)
        {
            songLibUI.ForceShowDifficultyPopup();
            Debug.Log("[ClickDifficulty] 팝업 열기 완료");
        }
        else
        {
            Debug.LogError("[ClickDifficulty] SongLibraryUI를 찾을 수 없습니다");
            return;
        }

        // delayCall로 안전하게 버튼 클릭 (Step() 사용 금지)
        EditorApplication.delayCall += () =>
        EditorApplication.delayCall += () =>
        {
            var btns = Resources.FindObjectsOfTypeAll<Button>();
            foreach (var btn in btns)
            {
                if (btn.gameObject.name == "Btn_쉬움" && btn.gameObject.activeInHierarchy)
                {
                    Debug.Log("[ClickDifficulty] '쉬움' 버튼 클릭!");
                    btn.onClick.Invoke();
                    return;
                }
            }
            Debug.LogError("[ClickDifficulty] 'Btn_쉬움'을 찾을 수 없습니다");
        };
    }

    /// <summary>
    /// 팝업 없이 바로 쉬움(3) 난이도로 게임 시작 (에디터 테스트용)
    /// </summary>
    [MenuItem("Tools/A.I. BEAT/Force Start Easy Game")]
    public static void ForceStartEasyGame()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ForceStartEasy] Play 모드에서만 실행 가능합니다");
            return;
        }

        Application.runInBackground = true;

        var songLibUI = Object.FindFirstObjectByType<AIBeat.UI.SongLibraryUI>();
        if (songLibUI != null)
        {
            songLibUI.ForceStartWithDifficulty(3);
        }
        else
        {
            Debug.LogError("[ForceStartEasy] SongLibraryUI를 찾을 수 없습니다");
        }
    }

    /// <summary>
    /// Christmas Eve 곡으로 바로 게임 시작 (쉬움 난이도 3)
    /// </summary>
    [MenuItem("Tools/A.I. BEAT/Test Christmas Eve Easy")]
    public static void TestChristmasEasyGame()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[TestChristmasEve] Play 모드에서만 실행 가능합니다");
            return;
        }

        Application.runInBackground = true;

        var songLibUI = Object.FindFirstObjectByType<AIBeat.UI.SongLibraryUI>();
        if (songLibUI != null)
        {
            songLibUI.ForceStartWithFile("Christmas Eve.mp3", 3);
        }
        else
        {
            Debug.LogError("[TestChristmasEve] SongLibraryUI를 찾을 수 없습니다");
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

        var songData = Resources.Load<SongData>("Songs/SimpleTest");
        if (songData == null)
        {
            songData = Resources.Load<SongData>("Songs/TestBeat");
        }

        if (songData == null)
        {
            songData = ScriptableObject.CreateInstance<SongData>();
            songData.Title = "Test Song";
            songData.Artist = "Debug";
            songData.BPM = 120f;
            songData.Duration = 30f;
            songData.Difficulty = 3;

            var notes = new System.Collections.Generic.List<NoteData>();
            float beat = 0.5f;
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

    /// <summary>
    /// Play 모드 자동 진입 + 팝업 표시 + 캡처.
    /// 이미 Play 모드면 바로 캡처.
    /// 아니면 SessionState에 플래그 설정 후 delayCall로 Play 진입 → Domain Reload 후 자동 캡처.
    /// </summary>
    [MenuItem("Tools/A.I. BEAT/Play And Capture Popup")]
    public static void PlayAndCapturePopup()
    {
        if (EditorApplication.isPlaying)
        {
            CapturePopupSafe();
            return;
        }

        // SessionState에 캡처 예약 플래그 설정 (Domain Reload에서도 유지됨)
        PlayModeCaptureBridge.RequestCaptureOnPlay();
        Debug.Log("[PlayAndCapture] Play 모드 진입 예약 (update 콜백)...");
        // delayCall은 에디터 포커스 없을 때 실행 안 됨 → update 사용
        EditorApplication.update += StartPlayOnNextUpdate;
    }

    private static void StartPlayOnNextUpdate()
    {
        EditorApplication.update -= StartPlayOnNextUpdate;
        Debug.Log("[PlayAndCapture] update 콜백 → Play 모드 시작");
        EditorApplication.isPlaying = true;
    }

    public static void CapturePopupSafe()
    {
        Application.runInBackground = true;

        var songLibUI = Object.FindFirstObjectByType<AIBeat.UI.SongLibraryUI>();
        if (songLibUI == null)
        {
            Debug.LogError("[PlayAndCapture] SongLibraryUI를 찾을 수 없습니다");
            return;
        }

        songLibUI.ForceShowDifficultyPopup();
        Debug.Log("[PlayAndCapture] 팝업 표시 완료");

        EditorApplication.delayCall += () =>
        EditorApplication.delayCall += () =>
        {
            string dir = System.IO.Path.Combine(Application.dataPath, "..", "Screenshots");
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, $"DifficultyPopup_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
            UnityEngine.ScreenCapture.CaptureScreenshot(path, 2);
            Debug.Log($"[PlayAndCapture] 캡처 완료: {path}");
        };
    }
}
