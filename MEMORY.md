# MEMORY.md - A.I. BEAT: Infinite Mix (SSOT)

> 프로젝트의 단일 진실 원천(Single Source of Truth). 모든 규칙, 기술 스택, 아키텍처를 이 파일에서 관리한다.
> 최종 갱신: 2026-02-07 (세션 29 기준)

---

## 1) 프로젝트 개요

| 항목 | 내용 |
|------|------|
| **이름** | A.I. BEAT: Infinite Mix |
| **장르** | AI 생성 음악 기반 리듬게임 (비트매니아 스타일) |
| **엔진** | Unity 6000.3.2f1 (Unity 6 LTS 계열) |
| **대상 플랫폼** | Android (모바일 우선), PC (에디터 테스트용) |
| **핵심 컨셉** | 프롬프트 입력 -> AI 음악 생성 -> 자동 노트 매핑 -> 리듬게임 플레이 |
| **비주얼 테마** | Cyberpunk / Neon (DeepPurple, NeonCyan, NeonMagenta) |

### Non-goals
- 온라인 멀티플레이어 (현재 단계에서 제외)
- iOS 빌드 (Android 우선)
- 실시간 AI 음악 스트리밍 (사전 생성 후 재생)

---

## 2) 기술 스택

| 카테고리 | 기술 |
|----------|------|
| **엔진** | Unity 6000.3.2f1 |
| **언어** | C# (.NET Standard 2.1) |
| **렌더링** | URP (Universal Render Pipeline) 가정, 프로시저럴 비주얼 중심 |
| **UI** | Unity UI (Canvas) + TextMeshPro |
| **오디오** | Unity AudioSource, UnityWebRequest (MP3/OGG/WAV 로드) |
| **네트워크** | UnityWebRequest (AI API 통신용) |
| **씬 관리** | SceneManager (비동기 로드 + 커스텀 Fade 전환) |
| **상태 관리** | 싱글톤 패턴 (GameManager, AudioManager) + DontDestroyOnLoad |
| **애니메이션** | TweenHelper (커스텀 코루틴 기반, 외부 라이브러리 없음) |
| **파티클** | 프로시저럴 Quad 기반 (ParticleSystem 미사용) |

---

## 3) 프로젝트 경로 구조

```
h:\Claude_work\bit\                    # 작업 루트
  CLAUDE.md                            # 라우터 (작업 순서 안내)
  MEMORY.md                            # 이 파일 (SSOT)
  PROGRESS.md                          # 현재 진행 상황
  .commit_message.txt                  # 커밋 메시지 (자동 갱신)
  AI_BEAT\                             # Unity 프로젝트 (Assets 전용)
    Assets\
      Scripts\
        Core\                          # 게임 전역 매니저
        Gameplay\                      # 게임플레이 로직
        Audio\                         # 오디오 분석/비트매핑
        Network\                       # AI API 통신, 곡 생성
        Data\                          # 데이터 모델 (NoteData, SongData)
        UI\                            # UI 컨트롤러
        Utils\                         # 유틸리티 (TweenHelper, TextureGenerator)
        Editor\                        # 에디터 확장 도구
      Scenes\                          # 씬 파일 (MainMenu, SongSelect, Gameplay)
      Resources\                       # 런타임 로드 리소스
      StreamingAssets\                  # 외부 오디오 파일
  bit\                                 # Unity 프로젝트 (전체, Library 포함)
    ProjectSettings\                   # Unity 프로젝트 설정
    Library\                           # Unity 캐시 (빌드 시 사용)
```

---

## 4) 네임스페이스별 역할

### `AIBeat.Core` - 게임 전역 매니저

