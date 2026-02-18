# PROGRESS.md - AI Beat 개발 진행 상황

## 📋 최신 개선 사항 (2026-02-18)

> 📝 **다음 AI 작업자를 위한 가이드**: 상세 기획안은 `Docs/BUG_FIX_AND_IMPROVEMENT_PLAN.md` 참조

---

## 🆕 신규 기획안 작성 완료 (2026-02-18)

### 📄 기획 문서
- **버그 수정 및 개선 기획안**: `Docs/BUG_FIX_AND_IMPROVEMENT_PLAN.md`

### 🐛 Critical 버그 (4개) — ✅ 전수 검증 완료 (2026-02-18)
| # | 문제 | 파일 | 상태 | 검증 결과 |
|---|------|------|------|-----------|
| C-1 | ~~SettingsManager DontDestroyOnLoad 누락~~ | `SettingsManager.cs` | ✅ 오진 | 의도적 미사용 (PlayerPrefs 유지, 에디터 중복 방지) |
| C-2 | ~~AudioManager DontDestroyOnLoad 누락~~ | `AudioManager.cs` | ✅ 오진 | 의도적 미사용 (씬별 재생성, 코드 주석 확인) |
| C-3 | ~~JudgementSystem 이벤트 구독 해제 누락~~ | `JudgementSystem.cs` | ✅ 수정완료 | OnDestroy() L287-290에서 해제 구현됨 |
| C-4 | ~~NoteSpawner 이벤트 구독 해제 + 메모리 누수~~ | `NoteSpawner.cs` | ✅ 수정완료 | OnDestroy() L644-669에서 이벤트+Material+프리팹 정리 |

### 🟡 High 버그 (2개) — ✅ 전수 검증 완료 (2026-02-18)
| # | 문제 | 파일 | 상태 | 검증 결과 |
|---|------|------|------|-----------|
| H-1 | ~~InputHandler 예외 처리 미흡~~ | `InputHandler.cs` | ✅ 수정완료 | try-catch + 균등분할 폴백 구현 (L58-66) |
| H-3 | ~~동적 프리팹 메모리 누수~~ | `NoteSpawner.cs` | ✅ 수정완료 | C-4와 동일 (OnDestroy에서 정리) |

### 🚀 기능 개선 (5개)
- [ ] 오브젝트 풀링 동적 확장
- [ ] GC Allocation 최적화 (ListPool)
- [x] 콤보 UI 강화 ✅ GameplayUI.UpdateCombo 구현됨
- [x] 판정 표시 개선 (Early/Late) ✅ GameplayUI.ShowJudgementDetailed 구현됨
- [ ] 스킵/리트라이 기능

### 📁 신규 파일 (5개)
- [x] `GameConstants.cs` - 상수 정의 ✅ `Scripts/Core/` 에 생성됨
- [x] `ErrorHandler.cs` - 예외 처리 ✅ `Scripts/Core/` 에 생성됨
- [ ] `AutoSave.cs` - 자동 저장
- [ ] `ListPool.cs` - List 풀링
- [ ] `AudioBuffer.cs` - 오디오 버퍼링

---

### 🐛 발견된 버그 및 수정 필요 사항

#### 🔴 Critical (분석 완료 - 2026-02-18)
| # | 문제 | 위치 | 상태 | 비고 |
|---|------|------|------|------|
| 1 | ~~SettingsManager DontDestroyOnLoad 누락~~ | `SettingsManager.cs:96-107` | ✅ 오진 | 의도적 미사용 (에디터 인스턴스 중복 방지, PlayerPrefs로 설정 유지) |
| 2 | ~~AudioManager DontDestroyOnLoad 누락~~ | `AudioManager.cs:72-83` | ✅ 오진 | 의도적 미사용 (씬별 재생성, 코드 주석으로 확인) |
| 3 | ~~JudgementSystem 이벤트 구독 해제 누락~~ | `JudgementSystem.cs:287-290` | ✅ 수정완료 | OnDestroy()에서 이벤트 해제 구현됨 |
| 4 | ~~NoteSpawner 동적 프리팹 메모리 누수~~ | `NoteSpawner.cs:644-669` | ✅ 수정완료 | OnDestroy()에서 Material/프리팹 정리 구현됨 |

#### 🟡 High (분석 완료 - 2026-02-18)
| # | 문제 | 위치 | 상태 | 비고 |
|---|------|------|------|------|
| 5 | ~~GameplayController debugMode 런타임 토글~~ | `GameplayController.cs:31-35` | ✅ 수정완료 | `#if UNITY_EDITOR` 컴파일 조건 사용 중 |
| 6 | ~~InputHandler 레인 경계 예외 처리~~ | `InputHandler.cs:58-66` | ✅ 수정완료 | try-catch + 균등분할 폴백 구현됨 |
| 7 | ~~Coroutine 중복 시작 방지~~ | `GameplayController.cs:59-62, 79-82` | ✅ 수정완료 | null 체크 후 시작 구현됨 |

