# PROGRESS.md (현재 진행: 얇게 유지)

## Dashboard
- Progress: 100% (세션 31 - Play 모드 AutoPlay 테스트 + 버그 수정)
- Risk: 낮음

## Today Goal
- Unity Play 모드에서 AutoPlay 테스트 + 런타임 버그 수정

## What changed (세션 31)

### Play 모드 AutoPlay 테스트 + 버그 수정 (4개 이슈)
- **InputHandler 씬 값 수정**: touchZoneCount 5→2 (씬 직렬화 값 불일치)
- **AutoPlay 히트 윈도우 확장**: ±20ms → ±40ms (프레임 지터 대응)
- **AutoPlay 롱노트 release 후 즉시 다음 노트 처리**: continue 제거 → 같은 프레임 처리
- **디버그 노트 생성 롱노트 간격 보장**: `t += duration + interval` (duration 이상 간격)
- **NoteSpawner.LoadNotes 겹침 필터링**: 같은 시간+레인 중복 + 롱노트 구간 내 겹침 제거
- **PlayModeHelper.cs**: 에디터 메뉴 Play/Exit 커맨드 추가

### AutoPlay 테스트 결과 (최종)
| 항목 | 수정 전 | 수정 후 |
|------|--------|--------|
| Score | 74,105 | **77,055** |
| Rank | A | **S+** |
| Perfect | 70 | 66 (겹침 노트 제거) |
| Miss | 6 | **0** |
| MaxCombo | ~23 | **66 (풀콤보)** |
| 런타임 에러 | 0 | 0 |

## Commands & Results
- recompile_scripts → **0 에러, 0 경고**
- run_tests (EditMode) → **49/49 통과**, 0 실패
- Play Mode AutoPlay → **S+ 랭크, Miss 0, 풀콤보**
- get_console_logs(error) → **0개** (MCP WebSocket 에러만 - 게임 무관)

## Open issues
- Unity Play 모드 MCP 진입 시 타임아웃 발생 (게임 자체는 정상 작동)
- 롱노트 hold 비주얼 피드백 미확인 (실제 화면 확인 필요)

## Next
1) 캘리브레이션 탭 테스트 → 오프셋 자동 측정 확인
2) 실기기(스마트폰) 테스트: 터치 2존 + 스크래치 가장자리존
3) Android 빌드 파이프라인 설정
4) AI API 연동 (ISongGenerator → 실제 API)

---
## Archive Rule (요약)
- 완료 항목이 20개를 넘거나 파일이 5KB를 넘으면,
  완료된 내용을 `ARCHIVE_YYYY_MM.md`로 옮기고 PROGRESS는 "현재 이슈"만 남긴다.
