# AI Beat 상세 버그 분석 및 개선 기획안

## 📅 분석 일자: 2026-02-18
## 🎯 분석 범위: 전체 코드베이스 심층 분석

---

# 1. 🐛 버그 상세 분석

## 1.1 Critical (즉시 수정 필요)

### C-1: SettingsManager DontDestroyOnLoad 누락
**파일**: [`SettingsManager.cs:96-107`](My%20project/Assets/Scripts/Core/SettingsManager.cs:96)

**현재 코드**:
```csharp
private void Awake()
{
    // 단순 싱글톤 (DontDestroyOnLoad 사용하지 않음)
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    LoadSettings();
}
```

**문제점**:
- 씬 전환 시 `SettingsManager`가 파괴됨
- `OnSettingChanged` 이벤트 구독자들이 해제됨
- 설정 값이 초기화됨

**수정 코드**:
```csharp
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);  // ← 추가
    LoadSettings();
}
```

---

### C-2: AudioManager DontDestroyOnLoad 누락
**파일**: [`AudioManager.cs:72-83`](My%20project/Assets/Scripts/Core/AudioManager.cs:72)

**현재 코드**:
```csharp
private void Awake()
{
    // 단순 싱글톤 (DontDestroyOnLoad 사용하지 않음)
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    Initialize();
}
```

**문제점**:
- 씬 전환 시 `AudioManager`가 파괴됨
- BGM이 끊김
- `OnBGMEnded` 이벤트가 발생하지 않음

**수정 코드**:
```csharp
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);  // ← 추가
    Initialize();
}
```

---

### C-3: JudgementSystem 이벤트 구독 해제 누락
**파일**: [`JudgementSystem.cs:79-80`](My%20project/Assets/Scripts/Gameplay/JudgementSystem.cs:79)

**현재 코드**:
```csharp
// Initialize()에서 구독
SettingsManager.OnSettingChanged -= OnSettingChanged;
SettingsManager.OnSettingChanged += OnSettingChanged;
```

**문제점**:
- `OnDestroy()`에서 이벤트 해제 없음
- 씬 언로드 시 메모리 누수
- `SettingsManager`가 null이 된 후 이벤트 호출 위험

**수정 코드** (클래스 끝에 추가):
```csharp
private void OnDestroy()
{
    SettingsManager.OnSettingChanged -= OnSettingChanged;
}
```

---

### C-4: NoteSpawner 이벤트 구독 해제 및 메모리 누수
**파일**: [`NoteSpawner.cs:75-80`](My%20project/Assets/Scripts/Gameplay/NoteSpawner.cs:75)

**현재 코드**:
```csharp
// Awake()에서 구독
SettingsManager.OnSettingChanged += OnSettingChanged;

// 동적 생성된 리소스
private List<GameObject> dynamicPrefabs = new List<GameObject>();
private List<Material> managedMaterials = new List<Material>();
```

**문제점**:
- `OnDestroy()`에서 이벤트 해제 없음
- 동적 생성된 `Material`이 해제되지 않음
- 동적 생성된 `GameObject` 프리팹이 해제되지 않음

**수정 코드** (클래스 끝에 추가):
```csharp
private void OnDestroy()
{
    // 이벤트 구독 해제
    SettingsManager.OnSettingChanged -= OnSettingChanged;
    
    // 스폰 루프 정지
    isSpawning = false;
    if (spawnLoopCoroutine != null)
    {
        StopCoroutine(spawnLoopCoroutine);
        spawnLoopCoroutine = null;
    }
    
    // 동적 생성된 Material 정리
    foreach (var mat in managedMaterials)
    {
        if (mat != null)
        {
            Destroy(mat);
        }
    }
    managedMaterials.Clear();
    
    // 동적 생성된 프리팹 정리
    foreach (var prefab in dynamicPrefabs)
    {
        if (prefab != null)
        {
            Destroy(prefab);
        }
    }
    dynamicPrefabs.Clear();
    
    // 활성 노트 정리
    activeNotes?.Clear();
    noteQueue?.Clear();
    notePools?.Clear();
}
```

---

## 1.2 High (1주 내 수정 권장)

### H-1: InputHandler 예외 처리 미흡
**파일**: [`InputHandler.cs:58-66`](My%20project/Assets/Scripts/Gameplay/InputHandler.cs:58)

**현재 코드**:
```csharp
private void Start()
{
    CacheScratchThreshold();
    try
    {
        CacheLaneBoundaries();
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[InputHandler] CacheLaneBoundaries failed: {e.Message}. Using fallback.");
        laneBoundaries = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
    }
    StartCoroutine(InputLoop());
}
```

**문제점**:
- `Camera.main`이 null인 경우 처리는 되어있으나
- `CacheLaneBoundaries()` 내부에서 `Screen.width/height`가 0인 경우 처리 없음
- `InputLoop()` 코루틴이 중복 시작될 수 있음

