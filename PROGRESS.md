# PROGRESS.md (현재 진행: 얇게 유지)

## Dashboard
- Progress: 100% (세션 34 - 전체 플로우 종합 재검증 통과)
- Risk: 낮음

## Today Goal
- 앱 시작 → 게임 종료까지 전체 플로우 종합 재검증

## What changed (세션 34)

### 전체 플로우 종합 재검증 (3개 씬 모두 통과)

| 씬 | Play 모드 | 에러 | 경고 | UI 요소 검증 |
|----|-----------|------|------|-------------|
| MainMenu | 정상 | 0 | 0 | PlayButton("PLAY"), LibraryButton("LIBRARY"), SettingsButton("SETTINGS"), ExitButton("EXIT"), TutorialUI 자동시작 |
| SongSelect | 정상 | 0 | 0 | GenerateButton("GENERATE"), BackButton("BACK"), GenreContainer(8개), MoodContainer(8개), BpmSlider(80-180, val=140), LoadingPanel(대기) |
| Gameplay | 정상 | 0 | 1 (중복노트 필터링) | AutoPlay Perfect 연속 (combo 22+), Tap/Scratch/Long 노트 모두 스폰, 점수 23,265+ |

### 코드 레벨 검증
- **씬 전환 플로우**: MainMenu→SongSelect→Gameplay→MainMenu 순환 확인
  - Play → `LoadScene("SongSelect")`
  - Generate → `StartGame(songData)` → `LoadScene("Gameplay")`
  - Quit → `ReturnToMenu()` → `LoadScene("MainMenu")`
  - Retry → `SceneManager.LoadScene("Gameplay")` (씬 재로드)
- **결과 화면**: Score/Combo/Accuracy/Rank + PERFECT~MISS 5단계 카운트 + NEW RECORD
- **일시정지**: Resume/Restart/Quit 3버튼
- **설정**: 5개 슬라이더 + CALIBRATE + RESET/CLOSE

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
1. **AI API 연동** — ISongGenerator 인터페이스만 존재, 실제 API 미연결 (FakeSongGenerator 사용 중)
2. **실제 오디오 재생** — AudioManager에 더미 타이머 사용 (MP3 스트리밍 미구현)
3. **캘리브레이션** — CalibrationManager 코드 존재하나 실제 탭 테스트 미수행
4. **Android 빌드** — 빌드 파이프라인 미설정
5. **터치 입력** — InputHandler에 터치 코드 존재, 실기기 테스트 미수행
6. **곡 라이브러리** — SongLibrary에 0곡 (AI 생성 곡 저장/관리 미구현)
7. **에너지 시스템** — UI 존재, 실제 차감/충전 로직 미확인

## Next
1) 2키+스크래치 레인 구조 변경 (계획 파일 존재)
2) AI API 연동 (ISongGenerator → 실제 API)
3) 실제 오디오 재생 (MP3 스트리밍)
4) Android 빌드 파이프라인 설정
5) 실기기 테스트

---
## Archive Rule (요약)
- 완료 항목이 20개를 넘거나 파일이 5KB를 넘으면,
  완료된 내용을 `ARCHIVE_YYYY_MM.md`로 옮기고 PROGRESS는 "현재 이슈"만 남긴다.