#### 🟢 Medium (개선 권장)
| # | 문제 | 위치 | 상태 | 비고 |
|---|------|------|------|------|
| 8 | **Magic Number 상수화** | 여러 파일 | ⏸ 보류 | `GameConstants` 클래스 생성 권장 |
| 9 | **주석과 코드 불일치** | `GameplayController.cs:46-48` | ⏸ 보류 | 문서화 작업 |

---

## 🔧 버그 수정 가이드 (AI 작업용)

> ⚠️ **2026-02-18 검증 결과**: 아래 가이드 #1~#4는 **이미 구현되었거나 오진으로 판명**됨.
> - #1, #2: DontDestroyOnLoad는 **의도적으로 미사용** (에디터 중복 방지 + PlayerPrefs 유지)
> - #3: JudgementSystem OnDestroy() **이미 구현됨** (L287-290)
> - #4: NoteSpawner OnDestroy() **이미 구현됨** (L644-669, Material/프리팹/이벤트 정리 포함)
> - 참고: 수정 가이드는 참조용으로 유지하되, 실제 적용 불필요.

### 수정 가이드 #1: SettingsManager DontDestroyOnLoad
**파일**: `My project/Assets/Scripts/Core/SettingsManager.cs`
**위치**: `Awake()` 메서드

```csharp
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);  // ← 이 줄 추가
    LoadSettings();
}
```

### 수정 가이드 #2: AudioManager DontDestroyOnLoad
**파일**: `My project/Assets/Scripts/Core/AudioManager.cs`
**위치**: `Awake()` 메서드

```csharp
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);  // ← 이 줄 추가
    Initialize();
}
```

### 수정 가이드 #3: JudgementSystem 이벤트 해제
**파일**: `My project/Assets/Scripts/Gameplay/JudgementSystem.cs`
**위치**: 클래스 맨 끝에 `OnDestroy()` 메서드 추가

```csharp
private void OnDestroy()
{
    SettingsManager.OnSettingChanged -= OnSettingChanged;
}
```

### 수정 가이드 #4: NoteSpawner 메모리 정리
**파일**: `My project/Assets/Scripts/Gameplay/NoteSpawner.cs`
**위치**: 클래스 맨 끝에 `OnDestroy()` 메서드 추가

```csharp
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
}
```

---

## 🚀 기능 개선 진행 상황 (신규 작업)

### Phase 1: 안정성 향상
- [x] **ErrorHandler 시스템** - `Core/ErrorHandler.cs` ✅ 생성 완료
- [ ] **NullCheckUtility** - `Utils/NullCheckUtility.cs` 신규 생성
- [x] **GameConstants** - `Core/GameConstants.cs` ✅ 생성 완료

### Phase 2: 성능 최적화
- [ ] **오브젝트 풀링 동적 확장** - `NoteSpawner.cs` 개선
- [ ] **오디오 버퍼링** - `Audio/AudioBuffer.cs` 신규 생성
- [ ] **GC Allocation 최적화** - 전체 코드 리뷰

### Phase 3: 게임플레이 개선
- [ ] **스킵/리트라이 기능** - `GameplayController.cs`에 메서드 추가
- [ ] **자동 저장 시스템** - `Core/AutoSave.cs` 신규 생성
- [ ] **어댑티브 튜토리얼** - `TutorialManager.cs` 개선

### Phase 4: UX 개선
- [x] 메인 메뉴 버튼 한국어화
- [x] 씬 전환 페이드 효과
- [x] 연주자 애니메이션
- [x] **SETTINGS 버튼 가시성 개선** - ✅ FAB 스타일 적용 완료
- [x] **콤보 UI 추가** - ✅ `GameplayUI.cs` UpdateCombo 구현됨
- [ ] **상세 결과 화면** - `UI/ResultUI.cs` 신규 생성

---

## 📝 신규 기능 구현 가이드 (AI 작업용)

### 기능 #1: ErrorHandler 시스템
**파일**: `My project/Assets/Scripts/Core/ErrorHandler.cs` (신규)

```csharp
using System;
using UnityEngine;

namespace AIBeat.Core
{
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
}
```

### 기능 #2: GameConstants
**파일**: `My project/Assets/Scripts/Core/GameConstants.cs` (신규)

```csharp
namespace AIBeat.Core
{
    public static class GameConstants
    {
        // 레인 설정
        public const int LaneCount = 4;
        public const float LaneWidth = 1.4f;
        
        // 노트 설정
        public const float DefaultNoteSpeed = 5f;
        public const float MinNoteSpeed = 1f;
        public const float MaxNoteSpeed = 15f;
        
        // 판정 윈도우 (초)
        public const float PerfectWindow = 0.050f;  // ±50ms
        public const float GreatWindow = 0.100f;    // ±100ms
        public const float GoodWindow = 0.200f;     // ±200ms
        public const float BadWindow = 0.350f;      // ±350ms
        
        // 점수 설정
        public const int BaseScorePerNote = 1000;
        public const float MaxComboBonus = 0.5f;
        public const int ComboForMaxBonus = 100;
        public const float HoldBonusTickInterval = 0.1f;
        public const int HoldBonusPerTick = 50;
    }
}
```

