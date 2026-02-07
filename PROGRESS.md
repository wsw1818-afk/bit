# PROGRESS.md (현재 진행: 얇게 유지)

## Dashboard
- Progress: 100% (세션 34 - 전체 플로우 종합 재검증 통과)
- Risk: 낮음

## Today Goal
- 앱 시작 → 게임 종료까지 전체 플로우 종합 재검증

## What changed (세션 42)

### SongSelect UI 전면 간소화
- **OptionTabBar 완전 제거**: 장르/분위기/빠르기 3개 서브 탭을 없앰
- **한 화면 스크롤**: 모든 옵션(장르→분위기→빠르기)을 섹션 헤더와 함께 하나의 스크롤 뷰에 표시
- **섹션 헤더**: `CreateInlineSectionLabel()` — "장르", "분위기", "빠르기 (BPM)" 라벨을 스크롤 내부에 배치
- **한국어 폰트 □□ 깨짐 해결**: `KoreanFontManager.ApplyFontToAll()` 호출을 모든 UI 생성 완료 후로 이동
- **불필요 코드 정리**: CreateOptionTabBar, SwitchToOptionTab, UpdateOptionTabHighlight, CreateColumnContainer, CreateBpmSlider, LayoutColumn, CreateSectionLabel, CreateOptionButtons, CreateOptionButton 등 미사용 메서드 제거
- **OptionContainer offsetMax**: -120 → -62 (메인 탭 56px + 여백 6px만)

### 전체 플로우 종합 테스트 (3개 씬 모두 통과)

| 씬 | Play 모드 | 게임 에러 | 비고 |
|----|-----------|----------|------|
| MainMenu | 정상 | 0 | TutorialUI 자동시작 |
| SongSelect | 정상 | 0 | SongLibrary 0곡 로드 |
| Gameplay | 정상 | 0 | Score 65,276 (보너스 포함), P:40 G:16 Good:8 B:2 M:0 |

## Commands & Results (세션 42)
- recompile_scripts → **0 에러, 0 경고** ✅
- MCP Play 모드 검증: SongSelect 씬 에러 0개, 한국어 폰트 정상 적용 ✅
- Label_장르, Label_분위기, Label_빠르기 (BPM) 모두 MalgunGothicBold SDF 폰트 확인 ✅
- OptionTabBar 제거 확인 (MCP get_gameobject 검색 결과 없음) ✅

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
7. **에너지 시스템** — UI 존재, 실제 차감/충전 로직 미확인

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
