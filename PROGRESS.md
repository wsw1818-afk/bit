# PROGRESS.md (현재 진행: 얇게 유지)

## Dashboard
- Progress: 100% (세션 29 - 8개 TODO 전체 구현 완료)
- Risk: 낮음

## Today Goal
- 다른 AI가 분석한 8개 TODO 항목 코드 반영

## What changed (세션 29)

### TODO-1: 4레인 잔여 정리
- **SimpleTest.asset**: 71개 노트 전수 리매핑 (LaneIndex 4-6 → 0-3 범위)
- **AIBeatEditorTests.cs**: Assert 20→71, LaneIndex 검증 ≤6 → ≤3
- **AudioAnalyzer.cs**: MapBandToLane 7레인→4레인 매핑 재작성

### TODO-2: 롱노트 라이프사이클 수정
- **Note.cs**: IsExpired에 `isHolding` 가드 추가, 롱노트 만료시간 HitTime+Duration+0.5s
- **GameplayController.cs**: ProcessNoteHit에서 롱노트 press 시 MarkAsJudged 제거 (release로 이동)
- **GameplayController.cs**: ProcessNoteRelease → FindHoldingNote 헬퍼로 재작성
- **GameplayController.cs**: AutoPlayLoop 롱노트 hold/release 처리 추가
- **NoteSpawner.cs**: GetNearestNote 홀딩 노트 우선 반환

### TODO-3: 스크래치/키 이중 트리거 해결
- **InputHandler.cs**: scratchEdgeRatio 12% 가장자리 전용 스크래치존 추가
- IsScratchOnly 터치 → Scratch 즉시 발동 (Key Down 미발생)

### TODO-4: BeatMapper 결정론적 시드
- **BeatMapper.cs**: System.Random(seed) 인스턴스 기반으로 전환, UnityEngine.Random 제거

### TODO-5: ISongGenerator 토글
- **ISongGenerator.cs**: 신규 인터페이스
- **FakeSongGenerator.cs**, **AIApiClient.cs**: ISongGenerator 구현
- **SongSelectUI.cs**: useApiClient 토글 + activeGenerator 패턴

### TODO-6: 에너지 UX
- **SongSelectUI.cs**: RechargeEnergyFromTime(), EnergyRechargeLoop(), ShowNoEnergyDialog()

### TODO-7: OfflineAudioAnalyzer 비동기화
- **OfflineAudioAnalyzer.cs**: AnalyzeAsync(IEnumerator) 추가, FRAMES_PER_CHUNK=64 단위 yield
- **GameplayController.cs**: 두 호출부 모두 AnalyzeAsync로 전환, 진행률 UI 표시

### TODO-8: 오프셋 캘리브레이션 + Early/Late 피드백
- **CalibrationManager.cs**: 신규 (탭 테스트, 메트로놈 틱 생성, IQR 이상치 제거)
- **JudgementSystem.cs**: OnJudgementDetailed 이벤트 추가 (rawDiff: early/late)
- **GameplayUI.cs**: ShowJudgementDetailed + EarlyLateText 동적 생성
- **SettingsUI.cs**: CALIBRATE 버튼 + Update()에서 탭 감지

## Commands & Results
- recompile_scripts → **0 에러, 0 경고**

## Open issues
- Unity Play 모드 진입 후 MCP로 종료 불가 (수동 ▶ 필요)

## Analysis / TODO (2026-02-07)
- ✅ DONE: Align 4-lane leftovers
- ✅ DONE: Fix long-note lifecycle
- ✅ DONE: Resolve scratch vs key double-trigger
- ✅ DONE: Make BeatMapper deterministic per seed
- ✅ DONE: Integrate ISongGenerator toggle
- ✅ DONE: Implement Energy UX
- ✅ DONE: Move OfflineAudioAnalyzer off main thread
- ✅ DONE: Add offset calibration + early/late feedback

## Next
1) Unity Play 모드에서 AutoPlay 테스트 (롱노트 hold/release 동작 확인)
2) 캘리브레이션 탭 테스트 → 오프셋 자동 측정 확인
3) 실기기(스마트폰) 테스트: 스크래치 가장자리존 + Early/Late 피드백

---
## Archive Rule (요약)
- 완료 항목이 20개를 넘거나 파일이 5KB를 넘으면,
  완료된 내용을 `ARCHIVE_YYYY_MM.md`로 옮기고 PROGRESS는 "현재 이슈"만 남긴다.