### 기능 #3: 콤보 UI
**파일**: `My project/Assets/Scripts/UI/GameplayUI.cs`에 추가

```csharp
// 콤보 표시 메서드 추가
public void ShowCombo(int combo)
{
    if (combo < 2) return;
    
    comboText.text = combo.ToString();
    comboLabel.text = "COMBO";
    
    // 콤보에 따른 색상 변화
    Color comboColor = combo switch
    {
        >= 100 => new Color(1f, 0.5f, 0f),    // 오렌지
        >= 50 => new Color(1f, 0.84f, 0f),    // 골드
        >= 25 => new Color(0.58f, 0.29f, 0.98f), // 퍼플
        >= 10 => new Color(0f, 1f, 1f),       // 시안
        _ => new Color(1f, 0.84f, 0f)         // 골드
    };
    
    comboText.color = comboColor;
    comboLabel.color = comboColor;
    
    // 팝 애니메이션
    StartCoroutine(ComboPopAnimation(comboText.transform));
}

private System.Collections.IEnumerator ComboPopAnimation(Transform target)
{
    Vector3 originalScale = Vector3.one;
    Vector3 targetScale = originalScale * 1.3f;
    
    float elapsed = 0f;
    float duration = 0.15f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        target.localScale = Vector3.Lerp(originalScale, targetScale, Mathf.Sin(t * Mathf.PI));
        yield return null;
    }
    
    target.localScale = originalScale;
}
```

### 기능 #4: 판정 표시 개선
**파일**: `My project/Assets/Scripts/UI/GameplayUI.cs`에 추가

```csharp
// 판정별 색상 및 애니메이션
public void ShowJudgement(JudgementResult result)
{
    var (text, color) = result switch
    {
        JudgementResult.Perfect => ("PERFECT!", new Color(1f, 0.84f, 0f)),
        JudgementResult.Great => ("GREAT", new Color(0f, 1f, 1f)),
        JudgementResult.Good => ("GOOD", new Color(0.5f, 1f, 0.5f)),
        JudgementResult.Bad => ("BAD", new Color(1f, 0.5f, 0.5f)),
        _ => ("MISS", Color.gray)
    };
    
    judgementText.text = text;
    judgementText.color = color;
    judgementText.fontSize = result == JudgementResult.Perfect ? 48 : 36;
    
    // 글로우 효과
    var outline = judgementText.gameObject.GetComponent<UnityEngine.UI.Outline>();
    if (outline == null) outline = judgementText.gameObject.AddComponent<UnityEngine.UI.Outline>();
    outline.effectColor = color;
    outline.effectDistance = new Vector2(2, 2);
    
    // 페이드 아웃
    StartCoroutine(FadeOutJudgement());
}
```

### 기능 #5: SETTINGS FAB 버튼
**파일**: `My project/Assets/Scripts/UI/SongSelectUI.cs`에 추가

```csharp
// 곡 선택 화면의 SETTINGS 버튼을 FAB 스타일로 변경
private void CreateFloatingSettingsButton()
{
    // 기존 버튼 찾기
    var settingsButton = GameObject.Find("SettingsButton");
    if (settingsButton == null) return;
    
    var rect = settingsButton.GetComponent<RectTransform>();
    
    // 위치 변경: 우하단
    rect.anchorMin = new Vector2(1, 0);
    rect.anchorMax = new Vector2(1, 0);
    rect.pivot = new Vector2(1, 0);
    rect.anchoredPosition = new Vector2(-30, 30);
    rect.sizeDelta = new Vector2(64, 64);
    
    // 시안 색상 적용
    var img = settingsButton.GetComponent<Image>();
    img.color = new Color(0f, 1f, 1f, 1f); // 네온 시안
    
    // 그림자 추가
    var shadow = settingsButton.AddComponent<UnityEngine.UI.Shadow>();
    shadow.effectColor = new Color(0, 0, 0, 0.5f);
    shadow.effectDistance = new Vector2(3, -3);
    
    // 글로우 효과를 위한 아웃라인
    var outline = settingsButton.AddComponent<UnityEngine.UI.Outline>();
    outline.effectColor = new Color(0f, 1f, 1f, 0.6f);
    outline.effectDistance = new Vector2(2, 2);
}
```


---

### 🚀 기능 개선 진행 상황

#### Phase 1: 안정성 향상
- [x] ErrorHandler 시스템 구현
- [x] GameConstants 상수 클래스 구현
- [x] Coroutine 중복 시작 방지
- [x] Critical 버그 수정 (대부분 오진 판명, Material 누수만 실제 수정)

#### Phase 2: 성능 최적화
- [ ] 오브젝트 풀링 동적 확장
- [ ] 오디오 버퍼링 구현
- [ ] GC Allocation 최적화

