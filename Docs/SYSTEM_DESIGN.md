# A.I. BEAT: Infinite Mix - 시스템 설계서

## 1. 시스템 개요

```
┌─────────────────────────────────────────────────────────────────┐
│                        GAME FLOW                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [Main Menu] → [Prompt Input] → [AI Generate] → [Gameplay]      │
│       ↑              ↓               ↓              ↓           │
│       └──────── [Song Library] ←─ [Publish] ←── [Result]        │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. 핵심 시스템 상세

### 2.1 AudioAnalyzer (오디오 분석 시스템)

**목적**: 음악의 BPM, 주파수 스펙트럼을 분석하여 노트 생성 데이터 제공

```csharp
public class AudioAnalyzer : MonoBehaviour
{
    // 스펙트럼 분석 설정
    private const int SPECTRUM_SIZE = 1024;
    private const int FREQUENCY_BANDS = 8;

    // 주파수 밴드 (Hz)
    // Band 0: 20-60    (Sub Bass)    → 스크래치
    // Band 1: 60-250   (Bass)        → 키 1, 5
    // Band 2: 250-500  (Low Mid)     → 키 2, 4
    // Band 3: 500-2k   (Mid)         → 키 3 (중앙)
    // Band 4: 2k-4k    (High Mid)    → 키 2, 4
    // Band 5: 4k-6k    (Presence)    → 키 1, 5
    // Band 6: 6k-12k   (Brilliance)  → 스크래치
    // Band 7: 12k-20k  (Air)         → 이펙트용

    public float[] GetFrequencyBands();
    public float GetCurrentBeatStrength();
    public bool IsOnBeat(float threshold);
}
```

**노트 매핑 로직**:
```
1. BPM 그리드 생성 (예: 140 BPM = 0.428초 간격)
2. 각 비트에서 스펙트럼 분석
3. 가장 강한 주파수 밴드 → 해당 키에 노트 배치
4. 강도가 임계값 이상이면 노트 생성
```

---

### 2.2 NoteSpawner (노트 생성 시스템)

**목적**: 미리 계산된 노트 데이터를 기반으로 노트 오브젝트 생성

```csharp
public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] notePrefabs; // 키별 노트 프리팹
    [SerializeField] private Transform[] spawnPoints;  // 7개 레인
    [SerializeField] private float noteSpeed = 5f;

    private Queue<NoteData> noteQueue;
    private ObjectPool<Note> notePool;

    // 노트 데이터 구조
    public struct NoteData
    {
        public float spawnTime;    // 생성 시간 (곡 시작 기준)
        public int laneIndex;      // 0-6 (스크래치L, 1-5, 스크래치R)
        public NoteType type;      // Tap, Long, Scratch
        public float duration;     // 롱노트 지속시간
    }
}
```

**오브젝트 풀링**:
- 최대 동시 노트 수: 50개
- 풀 크기: 100개 (버퍼 포함)
- 재사용으로 GC 최소화

---

### 2.3 JudgementSystem (판정 시스템)

**목적**: 입력 타이밍과 노트 타이밍을 비교하여 판정

```csharp
public class JudgementSystem : MonoBehaviour
{
    // 판정 타이밍 (초 단위)
    private const float PERFECT_WINDOW = 0.030f;  // ±30ms
    private const float GREAT_WINDOW = 0.060f;    // ±60ms
    private const float GOOD_WINDOW = 0.100f;     // ±100ms
    private const float BAD_WINDOW = 0.150f;      // ±150ms

    public enum JudgementResult
    {
        Perfect,
        Great,
        Good,
        Bad,
        Miss
    }