| 클래스 | 역할 |
|--------|------|
| **GameManager** | 싱글톤. 게임 상태 머신 (MainMenu/SongSelect/Loading/Gameplay/Paused/Result). 씬 전환, 일시정지/재개. `CurrentSongData` 보유. `targetFrameRate=60`. |
| **AudioManager** | 싱글톤. BGM/SFX 재생 관리. 디버그 모드(오디오 없이 시간 진행) 지원. URL/Resources/StreamingAssets에서 AudioClip 로드. 판정별 히트 사운드(Perfect/Great/Good/Bad) + 스크래치 사운드. `JudgementResult` enum 정의. |

### `AIBeat.Data` - 데이터 모델

| 클래스/구조체 | 역할 |
|--------------|------|
| **NoteData** | 개별 노트: `HitTime`(초), `LaneIndex`(0~3), `NoteType`(Tap/Long/Scratch), `Duration` |
| **NoteType** | enum: Tap, Long, Scratch |
| **SongData** | 곡 전체 데이터: Id, Title, Artist, BPM, Duration, AudioUrl, Sections, Notes, Difficulty(1-10), Genre, Mood |
| **SongSection** | 구간: Name(intro/build/drop/outro), StartTime, EndTime, DensityMultiplier |
| **PromptOptions** | 프롬프트 입력: Genre, BPM, Mood, Duration, Structure. `Genres[]` 및 `Moods[]` 상수 배열 포함 |

### `AIBeat.Gameplay` - 게임플레이 로직

| 클래스 | 역할 |
|--------|------|
| **GameplayController** | 게임플레이 씬의 총 지휘자. 초기화/카운트다운/노트히트/일시정지/결과 표시. AutoPlay 모드 지원. 오디오 분석 후 노트 생성 오케스트레이션. |
| **NoteSpawner** | 노트 생성/관리. 7레인 레이아웃 기반 노트 Quad 생성. 판정선(JudgementLine) 기준 스폰. 오브젝트 풀링으로 성능 최적화. `GetNearestNote()` 제공(홀딩 노트 우선). |
| **Note** | 개별 노트 MonoBehaviour. Quad 메쉬 기반. 하강 이동/판정/삭제 라이프사이클. 롱노트 홀드 상태 관리(`isHolding` 가드). 만료 시간: `HitTime + Duration + 0.5s`. |
| **InputHandler** | 입력 처리. 터치(모바일) + 키보드(PC) 이중 지원. 7레인 키보드 매핑. 터치존 분할. 스크래치 가장자리 감지(`scratchEdgeRatio` 12%). `IsScratchOnly` 판정. |
| **JudgementSystem** | 판정 로직. 시간 차이 기반 Perfect/Great/Good/Bad/Miss 판정. 콤보/스코어/정확도 계산. 판정별 카운트 추적(`PerfectCount`~`MissCount`). `OnJudgementDetailed` 이벤트(rawDiff: Early/Late). |
| **LaneVisualFeedback** | 레인 시각적 피드백. 네온 스타일 히트존/글로우/키라벨. 입력 시 플래시 효과. 롱노트 홀드 중 하이라이트. IIDX 스타일 7레인 배경 렌더링. |
| **JudgementEffect** | 판정선 이펙트. 레인별 확대+페이드 Quad 애니메이션. 판정 결과별 색상(노랑/연두/시안/주황/회색). |
| **HitParticleEffect** | 히트 파티클. Quad 기반 프로시저럴 파티클 풀(60개). 판정별 개수/속도/크기/색상 차등. 부채꼴 확산 + 중력. |

### `AIBeat.Audio` - 오디오 분석

