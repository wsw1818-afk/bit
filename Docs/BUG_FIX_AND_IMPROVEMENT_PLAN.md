# AI Beat 버그 수정 및 개선 기획안

## 📅 분석 일자: 2026-02-18
## 🎯 분석 목적: 앱의 버그 및 개선 필요 사항 식별

---

> ### ✅ 코드 검증 결과 (2026-02-18)
>
> **아래 Critical/High 버그는 전수 검증 완료되었습니다:**
> - **C-1, C-2**: 오진 — DontDestroyOnLoad는 **의도적으로 미사용** (에디터 인스턴스 중복 방지 + PlayerPrefs로 설정 유지)
> - **C-3**: 이미 수정됨 — `JudgementSystem.OnDestroy()` L287-290에서 이벤트 해제 구현
> - **C-4, H-3**: 이미 수정됨 — `NoteSpawner.OnDestroy()` L644-669에서 이벤트+Material+프리팹 정리 구현
> - **H-1**: 이미 수정됨 — `InputHandler.cs` L58-66에 try-catch + 균등분할 폴백 구현
> - **H-2**: 이미 수정됨 — null 체크로 방지 (문서 자체에서도 확인 완료)
> - **M-2**: Debug.Log → `#if UNITY_EDITOR` 래핑 완료 (GameplayController, NoteSpawner, InputHandler)
>
> **아래 수정 가이드(§2)의 코드는 참조용이며, 실제 코드에 이미 반영되어 있거나 적용 불필요합니다.**

---

## 1. 🐛 버그 분석

### 🔴 Critical — ✅ 전수 검증 완료 (모두 오진 또는 수정됨)

| # | 문제 | 파일 | 라인 | 상태 | 검증 결과 |
|---|------|------|------|------|-----------|
| C-1 | ~~SettingsManager DontDestroyOnLoad 누락~~ | `SettingsManager.cs` | 96-107 | ✅ 오진 | 의도적 미사용 (PlayerPrefs 유지, 에디터 중복 방지) |
| C-2 | ~~AudioManager DontDestroyOnLoad 누락~~ | `AudioManager.cs` | 72-83 | ✅ 오진 | 의도적 미사용 (씬별 재생성, 코드 주석 확인) |
| C-3 | ~~JudgementSystem 이벤트 구독 해제 누락~~ | `JudgementSystem.cs` | 287-290 | ✅ 수정완료 | OnDestroy()에서 이벤트 해제 구현됨 |
| C-4 | ~~NoteSpawner 이벤트 구독 해제 누락~~ | `NoteSpawner.cs` | 644-669 | ✅ 수정완료 | OnDestroy()에서 이벤트+Material+프리팹 정리 |

### 🟡 High — ✅ 전수 검증 완료 (모두 수정됨)

| # | 문제 | 파일 | 라인 | 상태 | 검증 결과 |
|---|------|------|------|------|-----------|
| H-1 | ~~InputHandler 예외 처리 미흡~~ | `InputHandler.cs` | 58-66 | ✅ 수정완료 | try-catch + 균등분할 폴백 구현 |
| H-2 | ~~Coroutine 중복 시작 가능성~~ | `GameplayController.cs` | 59-62, 79-82 | ✅ 수정완료 | null 체크로 방지 |
| H-3 | ~~동적 프리팹 메모리 누수~~ | `NoteSpawner.cs` | 644-669 | ✅ 수정완료 | C-4와 통합 (OnDestroy에서 정리) |

### 🟢 Medium (개선 권장)

| # | 문제 | 파일 | 증상 | 해결 방안 |
|---|------|------|------|-----------|
| M-1 | **Magic Number 상수화** | 여러 파일 | 유지보수 어려움 | `GameConstants` 클래스 생성 |
| M-2 | ~~**Debug.Log 빌드 성능**~~ | 여러 파일 | ✅ 수정완료 | `#if UNITY_EDITOR` 래핑 완료 (GameplayController, NoteSpawner, InputHandler) |

---

## 2. 🔧 버그 수정 가이드

### C-1: SettingsManager DontDestroyOnLoad

**파일**: `My project/Assets/Scripts/Core/SettingsManager.cs`

```csharp
// 수정 전 (라인 96-107)
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    LoadSettings();
}

// 수정 후
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

### C-2: AudioManager DontDestroyOnLoad

**파일**: `My project/Assets/Scripts/Core/AudioManager.cs`

```csharp
// 수정 전 (라인 72-83)
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    Initialize();
}

// 수정 후
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

### C-3: JudgementSystem 이벤트 해제

**파일**: `My project/Assets/Scripts/Gameplay/JudgementSystem.cs`

