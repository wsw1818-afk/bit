# PROGRESS.md - 게임플레이 화면 디자인 요청

- ✅ 게임 기능 모두 정상 작동 (노트 스폰, 판정, 점수 계산 등)
- ✅ 게임플레이 화면 비주얼 디자인 적용 완료 (Cyberpunk 테마)
- ✅ NanoBanana 배경 및 노트 텍스처 적용 완료
- ✅ Sensational Redesign 적용 (고강도 네온 컬러 및 전용 배경 로직)
- ✅ Unity MCP로 게임 상태 검증 완료 (2026-02-10 19:50)

## 재미나이에게 디자인 요청 사항

### 게임플레이 화면 구성 요소
1. **배경 (LaneBackground - 3D Quad)**
   - 위치: 4개 레인 뒤쪽
   - 크기: 화면 전체를 덮는 세로 방향 배경
   - 역할: 노트가 떨어지는 공간의 배경

2. **노트 (Note - 3D Cube)**
   - 4개 레인에서 위에서 아래로 떨어짐
   - 색상: 레인별로 다른 색 (현재: Cyan, Magenta, Yellow, White)
   - 판정선(화면 하단)에 도달하면 입력 판정

3. **UI 오버레이 (Canvas - Screen Space Overlay)**
   - 점수, 콤보, Early/Late 표시
   - 판정 텍스트 (Perfect/Great/Good/Bad/Miss)
   - 프로그레스 바

### 디자인 스타일 요구사항
- **장르**: 리듬 게임 (Beatmania IIDX, DJMAX, 비트매니아 스타일)
- **분위기**: Cyberpunk / Neon / 미래적
- **색감**: 고대비, 어두운 배경 + 밝은 네온 컬러
- **시인성**: 노트가 명확히 보여야 함 (배경이 노트를 가리면 안 됨)

### 코드 반영 방법

#### 1. LaneBackground 텍스처 생성
**파일**: `GameplayController.cs` → `ApplyCyberpunkLaneBackground()` 메서드 (라인 1082-1143)

```csharp
// 현재 코드 위치
private void ApplyCyberpunkLaneBackground()
{
    // ... GameObject 찾기 ...

    // 텍스처 설정
    int textureSize = 512;      // 텍스처 해상도
    int gridSize = 64;          // 그리드 간격

    // 색상 정의 (여기를 수정!)
    Color topColor = new Color(0.08f, 0.02f, 0.15f, 1f);     // 상단 색
    Color bottomColor = new Color(0.15f, 0.05f, 0.25f, 1f);  // 하단 색
    Color gridColor = new Color(1f, 0f, 0.8f, 0.04f);        // 그리드 색 (RGBA)

    // 픽셀별로 그리드/그라데이션 생성
    for (int y = 0; y < textureSize; y++)
    {
        for (int x = 0; x < textureSize; x++)
        {
            // 그라데이션 + 그리드 라인 + 노이즈
        }
    }
}
```

**수정 가능한 값**:
- `topColor` / `bottomColor`: 배경 그라데이션 색 (RGB 0~1 범위)
- `gridColor`: 그리드 라인 색 (RGBA, A는 투명도)
- `gridSize`: 그리드 간격 (작을수록 촘촘)
- `textureSize`: 텍스처 해상도 (높을수록 선명)

#### 2. 노트 색상 변경
**파일**: `NoteVisuals.cs` → `GetLaneColor()` 메서드

```csharp
private Color GetLaneColor(int lane)
{
    switch (lane)
    {
        case 0: return Color.cyan;      // 레인 0 색
        case 1: return Color.magenta;   // 레인 1 색
        case 2: return Color.yellow;    // 레인 2 색
        case 3: return Color.white;     // 레인 3 색
        default: return Color.white;
    }
}
```

#### 3. UI 색상 팔레트
**파일**: `UIColorPalette.cs`

```csharp
public static readonly Color Cyan = new Color(0f, 1f, 1f, 1f);
public static readonly Color Magenta = new Color(1f, 0f, 1f, 1f);
public static readonly Color Gold = new Color(1f, 0.84f, 0f, 1f);
public static readonly Color DeepBlack = new Color(0.05f, 0.05f, 0.08f, 1f);
```

### 디자인 제공 형식
1. **색상 코드** (RGB 또는 Hex)
   - 배경 상단 색: `#140226` (예시)
   - 배경 하단 색: `#260540`
   - 그리드 색: `#FF00CC` + 투명도 4%

2. **이미지 레퍼런스** (선택사항)
   - Beatmania IIDX, DJMAX 등의 스크린샷
   - 원하는 비주얼 분위기의 예시 이미지

3. **디자인 의도 설명**
   - 어떤 느낌을 주고 싶은지
   - 어떤 요소를 강조하고 싶은지

