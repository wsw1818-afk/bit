# AI Beat - 프로젝트 개선 및 버그 수정 기획안

## 📊 현재 프로젝트 상태 분석

### 아키텍처 개요
- **엔진**: Unity 6 (URP)
- **플랫폼**: Android 모바일 (세로 모드)
- **장르**: 리듬 게임 (4레인 키보드 + 양쪽 스크래치)

### 주요 시스템
| 시스템 | 파일 | 상태 |
|--------|------|------|
| 게임 매니저 | [`GameManager.cs`](My%20project/Assets/Scripts/Core/GameManager.cs:1) | 양호 |
| 설정 관리 | [`SettingsManager.cs`](My%20project/Assets/Scripts/Core/SettingsManager.cs:1) | 양호 |
| 오디오 관리 | [`AudioManager.cs`](My%20project/Assets/Scripts/Core/AudioManager.cs:1) | 개선 필요 |
| 노트 생성 | [`NoteSpawner.cs`](My%20project/Assets/Scripts/Gameplay/NoteSpawner.cs:1) | 개선 필요 |
| 입력 처리 | [`InputHandler.cs`](My%20project/Assets/Scripts/Gameplay/InputHandler.cs:1) | 양호 |
| 판정 시스템 | [`JudgementSystem.cs`](My%20project/Assets/Scripts/Gameplay/JudgementSystem.cs:1) | 양호 |
| 게임플레이 컨트롤러 | [`GameplayController.cs`](My%20project/Assets/Scripts/Gameplay/GameplayController.cs:1) | 개선 필요 |

---

## 🐛 발견된 버그 및 문제점

### 🔴 Critical (즉시 수정 필요)

| # | 문제 | 위치 | 영향 |
|---|------|------|------|
| 1 | **NoteSpawner 프리팹 null 참조** | [`NoteSpawner.cs:146-148`](My%20project/Assets/Scripts/Gameplay/NoteSpawner.cs:146) | 노트가 생성되지 않음 |
| 2 | **SettingsManager 싱글톤 DontDestroyOnLoad 누락** | [`SettingsManager.cs:98-103`](My%20project/Assets/Scripts/Core/SettingsManager.cs:98) | 씬 전환 시 설정 초기화 |
| 3 | **AudioManager 싱글톤 DontDestroyOnLoad 누락** | [`AudioManager.cs:74-79`](My%20project/Assets/Scripts/Core/AudioManager.cs:74) | 씬 전환 시 오디오 끊김 |
| 4 | **AudioManager 프로시저럴 사운드 null 체크 누락** | [`AudioManager.cs:124-133`](My%20project/Assets/Scripts/Core/AudioManager.cs:124) | NRE 위험 |

### 🟡 High (1주 내 수정 권장)

| # | 문제 | 위치 | 제안 |
|---|------|------|------|
| 5 | **GameplayController debugMode 조걶 컴파일 문제** | [`GameplayController.cs:31-35`](My%20project/Assets/Scripts/Gameplay/GameplayController.cs:31) | 런타임 디버그 토글 추가 |
| 6 | **JudgementSystem 이벤트 구독 해제 누락** | [`JudgementSystem.cs:79-80`](My%20project/Assets/Scripts/Gameplay/JudgementSystem.cs:79) | OnDestroy에서 해제 |
| 7 | **NoteSpawner 동적 프리팹 메모리 누수** | [`NoteSpawner.cs:41-42`](My%20project/Assets/Scripts/Gameplay/NoteSpawner.cs:41) | 정리 로직 추가 |
| 8 | **InputHandler 레인 경계 캐싱 실패 시 폭포** | [`InputHandler.cs:62-65`](My%20project/Assets/Scripts/Gameplay/InputHandler.cs:62) | 예외 처리 개선 |
| 9 | **AudioAnalyzer sampleRate 예외 처리** | [`AudioAnalyzer.cs:79-83`](My%20project/Assets/Scripts/Audio/AudioAnalyzer.cs:79) | 하드코딩 제거 |

### 🟢 Medium (개선 권장)