```csharp
// 클래스 끝에 추가
private void OnDestroy()
{
    SettingsManager.OnSettingChanged -= OnSettingChanged;
}
```

### C-4 & H-3: NoteSpawner 이벤트 해제 및 메모리 정리

**파일**: `My project/Assets/Scripts/Gameplay/NoteSpawner.cs`

```csharp
// 클래스 끝에 추가
private void OnDestroy()
{
    // 이벤트 구독 해제
    SettingsManager.OnSettingChanged -= OnSettingChanged;
    
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
    
    // 스폰 루프 정지
    isSpawning = false;
    if (spawnLoopCoroutine != null)
    {
        StopCoroutine(spawnLoopCoroutine);
        spawnLoopCoroutine = null;
    }
}
```

---

## 3. 🚀 기능 개선 기획

### 3.1 성능 최적화

#### 오브젝트 풀링 동적 확장

**현재 상태**: 고정 크기 풀 (100개)

**개선안**:
```csharp
// NoteSpawner.cs 개선
[Header("Dynamic Pool Settings")]
[SerializeField] private int initialPoolSize = 50;
[SerializeField] private int maxPoolSize = 200;
[SerializeField] private int expandAmount = 20;

private void ExpandPoolIfNeeded(NoteType type)
{
    var pool = notePools[type];
    int activeCount = activeNotes.Count(n => n.Type == type);
    
    if (activeCount > pool.Count * 0.8f && pool.Count < maxPoolSize)
    {
        int toAdd = Mathf.Min(expandAmount, maxPoolSize - pool.Count);
        for (int i = 0; i < toAdd; i++)
        {
            var note = CreatePooledNote(type);
            pool.Enqueue(note);
        }
        Debug.Log($"[NoteSpawner] Pool expanded: {type} +{toAdd}");
    }
}
```

#### GC Allocation 최소화

**문제점**:
- `Dictionary` 순회 중 수정 (롱노트 홀드)
- 매 프레임 `List<T>` 할당

**개선안**:
```csharp
// ListPool 유틸리티 클래스
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
        pool.Push(list);
    }
}

// 사용 예시 (GameplayController.cs)
private void UpdateHoldingNotes()
{
    var toRemove = ListPool<Note>.Get();
    
    foreach (var kvp in holdingNotes)
    {
        if (kvp.Key == null || !kvp.Key.IsHolding)
            toRemove.Add(kvp.Key);
    }
    
    foreach (var note in toRemove)
        holdingNotes.Remove(note);
    
    ListPool<Note>.Return(toRemove);
}
```

### 3.2 UX/UI 개선

#### 콤보 UI 강화

**현재**: 텍스트만 표시

**개선안**:
```csharp
// GameplayUI.cs에 추가
[Header("Combo Effects")]
[SerializeField] private ParticleSystem comboParticle;
[SerializeField] private AudioClip comboSound;

public void UpdateCombo(int combo)
{
    if (combo < 10) return;
    
    // 단계별 효과
    if (combo >= 100)
    {
        TriggerComboEffect(ComboLevel.Legendary);
    }
    else if (combo >= 50)
    {
        TriggerComboEffect(ComboLevel.Epic);
    }
    else if (combo >= 25)
    {
        TriggerComboEffect(ComboLevel.Great);
    }
    else if (combo >= 10)
    {
        TriggerComboEffect(ComboLevel.Good);
    }
}

private void TriggerComboEffect(ComboLevel level)
{
    if (comboParticle != null)
        comboParticle.Play();
    
    if (comboSound != null)
        AudioManager.Instance?.PlaySFX(comboSound);
}
```

#### 판정 표시 개선

**현재**: 단순 텍스트

**개선안**:
```csharp
// GameplayUI.cs
public void ShowJudgement(JudgementResult result, float timing)
{
    var (text, color, scale) = result switch
    {
        JudgementResult.Perfect => ("PERFECT!", Color.yellow, 1.3f),
        JudgementResult.Great => ("GREAT", Color.cyan, 1.1f),
        JudgementResult.Good => ("GOOD", Color.green, 1.0f),
        JudgementResult.Bad => ("BAD", Color.red, 0.9f),
        _ => ("MISS", Color.gray, 0.8f)
    };
    
    judgementText.text = text;
    judgementText.color = color;
    judgementText.transform.localScale = Vector3.one * scale;
    
    // 타이밍 표시 (Early/Late)
    if (result != JudgementResult.Miss)
    {
        string timingText = timing > 0 ? "LATE" : "EARLY";
        timingLabel.text = $"{Mathf.Abs(timing)*1000:F0}ms {timingText}";
    }
}
```

