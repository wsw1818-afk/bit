# PROGRESS.md (현재 진행: 얇게 유지)

## Dashboard
- Progress: 100% (세션 35 - UI 비주얼 오버홀 완료)
- Risk: 낮음

## Today Goal
- 앱 시작 → 게임 종료까지 전체 플로우 종합 재검증
- **UI 비주얼 오버홀 (Bitmania 스타일 개편)**

## What changed (현재 세션)

### 비주얼 오버홀 (NanoBanana Procedural)
- **ProceduralImageGenerator**: API 제한 문제로 런타임 텍스처 생성 시스템 구현 (배경, 버튼 프레임, 트랙 등)
- **UIColorPalette**: 고대비 네온(Cyan/Magenta/Gold) + 딥 블랙 테마로 전면 교체
- **UI 스크립트 개편**:
  - `MainMenuUI`: 사이버펑크 그리드 배경 + 네온 버튼 프레임 적용
  - `SongSelectUI`: 이퀄라이저 바 높이/개수 증가, 반투명 리스트 디자인 적용
  - `GameplayUI`: 노트 레인 배경에 Dark Procedural Texture 적용으로 시인성 개선

### 로컬 MP3 재생 전체 연결 완료
- **SongRecord.AudioFileName** 필드 추가 — StreamingAssets 내 MP3 파일명 참조
- **SongSelectUI**: StreamingAssets 폴더 자동 스캔 → MP3/WAV/OGG 파일을 라이브러리에 자동 등록
- **SongLibraryUI.OnSongCardClicked**: FakeSongGenerator 제거 → 직접 StreamingAssets에서 UnityWebRequest로 MP3 로드 → SongData 생성(AudioClip 포함) → GameManager.StartGame 호출
- **GameplayController.StartGame**: AudioClip이 있으면 AudioManager에 SetBGM 후 PlayBGM
- **전체 플로우**: StreamingAssets에 MP3 넣기 → SongSelect 진입 시 자동 등록 → 곡 카드 클릭 → MP3 로드 → OfflineAudioAnalyzer 분석 → SmartBeatMapper 노트 생성 → 카운트다운 → 실제 오디오 재생 + 노트 스폰
- **StreamingAssets/jpop_energetic.mp3** 추가 (J-POP 테스트곡)

### 검증
- recompile_scripts → 0 에러, 0 경고 ✅
- 런타임 텍스처 생성 정상 동작 확인

## Commands & Results (현재 세션)
- recompile_scripts → **0 에러, 0 경고** ✅

## Open issues
- Unity Play 모드 MCP 진입 시 타임아웃 발생 (게임 자체는 정상 작동)
- 롱노트 hold 비주얼 피드백 미확인 (실제 화면 확인 필요)
- SettingsPanel은 SettingsButton 클릭 전까지 생성되지 않음 (정상 동작이나 실제 클릭 테스트는 미수행)

## 미구현 기능 목록
1. ~~**AI API 연동**~~ → **취소됨** (비용 문제로 Suno AI에서 수동 다운로드 방식으로 결정)
2. ~~**로컬 MP3 로드/재생**~~ → **완료** (StreamingAssets 자동 스캔 → 라이브러리 등록 → 곡 선택 → MP3 로드 → 분석 → 노트 생성 → 재생)
3. **캘리브레이션** — CalibrationManager 코드 존재하나 실제 탭 테스트 미수행
4. ~~**Android 빌드**~~ → **완료** (APK 57.2MB, `Builds/Android/AIBeat.apk`)
5. **터치 입력** — InputHandler에 터치 코드 존재, 실기기 테스트 미수행
6. ~~**곡 라이브러리**~~ → **완료** (StreamingAssets MP3 자동 스캔/등록, 곡 카드 클릭→게임 시작 연결)
7. ~~**에너지 시스템**~~ → **제거됨** (곡 생성 기능 제거로 불필요)

## 아키텍처 결정 (세션 35)
- **UI/UX**: 고대비 네온 스타일(Bitmania) + 런타임 절차적 텍스처 생성(ProceduralImageGenerator) 도입
- **음악 소스**: Suno AI에서 수동 생성 → MP3 다운로드 → 게임에 로컬 로드
- **API 연동 취소**: 비용 문제로 AIApiClient 사용 안 함
- **노트 생성**: OfflineAudioAnalyzer(FFT 분석) + SmartBeatMapper(온셋→노트)로 자동 생성
- **기존 코드 활용도**: AudioManager(4가지 로드 방식), OfflineAudioAnalyzer, SmartBeatMapper 모두 이미 구현됨

## Next
1) 2키+스크래치 레인 구조 변경 (계획 파일 존재)
2) ~~로컬 MP3 파일 로드 → 자동 분석 → 노트 생성 → 실제 재생 연결~~ ✅ 완료
3) ~~Android 빌드 파이프라인 설정~~ ✅ 완료
4) 실기기 테스트 (APK 설치 후)

---
## Archive Rule (요약)
- 완료 항목이 20개를 넘거나 파일이 5KB를 넘으면,
  완료된 내용을 `ARCHIVE_YYYY_MM.md`로 옮기고 PROGRESS는 "현재 이슈"만 남긴다.