#### Phase 3: 게임플레이 개선
- [ ] 스킵/리트라이 기능
- [ ] 자동 저장 시스템
- [ ] 어댑티브 튜토리얼

#### Phase 4: UX 개선
- [x] 메인 메뉴 버튼 한국어화
- [x] 씬 전환 페이드 효과
- [x] 연주자 애니메이션
- [x] SETTINGS FAB 버튼 (곡 선택 화면)
- [x] 콤보 UI (이미 구현됨 확인)
- [ ] 상세 결과 화면

---

### 📊 UI/UX 개선 현황

| 화면 | 개선 필요 사항 | 상태 |
|------|---------------|------|
| **곡 선택** | 어두운 배경에 어두운 텍스트 (가독성 저하) | ✅ 텍스트 밝기 개선 (0.55→0.75) |
| **곡 선택** | SETTINGS 버튼이 거의 보이지 않음 | ✅ FAB 스타일 적용 완료 |
| **메인 메뉴** | 배경 색상 블록이 시각적으로 산만함 | ✅ MCP 확인 — 정상 |
| **게임플레이** | 콤보/판정 UI 미흡 | ✅ 카메라쉐이크+마일스톤 플래시 추가 |
| **게임플레이** | Gameplay_BG.jpg 체크보드 패턴 오류 | ✅ 네온 테두리로 교체 |
| **게임플레이** | NoteVisuals 7키→4키 불일치 | ✅ GameConstants 기반으로 수정 |
| **공통** | 폰트 계층 구조가 명확하지 않음 | ⏸ 보류 (기능 영향 없음) |

---

## ✅ 완료된 작업 (이력)

### 2026-02-16
- [x] SceneBuilder 리팩토링 및 씬 빌드
- [x] UIButtonStyleHelper 유틸리티 클래스 생성
- [x] SettingsUI/GameplayUI 버튼 디자인 적용
- [x] 노트 렌더링 버그 수정 (Alpha 오버플로우)
- [x] MCP 테스트 완료 (61개 노트 정상 처리)

### 2026-02-15
- [x] MainMenu 연주자 개별 애니메이션 구현
- [x] 씬 전환 페이드 효과 구현
- [x] 곡 카드 등장 애니메이션 구현
- [x] 절차적 에셋 생성 시스템 구축
- [x] UI 에셋 절차적 생성
- [x] MainMenu 버튼 한국어화

### 2026-02-10
- [x] NanoBanana 텍스처 로드 문제 해결
- [x] LaneBackground 텍스처 생성
- [x] NoteVisuals 색상 시스템 구현
- [x] UIColorPalette 시스템 구축

---

## 📁 관련 문서

- **UI/UX 개선 기획안**: `Docs/UI_UX_IMPROVEMENT_PLAN.md`
- **프로젝트 개선 기획안**: `Docs/PROJECT_IMPROVEMENT_PLAN.md`
- **디자인 명세서**: `UI_DESIGN_SPEC.md`

---

## 🎯 다음 단계 작업

### 우선순위 1 (즉시) — 2026-02-16 완료
1. ~~SettingsManager DontDestroyOnLoad~~ → 오진 (클래스 없음)
2. ~~AudioManager DontDestroyOnLoad~~ → 오진 (의도적 제거)
3. ~~JudgementSystem 이벤트 해제~~ → 오진 (발행 측)
4. ✅ NoteSpawner Material 캐싱 + OnDestroy 정리
5. ✅ InputHandler 레인 경계 인식 → 레인 중심 기준 계산
6. ✅ Debug.Log 빌드 성능 → 에디터 전용 래핑

### 우선순위 2 (이번 주) — 2026-02-17 완료
1. ✅ ErrorHandler 시스템 구현 (`Core/ErrorHandler.cs`)
2. ✅ GameConstants 상수 클래스 (`Core/GameConstants.cs`)
3. ✅ SETTINGS FAB 버튼 (곡 선택 화면 우하단)
4. ✅ Coroutine 중복 시작 방지 (`GameplayController.cs`)
5. ✅ 콤보 UI — 이미 구현됨 확인 (`GameplayUI.UpdateCombo`)
6. ✅ 판정 표시 개선 — 이미 구현됨 확인 (`GameplayUI.ShowJudgementDetailed`)

### 우선순위 3 (다음 주)
1. 텍스트 가독성 개선 (UIColorPalette 색상 조정)
2. 상세 결과 화면 구현
3. 스킵/리트라이 기능

---

### 🎨 사용자 관점 개선 제안 (AI 분석)
> **분석일**: 2026-02-17
> **분석 대상**: 씬 흐름, UI/UX, 게임플레이 피드백(Juice)