### 기술적 제약사항
- ⚠️ **노트는 Z=2, 배경은 Z=1** → 배경이 불투명해도 노트가 앞에 보임 ✅ (적용 완료)
- ⚠️ **그리드는 투명도 필수** (alpha 0.04 권장) → 너무 밝으면 노트 가림 ✅ (0.04 적용됨)
- ⚠️ **모바일 세로 모드** → 좁은 화면, 4개 레인이 화면 너비 전체 사용 ✅ (자동 조정)
- ✅ **런타임 텍스처 생성** → 코드로 실시간 생성 (이미지 파일 불필요) ✅ (구현 완료)

### RGB ↔ Unity Color 변환 참고
Unity의 `Color(r, g, b, a)`는 **0~1 범위**를 사용합니다:
- Hex `#140226` (RGB 20, 2, 38) → Unity `new Color(0.08f, 0.01f, 0.15f, 1f)`
- Hex `#260540` (RGB 38, 5, 64) → Unity `new Color(0.15f, 0.02f, 0.25f, 1f)`
- Hex `#FF00CC` (RGB 255, 0, 204) → Unity `new Color(1f, 0f, 0.8f, 1f)`

변환 공식: `UnityValue = HexValue / 255.0`

## 완료된 작업

### 코드 구현 상태 (검증 완료)
- [x] **LaneBackground 텍스처 생성** (`GameplayController.cs` 라인 1082-1148)
  - Deep Purple 그라데이션 (상단 #140226 → 하단 #260540)
  - Magenta 그리드 (alpha 0.04, 매우 투명)
  - 노이즈 효과 추가 (디지털 느낌)
  - 512x512 텍스처, 64px 그리드 간격

- [x] **NoteVisuals 색상 시스템** (`NoteVisuals.cs` 라인 30-40)
  - Lane 0: Cyan (#00FFFF)
  - Lane 1: Magenta (#FF00FF)
  - Lane 2: Yellow (#FFFF00)
  - Lane 3: White (#FFFFFF)
  - MaterialPropertyBlock 사용 (인스턴스별 색상)

- [x] **Sensational Redesign 적용**
  - `NoteVisuals.cs` 고강도 네온 컬러 적용 (intensity 1.2f)
  - `GameplayController.cs` 배경 로드 경로 `Background_Sensation`으로 업데이트
  - 상단 HUD 및 판정 바 디자인 유지 및 최적화

- [x] **NanoBanana 에셋 통합**
  - `Assets/Textures/Generated` -> `Assets/Resources/Skins/NanoBanana` 이동
  - `NoteSpawner.cs`에서 `TapNote`, `LongNoteBody`, `ScratchNote` 텍스처 로드 구현
  - `GameplayUI.cs`의 중복 UI 배경 비활성화 (3D 배경 가리는 문제 해결)

- [x] **Z-ordering 수정** (`NoteSpawner.cs` 라인 508)
  - 노트 Z 위치: **2** (배경 앞)
  - 배경 Z 위치: **1** (노트 뒤)
  - 카메라 Z 위치: **-10** (모두 관찰)

- [x] **카메라 설정** (`GameplayController.cs` 라인 87-95)
  - orthographicSize: **7** (범위 Y -1 ~ 13)
  - 카메라 Y 위치: **6** (판정선 0과 스폰점 12의 중간)
  - 4레인 전체가 화면 너비에 꽉 차도록 자동 조정

- [x] **UIColorPalette 시스템** (`UIColorPalette.cs`)
  - 87줄 분량의 완전한 Cyberpunk 색상 팔레트
  - Neon 계열: Cyan/Magenta/Yellow/Green/Blue/Purple/Gold
  - 배경: Deep Black → Dark Violet 계층
  - 판정 색상, 콤보 색상, 버튼 상태 등 모두 정의됨

### 검증 완료 항목
- ✅ NanoBanana 텍스처 로드 및 적용 확인
- ✅ 레인별 네온 컬러 선명도 향상 확인 (NoteVisuals intensity 1.2f 적용)
- ✅ UI 배경 비활성화를 통한 3D 배경 시인성 확보
- ❌ **이슈**: AI 이미지 생성 서버 용량 초과로 인해 `Background_Sensation.png`가 현재 기존 그리드 파일 복사본(임시)으로 되어 있음.
- 💡 **해결**: 서버 안정화 후 재시도하거나, 사용자가 직접 화려한 이미지를 해당 경로에 덮어쓰기 권장.
- ✅ `GameplayController.cs` 코드 실제 적용됨 (system-reminder로 확인)
- ✅ `NoteSpawner.cs` noteZ = 2f 적용됨
- ✅ `NoteVisuals.cs` GetLaneColor() 메서드 존재 및 색상 정의 확인
- ✅ `UIColorPalette.cs` 완전한 색상 시스템 구축됨
- ✅ **Unity MCP 실시간 검증 완료 (2026-02-10 18:29)**
  - LaneBackground: position (0,1,1), scale (4.5,15,1), isVisible=true
  - Main Camera: position (0.5,6,-10), orthographicSize=7, orthographic=true
  - 콘솔 로그: "[GameplayController] NanoBanana background asset applied"
  - 노트 스폰: Y=12, Z=2, scale=(0.80,0.30,1.00) 정상
  - 게임 루프: 노트 스폰/이동/판정 모두 정상 작동

### 절차적 에셋 생성 및 비주얼 개선 (2026-02-15)
- [x] **절차적 에셋 생성 시스템 구축** (`AssetGenTrigger.cs`, `ProceduralImageGenerator.cs`)
  - AI 이미지 생성 실패 대안으로 C# 코드로 텍스처 직접 생성 구현
  - **생성 항목**: 노트(Tap, Long Head, Scratch), 롱노트 바디, 판정 이펙트(Perfect~Bad 시트), 사이버펑크 배경
  - **자동화**: 유니티 에디터 Play 시 `Assets/Resources/AIBeat_Design` 폴더에 자동 생성 및 저장

- [x] **노트 비주얼 고도화** (`Note.cs`, `NoteSpawner.cs`)
  - **SpriteRenderer 도입**: 기존 Quad 메쉬 제거 및 스프라이트 기반 렌더링 전환
  - **롱노트 구조 변경**: Head(머리) + Body(몸통) 분리 렌더링. 몸통은 길이에 맞춰 늘어나고 머리는 비율 유지.
  - **색상 동기화**: `NoteVisuals.cs`를 수정하여 롱노트 몸통까지 레인 색상(네온) 적용

- [x] **판정 이펙트 구현** (`JudgementEffectController.cs`, `GameplayUI.cs`)
  - **스프라이트 애니메이션**: 4x4 스프라이트 시트를 로드하여 판정 시 폭발/이펙트 재생
  - **통합**: `GameplayUI`에서 판정 발생 시 해당 레인/중앙에 이펙트 스폰 연결

- [x] **에셋 패키징** (`PackageDesignAssets.ps1`)
  - 생성된 디자인 에셋을 1초 만에 zip으로 압축하는 PowerShell 스크립트 작성

### UI 디자인 및 에셋 생성 (2026-02-15) - 재미나이 요청
- [x] **UI 에셋 절차적 생성 구현** (`ProceduralImageGenerator.cs` 확장)
  - **배경**: 스플래시(Purple/Black), 메인메뉴(Blue/Cyber), 곡선택(Dark/Contrast) 화면용 그라데이션 배경 자동 생성
  - **버튼**: Normal(Dark Blue), Hover(Glow Cyan), Pressed(Teal) 상태별 텍스처 생성
  - **로고**: "A.I. BEAT" 분위기의 절차적 웨이브폼 로고 생성

- [x] **디자인 명세서 작성** (`UI_DESIGN_SPEC.md`)
  - [UI_DESIGN_SPEC.md](file:///C:/Users/wsw18/.gemini/antigravity/brain/fd660b48-2deb-4486-bab0-2b263b036f69/UI_DESIGN_SPEC.md) 생성
  - 색상 팔레트 (Neon Cyan #00FFFF, Magenta #FF00FF 등) 정의
  - 화면별(스플래시, 메뉴, 곡선택) 레이아웃 및 에셋 사용 가이드 정리
  - 생성된 에셋 파일 경로: `Assets/Resources/AIBeat_Design/UI/`



---

## 재미나이 작업 흐름
1. ✅ 디자인 컨셉 결정 (Cyberpunk Neon 스타일) → **완료**
2. ✅ 기본 색상 코드 적용 (Deep Purple + Magenta) → **완료**
3. ✅ Claude가 코드에 반영 → **완료**
4. ✅ Unity MCP로 실시간 검증 → **완료** (NanoBanana 텍스처 정상 적용)
5. ⏳ 스크린샷 확인 및 피드백 (필요 시 조정)

### ✅ 해결 완료: NanoBanana 텍스처 로드 문제 (2026-02-10)

**문제:**
- `Resources.Load<Texture2D>("Skins/NanoBanana/Background")` 가 null 반환
- 파일은 존재하지만 Unity 에셋 데이터베이스가 인식하지 못함
- 콘솔에 성공/실패 로그가 전혀 나타나지 않음

**해결:**
```bash
# Unity MCP로 Assets/Refresh 실행
mcp__mcp-unity__execute_menu_item("Assets/Refresh")
```

**결과:**
- ✅ 에셋 데이터베이스 재구축 완료
- ✅ 텍스처 로드 성공: `"[GameplayController] NanoBanana background asset applied"`
- ✅ LaneBackground Material에 텍스처 정상 적용
- ✅ 게임 실행 시 배경 제대로 표시됨

**교훈:**
- Unity에서 새 파일 추가 시 **Assets/Refresh** 필수
- 특히 Resources 폴더의 에셋은 반드시 Import 완료되어야 Resources.Load 가능
- MCP `get_gameobject`로는 Material의 Texture 속성이 직렬화되지 않아 확인 불가
- 콘솔 로그가 유일한 확실한 검증 수단

---

## Archive Rule
- 완료 항목이 20개를 넘거나 파일이 5KB를 넘으면,
  완료된 내용을 `ARCHIVE_YYYY_MM.md`로 옮기고 PROGRESS는 "현재 이슈"만 남긴다.