| 클래스 | 역할 |
|--------|------|
| **AudioAnalyzer** | 실시간 FFT 분석. `GetSpectrumData()` 기반 8밴드 주파수 분석. 실시간 비트 감지. `MapBandToLane()` 4레인 매핑. |
| **OfflineAudioAnalyzer** | PCM 오프라인 분석 엔진 (static). Cooley-Tukey Radix-2 FFT(2048크기, 512hop). Hann 윈도우. Spectral Flux 온셋 감지(적응형 임계값). IOI 히스토그램 BPM 추출(60~200범위, 옥타브 보정). 에너지 프로필 구간 감지(intro/build/drop/outro/calm). `AnalyzeAsync` 비동기 버전(FRAMES_PER_CHUNK=64). |
| **BeatMapper** | BPM+구간 기반 노트 자동 생성. `System.Random(seed)` 결정론적 시드. 구간별 밀도 차등 배치. `CreateDefaultSections()` 기본 구간 생성. `GenerateNotesFromBPM()` 메서드. |
| **SmartBeatMapper** | 오프라인 분석 결과 기반 고급 노트 매핑. 온셋 기반 노트 배치. 주파수 밴드별 레인 매핑. 난이도/구간별 밀도 동적 조절. 롱노트/스크래치 자동 배치. |

### `AIBeat.Network` - 곡 생성기

| 클래스 | 역할 |
|--------|------|
| **FakeSongGenerator** | MVP용 가짜 곡 생성기. 사전 정의된 4개 데모곡. 프롬프트 매칭(장르/BPM/분위기 점수화). BeatMapper로 노트 생성. 이벤트: `OnGenerationProgress`/`OnGenerationComplete`/`OnGenerationError`. |
| **AIApiClient** | 실제 AI API 통신 클라이언트. REST POST 요청 -> 폴링 상태 확인 -> 결과 수신. API 키 안전 관리(하드코딩 금지). 최대 대기 60초. 동일 이벤트 시그니처. |

### `AIBeat.UI` - UI 컨트롤러

| 클래스 | 역할 |
|--------|------|
| **MainMenuUI** | 메인 메뉴. Play/Library/Settings/Exit 버튼. 네온 인트로 시퀀스(타이틀 확대, 서브타이틀 페이드인, 버튼 순차 등장). 타이틀 글로우 펄스. |
| **SongSelectUI** | 곡 선택 UI. 장르/분위기 버튼 동적 생성. BPM 슬라이더(80~180). 에너지 시스템. 로딩 진행률 표시. FakeSongGenerator 연동. `useApiClient` 토글. |
| **GameplayUI** | 게임플레이 HUD. 스코어(상단 중앙)/콤보(화면 중앙)/판정 텍스트. 상단 우측 판정별 통계 HUD 동적 생성. 카운트다운/일시정지/결과 패널. 콤보 10단위 스케일 팝업. |
| **SceneTransitionManager** | 씬 전환 페이드 효과. Quad 기반 풀스크린 오버레이. SmoothStep 알파 보간(0.4초). DontDestroyOnLoad 싱글톤. |

### `AIBeat.Utils` - 유틸리티

| 클래스 | 역할 |
|--------|------|
| **TweenHelper** | LeanTween 대체 코루틴 트윈. ScaleTo/MoveTo/FadeTo. Ease 타입(OutBack/InQuad/Linear 등). |
| **TextureGenerator** | 프로시저럴 텍스처 생성. 배경(Deep Purple Gradient + Grid), 노트, 판정선, 구분선 텍스처. Cyberpunk 컬러 팔레트 상수(DeepPurple/NeonCyan/NeonMagenta/NeonBlue/DarkBlue). |
| **UIAnimator** | GameplayUI에서 사용하는 간단한 스케일 애니메이션 유틸. |

### `AIBeat.Editor` - 에디터 도구

| 클래스 | 역할 |
|--------|------|
| **GameSetupEditor** | `Tools/A.I. BEAT/` 메뉴. Setup Visuals (배경/구분선/판정선 자동 설정). Play Game 단축키. |
| **ScreenCapture** | 에디터 스크린샷 캡처 도구. |

---

## 5) 레인 구조 (4레인 시스템)

> 세션 29에서 7레인 -> 4레인으로 축소 완료 (NoteData.LaneIndex 범위: 0~3)

