# PROGRESS.md (현재 진행: 얇게 유지)

## Dashboard
- Progress: 100% (세션 34 - 전체 플로우 종합 재검증 통과)
- Risk: 낮음

## Today Goal
- 앱 시작 → 게임 종료까지 전체 플로우 종합 재검증

## What changed (세션 43)

### SongSelect 가로 화면 최적화
- **GridLayout 다열 배치**: 장르/분위기 4열, BPM 3열로 버튼 배치 (가로 화면 공간 활용)
- **CreateButtonGrid()**: 각 섹션별 GridLayoutGroup 컨테이너 생성, ContentSizeFitter로 높이 자동 조절
- **AdjustGridCellSize()**: Canvas 폭 기반으로 셀 너비 자동 계산 (코루틴 1프레임 대기)
- **VerticalLayoutGroup spacing/padding 축소**: 20→6, padding 20→10 (가로 화면에 맞게)
- **섹션 라벨 축소**: 높이 36→28, 폰트 20→17 (가로 화면 공간 절약)
- **버튼 폰트 축소**: 22→18 (그리드 셀에 맞게)

### 검증
- recompile_scripts → 0 에러, 0 경고 ✅
- Play 모드 SongSelect 씬: 에러 0개 ✅
- GridLayout 4열 동작 확인: EDM(1행1열), House(1행2열), Chiptune(2행1열) 위치 정상 ✅
- 한국어 폰트 정상 적용 (MalgunGothicBold SDF) ✅

## Commands & Results (세션 43)
- recompile_scripts → **0 에러, 0 경고** ✅
- MCP Play 모드 검증: SongSelect 씬 에러 0개 ✅
- Genre_EDM anchoredPosition=(100,-28), Genre_House=(308,-28), Genre_Chiptune=(100,-84) → 4열 그리드 정상 ✅

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
