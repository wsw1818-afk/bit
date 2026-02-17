# PROGRESS.md - AI Beat 개발 진행 상황

## 📋 최신 개선 사항 (2026-02-17)

> 📝 **다음 AI 작업자를 위한 가이드**: 각 항목의 "수정 가이드" 섹션을 참고하여 구현하세요.

### 🐛 발견된 버그 및 수정 필요 사항

#### 🔴 Critical (즉시 수정 필요)
| # | 문제 | 위치 | 상태 | 수정 가이드 | 비고 |
|---|------|------|------|-------------|------|
| 1 | **SettingsManager DontDestroyOnLoad 누락** | `SettingsManager.cs:98-106` | ❌ 미수정 | 아래 수정 가이드 #1 참고 | 씬 전환 시 설정 초기화됨 |
| 2 | **AudioManager DontDestroyOnLoad 누락** | `AudioManager.cs:72-83` | ❌ 미수정 | 아래 수정 가이드 #2 참고 | 씬 전환 시 오디오 끊김 |
| 3 | **JudgementSystem 이벤트 구독 해제 누락** | `JudgementSystem.cs:79-80` | ❌ 미수정 | 아래 수정 가이드 #3 참고 | 메모리 누수 위험 |
| 4 | **NoteSpawner 동적 프리팹 메모리 누수** | `NoteSpawner.cs:40-42` | ❌ 미수정 | 아래 수정 가이드 #4 참고 | 동적 생성된 Material 정리 필요 |

#### 🟡 High (1주 내 수정 권장)
| # | 문제 | 위치 | 상태 | 수정 가이드 | 비고 |
|---|------|------|------|-------------|------|
| 5 | **GameplayController debugMode 런타임 토글** | `GameplayController.cs:31-35` | ❌ 미수정 | 개발 중이므로 우선순위 낮음 | 현재 컴파일 조걶 사용 중 |
| 6 | **InputHandler 레인 경계 예외 처리** | `InputHandler.cs:62-66` | ❌ 미수정 | try-catch 강화, 폴팰백 추가 | 치메라 미확보 시 크래시 |
| 7 | **Coroutine 중복 시작 방지** | `GameplayController.cs:55-72` | ❌ 미수정 | null 체크 후 시작 | 성능 이슈 |

#### 🟢 Medium (개선 권장)
| # | 문제 | 위치 | 상태 | 비고 |
|---|------|------|------|------|
| 8 | **Magic Number 상수화** | 여러 파일 | ⏸ 보류 | `GameConstants` 클래스 생성 권장 |
| 9 | **주석과 코드 불일치** | `GameplayController.cs:46-48` | ⏸ 보류 | 문서화 작업 |

---

## 🔧 버그 수정 가이드 (AI 작업용)

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
- [ ] **ErrorHandler 시스템** - `Core/ErrorHandler.cs` 신규 생성
- [ ] **NullCheckUtility** - `Utils/NullCheckUtility.cs` 신규 생성
- [ ] **GameConstants** - `Core/GameConstants.cs` 상수 클래스 생성

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
- [ ] **SETTINGS 버튼 가시성 개선** - FAB 스타일 적용
- [ ] **콤보 UI 추가** - `GameplayUI.cs`에 구현
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
| **곡 선택** | 어두운 배경에 어두운 텍스트 (가독성 저하) | ❌ 미수정 |
| **곡 선택** | SETTINGS 버튼이 거의 보이지 않음 | ❌ 미수정 |
| **메인 메뉴** | 배경 색상 블록이 시각적으로 산만함 | ⚠️ 부분 수정 |
| **게임플레이** | 콤보/판정 UI 미흡 | ❌ 미수정 |
| **공통** | 폰트 계층 구조가 명확하지 않음 | ❌ 미수정 |

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
> **다음 작업자(Claude)를 위한 리소스 매핑 가이드**

#### 1. 배경 이미지 (Backgrounds)
| 파일 경로 | 적용 대상 | 비고 |
|-----------|-----------|------|
| `UI/Backgrounds/Menu_BG.png` | **MainMenuScene** | 메인 메뉴 배경 (Canvas 하위 가장 뒤쪽 Image) |
| `UI/Backgrounds/SongSelect_BG.png` | **SongSelectScene** | 곡 선택 화면 배경 |
| `UI/Backgrounds/Gameplay_BG.jpg` | **GameplayScene** | 게임 플레이 배경 (노트 레인 뒤쪽, `GameplayUI.cs`에서 로드) |
| `UI/Backgrounds/Splash_BG.png` | **SplashScene** | 앱 실행 시 로고와 함께 표시되는 배경 |

#### 2. UI 요소 (UI Elements)
| 파일 경로 | 적용 대상 | 비고 |
|-----------|-----------|------|
| `UI/Default_Album_Art.jpg` | **SongLibraryUI** | 앨범 아트가 없는 곡의 기본 커버 이미지 (SongCard 좌측) |
| `Illustrations/Cyberpunk_guitarist...` | **Result Screen** | (추후 적용) 결과 화면에서 랭크(S/A) 달성 시 등어장하는 캐릭터 |
| `Illustrations/Cyberpunk_keyboardist...` | **Character Select** | (추후 적용) 메인 메뉴에서 3D 캐릭터 대신 표시 가능한 2D 일러스트 |

---

**마지막 업데이트**: 2026-02-17
**다음 검토일**: 2026-02-18