| 인덱스 | 역할 | 키보드 키 | 터치 영역 |
|--------|------|----------|----------|
| 0 | ScratchL (왼쪽 스크래치) | S | 화면 왼쪽 가장자리 12% |
| 1 | Key1 (왼쪽 키) | D, F | 화면 왼쪽 중앙 |
| 2 | Key2 (오른쪽 키) | J, K | 화면 오른쪽 중앙 |
| 3 | ScratchR (오른쪽 스크래치) | L | 화면 오른쪽 가장자리 12% |

### 시각적 레이아웃 (7레인 렌더링)

실제 게임플레이는 4레인이지만, 시각적으로는 7레인 IIDX 스타일 레이아웃을 유지한다.
`LaneVisualFeedback`에서 7레인 배경/히트존/키라벨을 생성한다.

| 시각적 레인 | 0 | 1 | 2 | 3 | 4 | 5 | 6 |
|------------|---|---|---|---|---|---|---|
| 색상 | 빨강 | 보라 | 파랑 | 보라 | 파랑 | 보라 | 빨강 |
| 키보드 | S | D | F | SP | J | K | L |
| 타입 | Scratch L | White | Blue | White(Center) | Blue | White | Scratch R |

### 입력 우선순위
- 터치 가장자리 12% (`scratchEdgeRatio`) -> Scratch 전용 (키 Down 미발생)
- 나머지 영역 -> Key 판정
- 스크래치 vs 키 이중 트리거 방지됨 (세션 29 TODO-3)

---

## 6) 판정 시스템

### 판정 윈도우 (JudgementSystem)

| 판정 | 점수 배율 | 콤보 |
|------|----------|------|
| **Perfect** | 최고 | 유지 |
| **Great** | 높음 | 유지 |
| **Good** | 보통 | 유지 |
| **Bad** | 낮음 | 리셋 |
| **Miss** | 0 | 리셋 |

> 정확한 ms 윈도우 값은 JudgementSystem.cs 내 상수로 정의됨
> `OnJudgementDetailed` 이벤트: `rawDiff` (양수=Late, 음수=Early) 제공

### 이펙트 색상 매핑

| 판정 | 색상 | HEX 근사 |
|------|------|----------|
| Perfect | 노랑 (NeonYellow) | #FFFF00 |
| Great | 연두 (NeonGreen) | #33FF66 |
| Good | 시안 (NeonCyan) | #00FFFF |
| Bad | 주황 (NeonOrange) | #FF8019 |
| Miss | 회색 | #808080 |

### 스코어/결과 데이터 (GameResult)
- Score, MaxCombo, Accuracy(%), Rank(S+/S/A/B/C/D)
- PerfectCount, GreatCount, GoodCount, BadCount, MissCount

---

## 7) 에너지 시스템

| 항목 | 값 |
|------|---|
| **최대 에너지** | 3 |
| **소모** | 곡 생성 1회당 1 |
| **충전** | 시간 기반 자동 리차지 (`RechargeEnergyFromTime()`) |
| **에러 복구** | 생성 실패 시 에너지 자동 반환 |
| **저장** | PlayerPrefs("Energy") |
| **부족 시** | 다이얼로그 표시 (광고 시청 / 유료 결제 예정) |

---

## 8) ISongGenerator 패턴 및 데이터 흐름

### ISongGenerator 패턴

```
ISongGenerator (인터페이스, 계획)
  |-- FakeSongGenerator   : MVP 테스트용 (내장 데모곡 매칭)
  |-- AIApiClient         : 실제 AI API 통신
```

- `SongSelectUI`의 `useApiClient` 토글로 전환
- 두 구현체 모두 동일 이벤트 시그니처: `OnGenerationProgress` / `OnGenerationComplete` / `OnGenerationError`

### 전체 데이터 흐름