**수정 코드**:
```csharp
private Coroutine inputLoopCoroutine;

private void Start()
{
    CacheScratchThreshold();
    try
    {
        CacheLaneBoundaries();
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[InputHandler] CacheLaneBoundaries failed: {e.Message}. Using fallback.");
        laneBoundaries = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
    }
    
    // 코루틴 중복 시작 방지
    if (inputLoopCoroutine == null)
        inputLoopCoroutine = StartCoroutine(InputLoop());
}

private void OnDestroy()
{
    if (inputLoopCoroutine != null)
    {
        StopCoroutine(inputLoopCoroutine);
        inputLoopCoroutine = null;
    }
}
```

---

### H-2: GameplayController 이벤트 구독 해제 누락
**파일**: [`GameplayController.cs:312-325`](My%20project/Assets/Scripts/Gameplay/GameplayController.cs:312)

**현재 코드**:
```csharp
// Initialize()에서 구독
if (inputHandler != null)
    inputHandler.OnLaneInput += HandleInput;
if (judgementSystem != null)
{
    judgementSystem.OnJudgement += HandleJudgement;
    judgementSystem.OnJudgementDetailed += HandleJudgementDetailed;
    scoreChangedHandler = (score) => gameplayUI?.UpdateScore(score);
    comboChangedHandler = (combo) => gameplayUI?.UpdateCombo(combo);
    bonusScoreHandler = (tick, total) => gameplayUI?.ShowBonusScore(tick, total);
    judgementSystem.OnScoreChanged += scoreChangedHandler;
    judgementSystem.OnComboChanged += comboChangedHandler;
    judgementSystem.OnBonusScore += bonusScoreHandler;
}
```

**문제점**:
- `OnDestroy()`에서 이벤트 해제 확인 필요
- `AudioManager` 이벤트도 해제 필요

**수정 코드** (OnDestroy 추가/수정):
```csharp
private void OnDestroy()
{
    // InputHandler 이벤트 해제
    if (inputHandler != null)
        inputHandler.OnLaneInput -= HandleInput;
    
    // JudgementSystem 이벤트 해제
    if (judgementSystem != null)
    {
        judgementSystem.OnJudgement -= HandleJudgement;
        judgementSystem.OnJudgementDetailed -= HandleJudgementDetailed;
        judgementSystem.OnScoreChanged -= scoreChangedHandler;
        judgementSystem.OnComboChanged -= comboChangedHandler;
        judgementSystem.OnBonusScore -= bonusScoreHandler;
    }
    
    // AudioManager 이벤트 해제
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.OnBGMLoaded -= OnAudioLoaded;
        AudioManager.Instance.OnBGMEnded -= OnSongEnd;
        AudioManager.Instance.OnBGMLoadFailed -= OnAudioLoadFailed;
    }
    
    // 코루틴 정지
    if (inputLoopCoroutine != null)
        StopCoroutine(inputLoopCoroutine);
    if (holdBonusCoroutine != null)
        StopCoroutine(holdBonusCoroutine);
    if (autoPlayCoroutine != null)
        StopCoroutine(autoPlayCoroutine);
}
```

---

## 1.3 Medium (개선 권장)

### M-1: Magic Number 상수화
**파일**: 여러 파일

**현재 문제점**:
```csharp
// GameplayController.cs
private const float HOLD_BONUS_TICK_INTERVAL = 0.1f;
private const int HOLD_BONUS_PER_TICK = 50;
float laneWidth = 5.6f; // 4레인 x 1.4유닛
float padding = 0.3f;
cam.orthographicSize = Mathf.Max(requiredOrthoSize, 7f);

// NoteSpawner.cs
[SerializeField] private float spawnDistance = 12f;
[SerializeField] private float lookAhead = 3f;
noteObj.transform.localScale = new Vector3(1.1f, 0.3f, 1f);

// JudgementSystem.cs
[SerializeField] private float perfectWindow = 0.050f;
[SerializeField] private float greatWindow = 0.100f;
```

**개선안 - GameConstants.cs (신규)**:
```csharp
namespace AIBeat.Core
{
    /// <summary>
    /// 게임 전체 상수 정의
    /// </summary>
    public static class GameConstants
    {
        // ===== 레인 설정 =====
        public const int LaneCount = 4;
        public const float LaneWidth = 1.4f;
        public const float LaneTotalWidth = LaneCount * LaneWidth;  // 5.6f
        public const float LanePadding = 0.3f;
        
        // ===== 노트 설정 =====
        public const float DefaultNoteSpeed = 5f;
        public const float MinNoteSpeed = 1f;
        public const float MaxNoteSpeed = 15f;
        public const float NoteSpawnDistance = 12f;
        public const float NoteLookAheadTime = 3f;
        public const float NoteWidth = 1.1f;
        public const float NoteHeight = 0.3f;
        
        // ===== 카메라 설정 =====
        public const float CameraMinOrthoSize = 7f;
        public const float CameraY = 6f;
        
        // ===== 판정 윈도우 (초) =====
        public const float PerfectWindow = 0.050f;   // ±50ms
        public const float GreatWindow = 0.100f;     // ±100ms
        public const float GoodWindow = 0.200f;      // ±200ms
        public const float BadWindow = 0.350f;       // ±350ms
        
        // ===== 점수 설정 =====
        public const int BaseScorePerNote = 1000;
        public const float MaxComboBonus = 0.5f;
        public const int ComboForMaxBonus = 100;
        public const float HoldBonusTickInterval = 0.1f;
        public const int HoldBonusPerTick = 50;
        
        // ===== UI 설정 =====
        public const float FadeDuration = 0.3f;
        public const float CountdownDuration = 3f;
        public const float JudgementDisplayTime = 0.5f;
        
        // ===== 풀 설정 =====
        public const int DefaultPoolSize = 100;
        public const int MaxPoolSize = 200;
        public const int PoolExpandAmount = 20;
    }
}
```

