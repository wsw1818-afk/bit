# AI Beat - UI/UX 개선 기획안

## 📊 현재 상태 분석

### 발견된 주요 문제점

| 화면 | 문제점 | 심각도 |
|------|--------|--------|
| **곡 선택** | 어두운 배경에 어두운 텍스트 (가독성 저하) | 🔴 높음 |
| **곡 선택** | SETTINGS 버튼이 거의 보이지 않음 | 🔴 높음 |
| **메인 메뉴** | 버튼 디자인이 평면적이고 단조로움 | 🟡 중간 |
| **메인 메뉴** | 배경 색상 블록이 시각적으로 산만함 | 🟡 중간 |
| **게임플레이** | UI 요소들이 레인과 구분이 어려움 | 🟡 중간 |
| **공통** | 폰트 계층 구조가 명확하지 않음 | 🟡 중간 |

---

## 🎯 개선 목표

1. **가독성 향상**: 모든 텍스트가 배경에서 명확히 구분
2. **시각적 계층 구조**: 중요한 정보가 눈에 잘 들어오도록
3. **일관된 디자인 언어**: 모든 화면에서 통일된 느낌
4. **현대적인 느낌**: 네온/글로우 효과를 활용한 사이버펑크 감성 강화
5. **모바일 최적화**: 터치 타겟 크기 및 위치 최적화

---

## 🎨 개선 방안

### 1. 색상 시스템 개선

#### 현재 문제
```
BG_DEEP: (0.03, 0.05, 0.10) - 너무 어두워 텍스트와 구분 어려움
TEXT_GRAY: (0.7, 0.7, 0.8) - 배경과 대비가 충분하지 않음
```

#### 개선안
```csharp
// ===== 개선된 배경 =====
BG_DEEP: new Color(0.05f, 0.07f, 0.14f, 1f)        // 약간 밝게
BG_CARD: new Color(0.10f, 0.12f, 0.22f, 0.95f)     // 투명도 조정

// ===== 개선된 텍스트 =====
TEXT_PRIMARY: Color.white                           // 주요 텍스트
TEXT_SECONDARY: new Color(0.85f, 0.85f, 0.90f, 1f) // 보조 텍스트
TEXT_MUTED: new Color(0.5f, 0.5f, 0.6f, 1f)        // 비활성/힌트

// ===== 강조 색상 통일 =====
ACCENT_PRIMARY: NEON_CYAN      // 메인 액션
ACCENT_SECONDARY: NEON_GOLD    // 성과/점수
ACCENT_TERTIARY: NEON_PURPLE   // 보조 액션
```

---

### 2. 메인 메뉴 개선

#### 개선 사항

| 요소 | 현재 상태 | 개선 방향 |
|------|-----------|-----------|
| **배경** | 색상 블록이 산만 | 단일 그라데이션 + 파티클 효과 |
| **버튼** | 평면적, 테두리 없음 | 글로우 테두리 + 그라데이션 필 |
| **타이포그래피** | 단일 폰트 크기 | 계층적 타이포그래피 시스템 |
| **애니메이션** | 파동 로고만 | 버튼 호버 시 네온 펄스 효과 추가 |

#### 버튼 스타일 개선
```csharp
// 버튼 스타일 설정 (MainMenuUI.cs)
private void CreateStyledButton(Button button, string text, Color accentColor)
{
    // 배경: 반투명 다크 + 그라데이션
    var colors = button.colors;
    colors.normalColor = new Color(0.08f, 0.10f, 0.20f, 0.9f);
    colors.highlightedColor = accentColor.WithAlpha(0.3f);
    colors.pressedColor = accentColor.WithAlpha(0.5f);
    button.colors = colors;
    
    // 네온 테두리 효과
    var outline = button.gameObject.AddComponent<Outline>();
    outline.effectColor = accentColor.WithAlpha(0.6f);
    outline.effectDistance = new Vector2(2, 2);
    
    // 텍스트 스타일
    var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
    tmp.fontSize = 32;
    tmp.color = Color.white;
    tmp.fontStyle = FontStyles.Bold;
}
```

---

### 3. 곡 선택 화면 개선

#### 개선 사항

| 요소 | 현재 상태 | 개선 방향 |
|------|-----------|-----------|
| **곡 아이템** | 텍스트만 표시 | 앨범아트 + 제목 + 아티스트 + 난이도 |
| **SETTINGS 버튼** | 거의 보이지 않음 | 플로팅 액션 버튼(FAB) 스타일로 변경 |
| **이퀄라이저** | 하단 고정 | 배경으로 이동, 투명도 조절 |
| **리스트 스크롤** | 기본 스크롤 | 바운스 효과 + 스냅 스크롤 |

#### 곡 리스트 아이템 디자인
```
┌─────────────────────────────────────────┐
│ ┌─────┐  곡 제목 (Bold, 24pt, White)    │
│ │     │  아티스트 (Regular, 16pt, Gray) │
│ │앨범 │  ┌─────────────────────────┐    │
│ │아트 │  │ ████████░░░░  Hard      │    │
│ │     │  └─────────────────────────┘    │
│ └─────┘                                 │
└─────────────────────────────────────────┘
```