| # | 문제 | 위치 | 제안 |
|---|------|------|------|
| 10 | **주석과 코드 불일치** | [`GameplayController.cs:46-48`](My%20project/Assets/Scripts/Gameplay/GameplayController.cs:46) | 주석 업데이트 |
| 11 | **Magic Number 남용** | 여러 파일 | 상수화 |
| 12 | **Debug.Log 빌드 성능 영향** | 여러 파일 | 컴파일 조걶 강화 |
| 13 | **Coroutine 중복 시작 가능성** | [`GameplayController.cs:55-72`](My%20project/Assets/Scripts/Gameplay/GameplayController.cs:55) | null 체크 강화 |

---

## 🔧 버그 수정 가이드

### 1. SettingsManager 싱글톤 수정
```csharp
// SettingsManager.cs - Awake() 수정
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject); // 추가
    LoadSettings();
}
```

### 2. AudioManager 싱글톤 수정
```csharp
// AudioManager.cs - Awake() 수정
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject); // 추가
    Initialize();
}
```

### 3. NoteSpawner 프리팹 로드 수정
```csharp
// NoteSpawner.cs - AutoSetupReferences() 수정
private void AutoSetupReferences()
{
    // ... 기존 코드 ...
    
    // Resources에서 프리팹 로드 시도
    if (tapNotePrefab == null)
        tapNotePrefab = Resources.Load<GameObject>("Prefabs/TapNote");
    if (longNotePrefab == null)
        longNotePrefab = Resources.Load<GameObject>("Prefabs/LongNote");
    if (scratchNotePrefab == null)
        scratchNotePrefab = Resources.Load<GameObject>("Prefabs/ScratchNote");
    
    // 여전히 null이면 동적 생성
    if (tapNotePrefab == null)
        tapNotePrefab = CreateNotePrefab("TapNote", new Color(1f, 0.84f, 0f));
    // ...
}
```

### 4. JudgementSystem 이벤트 해제
```csharp
// JudgementSystem.cs - 추가
private void OnDestroy()
{
    SettingsManager.OnSettingChanged -= OnSettingChanged;
}
```

---

## 🚀 기능 개선 기획

### Phase 1: 안정성 향상 (즉시)

#### 1.1 에러 핸들링 시스템
```csharp
// Core/ErrorHandler.cs
public static class ErrorHandler
{
    public static void SafeCall(Action action, string context = "")
    {
        try
        {
            action?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{context}] Error: {e.Message}\n{e.StackTrace}");
            // 사용자 피드백 (선택사항)
            ShowUserError(context);
        }
    }
    
    public static T SafeCall<T>(Func<T> func, T defaultValue, string context = "")
    {
        try
        {
            return func.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{context}] Error: {e.Message}");
            return defaultValue;
        }
    }
}
```

#### 1.2 널 체크 유틸리티
```csharp
// Utils/NullCheckUtility.cs
public static class NullCheckUtility
{
    public static bool IsValid<T>(this T obj) where T : class
    {
        return obj != null && !obj.Equals(null);
    }
    
    public static void EnsureComponent<T>(this GameObject go, ref T component) where T : Component
    {
        if (!component.IsValid())
            component = go.GetComponent<T>() ?? go.AddComponent<T>();
    }
}
```

### Phase 2: 성능 최적화 (1주)

#### 2.1 오브젝트 풀링 개선
```csharp
// Gameplay/NotePool.cs
public class NotePool : MonoBehaviour
{
    [Header("Pool Configuration")]
    [SerializeField] private int initialSize = 50;
    [SerializeField] private int maxSize = 200;
    
    private Dictionary<NoteType, Queue<Note>> pools = new();
    private Dictionary<NoteType, int> activeCounts = new();
    
    // 동적 풀 크기 조정
    public void ExpandPoolIfNeeded(NoteType type)
    {
        if (activeCounts[type] > pools[type].Count * 0.8f)
        {
            int expandAmount = Mathf.Min(20, maxSize - pools[type].Count);
            PreloadNotes(type, expandAmount);
        }
    }
}
```

#### 2.2 오디오 버퍼링
```csharp
// Audio/AudioBuffer.cs
public class AudioBuffer : MonoBehaviour
{
    [Header("Buffer Settings")]
    [SerializeField] private int bufferSize = 2048;
    [SerializeField] private int bufferCount = 3;
    
    private float[][] buffers;
    private int currentBuffer = 0;
    
    // 더블/트리플 버퍼링으로 오디오 끊김 방지
    public void ProcessAudio(float[] data)
    {
        Array.Copy(data, buffers[currentBuffer], data.Length);
        currentBuffer = (currentBuffer + 1) % bufferCount;
    }
}
```