#### 1. 게임플레이 "Juice" (타격감/몰입감) 부족
| 항목 | 현상 | 개선 제안 |
|------|------|-----------|
| **카메라 쉐이크** | 현재 없음 | 판정 'Perfect' 또는 콤보 50단위 돌파 시 미세한 카메라 흔들림 추가 |
| **배경 반응** | 단순 '숨쉬기(Breathe)' 또는 이퀄라이저 | 오디오 스펙트럼/Kick Drum에 맞춰 배경 밝기나 줌(Zoom)이 반응하도록 동기화 |
| **노트 타격** | 파티클과 텍스트만 표시됨 | 타격 시 레인 자체가 살짝 눌리거나(Scale), 레인 경계선이 발광하는 등 공간적 피드백 추가 |
| **콤보 연출** | 텍스트 색상 변경 및 팝업만 있음 | 100콤보 단위로 화면 전체에 미세한 글리치(Glitch) 효과나 테두리 발광 추가 |

#### 2. 시각적 완성도 (Visual Polish)
| 항목 | 현상 | 개선 제안 |
|------|------|-----------|
| **앨범 아트** | `SongLibraryUI`에 앨범 아트 미표시 | 곡 별 고유 앨범 아트(또는 장르별 기본 이미지)를 카드 좌측에 표시하여 시각적 정보 강화 |
| **결과 화면** | `GameplayUI` 내 단순 패널로 처리됨 | 별도의 **Result Scene**으로 분리하여 랭크(S/A/B) 등장 연출, 점수 카운트업 등을 화려하게 구현 |
| **스킨 테마** | 'Cyberpunk'와 'Music Theme' 혼재 | 색상 팔레트와 UI 디자인 언어를 하나로 통일 (네온 사이버펑크 추천) |

#### 3. 코드/데이터 일관성
| 항목 | 현상 | 개선 제안 |
|------|------|-----------|
| **레인 개수** | `GameConstants`는 4키, `NoteVisuals`는 7키 대응 | `NoteVisuals.cs:35`의 7키 하드코딩을 `GameConstants.LaneCount` 기반으로 동적 처리하도록 수정 |
| **로딩 영상** | 기능 비활성화 상태 | 분석 중 지루함을 덜기 위해 로딩 영상 또는 팁 화면 활성화 필요 |

---

### 🖼️ AI 생성 이미지 적용 가이드 (Assets/Resources/AIBeat_Design)
> **⚠️ 중요: 다음 작업자(Claude)는 반드시 아래 지정된 파일명을 사용하세요.** (임의의 이미지 사용 금지)

#### 1. 배경 이미지 (Backgrounds)
| 파일명 (정확한 이름) | 경로 | 적용 대상 | 코드 적용 가이드 |
|-------------------|------|-----------|------------------|
| **`Menu_BG.png`** | `UI/Backgrounds/` | **MainMenuScene** | `SceneBuilder.cs`에서 Canvas 하위 BG Image에 할당 |
| **`SongSelect_BG.png`** | `UI/Backgrounds/` | **SongSelectScene** | `SceneBuilder.cs`에서 Canvas 하위 BG Image에 할당 |
| **`Gameplay_BG.jpg`** | `UI/Backgrounds/` | **GameplayScene** | `GameplayUI.cs`의 `CreateHUDFrameOverlay()` 또는 `CreateGameplayBackground()`에서 `Resources.Load` 사용 |
| **`Splash_BG.png`** | `UI/Backgrounds/` | **SplashScene** | 앱 실행 시 스플래시 화면 배경으로 사용 |

#### 2. UI 요소 (UI Elements)
| 파일명 (정확한 이름) | 경로 | 적용 대상 | 코드 적용 가이드 |
|-------------------|------|-----------|------------------|
| **`Default_Album_Art.jpg`** | `UI/` | **SongLibraryUI** | `SongLibraryUI.cs`의 `defaultAlbumArt` 변수에 로드. 곡 카드의 앨범 아트가 없을 때 기본값으로 표시. |
| `Cyberpunk_guitarist...` | `Illustrations/` | **Result Screen** | (추후 적용) 결과 화면에서 랭크(S/A) 달성 시 등장하는 캐릭터 |
| `Cyberpunk_keyboardist...` | `Illustrations/` | **Character Select** | (추후 적용) 메인 메뉴에서 3D 캐릭터 대신 표시 가능한 2D 일러스트 |

---

### 2026-02-17 (디자인 수정)
- [x] Gameplay_BG.jpg 체크보드 패턴 제거 → 프로그래밍 네온 테두리로 교체
- [x] NoteVisuals 7키 하드코딩 → 4키 GameConstants 기반으로 수정
- [x] 카메라 쉐이크 (Perfect 판정 + 콤보 50/100 마일스톤)
- [x] 콤보 100 마일스톤: 화면 테두리 플래시 효과
- [x] 곡 선택 텍스트 가독성 개선 (랭크/점수/빈목록 텍스트 밝기 상향)
- [x] MCP 스크린샷 검증 (Splash→MainMenu→SongSelect→Gameplay→Result 전체 확인)