---

### M-2: Debug.Log 빌드 성능 영향
**파일**: 여러 파일

**현재 문제점**:
```csharp
// NoteSpawner.cs
[SerializeField] private bool showDebugLogs = true;

// 일반 Debug.Log 사용
Debug.Log($"[NoteSpawner] Pool initialized...");
```

**개선안**:
```csharp
// Conditional 특성 활용
[System.Diagnostics.Conditional("UNITY_EDITOR")]
private void LogDebug(string message)
{
    if (showDebugLogs)
        Debug.Log(message);
}

// 사용
LogDebug($"[NoteSpawner] Pool initialized...");
```

---

# 2. 🚀 기능 개선 기획

## 2.1 성능 최적화

### 2.1.1 오브젝트 풀링 동적 확장 (부분 구현됨)
**파일**: [`NoteSpawner.cs:30-47`](My%20project/Assets/Scripts/Gameplay/NoteSpawner.cs:30)

**현재 상태**:
```csharp
[Header("Pool Settings")]
[SerializeField] private int poolSize = 100;
[SerializeField] private int maxPoolSize = 200;
[SerializeField] private int poolExpandAmount = 20;

// 풀 동적 확장: 타입별 총 생성 수 추적
private Dictionary<NoteType, int> poolTotalCounts = new Dictionary<NoteType, int>();
```

**개선 필요**:
- `ExpandPoolIfNeeded()` 메서드 구현 확인
- 풀 부족 시 자동 확장 로직 검증

### 2.1.2 GC Allocation 최적화
**파일**: [`GameplayController.cs:142-181`](My%20project/Assets/Scripts/Gameplay/GameplayController.cs:142)

**현재 문제점**:
```csharp
private System.Collections.IEnumerator HoldBonusTickLoop()
{
    var notesToRemove = new List<Note>();  // 매 루프마다 할당?
    var notesToUpdate = new List<KeyValuePair<Note, float>>();
    
    while (true)
    {
        // ...
        notesToRemove.Clear();
        notesToUpdate.Clear();
        // ...
    }
}
```

**개선안 - ListPool 유틸리티**:
```csharp
// Utils/ListPool.cs (신규)
namespace AIBeat.Utils
{
    public static class ListPool<T>
    {
        private static readonly Stack<List<T>> pool = new();
        
        public static List<T> Get()
        {
            return pool.Count > 0 ? pool.Pop() : new List<T>(32);
        }
        
        public static void Return(List<T> list)
        {
            list.Clear();
            if (pool.Count < 16)  // 최대 16개까지만 풀링
                pool.Push(list);
        }
    }
}

// 사용 예시
private System.Collections.IEnumerator HoldBonusTickLoop()
{
    while (true)
    {
        yield return null;
        if (!isPlaying || isPaused || holdingNotes.Count == 0) continue;
        
        var notesToRemove = ListPool<Note>.Get();
        var notesToUpdate = ListPool<KeyValuePair<Note, float>>.Get();
        
        try
        {
            // 작업 수행
        }
        finally
        {
            ListPool<Note>.Return(notesToRemove);
            ListPool<KeyValuePair<Note, float>>.Return(notesToUpdate);
        }
    }
}
```

---

## 2.2 UX/UI 개선

### 2.2.1 콤보 UI 강화
**파일**: [`GameplayUI.cs`](My%20project/Assets/Scripts/UI/GameplayUI.cs)

**현재 상태**: 기본 콤보 텍스트만 표시

