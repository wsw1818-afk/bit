# PROGRESS.md (현재 진행: 얇게 유지)

## Dashboard
- Progress: 100% (세션 34 - 전체 플로우 종합 재검증 통과)
- Risk: 낮음

## Today Goal
- 앱 시작 → 게임 종료까지 전체 플로우 종합 재검증

## What changed (세션 44)

### SongSelect 대폭 단순화 — 라이브러리 전용으로 전환
- **제거됨**: 탭 시스템 ("새 곡 만들기" | "내 라이브러리")
- **제거됨**: 곡 생성 UI (장르/분위기/BPM 버튼, Generate 버튼, GridLayout)
- **제거됨**: 에너지 시스템 (표시/충전/소모 로직 전체)
- **제거됨**: 로딩 패널 (프로그레스 바, 로딩 메시지)
- **제거됨**: ISongGenerator/FakeSongGenerator 참조 (SongSelectUI에서)
- **제거됨**: OptionContainer, BPM Slider, Preview Texts
- **SongSelectUI.cs**: 1333줄 → 155줄 (88% 코드 감소)
- **추가**: "내 라이브러리" 타이틀 바 (탭 대체)
- **SongLibraryUI.cs**: 빈 목록 메시지에서 "새 곡 만들기 탭" 참조 제거
- 이유: AI API 취소됨 → Suno AI 수동 다운로드 → 곡 생성 UI 불필요

### 검증
- recompile_scripts → 0 에러, 0 경고 ✅

## Commands & Results (세션 44)
- recompile_scripts → **0 에러, 0 경고** ✅

## Open issues
- Unity Play 모드 MCP 진입 시 타임아웃 발생 (게임 자체는 정상 작동)
- 롱노트 hold 비주얼 피드백 미확인 (실제 화면 확인 필요)
- SettingsPanel은 SettingsButton 클릭 전까지 생성되지 않음 (정상 동작이나 실제 클릭 테스트는 미수행)

## 미구현 기능 목록
1. ~~**AI API 연동**~~ → **취소됨** (비용 문제로 Suno AI에서 수동 다운로드 방식으로 결정)
2. **로컬 MP3 로드/재생** — AudioManager 로드 코드 존재, 실제 MP3 파일 연결 미완료. OfflineAudioAnalyzer + SmartBeatMapper로 자동 노트 생성 가능
3. **캘리브레이션** — CalibrationManager 코드 존재하나 실제 탭 테스트 미수행
4. ~~**Android 빌드**~~ → **완료** (APK 57.2MB, `Builds/Android/AIBeat.apk`)
5. **터치 입력** — InputHandler에 터치 코드 존재, 실기기 테스트 미수행
6. **곡 라이브러리** — SongLibrary에 0곡 (다운로드한 곡 관리 UI 미구현)
7. ~~**에너지 시스템**~~ → **제거됨** (곡 생성 기능 제거로 불필요)

## 아키텍처 결정 (세션 35)
- **음악 소스**: Suno AI에서 수동 생성 → MP3 다운로드 → 게임에 로컬 로드
- **API 연동 취소**: 비용 문제로 AIApiClient 사용 안 함
- **노트 생성**: OfflineAudioAnalyzer(FFT 분석) + SmartBeatMapper(온셋→노트)로 자동 생성
- **기존 코드 활용도**: AudioManager(4가지 로드 방식), OfflineAudioAnalyzer, SmartBeatMapper 모두 이미 구현됨

## Next
1) 2키+스크래치 레인 구조 변경 (계획 파일 존재)
2) 로컬 MP3 파일 로드 → 자동 분석 → 노트 생성 → 실제 재생 연결
3) ~~Android 빌드 파이프라인 설정~~ ✅ 완료
4) 실기기 테스트 (APK 설치 후)

---
## Archive Rule (요약)
- 완료 항목이 20개를 넘거나 파일이 5KB를 넘으면,
  완료된 내용을 `ARCHIVE_YYYY_MM.md`로 옮기고 PROGRESS는 "현재 이슈"만 남긴다.