### 2026-02-18 (씬 통합 + Result 패널 수정)
- [x] GameplayScene.unity 중복 씬 삭제 (미사용 → Gameplay.unity만 사용)
- [x] SceneBuilder.cs: GameplayScene.unity → Gameplay.unity 참조 통일
- [x] ResultPanel SafeAreaPanel 내부 비활성화 버그 수정 → Canvas 루트로 이동
- [x] ResultPanel 활성 상태 유지 코루틴 안전장치 추가
- [x] **ResultPanel 렌더링 안 되는 근본 원인 수정** (아래 상세)
- [x] Force Capture / Force Show Result 에디터 도구 추가
- [x] 전체 게임 플로우 MCP 캡처 검증 완료 (Splash→MainMenu→SongSelect→Gameplay→Result)
- [x] **AI 생성 이미지 적용 검증 완료** (아래 상세)

#### 🖼️ AI 생성 이미지(Gemini) 적용 검증 결과
| 이미지 | 씬 | 적용 여부 | 검증 방법 | 비고 |
|--------|-----|----------|-----------|------|
| **Splash_BG.png** | SplashScene | ✅ 적용됨 | 콘솔: `[SplashController] Loaded Splash_BG` | 보라색 그라데이션 배경 |
| **Menu_BG.png** | MainMenuScene | ✅ 적용됨 | 콘솔: `[MainMenuUI] Loaded Menu_BG as background` + 캡처 | 어두운 네이비 그라데이션 + DarkOverlay(0.6α) |
| **SongSelect_BG.png** | SongSelectScene | ✅ 적용됨 | 콘솔: `[SongSelectUI] Loaded SongSelect_BG as background` + 캡처 | 어두운 그레이/블랙 그라데이션 |
| **Default_Album_Art.jpg** | SongSelectScene | ✅ 적용됨 | 콘솔: `Texture2D→Sprite 폴백 성공 (1024x2048)` + 캡처 | 사이버펑크 DJ 캐릭터, 곡 썸네일에 표시 |
| **Gameplay_BG.jpg** | GameplayScene | ⚠️ 의도적 미사용 | 코드 주석 + 캡처 확인 | JPG→투명도 미지원→체크보드 문제. 대신 ProceduralImageGenerator 사용 |

#### 🔥 ResultPanel 렌더링 버그 근본 원인 (중요 교훈)
**증상**: ResultPanel이 `activeSelf: true`인데 화면에 전혀 렌더링 되지 않음 (초록색 디버그 배경도 안 보임)
**근본 원인**: Gameplay.unity 씬 파일에서 `[SerializeField] resultPanel`이 빈 "New Game Object" (Transform만 있는 루트 오브젝트)를 참조
- `resultPanel != null` 체크가 통과 → `CreateResultPanel()` 미호출
- 빈 오브젝트에는 RectTransform, Image, Canvas 하위 구조가 없어 렌더링 불가
- `pausePanel`, `countdownPanel`도 동일한 문제 (모두 stale "New Game Object" 참조)
**수정**: 씬 파일에서 3개의 잘못된 SerializedField 참조를 `{fileID: 0}`으로 초기화 + stale 오브젝트 3개 제거
**교훈**: `[SerializeField]` 필드가 존재하면 Unity는 씬의 오브젝트를 연결할 수 있음 → 코드에서 동적 생성하는 패널이 씬에 빈 오브젝트로 남아있으면 생성 로직이 건너뛰어짐


---

## 🔧 추가 개선 기획안 (2026-02-18 분석)

### 1. 아키텍처/구조적 개선

#### 1.1 싱글톤 관리 (2026-02-18 검증 완료)
| 항목 | 현재 상태 | 설계 의도 | 비고 |
|------|-----------|-----------|------|
| **SettingsManager** | `DontDestroyOnLoad` 미사용 | ✅ 의도적 — PlayerPrefs로 설정 유지, 씬별 재생성 | 에디터 인스턴스 중복 방지 |
| **AudioManager** | `DontDestroyOnLoad` 미사용 | ✅ 의도적 — 씬별 재생성, 코루틴 문제 회피 | OnDestroy에서 이벤트 해제 |
| **GameManager** | `DontDestroyOnLoad` 사용 | ✅ 양호 — 게임 상태 관리용 | 참조용으로 유지 |

#### 1.2 이벤트 구독 관리 (2026-02-18 검증 완료)
| 위치 | 이벤트 구독 | 해제 여부 | 비고 |
|------|-------------|-----------|------|
| **JudgementSystem** | `SettingsManager.OnSettingChanged` | ✅ 해제됨 | `OnDestroy()` (L287-290) |
| **NoteSpawner** | `SettingsManager.OnSettingChanged` | ✅ 해제됨 | `OnDestroy()` (L648) |
| **AudioManager** | `SettingsManager.OnSettingChanged` | ✅ 해제됨 | `OnDestroy()` (L87) |
| **GameplayController** | 여러 이벤트 | ✅ 해제됨 | `OnDestroy()` (L1173-1204) |