```
[SongSelect 씬]
  사용자 프롬프트 입력 (Genre, BPM, Mood)
    -> 에너지 소모 (currentEnergy--)
    -> ISongGenerator.GenerateSong(PromptOptions)
    -> 진행률 콜백 (UI 로딩 바 + 단계별 텍스트)
    -> SongData 생성 완료
    -> GameManager.StartGame(songData)
    -> "Gameplay" 씬 로드 (SceneTransitionManager Fade 전환)

[Gameplay 씬]
  GameplayController 초기화
    -> AudioManager에서 오디오 로드
       (URL/Resources/StreamingAssets/DebugMode 중 선택)
    -> OfflineAudioAnalyzer.Analyze(clip) 또는 AnalyzeAsync(비동기)
    -> SmartBeatMapper.GenerateNotes(analysisResult) 또는 BeatMapper
    -> NoteSpawner에 노트 데이터 전달
    -> 카운트다운 (3-2-1-GO)
    -> 게임 루프 시작
      -> NoteSpawner: 노트 Quad 하강 (상단 -> 판정선)
      -> InputHandler: 터치/키보드 입력 감지 (레인별)
      -> JudgementSystem: 시간차 판정 + 스코어/콤보 갱신
      -> GameplayUI: HUD 업데이트 (스코어/콤보/판정/통계)
      -> JudgementEffect: 판정선 확대+페이드
      -> HitParticleEffect: 판정별 파티클 분출
      -> LaneVisualFeedback: 레인 플래시/하이라이트
    -> 곡 종료 (AudioManager.OnBGMEnded)
    -> 결과 화면 (GameplayUI.ShowResult)
```

---

## 9) 씬 구조

| 씬 이름 | 역할 | 핵심 컴포넌트 |
|---------|------|-------------|
| **MainMenu** | 게임 진입점 | MainMenuUI, GameManager(DontDestroyOnLoad 초기화) |
| **SongSelect** | 프롬프트 입력 + 곡 생성 | SongSelectUI, FakeSongGenerator, BeatMapper |
| **Gameplay** | 리듬게임 플레이 | GameplayController, NoteSpawner, InputHandler, JudgementSystem, GameplayUI, LaneVisualFeedback, AudioAnalyzer |

### 씬 전환 흐름
```
MainMenu --(Play)--> SongSelect --(Generate)--> [Loading] --> Gameplay
Gameplay --(Retry)--> Gameplay
Gameplay --(Menu)---> MainMenu
MainMenu --(Library)--> Library (미구현)
```
모든 전환은 `SceneTransitionManager`의 Fade 효과(0.4초 SmoothStep)를 거친다.

---

## 10) 오디오 분석 상세

### OfflineAudioAnalyzer 설정

| 상수 | 값 | 설명 |
|------|---|------|
| FFT_SIZE | 2048 | FFT 윈도우 크기 |
| HOP_SIZE | 512 | FFT 홉 크기 (프레임 간격) |
| BAND_COUNT | 8 | 주파수 밴드 수 |
| MIN_BPM | 60 | BPM 탐색 하한 |
| MAX_BPM | 200 | BPM 탐색 상한 |
| ONSET_THRESHOLD_MULTIPLIER | 1.5 | 온셋 감지 임계값 배율 |
| ONSET_MEDIAN_WINDOW | 7 | 적응형 임계값 중간값 윈도우 |
| FRAMES_PER_CHUNK | 64 | 비동기 분석 시 프레임당 처리 단위 |

### 8밴드 주파수 범위 (Hz)

| 밴드 | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|------|---|---|---|---|---|---|---|---|
| 범위 | 20-60 | 60-250 | 250-500 | 500-2K | 2K-4K | 4K-6K | 6K-12K | 12K-20K |
| 특성 | 서브베이스 | 베이스 | 저중음 | 중음 | 고중음 | 프레즌스 | 브릴리언스 | 에어 |