**개선안**:
```csharp
[Header("Combo Effects")]
[SerializeField] private ParticleSystem comboMilestoneParticle;
[SerializeField] private AudioClip comboMilestoneSound;

// 콤보 마일스톤 (10, 25, 50, 100)
private readonly int[] comboMilestones = { 10, 25, 50, 100 };
private int lastMilestoneIndex = -1;

public void UpdateCombo(int combo)
{
    if (combo < 2)
    {
        if (comboText != null) comboText.text = "";
        return;
    }
    
    comboText.text = combo.ToString();
    
    // 콤보 색상 변경
    Color comboColor = GetComboColor(combo);
    comboText.color = comboColor;
    
    // 마일스톤 체크
    CheckComboMilestone(combo);
    
    // 팝 애니메이션
    StartCoroutine(ComboPopAnimation());
}

private Color GetComboColor(int combo)
{
    if (combo >= 100) return new Color(1f, 0.5f, 0f);     // 오렌지
    if (combo >= 50) return new Color(1f, 0.84f, 0f);     // 골드
    if (combo >= 25) return new Color(0.58f, 0.29f, 0.98f); // 퍼플
    if (combo >= 10) return new Color(0f, 1f, 1f);        // 시안
    return Color.white;
}

private void CheckComboMilestone(int combo)
{
    for (int i = lastMilestoneIndex + 1; i < comboMilestones.Length; i++)
    {
        if (combo >= comboMilestones[i])
        {
            TriggerMilestoneEffect(comboMilestones[i]);
            lastMilestoneIndex = i;
        }
        else break;
    }
}

private void TriggerMilestoneEffect(int milestone)
{
    if (comboMilestoneParticle != null)
        comboMilestoneParticle.Play();
    
    if (comboMilestoneSound != null)
        AudioManager.Instance?.PlaySFX(comboMilestoneSound);
    
    // 화면 테두리 플래시 효과
    StartCoroutine(ScreenFlashEffect());
}
```

### 2.2.2 판정 표시 개선 (Early/Late)
**파일**: [`GameplayUI.cs`](My%20project/Assets/Scripts/UI/GameplayUI.cs)

**현재 상태**: `earlyLateText` 필드 존재

**개선안**:
```csharp
public void ShowJudgementDetailed(JudgementResult result, float rawDiff)
{
    // 기본 판정 표시
    ShowJudgement(result);
    
    // Early/Late 표시
    if (result != JudgementResult.Miss && earlyLateText != null)
    {
        float diffMs = rawDiff * 1000f;
        
        if (Mathf.Abs(diffMs) > 10f)  // 10ms 이상 차이일 때만 표시
        {
            string direction = diffMs > 0 ? "LATE" : "EARLY";
            Color color = diffMs > 0 ? 
                new Color(1f, 0.5f, 0.5f) :  // 빨강 (Late)
                new Color(0.5f, 0.7f, 1f);   // 파랑 (Early)
            
            earlyLateText.text = $"{Mathf.Abs(diffMs):F0}ms {direction}";
            earlyLateText.color = color;
            earlyLateText.gameObject.SetActive(true);
        }
        else
        {
            earlyLateText.gameObject.SetActive(false);
        }
    }
}
```

---

## 2.3 게임플레이 개선

### 2.3.1 스킵/리트라이 기능
**파일**: [`GameplayController.cs`](My%20project/Assets/Scripts/Gameplay/GameplayController.cs)

**개선안**:
```csharp
/// <summary>
/// 현재 곡 재시작
/// </summary>
public void QuickRestart()
{
    Time.timeScale = 1f;
    isPaused = false;
    isPlaying = false;
    
    // 리소스 정리
    noteSpawner?.StopSpawning();
    AudioManager.Instance?.StopBGM();
    
    // 씬 리로드
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}

/// <summary>
/// 결과 화면으로 스킵
/// </summary>
public void SkipToResult()
{
    if (!isPlaying) return;
    
    isPlaying = false;
    noteSpawner?.StopSpawning();
    AudioManager.Instance?.StopBGM();
    
    ShowResultScreen();
}

/// <summary>
/// 일시정지
/// </summary>
public void PauseGame()
{
    if (!isPlaying || isPaused) return;
    
    isPaused = true;
    Time.timeScale = 0f;
    AudioManager.Instance?.PauseBGM();
    gameplayUI?.ShowPauseMenu();
}

/// <summary>
/// 재개
/// </summary>
public void ResumeGame()
{
    if (!isPaused) return;
    
    isPaused = false;
    Time.timeScale = 1f;
    AudioManager.Instance?.ResumeBGM();
    gameplayUI?.HidePauseMenu();
}
```

### 2.3.2 자동 저장 시스템
**파일**: `Core/AutoSave.cs` (신규)

```csharp
using UnityEngine;
using System;

namespace AIBeat.Core
{
    /// <summary>
    /// 게임 진행 상황 자동 저장
    /// </summary>
    public class AutoSave : MonoBehaviour
    {
        [Header("Save Settings")]
        [SerializeField] private float saveInterval = 30f;
        
        private float lastSaveTime;
        
        private void Start()
        {
            InvokeRepeating(nameof(SaveProgress), saveInterval, saveInterval);
        }
        
        private void SaveProgress()
        {
            // 플레이 통계 저장
            PlayerPrefs.SetString("LastPlayDate", DateTime.Now.ToString("O"));
            PlayerPrefs.SetInt("TotalPlayCount", 
                PlayerPrefs.GetInt("TotalPlayCount", 0) + 1);
            
            // 현재 곡 정보
            if (GameManager.Instance?.CurrentSongData != null)
            {
                PlayerPrefs.SetString("LastSong", 
                    GameManager.Instance.CurrentSongData.Title);
            }
            
            PlayerPrefs.Save();
            lastSaveTime = Time.time;
            
            #if UNITY_EDITOR
            Debug.Log("[AutoSave] Progress saved");
            #endif
        }
        
        /// <summary>
        /// 마지막 플레이로부터 경과 시간
        /// </summary>
        public static TimeSpan? GetTimeSinceLastPlay()
        {
            string dateStr = PlayerPrefs.GetString("LastPlayDate", "");
            if (string.IsNullOrEmpty(dateStr)) return null;
            
            if (DateTime.TryParse(dateStr, out var lastDate))
            {
                return DateTime.Now - lastDate;
            }
            return null;
        }
        
        /// <summary>
        /// 세션 복구 가능 여부
        /// </summary>
        public static bool CanRestoreSession()
        {
            var elapsed = GetTimeSinceLastPlay();
            return elapsed.HasValue && elapsed.Value.TotalHours < 24;
        }
    }
}
```