#### 설정 버튼 개선
```csharp
// SongSelectUI.cs - 플로팅 액션 버튼
private void CreateFloatingSettingsButton()
{
    var fab = new GameObject("SettingsFAB");
    var rect = fab.AddComponent<RectTransform>();
    rect.anchorMin = rect.anchorMax = new Vector2(1, 0); // 우하단
    rect.pivot = new Vector2(1, 0);
    rect.anchoredPosition = new Vector2(-30, 30);
    rect.sizeDelta = new Vector2(64, 64);
    
    var img = fab.AddComponent<Image>();
    img.sprite = settingsIcon;
    img.color = UIColorPalette.NEON_CYAN;
    
    // 그림자 + 글로우
    var shadow = fab.AddComponent<Shadow>();
    shadow.effectColor = Color.black.WithAlpha(0.5f);
    shadow.effectDistance = new Vector2(3, -3);
}
```

---

### 4. 게임플레이 UI 개선

#### 개선 사항

| 요소 | 현재 상태 | 개선 방향 |
|------|-----------|-----------|
| **점수 표시** | 상단 중앙 작은 텍스트 | 상단 좌측, 큰 폰트 + 애니메이션 |
| **콤보 표시** | 없음 | 중앙 상단, 큰 금색 숫자 + "COMBO" 텍스트 |
| **판정 표시** | 단순 텍스트 | 판정별 다른 색상 + 팝 애니메이션 |
| **HP/에너지 바** | 없음 | 상단 우측에 곡 진행률 + HP 표시 |
| **일시정지** | 작은 버튼 | 우측 상단 고정, 아이콘 버튼 |

#### 콤보 시스템 UI
```csharp
// GameplayUI.cs
public void ShowCombo(int combo)
{
    if (combo < 2) return;
    
    comboText.text = combo.ToString();
    comboLabel.text = "COMBO";
    
    // 콤보에 따른 색상 변화
    Color comboColor = combo switch
    {
        >= 100 => UIColorPalette.COMBO_100,  // 오렌지
        >= 50 => UIColorPalette.COMBO_50,    // 골드
        >= 25 => UIColorPalette.COMBO_25,    // 퍼플
        >= 10 => UIColorPalette.COMBO_10,    // 시안
        _ => UIColorPalette.COMBO_NORMAL     // 골드
    };
    
    comboText.color = comboColor;
    comboLabel.color = comboColor.WithAlpha(0.8f);
    
    // 팝 애니메이션
    UIAnimator.PopText(comboText.transform, 1.2f, 0.15f);
}
```

#### 판정 표시 개선
```csharp
// 판정별 시각적 효과
private void ShowJudgement(Judgement judgement)
{
    var (text, color) = judgement switch
    {
        Judgement.Perfect => ("PERFECT!", UIColorPalette.JUDGE_PERFECT),
        Judgement.Great => ("GREAT", UIColorPalette.JUDGE_GREAT),
        Judgement.Good => ("GOOD", UIColorPalette.JUDGE_GOOD),
        Judgement.Bad => ("BAD", UIColorPalette.JUDGE_BAD),
        _ => ("MISS", UIColorPalette.JUDGE_MISS)
    };
    
    judgementText.text = text;
    judgementText.color = color;
    judgementText.fontSize = judgement == Judgement.Perfect ? 48 : 36;
    
    // 글로우 효과
    UIAnimator.GlowText(judgementText, color, 0.3f);
    
    // 희미해지는 애니메이션
    UIAnimator.FadeOut(judgementText, 0.5f);
}
```

---

### 5. 일관된 컴포넌트 라이브러리

#### NeonButton 컴포넌트 생성
```csharp
// UI/NeonButton.cs
public class NeonButton : MonoBehaviour
{
    [Header("Neon Style")]
    public Color neonColor = UIColorPalette.NEON_CYAN;
    public float glowIntensity = 1.5f;
    public float pulseSpeed = 1f;
    
    [Header("Animation")]
    public bool pulseOnIdle = true;
    public bool scaleOnHover = true;
    public float hoverScale = 1.05f;
    
    private Image background;
    private Outline outline;
    private Shadow shadow;
    
    void Start()
    {
        SetupVisuals();
        if (pulseOnIdle) StartCoroutine(PulseAnimation());
    }
    
    void SetupVisuals()
    {
        // 배경 설정
        background = GetComponent<Image>();
        background.color = new Color(0.08f, 0.10f, 0.20f, 0.9f);
        
        // 네온 아웃라인
        outline = gameObject.AddComponent<Outline>();
        outline.effectColor = neonColor.WithAlpha(0.6f);
        outline.effectDistance = new Vector2(2, 2);
        
        // 그림자
        shadow = gameObject.AddComponent<Shadow>();
        shadow.effectColor = Color.black.WithAlpha(0.4f);
        shadow.effectDistance = new Vector2(4, -4);
    }
    
    IEnumerator PulseAnimation()
    {
        while (true)
        {
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * pulseSpeed;
                float alpha = 0.4f + Mathf.Sin(t * Mathf.PI * 2) * 0.2f;
                outline.effectColor = neonColor.WithAlpha(alpha);
                yield return null;
            }
        }
    }
}
```