### 분석 알고리즘 요약
1. **PCM 추출**: AudioClip -> 모노 PCM float 배열
2. **프레임별 FFT**: Hann 윈도우 + Cooley-Tukey Radix-2 FFT
3. **Spectral Flux**: 프레임 간 매그니튜드 양의 변화량
4. **온셋 감지**: 적응형 임계값(로컬 중간값 기반) + 50ms 쿨다운
5. **BPM 추출**: IOI(Inter-Onset Interval) -> 히스토그램(가우시안 스무딩) -> 가중 평균
6. **구간 감지**: 전체 에너지 정규화 -> 2초 스무딩 -> 에너지 기반 분류

### 구간 분류 기준
- `intro`: 곡 시작 8% 이내
- `outro`: 곡 마지막 10% 이후
- `drop`: 에너지 > 평균 * 1.5
- `build`: 에너지 > 평균 * 0.8
- `calm`: 나머지
- 최소 구간 길이: 2초

---

## 11) BeatMapper 결정론적 시드

- `System.Random(seed)` 인스턴스 기반 (UnityEngine.Random 미사용)
- 동일 시드 + 동일 BPM/Duration/Sections -> 항상 동일한 노트 패턴 생성
- 시드 값: SongData.Id 또는 명시적 전달
- `GenerateNotesFromBPM(bpm, duration, sections)` 메서드
- 기본 구간: intro(12.5%) -> build(25%) -> drop(37.5%) -> outro(25%)

---

## 12) 주요 상수/설정값

### GameManager
- `targetFrameRate`: 60
- `Screen.sleepTimeout`: NeverSleep

### AudioManager
- 기본 BGM Volume: 0.8
- 기본 SFX Volume: 1.0
- 볼륨 저장: PlayerPrefs("BGMVolume", "SFXVolume")
- 지원 포맷: MPEG(MP3), OGG Vorbis, WAV

### NoteSpawner
- 판정선 Y 위치: JudgementLine 오브젝트 기준
- 노트 하강: 상단에서 판정선으로
- 노트 렌더링: Quad 메쉬 + 프로시저럴 텍스처

### SongSelectUI
- BPM 슬라이더: 80~180 (기본 140, 정수)
- 에너지 저장: PlayerPrefs("Energy")
- 장르 목록: EDM, House, Cyberpunk, Synthwave, Chiptune, Dubstep, Trance, Techno
- 분위기 목록: Aggressive, Chill, Epic, Dark, Happy, Melancholic, Energetic, Mysterious

### LaneVisualFeedback
- LANE_COUNT: 7 (시각적 렌더링)
- laneWidth: 1.0f
- flashDuration: 0.2초
- 글로우 펄스: 알파 0.45~0.85, 주기 2.5Hz
- 히트존 높이: 2.8f

### HitParticleEffect
- poolSize: 60개
- Perfect: 8개 파티클, 속도 4, 크기 0.15, NeonYellow
- Great: 6개 파티클, 속도 3, 크기 0.12, NeonGreen
- Good: 4개 파티클, 속도 2.5, 크기 0.1, NeonCyan
- Bad: 2개 파티클, 속도 2, 크기 0.08, NeonOrange
- Miss: 파티클 없음
- 파티클 수명: 0.3~0.6초, 중력 가속도 3

### SceneTransitionManager
- fadeDuration: 0.4초
- fadeColor: 검정
- 보간: SmoothStep (t*t*(3-2t))

### FakeSongGenerator
- 가짜 생성 대기 시간: 3초

### AIApiClient
- pollInterval: 3초
- maxWaitTime: 60초

### 데모곡 목록 (FakeSongGenerator)

| ID | 제목 | 장르 | 분위기 | BPM | 난이도 | 길이(초) |
|----|------|------|--------|-----|--------|---------|
| demo_edm_fast | Neon Rush | EDM | Aggressive | 140 | 5 | 90 |
| demo_house_medium | Midnight Groove | House | Chill | 120 | 3 | 120 |
| demo_cyberpunk_fast | Digital Warfare | Cyberpunk | Dark | 160 | 7 | 100 |
| demo_synthwave_slow | Retro Dreams | Synthwave | Melancholic | 100 | 4 | 110 |