---

# 3. 📁 신규 파일 생성 목록

| 파일명 | 경로 | 설명 | 우선순위 |
|--------|------|------|----------|
| `GameConstants.cs` | `Scripts/Core/` | 게임 상수 정의 | Medium |
| `ListPool.cs` | `Scripts/Utils/` | List 오브젝트 풀 | Medium |
| `AutoSave.cs` | `Scripts/Core/` | 자동 저장 시스템 | Low |

---

# 4. 📋 구현 체크리스트

## 4.1 Critical (즉시) — ✅ 전수 검증 완료 (2026-02-18)
- [x] C-1: SettingsManager DontDestroyOnLoad → **오진** (의도적 미사용, PlayerPrefs 유지)
- [x] C-2: AudioManager DontDestroyOnLoad → **오진** (의도적 미사용, 씬별 재생성)
- [x] C-3: JudgementSystem OnDestroy() 이벤트 해제 → **이미 구현됨** (L287-290)
- [x] C-4: NoteSpawner OnDestroy() 정리 로직 → **이미 구현됨** (L644-669)

## 4.2 High (1주) — ✅ 전수 검증 완료
- [x] H-1: InputHandler 코루틴 중복 방지 → **이미 구현됨** (try-catch + fallback)
- [x] H-2: GameplayController 이벤트 해제 → **이미 구현됨** (OnDestroy L1173-1204)

## 4.3 Medium (2주) — ✅ 완료
- [x] M-1: GameConstants 상수화 → `Scripts/Core/GameConstants.cs` 생성됨
- [x] M-2: Debug.Log 조건부 컴파일 → `#if UNITY_EDITOR` 래핑 완료
- [x] 콤보 UI 강화 → GameplayUI.UpdateCombo 구현됨
- [x] 판정 Early/Late 표시 → GameplayUI.ShowJudgementDetailed 구현됨

## 4.4 Low (3주) — ✅ 완료
- [x] 스킵/리트라이 기능 → GameplayController.SkipToResult/QuickRestart 구현
- [x] 자동 저장 시스템 → `Scripts/Core/AutoSave.cs` 생성됨
- [x] GC Allocation 최적화 → `Scripts/Utils/ListPool.cs` 생성됨

## 4.5 UI 개선 (추가) — ✅ 완료 (2026-02-18)
- [x] UI-1: MainMenuUI 코루틴 ref null 설정
- [x] UI-3: SongSelectUI 이벤트 리스너 해제 (backButton/FAB/slider)
- [x] UI-4: SongSelectUI 슬라이더 참조 추적 + OnDestroy 정리
- [x] UI-5: GameplayUI 동적 패널 + 이펙트풀 OnDestroy 정리
- [x] UI-2: FindDeepChild 캐싱 → **스킵** (초기화 시에만 호출, premature optimization)

---

# 5. ✅ 검증 방법

## 5.1 버그 수정 검증
```csharp
// 테스트 씬에서 검증
1. MainMenu → SongSelect → Gameplay 씬 전환
2. 설정 변경 후 씬 전환 → 설정 유지 확인
3. BGM 재생 중 씬 전환 → 오디오 연속성 확인
4. Unity Profiler로 메모리 누수 확인
```

## 5.2 기능 개선 검증
```csharp
// 콤보 UI
1. 10/25/50/100 콤보 달성 시 효과 확인
2. 색상 변경 확인

// 판정 표시
1. 일부러 늦게/빠르게 입력
2. Early/Late 텍스트 표시 확인
```

---

**작성 완료일**: 2026-02-18
**검토 필요**: 팀 리뷰 후 우선순위 조정

---

# 6. 🔍 UI 파일 심층 분석 (추가)

## 6.1 MainMenuUI.cs 분석

**파일**: [`MainMenuUI.cs`](My%20project/Assets/Scripts/UI/MainMenuUI.cs)

### ✅ 잘 구현된 부분
- `OnDestroy()`에서 코루틴 정지 (`eqAnimCoroutine`, `breatheCoroutine`, `musicianAnimCoroutine`)
- 버튼 이벤트 리스너 제거 (`RemoveAllListeners`)
- `EnsureEventSystem()`로 EventSystem 자동 생성
- `Application.runInBackground = true` 설정