### Phase 3: 게임플레이 개선 (2주)

#### 3.1 스킵/리트라이 기능
```csharp
// Gameplay/GameplayController.cs - 추가
public void SkipToResult()
{
    if (!isPlaying) return;
    
    // 현재까지의 점수로 결과 화면으로 스킵
    ShowResultScreen();
}

public void QuickRestart()
{
    // 현재 곡 즉시 재시작
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
```

#### 3.2 자동 저장 시스템
```csharp
// Core/AutoSave.cs
public class AutoSave : MonoBehaviour
{
    [Header("Auto Save Settings")]
    [SerializeField] private float saveInterval = 30f;
    
    private void Start()
    {
        InvokeRepeating(nameof(SaveProgress), saveInterval, saveInterval);
    }
    
    private void SaveProgress()
    {
        PlayerPrefs.SetString("LastPlayDate", DateTime.Now.ToString());
        PlayerPrefs.SetInt("TotalPlayCount", PlayerPrefs.GetInt("TotalPlayCount", 0) + 1);
        PlayerPrefs.Save();
    }
}
```

### Phase 4: UX 개선 (2주)

#### 4.1 튜토리얼 개선
```csharp
// Core/TutorialManager.cs - 개선
public class TutorialManager : MonoBehaviour
{
    [Header("Adaptive Tutorial")]
    [SerializeField] private bool skipCompletedSteps = true;
    
    public void StartAdaptiveTutorial()
    {
        // 사용자 실패 패턴 분석
        var failPatterns = AnalyzeFailPatterns();
        
        // 필요한 부분만 튜토리얼 표시
        foreach (var step in tutorialSteps)
        {
            if (skipCompletedSteps && IsStepMastered(step))
                continue;
                
            ShowTutorialStep(step);
        }
    }
}
```

#### 4.2 결과 화면 개선
```csharp
// UI/ResultUI.cs - 새로 작성
public class ResultUI : MonoBehaviour
{
    [Header("Statistics")]
    [SerializeField] private TextMeshProUGUI accuracyText;
    [SerializeField] private TextMeshProUGUI maxComboText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Image rankBadge;
    
    [Header("Graph")]
    [SerializeField] private RectTransform timingGraph;
    
    public void ShowDetailedResult(GameResult result)
    {
        // 타이밍 그래프 표시
        DrawTimingGraph(result.timingData);
        
        // 등급 계산
        var rank = CalculateRank(result);
        rankText.text = rank.ToString();
        rankBadge.color = GetRankColor(rank);
        
        // 개선점 제시
        ShowImprovementTips(result);
    }
}
```

---

## 📋 구현 체크리스트

### 버그 수정
- [ ] SettingsManager DontDestroyOnLoad 추가
- [ ] AudioManager DontDestroyOnLoad 추가
- [ ] NoteSpawner 프리팹 로드 로직 수정
- [ ] JudgementSystem 이벤트 해제 추가
- [ ] GameplayController 코루틴 중복 방지
- [ ] InputHandler 예외 처리 개선
- [ ] AudioAnalyzer 샘플레이트 처리 개선

### 기능 개선
- [ ] ErrorHandler 시스템 구현
- [ ] NotePool 동적 확장 구현
- [ ] 오디오 버퍼링 구현
- [ ] 스킵/리트라이 기능 추가
- [ ] 자동 저장 시스템 추가
- [ ] 어댑티브 튜토리얼 구현
- [ ] 상세 결과 화면 구현

### 성능 최적화
- [ ] Object Pool 프로파일링
- [ ] GC Allocation 최적화
- [ ] 쉐이더 최적화
- [ ] 텍스처 압축 설정

---

## 🎨 코드 품질 개선

### 네이밍 컨벤션
```csharp
// ❌ 기존
private float noteSpeed = 5f;
private const float HOLD_BONUS_TICK_INTERVAL = 0.1f;

// ✅ 개선
private float _noteSpeed = 5f;
private const float HoldBonusTickInterval = 0.1f;
```

