# PROGRESS.md (현재 진행: 얇게 유지)

## Dashboard
- Progress: 100% (세션 32 - 모바일 플레이어빌리티 검증 + 난이도 조정)
- Risk: 낮음

## Today Goal
- 사람 손(스마트폰 엄지)으로 플레이 가능한지 검증 + 난이도 조정

## What changed (세션 32)

### 모바일 플레이어빌리티 분석 결과
- **판정 시스템**: Perfect/Great/Good/Bad/Miss 5단계 이미 존재 (추가 불필요)
- **터치 존**: Key 38% × 2 + Scratch 12% × 2 → 엄지 조작 충분
- **반응 시간**: 노트 스폰→판정선 2초 (lookAhead=2) → 충분
- **판정 윈도우**: Good ±200ms, Bad ±350ms → 모바일에 관대

### 난이도 조정 (사람 엄지 기준)
| 변경 항목 | 수정 전 | 수정 후 | 이유 |
|----------|--------|--------|------|
| Climax 16분음표 확률 | 50% | **20%** | 150ms 연타는 엄지로 불가능 |
| Climax 스크래치 후 간격 | 16분음표(150ms) | **8분음표(300ms)** | 가장자리→키존 엄지 복귀 시간 |
| Climax 롱노트 후 간격 | 16분음표(150ms) | **8분음표(300ms)** | release→다음 노트 반응 시간 |
| Build 16분음표 확률 | 30% | **15%** | 초중반 난이도 완화 |

### AutoPlay 테스트 결과 (난이도 조정 후)
- Score: **77,055** | Rank: **S+** | Perfect: 66 | Miss: **0** | 풀콤보 유지

## Commands & Results
- recompile_scripts → **0 에러, 0 경고**
- run_tests (EditMode) → **49/49 통과**, 0 실패
- Play Mode AutoPlay → **S+ 랭크, Miss 0, 풀콤보**
- get_console_logs(error) → **0개** (게임 에러 없음)

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