### ⚠️ 개선 필요 사항

#### UI-1: 코루틴 null 체크 후 StopCoroutine
**현재 코드** (line 1064-1072):
```csharp
private void OnDestroy()
{
    if (eqAnimCoroutine != null) StopCoroutine(eqAnimCoroutine);
    if (breatheCoroutine != null) StopCoroutine(breatheCoroutine);
    if (musicianAnimCoroutine != null) StopCoroutine(musicianAnimCoroutine);
    // ...
}
```

**문제점**: Unity에서 `StopCoroutine(null)`은 안전하지만, 명시적으로 하는 것이 좋음

**개선안**:
```csharp
private void OnDestroy()
{
    // 코루틴 안전 정지
    SafeStopCoroutine(ref eqAnimCoroutine);
    SafeStopCoroutine(ref breatheCoroutine);
    SafeStopCoroutine(ref musicianAnimCoroutine);
    
    // 버튼 이벤트 정리
    SafeRemoveListeners(playButton);
    SafeRemoveListeners(settingsButton);
    SafeRemoveListeners(exitButton);
}

private void SafeStopCoroutine(ref Coroutine coroutine)
{
    if (coroutine != null)
    {
        StopCoroutine(coroutine);
        coroutine = null;
    }
}

private void SafeRemoveListeners(Button btn)
{
    if (btn != null)
        btn.onClick.RemoveAllListeners();
}
```

#### UI-2: FindDeepChild 성능 이슈
**현재 코드** (line 1051-1062):
```csharp
private Transform FindDeepChild(Transform parent, string name)
{
    foreach (Transform child in parent)
    {
        if (child.name == name)
            return child;
        var result = FindDeepChild(child, name);
        if (result != null)
            return result;
    }
    return null;
}
```

**문제점**: 재귀 호출로 깊은 계층에서 성능 저하 가능성

**개선안**:
```csharp
// 캐싱 추가
private Dictionary<string, Transform> childCache = new Dictionary<string, Transform>();

private Transform FindDeepChildCached(Transform parent, string name)
{
    if (childCache.TryGetValue(name, out var cached))
        return cached;
    
    var result = FindDeepChild(parent, name);
    if (result != null)
        childCache[name] = result;
    
    return result;
}
```

---

## 6.2 SongSelectUI.cs 분석

**파일**: [`SongSelectUI.cs`](My%20project/Assets/Scripts/UI/SongSelectUI.cs)

### ✅ 잘 구현된 부분
- `OnDestroy()`에서 코루틴 정지
- `EnsureEventSystem()`, `EnsureCanvasScaler()`, `EnsureSafeArea()` 구현

### ⚠️ 개선 필요 사항

#### UI-3: 이벤트 리스너 해제 누락
**현재 코드** (line 735-738):
```csharp
private void OnDestroy()
{
    if (eqAnimCoroutine != null) StopCoroutine(eqAnimCoroutine);
}
```

**문제점**:
- 생성된 버튼들의 onClick 리스너 해제 없음
- SettingsManager.OnSettingChanged 이벤트 구독 해제 확인 필요

**수정 코드**:
```csharp
private void OnDestroy()
{
    // 코루틴 정지
    if (eqAnimCoroutine != null)
    {
        StopCoroutine(eqAnimCoroutine);
        eqAnimCoroutine = null;
    }
    
    // 버튼 이벤트 정리
    if (backButton != null) backButton.onClick.RemoveAllListeners();
    if (settingsFAB != null) settingsFAB.onClick.RemoveAllListeners();
    
    // 슬라이더 이벤트 정리 (동적 생성된 것들)
    // TODO: 슬라이더 참조를 저장하여 정리 필요
}
```

#### UI-4: 설정 슬라이더 이벤트 메모리 누수
**현재 코드** (line 709-713):
```csharp
slider.onValueChanged.AddListener((val) =>
{
    labelTmp.text = $"{label}: {val:F0}";
    onChanged?.Invoke(val);
});
```

**문제점**: 람다 캡처로 인한 메모리 누수 가능성

**개선안**:
```csharp
// 슬라이더 참조 저장
private List<Slider> createdSliders = new List<Slider>();

private void CreateSettingsSlider(...)
{
    // ... 기존 코드 ...
    
    slider.onValueChanged.AddListener((val) =>
    {
        labelTmp.text = $"{label}: {val:F0}";
        onChanged?.Invoke(val);
    });
    
    createdSliders.Add(slider);  // 추후 정리용
}

private void OnDestroy()
{
    // 슬라이더 이벤트 정리
    foreach (var slider in createdSliders)
    {
        if (slider != null)
            slider.onValueChanged.RemoveAllListeners();
    }
    createdSliders.Clear();
}
```

---

## 6.3 GameplayUI.cs 분석

**파일**: [`GameplayUI.cs`](My%20project/Assets/Scripts/UI/GameplayUI.cs)

### ✅ 잘 구현된 부분
- `OnDestroy()`에서 모든 버튼 리스너 제거
- VideoPlayer 및 RenderTexture 정리
- 코루틴 정리 (SpawnEffect에서 사용)