**권장 패턴:**
```csharp
private void OnEnable()  // 또는 Awake/Start
{
    SettingsManager.OnSettingChanged += OnSettingChanged;
}

private void OnDisable()  // 또는 OnDestroy
{
    SettingsManager.OnSettingChanged -= OnSettingChanged;
}
```

### 2. 성능 최적화

#### 2.1 오브젝트 풀링 개선
**현재:** `NoteSpawner.cs:30-31` - 고정 크기 풀
```csharp
[SerializeField] private int poolSize = 100;  // 고정 크기
```

**개선안 - 동적 풀 확장:**
```csharp
public class NotePool : MonoBehaviour
{
    [Header("Pool Configuration")]
    [SerializeField] private int initialSize = 50;
    [SerializeField] private int maxSize = 200;
    [SerializeField] private float expandThreshold = 0.8f;  // 80% 사용 시 확장
    
    private Dictionary<NoteType, Queue<Note>> pools = new();
    private Dictionary<NoteType, int> activeCounts = new();
    
    public Note GetNote(NoteType type)
    {
        // 풀이 부족하면 동적 확장
        if (pools[type].Count == 0 && activeCounts[type] < maxSize)
        {
            ExpandPool(type, 20);  // 20개씩 증가
        }
        
        var note = pools[type].Dequeue();
        activeCounts[type]++;
        return note;
    }
}
```

#### 2.2 GC Allocation 최적화
**문제 지점:**
1. **InputHandler.cs** - 터치 처리 시 매 프레임 Dictionary 순회
2. **GameplayController.cs** - 롱노트 홀드 복합 계산 시 List 할당
3. **JudgementSystem.cs** - 판정 시 이벤트 호출 (Action 할당)

**개선 방안:**
```csharp
// Object Pooling for Lists
private static class ListPool<T>
{
    private static readonly Queue<List<T>> pool = new();
    
    public static List<T> Get()
    {
        return pool.Count > 0 ? pool.Dequeue() : new List<T>(32);
    }
    
    public static void Return(List<T> list)
    {
        list.Clear();
        pool.Enqueue(list);
    }
}
```

### 3. 게임플레이 개선

#### 3.1 자동 저장 시스템
**신규 - AutoSave.cs:**
```csharp
public class AutoSave : MonoBehaviour
{
    [SerializeField] private float saveInterval = 30f;
    
    private void Start()
    {
        InvokeRepeating(nameof(SaveProgress), saveInterval, saveInterval);
    }
    
    private void SaveProgress()
    {
        if (!GameplayController.IsPlaying) return;
        
        PlayerPrefs.SetString("LastPlayDate", DateTime.Now.ToString("O"));
        PlayerPrefs.SetInt("TotalPlayCount", PlayerPrefs.GetInt("TotalPlayCount", 0) + 1);
        PlayerPrefs.SetString("LastSong", GameManager.Instance.CurrentSongData?.Title ?? "");
        PlayerPrefs.Save();
    }
}
```

#### 3.2 스킵/리트라이 기능
**GameplayController.cs에 추가:**
```csharp
public void SkipToResult()
{
    if (!isPlaying) return;
    isPlaying = false;
    ShowResultScreen();
}

public void QuickRestart()
{
    Time.timeScale = 1f;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
```

### 4. 코드 품질 개선

#### 4.1 Magic Number 상수화
**GameConstants.cs (신규):**
```csharp
public static class GameConstants
{
    // 레인 설정
    public const int LaneCount = 4;
    public const float LaneWidth = 1.4f;
    public const float LaneCenterOffset = 0.5f;
    
    // 노트 설정
    public const float DefaultNoteSpeed = 5f;
    public const float MinNoteSpeed = 1f;
    public const float MaxNoteSpeed = 15f;
    public const float NoteSpawnDistance = 12f;
    public const float NoteLookAheadTime = 3f;
    
    // 판정 윈도우 (초)
    public const float PerfectWindow = 0.050f;  // ±50ms
    public const float GreatWindow = 0.100f;    // ±100ms
    public const float GoodWindow = 0.200f;     // ±200ms
    public const float BadWindow = 0.350f;      // ±350ms
    
    // 점수 설정
    public const int BaseScorePerNote = 1000;
    public const float MaxComboBonus = 0.5f;
    public const int ComboForMaxBonus = 100;
    public const float HoldBonusTickInterval = 0.1f;
    public const int HoldBonusPerTick = 50;
}
```

#### 4.2 ErrorHandler 시스템
**ErrorHandler.cs (신규):**
```csharp
public static class ErrorHandler
{
    public static void SafeCall(Action action, string context = "")
    {
        try { action?.Invoke(); }
        catch (Exception e)
        {
            Debug.LogError($"[{context}] Error: {e.Message}\n{e.StackTrace}");
        }
    }
    
    public static T SafeCall<T>(Func<T> func, T defaultValue, string context = "")
    {
        try { return func.Invoke(); }
        catch (Exception e)
        {
            Debug.LogError($"[{context}] Error: {e.Message}");
            return defaultValue;
        }
    }
}
```