---

## 13) 빌드/실행 방법

### 에디터 테스트
1. Unity 6000.3.2f1로 `h:\Claude_work\bit\bit\` 프로젝트 열기
2. `AI_BEAT\Assets\` 폴더의 스크립트가 연동되어 있어야 함
3. `Tools > A.I. BEAT > Play Game` 또는 Gameplay 씬 직접 Play
4. AutoPlay 모드: GameplayController의 `autoPlay` 토글 활성화

### Android 빌드
- 아직 빌드 파이프라인 미설정 (Phase 1 진행 중)

### 디버그 모드
- `AudioManager.StartDebugPlayback(duration)`: 실제 오디오 없이 시간만 진행
- 오디오 파일 없이도 게임플레이 테스트 가능
- 1초 간격으로 디버그 시간 로그 출력
- `Time.unscaledDeltaTime` 기반 (일시정지 영향 없음)

### MCP 연동 (Unity Editor)
- recompile_scripts: 스크립트 재컴파일 (에러 0 확인)
- 씬 생성/로드/저장 가능
- Play 모드 진입은 가능하나 종료는 수동 필요

---

## 14) 코딩 규칙

### 필수
- **최소 diff**: 한 번에 하나의 기능만 수정
- **비밀정보 금지**: API 키/비밀번호 하드코딩 절대 불가 (PlayerPrefs 또는 환경변수)
- **한국어 진행 보고**: 모든 설명은 한국어
- **컴파일 에러 0**: 작업 완료 시 반드시 에러/경고 0 상태
- **큰 변경**: 프레임워크/DB/상태관리 교체는 사용자 1회 확인 후 진행

### 네이밍 규칙
- 네임스페이스: `AIBeat.{모듈명}` (Core/Gameplay/Audio/Network/Data/UI/Utils/Editor)
- 클래스: PascalCase
- 메서드: PascalCase
- 필드: camelCase (`[SerializeField]` private 필드)
- 상수: UPPER_SNAKE_CASE 또는 PascalCase
- 이벤트: `On` + PascalCase (예: `OnGenerationComplete`)

### 싱글톤 패턴
- `Instance` 프로퍼티 + `Awake()`에서 중복 체크
- DontDestroyOnLoad 적용: GameManager, AudioManager, SceneTransitionManager
- 비 DontDestroyOnLoad: LaneVisualFeedback, HitParticleEffect (씬별 인스턴스, static 참조)

### 리소스 관리
- Quad 메쉬 기반 프로시저럴 생성 (Prefab 최소화)
- 인스턴스 Material은 `OnDestroy()`에서 반드시 `Destroy(material)`
- 오브젝트 풀링: Note, HitParticle 등 반복 생성 오브젝트
- static 참조는 씬 전환 시 `Cleanup()` 호출로 정리
- MonoBehaviour 이벤트 리스너는 `OnDestroy()`에서 `RemoveAllListeners()` 호출

### UI 규칙
- 동적 UI 생성 시 `AutoSetupReferences()`로 누락 참조 자동 탐색
- Inspector 할당 우선, 없으면 `transform.Find()` 폴백
- 판정 통계 HUD 등 보조 UI는 코드에서 동적 생성

---

## 15) 알려진 제한사항

1. **Unity Play 모드 종료**: MCP로 Play 모드 진입 후 종료 불가 (수동 중지 필요)
2. **ISongGenerator 인터페이스 파일 부재**: PROGRESS.md에서 구현 완료 표시되었으나, `AI_BEAT/Assets/Scripts/Network/ISongGenerator.cs` 파일 미존재 (FakeSongGenerator와 AIApiClient가 이벤트 시그니처만 동일하게 구현)
3. **CalibrationManager 파일 부재**: PROGRESS.md TODO-8에서 구현 완료 표시되었으나, AI_BEAT 폴더 내 파일 미확인
4. **SettingsUI 파일 부재**: PROGRESS.md TODO-8에서 구현 완료 표시되었으나, AI_BEAT 폴더 내 파일 미확인
5. **씬 파일 부재**: `AI_BEAT/Assets/Scenes/` 폴더에 `.unity` 씬 파일이 아직 없음 (에디터에서 수동 생성 필요)
6. **7레인 시각 vs 4레인 입력**: LaneVisualFeedback은 7레인, NoteData.LaneIndex는 0~3(4레인). 시각적 매핑 로직 정합성 확인 필요
7. **AI API 미연결**: AIApiClient 구조만 존재, 실제 엔드포인트/인증 미설정
8. **Android 빌드 미설정**: 빌드 파이프라인, 키스토어 등 미구성
9. **사운드 에셋 부재**: 히트 사운드(Perfect/Great/Good/Bad), 스크래치 사운드 AudioClip 미할당
10. **Library 씬 미구현**: MainMenuUI의 Library 버튼 연결 씬 없음

---

## 16) Phase 로드맵

### Phase 1: 기반 마련 (현재)
- [x] Unity 프로젝트 생성
- [x] 핵심 스크립트 아키텍처 완성 (25개 C# 파일)
- [x] 4레인 시스템 전환 (세션 29)
- [x] 오디오 분석/비트매핑 엔진 (OfflineAudioAnalyzer, BeatMapper, SmartBeatMapper)
- [x] 게임플레이 루프 (노트 하강/판정/스코어)
- [x] UI 프레임워크 (메뉴/선곡/게임플레이/결과)
- [x] 프로시저럴 비주얼 (네온/사이버펑크)
- [x] 롱노트 라이프사이클 수정 (세션 29)
- [x] 스크래치/키 이중 트리거 해결 (세션 29)
- [x] BeatMapper 결정론적 시드 (세션 29)
- [x] OfflineAudioAnalyzer 비동기화 (세션 29)
- [ ] ISongGenerator.cs 인터페이스 파일 생성
- [ ] CalibrationManager/SettingsUI 파일 반영
- [ ] 씬 파일 생성 및 컴포넌트 배치
- [ ] 사운드 에셋 추가
- [ ] 실기기 테스트

### Phase 2: AI 연동
- [ ] 실제 AI 음악 생성 API 연동 (Suno/Udio 등)
- [ ] 생성된 음악 스트리밍 재생
- [ ] 스마트 노트 매핑 고도화

### Phase 3: 완성도
- [ ] Android 빌드 파이프라인
- [ ] 곡 라이브러리 관리 (Library 씬)
- [ ] 사운드 에셋 완성 (히트 사운드, 스크래치, UI 효과음)
- [ ] 수익화 (에너지 시스템, 광고, 유료 결제)
- [ ] 성능 최적화 (프로파일링, GC 최소화)

---

## 17) 문서 관리 규칙

| 파일 | 역할 | 갱신 시점 |
|------|------|----------|
| **CLAUDE.md** | 라우터 (읽기 순서 안내) | 거의 변경 없음 |
| **MEMORY.md** | SSOT (규칙/기술/아키텍처) | 아키텍처/규칙 변경 시 |
| **PROGRESS.md** | 현재 진행/이슈 | 매 세션 |
| **ARCHIVE_*.md** | 완료 기록 보관 | PROGRESS 20항목 또는 5KB 초과 시 |
| **.commit_message.txt** | 커밋 메시지 | 매 코드 변경 시 (이모지+한국어 한 줄) |

### 갱신 원칙
- MEMORY.md는 **사실만 기록** (추측/예정 사항은 PROGRESS.md에)
- 클래스 추가/삭제/역할 변경 시 네임스페이스 섹션 갱신
- 상수/설정값 변경 시 해당 섹션 갱신
- 레인 구조/판정 윈도우 등 핵심 게임 설계 변경 시 즉시 반영