---

## 📱 반응형 레이아웃 개선

### SafeArea 적용
```csharp
// UI/SafeAreaLayout.cs
public class SafeAreaLayout : MonoBehaviour
{
    void Start()
    {
        ApplySafeArea();
    }
    
    void ApplySafeArea()
    {
        var rectTransform = GetComponent<RectTransform>();
        var safeArea = Screen.safeArea;
        
        var anchorMin = safeArea.position;
        var anchorMax = safeArea.position + safeArea.size;
        
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }
}
```

---

## 🎬 애니메이션 가이드

### 전환 애니메이션
| 전환 | 효과 | 지속시간 |
|------|------|----------|
| 화면 전환 | 슬라이드 + 페이드 | 0.3s |
| 버튼 클릭 | 스케일 다운 + 플래시 | 0.1s |
| 팝업 등장 | 스케일 업 + 페이드 인 | 0.2s |
| 콤보 증가 | 팝 + 색상 변화 | 0.15s |

### Easing 함수
```csharp
public static class AnimationCurves
{
    public static AnimationCurve EaseOutBack = new AnimationCurve(
        new Keyframe(0, 0, 0, 0),
        new Keyframe(1, 1, 1.7f, 0)
    );
    
    public static AnimationCurve EaseInOutCubic = new AnimationCurve(
        new Keyframe(0, 0, 0, 0),
        new Keyframe(0.5f, 0.5f, 1.5f, 1.5f),
        new Keyframe(1, 1, 0, 0)
    );
}
```

---

## 📋 구현 우선순위

### Phase 1: 핫픽스 (즉시 적용)
- [ ] 텍스트 색상 대비 개선 (UIColorPalette 수정)
- [ ] SETTINGS 버튼 가시성 개선
- [ ] 기본 폰트 크기 조정

### Phase 2: 컴포넌트 개선 (1주)
- [ ] NeonButton 컴포넌트 개발
- [ ] 버튼 애니메이션 통일
- [ ] 판정 표시 개선

### Phase 3: 화면 개선 (2주)
- [ ] 메인 메뉴 리디자인
- [ ] 곡 선택 화면 개선
- [ ] 게임플레이 UI 개선

### Phase 4: 폴리싱 (1주)
- [ ] 전환 애니메이션 추가
- [ ] 사운드 피드백 연동
- [ ] 최종 테스트 및 버그 수정

---

## 🔧 기술 구현 참고

### 폰트 설정
```csharp
// Core/KoreanFontManager.cs
public static class KoreanFontManager
{
    private static TMP_FontAsset _koreanFont;
    
    public static TMP_FontAsset KoreanFont
    {
        get
        {
            if (_koreanFont == null)
            {
                _koreanFont = Resources.Load<TMP_FontAsset>("Fonts/NotoSansCJKkr-Bold");
                if (_koreanFont != null)
                {
                    // 폰트 기본값 설정
                    _koreanFont.material.shader = Shader.Find("TextMeshPro/Distance Field");
                }
            }
            return _koreanFont;
        }
    }
    
    public static void ApplyToAllText()
    {
        var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (var text in texts)
        {
            text.font = KoreanFont;
            text.fontSize = 24;
            text.color = UIColorPalette.TEXT_PRIMARY;
        }
    }
}
```

---

## ✅ 체크리스트

### 디자인 시스템
- [ ] 색상 팔레트 정의서
- [ ] 타이포그래피 스케일
- [ ] 스페이싱 시스템 (8px 기반)
- [ ] 컴포넌트 라이브러리

### 구현 완료
- [ ] 모든 텍스트 가독성 확보
- [ ] 버튼 인터랙션 개선
- [ ] 애니메이션 추가
- [ ] 반응형 레이아웃
- [ ] 모바일 최적화

### 테스트
- [ ] 다양한 해상도 테스트
- [ ] 가독성 테스트 (빠른 시선 이동)
- [ ] 터치 반응성 테스트
- [ ] 성능 테스트

---

## 📎 참고자료

- 현재 컬러 팔레트: [`UIColorPalette.cs`](My%20project/Assets/Scripts/UI/UIColorPalette.cs)
- 메인 메뉴 UI: [`MainMenuUI.cs`](My%20project/Assets/Scripts/UI/MainMenuUI.cs)
- 곡 선택 UI: [`SongSelectUI.cs`](My%20project/Assets/Scripts/UI/SongSelectUI.cs)
- 게임플레이 UI: [`GameplayUI.cs`](My%20project/Assets/Scripts/UI/GameplayUI.cs)
- 버튼 애니메이션: [`ButtonAnimation.cs`](My%20project/Assets/Scripts/UI/ButtonAnimation.cs)