### 3.3 게임플레이 개선

#### 스킵/리트라이 기능

**GameplayController.cs에 추가**:
```csharp
public void SkipToResult()
{
    if (!isPlaying) return;
    
    isPlaying = false;
    noteSpawner?.StopSpawning();
    AudioManager.Instance?.StopBGM();
    ShowResultScreen();
}

public void QuickRestart()
{
    Time.timeScale = 1f;
    isPaused = false;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}

public void PauseGame()
{
    if (!isPlaying || isPaused) return;
    
    isPaused = true;
    Time.timeScale = 0f;
    AudioManager.Instance?.PauseBGM();
    gameplayUI?.ShowPauseMenu();
}

public void ResumeGame()
{
    if (!isPaused) return;
    
    isPaused = false;
    Time.timeScale = 1f;
    AudioManager.Instance?.ResumeBGM();
    gameplayUI?.HidePauseMenu();
}
```

#### 자동 저장 시스템

**AutoSave.cs (신규)**:
```csharp
using UnityEngine;
using System;

namespace AIBeat.Core
{
    public class AutoSave : MonoBehaviour
    {
        [SerializeField] private float saveInterval = 30f;
        
        private void Start()
        {
            InvokeRepeating(nameof(SaveProgress), saveInterval, saveInterval);
        }
        
        private void SaveProgress()
        {
            PlayerPrefs.SetString("LastPlayDate", DateTime.Now.ToString("O"));
            PlayerPrefs.SetInt("TotalPlayCount", PlayerPrefs.GetInt("TotalPlayCount", 0) + 1);
            PlayerPrefs.Save();
        }
        
        public static DateTime? GetLastPlayDate()
        {
            string dateStr = PlayerPrefs.GetString("LastPlayDate", "");
            if (string.IsNullOrEmpty(dateStr)) return null;
            
            if (DateTime.TryParse(dateStr, out var date))
                return date;
            return null;
        }
    }
}
```

---

## 4. 📁 신규 파일 생성 목록

| 파일명 | 경로 | 설명 |
|--------|------|------|
| `GameConstants.cs` | `Scripts/Core/` | 상수 정의 클래스 |
| `ErrorHandler.cs` | `Scripts/Core/` | 예외 처리 유틸리티 |
| `AutoSave.cs` | `Scripts/Core/` | 자동 저장 시스템 |
| `ListPool.cs` | `Scripts/Utils/` | List 오브젝트 풀 |
| `AudioBuffer.cs` | `Scripts/Audio/` | 오디오 버퍼링 |

---

## 5. 📋 구현 우선순위

### Phase 1: Critical 버그 수정 — ✅ 완료
- [x] C-1: SettingsManager DontDestroyOnLoad → 오진 (의도적 미사용)
- [x] C-2: AudioManager DontDestroyOnLoad → 오진 (의도적 미사용)
- [x] C-3: JudgementSystem 이벤트 해제 → 이미 구현됨
- [x] C-4: NoteSpawner 이벤트 해제 + 메모리 정리 → 이미 구현됨

### Phase 2: High 버그 수정 — ✅ 완료
- [x] H-1: InputHandler 예외 처리 검증 → try-catch 구현됨
- [x] H-3: NoteSpawner 메모리 정리 (C-4와 통합) → 이미 구현됨

### Phase 3: 기능 개선 (2주)
- [ ] 오브젝트 풀 동적 확장
- [ ] GC Allocation 최적화
- [ ] 콤보 UI 강화
- [ ] 판정 표시 개선

### Phase 4: 추가 기능 (3주)
- [ ] 스킵/리트라이 기능
- [ ] 자동 저장 시스템
- [ ] GameConstants 상수화

---

## 6. ✅ 검증 체크리스트

### 버그 수정 후 검증 — ✅ 코드 검증 완료 (2026-02-18)
- [x] 씬 전환 시 설정 유지 확인 → PlayerPrefs 기반으로 영속적 유지
- [x] 씬 전환 시 오디오 연속 재생 확인 → 씬별 재생성 방식 (의도적 설계)
- [x] 메모리 누수 없음 → OnDestroy()에서 Material/프리팹 정리 구현
- [x] 이벤트 구독/해제 정상 동작 → 4개 컴포넌트 모두 OnDestroy에서 해제

### 기능 개선 후 검증
- [ ] 오브젝트 풀 동적 확장 동작
- [ ] GC Allocation 감소 확인
- [ ] 콤보 효과 정상 표시
- [ ] 판정 표시 개선 확인

---

**작성자**: AI 분석
**검토 필요**: 팀 리뷰 후 우선순위 조정