    public JudgementResult Judge(float inputTime, float noteTime)
    {
        float diff = Mathf.Abs(inputTime - noteTime);

        if (diff <= PERFECT_WINDOW) return JudgementResult.Perfect;
        if (diff <= GREAT_WINDOW) return JudgementResult.Great;
        if (diff <= GOOD_WINDOW) return JudgementResult.Good;
        if (diff <= BAD_WINDOW) return JudgementResult.Bad;
        return JudgementResult.Miss;
    }
}
```

**점수 계산**:
```
기본 점수 = 1,000,000 / 총 노트 수
PERFECT: 100% × 기본 점수 × 콤보 보너스
GREAT:   80% × 기본 점수 × 콤보 보너스
GOOD:    50% × 기본 점수 × 콤보 보너스
BAD:     20% × 기본 점수 (콤보 리셋)
MISS:    0점 (콤보 리셋)

콤보 보너스: 1.0 + (콤보 / 100) × 0.1 (최대 1.5)
```

---

### 2.4 InputHandler (입력 처리 시스템)

**목적**: 터치/키보드 입력을 추상화하여 게임에 전달

```csharp
public class InputHandler : MonoBehaviour
{
    // 터치 영역 (화면 비율)
    // |  스크래치  |  1  |  2  |  3  |  4  |  5  |  스크래치  |
    // |   15%     | 14% | 14% | 14% | 14% | 14% |   15%     |

    public event Action<int, InputType> OnLaneInput;

    public enum InputType
    {
        Down,      // 터치 시작
        Hold,      // 터치 유지
        Up,        // 터치 종료
        Scratch    // 스크래치 (드래그)
    }

    // 스크래치 판정
    // - 터치 시작 후 세로 드래그 거리 > 50px
    // - 드래그 방향에 따라 상/하 스크래치 구분
}
```

**키보드 매핑 (디버그용)**:
```
S/D: 왼쪽 스크래치 상/하
F/G/H/J/K: 키 1-5
L/;: 오른쪽 스크래치 상/하
```

---

### 2.5 노트 생성 알고리즘

**Phase 1: BPM 그리드 생성**
```python
# 의사 코드
bpm = 140
beat_interval = 60.0 / bpm  # 0.428초

beats = []
for time in range(0, song_duration, beat_interval):
    beats.append(time)
```

**Phase 2: 구간별 난이도 조절**
```
Intro (0-15초):
  - 노트 밀도: 25%
  - 동시타: 없음
  - 롱노트: 없음

Build (15-45초):
  - 노트 밀도: 50%
  - 동시타: 2키까지
  - 롱노트: 가끔

Drop (45-90초):
  - 노트 밀도: 100%
  - 동시타: 3키까지
  - 롱노트: 자주
  - 스크래치: 활성화

Outro (90-120초):
  - 노트 밀도: 50% → 25%
  - 점점 감소
```

**Phase 3: 주파수 기반 레인 할당**
```csharp
int GetLaneFromFrequency(float[] bands)
{
    int maxBand = GetMaxBandIndex(bands);

    switch (maxBand)
    {
        case 0: case 7: // Sub Bass, Air
            return Random.Range(0, 2) == 0 ? 0 : 6; // 스크래치
        case 1: case 5: // Bass, Presence
            return Random.Range(0, 2) == 0 ? 1 : 5; // 외곽 키
        case 2: case 4: // Low Mid, High Mid
            return Random.Range(0, 2) == 0 ? 2 : 4; // 중간 키
        case 3: // Mid
            return 3; // 중앙 키
        default:
            return 3;
    }
}
```

---

## 3. 씬 구조

### 3.1 MainMenu
```
Canvas
├── TitleText ("A.I. BEAT")
├── ButtonGroup
│   ├── PlayButton → SongSelect
│   ├── LibraryButton → Library
│   └── SettingsButton → Settings
└── VersionText
```

### 3.2 SongSelect (프롬프트 입력)
```
Canvas
├── Header
│   ├── BackButton
│   └── TitleText ("CREATE YOUR BEAT")
├── PromptPanel
│   ├── GenreButtons (EDM, House, Cyberpunk...)
│   ├── TempoSlider (80-180 BPM)
│   └── MoodButtons (Aggressive, Chill, Epic...)
├── PreviewPanel
│   ├── WaveformVisualizer
│   └── GenerateButton
└── EnergyDisplay (남은 생성 횟수)
```

### 3.3 Gameplay
```
Canvas (Overlay)
├── ScoreText
├── ComboText
├── JudgementText (PERFECT!/GREAT!...)
└── PauseButton

