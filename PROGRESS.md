# PROGRESS.md - 게임플레이 화면 디자인 요청

## 현재 상황
- 게임 기능은 모두 정상 작동 (노트 스폰, 판정, 점수 계산 등)
- 게임플레이 화면의 비주얼 디자인이 필요함

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
- ⚠️ **노트는 Z=2, 배경은 Z=1** → 배경이 불투명해도 노트가 앞에 보임
- ⚠️ **그리드는 투명도 필수** (alpha 0.04 권장) → 너무 밝으면 노트 가림
- ⚠️ **모바일 세로 모드** → 좁은 화면, 4개 레인이 화면 너비 전체 사용
- ✅ **런타임 텍스처 생성** → 코드로 실시간 생성 (이미지 파일 불필요)

---

## 재미나이 작업 흐름
1. 디자인 컨셉 결정 (색상, 스타일, 분위기)
2. 색상 코드 제공 (RGB 또는 Hex)
3. Claude가 코드에 반영
4. Unity에서 실행하여 스크린샷 확인
5. 피드백 후 조정

---

## Archive Rule
- 완료 항목이 20개를 넘거나 파일이 5KB를 넘으면,
  완료된 내용을 `ARCHIVE_YYYY_MM.md`로 옮기고 PROGRESS는 "현재 이슈"만 남긴다.