### 5. 📋 구현 우선순위 (2026-02-18 검증 완료)

#### ~~즉시 (Critical)~~ — 모두 해결됨
- [x] **SettingsManager** DontDestroyOnLoad → 오진 (의도적 미사용)
- [x] **AudioManager** DontDestroyOnLoad → 오진 (의도적 미사용)
- [x] **JudgementSystem** 이벤트 구독 해제 → OnDestroy() 구현됨
- [x] **NoteSpawner** OnDestroy 정리 → Material/프리팹 정리 구현됨
- [x] **Debug.Log** 빌드 성능 → `#if UNITY_EDITOR` 래핑 완료 (GameplayController, NoteSpawner, InputHandler)

#### ~~고우선순위 (1-2주)~~ — 모두 완료됨
- [x] **UIColorPalette** 색상 개선
- [x] **콤보 UI** 구현 (GameplayUI.UpdateCombo)
- [x] **판정 표시** 개선 (GameplayUI.ShowJudgementDetailed)
- [x] **SETTINGS FAB** 버튼

#### 중우선순위 (다음)
- [x] **ErrorHandler** 시스템
- [x] **GameConstants** 상수화
- [ ] **오브젝트 풀** 동적 확장
- [ ] **자동 저장** 시스템

### 6. 📁 신규 파일 목록

| 파일명 | 위치 | 설명 |
|--------|------|------|
| `GameConstants.cs` | `Scripts/Core/` | 상수 정의 |
| `ErrorHandler.cs` | `Scripts/Core/` | 예외 처리 유틸 |
| `NullCheckUtility.cs` | `Scripts/Utils/` | 널 체크 확장메서드 |
| `AutoSave.cs` | `Scripts/Core/` | 자동 저장 기능 |
| `AudioBuffer.cs` | `Scripts/Audio/` | 오디오 버퍼링 |
| `NotePool.cs` | `Scripts/Gameplay/` | 향상된 풀링 |
| `ListPool.cs` | `Scripts/Utils/` | List 오브젝트 풀 |
| `ResultUI.cs` | `Scripts/UI/` | 상세 결과 화면 |

---

### 🎨 NanoBanana 디자인 프롬프트 가이드 (AI 생성용)
> **사용법**: 아래 영문 프롬프트를 NanoBanana(또는 이미지 생성 툴)에 입력하여 에셋을 생성하세요.

#### 1. 씬(Scene)별 배경 및 화면
| 화면 (Scene) | 프롬프트 (Core Prompt) | 스타일 키워드 | 비고 |
|--------------|------------------------|---------------|------|
| **Splash Screen** | `Futuristic typography logo "AI BEAT" glowing in neon cyan and magenta, floating in dark void, digital particles, glitch effect, minimalism, 8k resolution` | Cyberpunk, Minimalist, Tech | 앱 실행 로고 화면 |
| **Main Menu** | `Cyberpunk city street at night, wet pavement reflecting neon signs, holographic advertisements, towering skyscrapers, dark blue and purple atmosphere, cinematic lighting, high detail` | Cyberpunk City, Atmospheric, Neon | 메인 메뉴 배경 |
| **Song Select** | `Futuristic digital music library interface, floating holographic vinyl records, data streams, equalizer bars in background, cool blue tones, organized, sleek UI design` | Holographic, UI, Data | 곡 선택 화면 |
| **Gameplay BG** | `Hyper-speed tunnel made of neon lights, abstract geometric shapes rushing past, deep depth of field, dark background for contrast, sense of speed and rhythm, music visualizer style` | Abstract, Speed, Dark | 게임 플레이 배경 |
| **Result Screen** | `Cyberpunk concert stage with spotlights shining down, digital confetti falling, vibrant colors, sense of victory and celebration, dynamic lighting, stadium atmosphere` | Stage, Victory, Spotlight | 결과 화면 |
| **Loading Screen** | `Abstract neon vortex or spinning digital circle, glowing lines, futuristic data loading visualization, dark background, clean and simple` | Abstract, Loading, Loop | 로딩 화면 |

#### 2. 추가 이펙트 및 요소 (Assets)
| 요소 (Element) | 프롬프트 (Core Prompt) | 용도 |
|----------------|------------------------|------|
| **Note Skin** | `Glowing neon bar, luminous crystal texture, cyan and magenta colors, 3d render, simple geometry` | 노트 디자인 (직사각형 바) |
| **Explosion VFX** | `Digital explosion burst, neon sparks, light flares, starburst shape, transparent background, high contrast` | 노트 타격 이펙트 (파티클) |
| **Character (DJ)** | `Cyberpunk DJ character wearing futuristic headphones and visor, mixing music on holographic deck, neon tattoos, anime style, cool pose` | 앨범 아트 또는 캐릭터 |

---

**마지막 업데이트**: 2026-02-18 (PROGRESS + BUG_FIX_PLAN 전체 일관성 검증 및 업데이트)
**다음 검토일**: 2026-02-19