GameplayRoot
├── NoteLanes (7개)
│   ├── Lane0_ScratchL
│   ├── Lane1-5_Keys
│   └── Lane6_ScratchR
├── JudgementLine
├── NoteContainer
└── EffectContainer

AudioManager
├── BGM
└── SFX (히트사운드)
```

### 3.4 Result
```
Canvas
├── Header ("RESULT")
├── ScorePanel
│   ├── TotalScore
│   ├── MaxCombo
│   ├── JudgementCounts (PERFECT/GREAT/GOOD/BAD/MISS)
│   └── Accuracy (%)
├── RankDisplay (S/A/B/C/D)
├── ButtonGroup
│   ├── RetryButton
│   ├── PublishButton (서버에 저장)
│   └── BackButton
└── SongInfo
```

---

## 4. 네트워크 설계

### 4.1 AI API 통신

**요청 흐름**:
```
1. [Client] POST /api/generate
   Body: { genre, bpm, mood, duration }

2. [Server] 202 Accepted
   Body: { taskId: "xxx" }

3. [Client] GET /api/status/{taskId} (폴링, 3초 간격)

4. [Server] 200 OK (완료 시)
   Body: { status: "completed", songUrl: "...", sections: [...] }
```

**타임아웃 처리**:
- 생성 대기: 최대 60초
- 60초 초과 시 "생성 실패" 메시지 + 재시도 버튼

### 4.2 곡 저장/공유

**저장 구조** (Firebase Firestore):
```json
{
  "songs": {
    "song_abc123": {
      "title": "Cyberpunk Rage",
      "creator": "user_xyz",
      "prompt": { "genre": "Cyberpunk", "bpm": 140, "mood": "Aggressive" },
      "audioUrl": "https://...",
      "noteData": "https://... (JSON)",
      "plays": 1234,
      "likes": 567,
      "createdAt": "2026-02-02T12:00:00Z"
    }
  }
}
```

---

## 5. 최적화 전략

### 5.1 메모리 최적화
- 노트 오브젝트 풀링 (100개)
- 오디오 스트리밍 (전체 로드 X)
- 텍스처 아틀라스 사용

### 5.2 CPU 최적화
- 스펙트럼 분석: 매 프레임 X → 50ms 간격
- 판정 계산: O(1) 해시맵 사용
- Update() 대신 FixedUpdate() (판정 정확도)

### 5.3 렌더링 최적화
- 노트: Sprite 배칭
- 이펙트: 파티클 풀링
- UI: Canvas 분리 (Static/Dynamic)

---

## 6. 에러 처리

| 상황 | 처리 |
|------|------|
| 네트워크 끊김 | 로컬 캐시 곡 재생 유도 |
| AI 생성 실패 | 재시도 버튼 + 에러 메시지 |
| 오디오 로드 실패 | 대체 곡 제안 |
| 판정 지연 | 오프셋 자동 조정 제안 |

---

## 7. 테스트 계획

### 7.1 Unit Test
- `JudgementSystem.Judge()`: 모든 판정 케이스
- `ScoreCalculator`: 점수 계산 정확도
- `AudioAnalyzer.GetFrequencyBands()`: 주파수 분리 정확도

### 7.2 Integration Test
- 노트 생성 → 판정 → 점수 반영 플로우
- 네트워크 요청 → 응답 → 곡 재생 플로우

### 7.3 Performance Test
- 60fps 유지 (최대 50노트 동시 표시)
- 메모리 512MB 이하
- 판정 오차 ±1ms 이내
