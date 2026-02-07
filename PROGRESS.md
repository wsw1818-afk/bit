# PROGRESS.md (현재 진행: 얇게 유지)

## Dashboard
- Progress: 100% (세션 34 - 전체 플로우 종합 재검증 통과)
- Risk: 낮음

## Today Goal
- 앱 시작 → 게임 종료까지 전체 플로우 종합 재검증

## What changed (세션 36)

### 롱노트 홀드 보너스 점수 시스템 추가
- **JudgementSystem**: `bonusScore` 필드, `AddBonusScore()`, `OnBonusScore` 이벤트, `TotalScore` 프로퍼티
- **GameplayController**: `HoldBonusTickLoop` 코루틴 — 홀드 중 0.1초마다 +50 BONUS 누적
- **GameplayUI**: 게임 중 "BONUS +N" 골드색 팝업 표시 (좌상단 콤보 아래)
- **GameResult**: `BaseScore` + `BonusScore` 분리, 결과 화면에 "(BONUS +N)" 별도 표시
- **AutoPlay 판정 다양화**: Perfect 55% / Great 25% / Good 15% / Bad 5% 분포

### 테스트 결과
- recompile_scripts → **0 에러, 0 경고**
- Gameplay Play Mode → **에러 0**, Score 62,908 (보너스 포함), Miss 0

## Commands & Results
- recompile_scripts → **0 에러, 0 경고**
- run_tests (EditMode) → **49/49 통과**, 0 실패
- MainMenu Play Mode → **에러 0**, 4개 버튼 + TutorialUI 정상
- SongSelect Play Mode → **에러 0**, 16개 옵션버튼 + 슬라이더 정상
- Gameplay Play Mode → **에러 0**, AutoPlay Perfect 연속
- get_console_logs(error) → **0개** (모든 씬, MCP WebSocket 에러 제외)

## Open issues
- Unity Play 모드 MCP 진입 시 타임아웃 발생 (게임 자체는 정상 작동)
- 롱노트 hold 비주얼 피드백 미확인 (실제 화면 확인 필요)
- SettingsPanel은 SettingsButton 클릭 전까지 생성되지 않음 (정상 동작이나 실제 클릭 테스트는 미수행)

## 미구현 기능 목록
1. ~~**AI API 연동**~~ → **취소됨** (비용 문제로 Suno AI에서 수동 다운로드 방식으로 결정)
2. **로컬 MP3 로드/재생** — AudioManager 로드 코드 존재, 실제 MP3 파일 연결 미완료. OfflineAudioAnalyzer + SmartBeatMapper로 자동 노트 생성 가능
3. **캘리브레이션** — CalibrationManager 코드 존재하나 실제 탭 테스트 미수행
4. **Android 빌드** — 빌드 파이프라인 미설정
5. **터치 입력** — InputHandler에 터치 코드 존재, 실기기 테스트 미수행
6. **곡 라이브러리** — SongLibrary에 0곡 (다운로드한 곡 관리 UI 미구현)
7. **에너지 시스템** — UI 존재, 실제 차감/충전 로직 미확인

## 아키텍처 결정 (세션 35)
- **음악 소스**: Suno AI에서 수동 생성 → MP3 다운로드 → 게임에 로컬 로드
- **API 연동 취소**: 비용 문제로 AIApiClient 사용 안 함
- **노트 생성**: OfflineAudioAnalyzer(FFT 분석) + SmartBeatMapper(온셋→노트)로 자동 생성
- **기존 코드 활용도**: AudioManager(4가지 로드 방식), OfflineAudioAnalyzer, SmartBeatMapper 모두 이미 구현됨

## Next
1) 2키+스크래치 레인 구조 변경 (계획 파일 존재)
2) 로컬 MP3 파일 로드 → 자동 분석 → 노트 생성 → 실제 재생 연결
3) Android 빌드 파이프라인 설정
4) 실기기 테스트

---
## Archive Rule (요약)
- 완료 항목이 20개를 넘거나 파일이 5KB를 넘으면,
  완료된 내용을 `ARCHIVE_YYYY_MM.md`로 옮기고 PROGRESS는 "현재 이슈"만 남긴다.