### 주석 표준화
```csharp
/// <summary>
/// 롱노트 홀드 중 별도로 계산되는 복합 점수를 추가합니다.
/// </summary>
/// <param name="amount">추가할 복합 점수량</param>
/// <remarks>
/// 0.1초마다 호출되며, 점수는 별도 누적되어 최종 점수에 합산됩니다.
/// </remarks>
public void AddBonusScore(int amount)
```

### 상수화
```csharp
public static class GameConstants
{
    public const int LaneCount = 4;
    public const float DefaultNoteSpeed = 5f;
    public const float PerfectWindowMs = 50f;
    public const float GreatWindowMs = 100f;
    public const float GoodWindowMs = 200f;
    public const float BadWindowMs = 350f;
}
```

---

## 📁 폴더 구조 개선 제안

```
Assets/
├── Scripts/
│   ├── Core/           # 싱글톤 매니저들
│   ├── Gameplay/       # 게임플레이 로직
│   ├── Audio/          # 오디오 관련
│   ├── UI/             # UI 컴포넌트
│   ├── Data/           # 데이터 구조
│   ├── Utils/          # 유틸리티
│   └── Editor/         # 에디터 툴
├── Resources/
│   ├── Prefabs/
│   ├── Sounds/
│   └── Fonts/
└── StreamingAssets/
    └── Songs/
```

---

## 📊 테스트 전략

### 단위 테스트
```csharp
// Tests/JudgementSystemTests.cs
[Test]
public void Judge_PerfectTiming_ReturnsPerfect()
{
    var system = new JudgementSystem();
    system.Initialize(1);
    
    var result = system.Judge(1.0f, 1.0f); // 정확한 타이밍
    
    Assert.AreEqual(JudgementResult.Perfect, result);
}
```

### 통합 테스트
- 씬 전환 시 설정 유지
- 오디오 재생/일시정지/재개
- 노트 생성에서 판정까지 전체 흐름

### 성능 테스트
- 1000개 이상 노트 생성 시 프레임 유지
- 메모리 사용량 모니터링
- 배터리 소모량 측정

---

## 📝 문서화

### API 문서
- 모든 public 메서드 XML 주석
- 이벤트 발행/구독 문서화
- 설정 값 범위 문서화

### 사용자 문서
- 튜토리얼 가이드
- 설정 설명서
- 문제 해결 가이드

---

## 🔄 CI/CD 개선

### 빌드 자동화
```yaml
# .github/workflows/build.yml
name: Build
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Build Android
        uses: game-ci/unity-builder@v3
        with:
          targetPlatform: Android
```

### 코드 품질 검사
- StyleCop 규칙 적용
- 코드 커버리지 리포트
- 정적 분석 (SonarQube)

---

## 📈 모니터링 및 분석

### 인게임 분석
```csharp
// Analytics/GameAnalytics.cs
public static class GameAnalytics
{
    public static void LogNoteHit(JudgementResult result, float timing)
    {
        // Firebase 또는 자체 서버로 전송
        Analytics.CustomEvent("note_hit", new Dictionary<string, object>
        {
            { "judgement", result.ToString() },
            { "timing_ms", timing * 1000 },
            { "song_id", CurrentSong.Id }
        });
    }
}
```

### 크래시 리포팅
- Firebase Crashlytics 연동
- 사용자 로그 수집
- 자동 버그 리포트 생성

---

## ✅ 마일스톤

| 주차 | 목표 | 완료 조건 |
|------|------|-----------|
| 1 | 버그 수정 | 모든 Critical 버그 해결, 테스트 통과 |
| 2 | 안정성 향상 | ErrorHandler 적용, 예외 처리 개선 |
| 3 | 성능 최적화 | 60fps 유지, 메모리 누수 제거 |
| 4 | 기능 개선 | 스킵/리트라이, 자동 저장 구현 |
| 5 | UX 개선 | 튜토리얼, 결과 화면 개선 |
| 6 | 테스트 및 문서화 | 전체 테스트 통과, 문서 완성 |

---

**작성일**: 2026-02-16  
**버전**: 1.0  
**담당자**: AI Beat 개발팀