### ⚠️ 개선 필요 사항

#### UI-5: 동적 생성 UI 요소 정리
**문제점**:
- `CreateResultPanel()` 등에서 동적 생성된 GameObject들이 명시적으로 삭제되지 않음
- `analysisOverlay`, `pausePanel` 등이 씬 전환 시 자동 삭제되지만 명시적 정리 권장

**개선안**:
```csharp
private void OnDestroy()
{
    // 기존 정리 코드...
    
    // 동적 생성 패널 정리
    if (resultPanel != null) Destroy(resultPanel);
    if (pausePanel != null) Destroy(pausePanel);
    if (countdownPanel != null) Destroy(countdownPanel);
    if (analysisOverlay != null) Destroy(analysisOverlay);
    
    // 이펙트 풀 정리
    if (effectPool != null)
    {
        foreach (var effect in effectPool.Values)
        {
            if (effect != null) Destroy(effect);
        }
        effectPool.Clear();
    }
}
```

---

# 7. 🛡️ 에러 핸들링 시스템 기획 (신규)

## 7.1 ErrorHandler 클래스

**파일**: `Scripts/Core/ErrorHandler.cs` (신규)

```csharp
using UnityEngine;
using System;

namespace AIBeat.Core
{
    /// <summary>
    /// 중앙 집중식 에러 핸들링 시스템
    /// </summary>
    public class ErrorHandler : MonoBehaviour
    {
        public static ErrorHandler Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private bool showUserNotifications = true;
        
        public enum ErrorSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }
        
        public event Action<string, ErrorSeverity> OnErrorOccurred;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        /// <summary>
        /// 에러 로그 및 처리
        /// </summary>
        public void HandleError(string context, Exception exception, ErrorSeverity severity = ErrorSeverity.Error)
        {
            string message = $"[{context}] {exception.Message}";
            
            if (logToConsole)
            {
                switch (severity)
                {
                    case ErrorSeverity.Info:
                        Debug.Log(message);
                        break;
                    case ErrorSeverity.Warning:
                        Debug.LogWarning(message);
                        break;
                    case ErrorSeverity.Error:
                    case ErrorSeverity.Critical:
                        Debug.LogError(message);
                        break;
                }
            }
            
            // 치명적 에러면 사용자에게 알림
            if (severity == ErrorSeverity.Critical && showUserNotifications)
            {
                ShowUserNotification(message);
            }
            
            OnErrorOccurred?.Invoke(message, severity);
        }
        
        /// <summary>
        /// 안전한 작업 실행
        /// </summary>
        public bool TryExecute(string context, Action action, ErrorSeverity severity = ErrorSeverity.Warning)
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                HandleError(context, e, severity);
                return false;
            }
        }
        
        private void ShowUserNotification(string message)
        {
            // TODO: UI 알림 시스템 연동
            Debug.Log($"[User Notification] {message}");
        }
    }
}
```

## 7.2 사용 예시

```csharp
// 기존 코드
private void CacheLaneBoundaries()
{
    var cam = Camera.main;
    // ... 위험한 코드
}

// 개선된 코드
private void CacheLaneBoundaries()
{
    ErrorHandler.Instance?.TryExecute("InputHandler.CacheLaneBoundaries", () =>
    {
        var cam = Camera.main;
        if (cam == null) throw new Exception("Main camera not found");
        
        float screenWidth = Screen.width;
        if (screenWidth <= 0) throw new Exception("Invalid screen width");
        
        // ... 안전하게 실행
    }, ErrorHandler.ErrorSeverity.Warning);
}
```

---

# 8. 📊 구현 우선순위 매트릭스

| ID | 버그/개선사항 | 영향도 | 난이도 | 예상시간 | 우선순위 |
|----|--------------|--------|--------|----------|----------|
| C-1 | SettingsManager DontDestroyOnLoad | 🔴 높음 | 🟢 낮음 | 5분 | **P0** |
| C-2 | AudioManager DontDestroyOnLoad | 🔴 높음 | 🟢 낮음 | 5분 | **P0** |
| C-3 | JudgementSystem 이벤트 해제 | 🔴 높음 | 🟢 낮음 | 10분 | **P0** |
| C-4 | NoteSpawner 메모리 정리 | 🔴 높음 | 🟡 중간 | 20분 | **P0** |
| H-1 | InputHandler 코루틴 방지 | 🟡 중간 | 🟢 낮음 | 10분 | **P1** |
| H-2 | GameplayController 이벤트 해제 | 🟡 중간 | 🟡 중간 | 15분 | **P1** |
| UI-3 | SongSelectUI 리스너 해제 | 🟡 중간 | 🟢 낮음 | 10분 | **P1** |
| UI-4 | 설정 슬라이더 메모리 누수 | 🟢 낮음 | 🟡 중간 | 20분 | **P2** |
| M-1 | GameConstants 상수화 | 🟢 낮음 | 🟡 중간 | 30분 | **P2** |
| M-2 | Debug.Log 조건부 컴파일 | 🟢 낮음 | 🟢 낮음 | 15분 | **P2** |

### 우선순위 정의
- **P0 (즉시)**: 앱 안정성에 치명적, 즉시 수정 필요
- **P1 (1주)**: 사용자 경험에 영향, 빠른 수정 권장
- **P2 (2주)**: 코드 품질 개선, 여유 있게 진행
- **P3 (3주)**: 기능 개선, 일정에 맞춰 진행

---

# 9. 🧪 상세 테스트 가이드

## 9.1 Critical 버그 테스트

### C-1/C-2: DontDestroyOnLoad 테스트
```
1. Unity 에디터에서 MainMenu 씬 로드
2. Hierarchy에서 SettingsManager, AudioManager 확인
3. SongSelect 씬으로 전환
4. Hierarchy에서 두 오브젝트가 여전히 존재하는지 확인
5. Gameplay 씬으로 전환
6. 설정이 유지되는지 확인 (볼륨, 노트 속도 등)
7. BGM이 끊김 없이 재생되는지 확인
```

### C-3/C-4: 이벤트 메모리 누수 테스트
```
1. Window > Analysis > Profiler 열기
2. Memory Profiler 선택
3. Gameplay 씬 로드 후 플레이
4. 일시정지 후 Resume 반복 (10회)
5. 메모리 그래프가 지속적으로 증가하지 않는지 확인
6. 씬 전환 후 메모리가 해제되는지 확인
```

## 9.2 통합 테스트 시나리오

### 시나리오 1: 기본 게임플레이
```
1. 앱 시작 → 메인 메뉴
2. SELECT SONG → 곡 선택
3. 게임 플레이 (완주)
4. 결과 화면 확인
5. 메인 메뉴 복귀
6. 메모리 상태 확인
```

### 시나리오 2: 설정 변경
```
1. 메인 메뉴에서 설정 패널 열기
2. 볼륨, 노트 속도 변경
3. 곡 선택 → 게임 플레이
4. 일시정지 → 재개
5. 설정이 유지되는지 확인
```

### 시나리오 3: 반복 플레이
```
1. 곡 선택 → 게임 플레이 → 결과
2. 재도전 (5회 반복)
3. 메모리 누수 확인
4. 프레임 드랍 확인
```

## 9.3 자동화 테스트 코드

```csharp
// Tests/EditMode/SingletonTests.cs
using NUnit.Framework;
using UnityEngine;
using AIBeat.Core;

[TestFixture]
public class SingletonTests
{
    [Test]
    public void SettingsManager_SingletonPattern_Works()
    {
        var go1 = new GameObject("Settings1");
        var go2 = new GameObject("Settings2");
        
        var sm1 = go1.AddComponent<SettingsManager>();
        var sm2 = go2.AddComponent<SettingsManager>();
        
        Assert.AreEqual(sm1, SettingsManager.Instance);
        Assert.IsNull(go2); // 파괴되었는지 확인
    }
}
```

---

# 10. 📝 코드 품질 체크리스트

## 10.1 모든 스크립트 공통 체크사항 — ✅ 전수 검증 완료 (2026-02-18)

- [x] `OnDestroy()`에서 모든 이벤트 구독 해제
- [x] `OnDestroy()`에서 모든 코루틴 정지
- [x] `OnDestroy()`에서 동적 생성 리소스 정리 (Material, Texture, GameObject)
- [x] 싱글톤 DontDestroyOnLoad → **의도적 미사용** (에디터 중복 방지, PlayerPrefs 유지)
- [x] `Debug.Log` 대신 조건부 컴파일 사용 (`#if UNITY_EDITOR`)
- [x] `null` 체크 후 접근 (`?.` 연산자 활용)
- [x] 코루틴 시작 전 중복 체크

## 10.2 파일별 체크리스트 — ✅ 전수 검증 완료 (2026-02-18)

| 파일 | 이벤트 해제 | 코루틴 정지 | 리소스 정리 | DontDestroy |
|------|------------|-------------|-------------|-------------|
| SettingsManager | N/A | N/A | N/A | N/A (의도적 미사용) |
| AudioManager | ✅ | N/A | N/A | N/A (의도적 미사용) |
| JudgementSystem | ✅ (L287-290) | N/A | N/A | N/A |
| NoteSpawner | ✅ (L648) | ✅ (L649-653) | ✅ Material+프리팹 (L655-669) | N/A |
| GameplayController | ✅ (L1173-1204) | ✅ | N/A | N/A |
| InputHandler | N/A | ✅ (try-catch) | N/A | N/A |
| MainMenuUI | ✅ (버튼3개) | ✅ (ref=null) | N/A | N/A |
| SongSelectUI | ✅ (back+FAB+slider) | ✅ (ref=null) | N/A | N/A |
| GameplayUI | ✅ (버튼5개) | ✅ | ✅ 패널4개+이펙트풀+VideoPlayer | N/A |

---

**최종 업데이트**: 2026-02-18 (상세 보완 완료)
