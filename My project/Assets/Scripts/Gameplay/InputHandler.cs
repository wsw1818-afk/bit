using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AIBeat.Gameplay
{
    /// <summary>
    /// 터치 및 키보드 입력 처리
    /// 모바일 최적화: 2개 터치 존 + 가장자리 스와이프 스크래치
    /// 레인 구조: 0=ScratchL, 1=Key1, 2=Key2, 3=ScratchR
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("Touch Zone Settings")]
        [SerializeField] private int touchZoneCount = 4;       // 4개 균등 터치 존 (Lane0~3 직접 매핑)
        [SerializeField] private float touchAreaRatio = 0.85f;  // 하단 85% 입력 영역 (상단 UI만 제외)
        [Header("Debug")]
        [SerializeField] private bool showTouchDebug = false;  // 빌드 성능을 위해 기본 false

        [Header("Scratch Swipe Settings")]
        [SerializeField] private float scratchThresholdMM = 7f;  // 스크래치 인식 최소 거리(mm)
        [SerializeField] private float fallbackDPI = 160f;       // DPI 미지원 기기 폴백값

        private Dictionary<int, TouchData> activeTouches = new Dictionary<int, TouchData>();
        private bool isEnabled = true;
        private float cachedScratchThreshold; // 픽셀 단위 캐시
        private float[] laneBoundaries; // 레인 경계 (정규화된 X 좌표, 5개: 0경계~4경계)

        // Lane indices: 0=ScratchL, 1=Key1, 2=Key2, 3=ScratchR
        public event Action<int, InputType> OnLaneInput;

        public enum InputType
        {
            Down,      // 터치/키 누름
            Hold,      // 터치 유지 (예약, 현재 미사용)
            Up,        // 터치/키 뗌
            Scratch    // 스크래치 동작
        }

        private struct TouchData
        {
            public int TouchZone;          // 0-3 (4개 존)
            public int MappedLane;         // 0-3 (직접 매핑)
            public Vector2 StartPosition;
            public bool IsEdgeZone;        // 스와이프 스크래치 가능 존 여부
            public bool ScratchTriggered;  // 스크래치 이미 발동 여부
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        private int cachedScreenWidth;
        private int cachedScreenHeight;

        private void Start()
        {
            CacheScratchThreshold();
            // CacheLaneBoundaries는 InputLoop에서 카메라 설정 완료 후 실행
            StartCoroutine(InputLoop());
        }

        /// <summary>
        /// 카메라 기반 레인 경계를 화면 정규화 좌표로 캐시
        /// 레인 중심 X: -2.1, -0.7, 0.7, 2.1 (laneWidth=1.4, 4레인)
        /// 경계는 각 레인 중심 기준으로 정확히 계산
        /// </summary>
        private void CacheLaneBoundaries()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                // 카메라 없으면 균등 분할 사용
                laneBoundaries = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
                return;
            }

            float laneWidth = 1.4f;  // 레인 간격

            // 레인 중심 위치 계산 (NoteSpawner와 동일한 방식)
            // Lane i: (i - 1.5) * laneWidth
            float[] laneCenters = new float[touchZoneCount];
            for (int i = 0; i < touchZoneCount; i++)
            {
                laneCenters[i] = (i - 1.5f) * laneWidth;
            }

            // 5개 경계: 각 레인 사이의 중간점
            laneBoundaries = new float[touchZoneCount + 1];

            // 첫 번째 경계 (레인 0 왼쪽): 레인 0 중심 - laneWidth/2
            float firstBoundaryWorld = laneCenters[0] - laneWidth / 2f;
            Vector3 firstScreenPos = cam.WorldToScreenPoint(new Vector3(firstBoundaryWorld, 0, 0));
            laneBoundaries[0] = Mathf.Clamp01(firstScreenPos.x / Screen.width);

            // 중간 경계들: 인접 레인 중심의 중간점
            for (int i = 1; i < touchZoneCount; i++)
            {
                float midX = (laneCenters[i - 1] + laneCenters[i]) / 2f;
                Vector3 screenPos = cam.WorldToScreenPoint(new Vector3(midX, 0, 0));
                laneBoundaries[i] = Mathf.Clamp01(screenPos.x / Screen.width);
            }

            // 마지막 경계 (레인 3 오른쪽): 레인 3 중심 + laneWidth/2
            float lastBoundaryWorld = laneCenters[touchZoneCount - 1] + laneWidth / 2f;
            Vector3 lastScreenPos = cam.WorldToScreenPoint(new Vector3(lastBoundaryWorld, 0, 0));
            laneBoundaries[touchZoneCount] = Mathf.Clamp01(lastScreenPos.x / Screen.width);

            // 좌우 끝을 0/1로 보장 (화면 밖 터치도 가장자리 레인으로)
            laneBoundaries[0] = 0f;
            laneBoundaries[touchZoneCount] = 1f;

#if UNITY_EDITOR
            string boundaryStr = "";
            for (int j = 0; j < laneBoundaries.Length; j++)
                boundaryStr += (j > 0 ? ", " : "") + laneBoundaries[j].ToString("F3");
            Debug.Log($"[InputHandler] Lane boundaries ({laneBoundaries.Length}): {boundaryStr}");

            // 레인 중심도 로그
            string centerStr = "";
            for (int j = 0; j < laneCenters.Length; j++)
            {
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(laneCenters[j], 0, 0));
                centerStr += (j > 0 ? ", " : "") + (sp.x / Screen.width).ToString("F3");
            }
            Debug.Log($"[InputHandler] Lane centers (normalized): {centerStr}");
#endif
        }

        /// <summary>
        /// DPI 기반 스크래치 임계값을 픽셀 단위로 캐시
        /// </summary>
        private void CacheScratchThreshold()
        {
            float dpi = Screen.dpi > 0 ? Screen.dpi : fallbackDPI;
            float mmToPixel = dpi / 25.4f; // 1mm = dpi/25.4 pixels
            cachedScratchThreshold = mmToPixel * scratchThresholdMM;
        }

        /// <summary>
        /// 코루틴 기반 입력 루프 (Update() 미호출 문제 우회)
        /// </summary>
        private IEnumerator InputLoop()
        {
            // 2프레임 대기: GameplayController.Start()의 AdjustCameraForPortrait() 완료 보장
            yield return null;
            yield return null;

            try
            {
                CacheLaneBoundaries();
                cachedScreenWidth = Screen.width;
                cachedScreenHeight = Screen.height;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InputHandler] CacheLaneBoundaries failed: {e.Message}. Using fallback.");
                laneBoundaries = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
            }

            while (true)
            {
                yield return null;
                if (!isEnabled) continue;

                // 화면 크기 변경 시 레인 경계 재계산 (회전/해상도 변경 대응)
                if (Screen.width != cachedScreenWidth || Screen.height != cachedScreenHeight)
                {
                    cachedScreenWidth = Screen.width;
                    cachedScreenHeight = Screen.height;
                    CacheScratchThreshold();
                    try { CacheLaneBoundaries(); }
                    catch { laneBoundaries = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f }; }
                }

                ProcessTouchInput();
                ProcessKeyboardInput();
            }
        }

        private void ProcessTouchInput()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                int touchId = touch.fingerId;

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandleTouchBegan(touchId, touch.position);
                        break;

                    case TouchPhase.Moved:
                        HandleTouchMoved(touchId, touch.position);
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        HandleTouchEnded(touchId);
                        break;
                }
            }
        }

        private void HandleTouchBegan(int touchId, Vector2 position)
        {
            // 하단 터치 영역만 입력 허용 (상단 UI 영역 무시)
            if (position.y > Screen.height * touchAreaRatio)
            {
                if (showTouchDebug)
                    Debug.Log($"[Touch] REJECTED y={position.y:F0}/{Screen.height} (>{touchAreaRatio*100}% cutoff)");
                return;
            }

            float normalizedX = position.x / Screen.width;

            // 4존 직접 매핑: 존 번호 = 레인 번호
            int zone = GetTouchZone(position);
            int lane = zone; // 4존이므로 존=레인 직접 매핑
            bool isEdge = zone == 0 || zone == touchZoneCount - 1;

            activeTouches[touchId] = new TouchData
            {
                TouchZone = zone,
                MappedLane = lane,
                StartPosition = position,
                IsEdgeZone = isEdge,
                ScratchTriggered = false
            };

            // 모든 레인에서 Down 발행 (스크래치 레인도 Down으로 처리)
            OnLaneInput?.Invoke(lane, InputType.Down);

            // 가장자리 존(레인 0, 3)에서는 추가로 Scratch 이벤트도 발행
            if (isEdge)
            {
                OnLaneInput?.Invoke(lane, InputType.Scratch);
                var data = activeTouches[touchId];
                data.ScratchTriggered = true;
                activeTouches[touchId] = data;
            }

            if (showTouchDebug)
                Debug.Log($"[Touch] DOWN lane={lane} zone={zone} edge={isEdge} x={normalizedX:F2} pos=({position.x:F0},{position.y:F0})");
        }

        private void HandleTouchMoved(int touchId, Vector2 position)
        {
            if (!activeTouches.TryGetValue(touchId, out TouchData data))
                return;

            // 가장자리 존에서 스와이프 스크래치 감지
            if (data.IsEdgeZone && !data.ScratchTriggered)
            {
                float yDelta = position.y - data.StartPosition.y;
                if (Mathf.Abs(yDelta) > cachedScratchThreshold)
                {
                    int scratchLane = GetScratchLane(data.TouchZone);
                    if (scratchLane >= 0)
                    {
                        OnLaneInput?.Invoke(scratchLane, InputType.Scratch);
                        data.ScratchTriggered = true;
                        // 시작 위치 리셋 (연속 스크래치 가능)
                        data.StartPosition = position;
                    }
                }
            }
            else if (data.IsEdgeZone && data.ScratchTriggered)
            {
                // 이미 스크래치 발동 후 계속 이동 중: 추가 스크래치 체크
                float yDelta = position.y - data.StartPosition.y;
                if (Mathf.Abs(yDelta) > cachedScratchThreshold)
                {
                    int scratchLane = GetScratchLane(data.TouchZone);
                    if (scratchLane >= 0)
                    {
                        OnLaneInput?.Invoke(scratchLane, InputType.Scratch);
                        data.StartPosition = position;
                    }
                }
            }
            // 일반 키 터치 이동: Hold 이벤트 불필요
            // GameplayController에서 Hold를 처리하지 않으므로 발화하지 않음
            // 롱노트 유지 확인은 Down/Up 이벤트 조합으로 처리됨

            activeTouches[touchId] = data;
        }

        private void HandleTouchEnded(int touchId)
        {
            if (activeTouches.TryGetValue(touchId, out TouchData data))
            {
                // 모든 존에서 Key Up 발행 (롱노트 릴리즈 처리)
                OnLaneInput?.Invoke(data.MappedLane, InputType.Up);
                activeTouches.Remove(touchId);
            }
        }

        /// <summary>
        /// 화면 X 좌표 → 터치 존 (0~3) 매핑
        /// 카메라 기반 레인 경계 사용
        /// </summary>
        private int GetTouchZone(Vector2 screenPosition)
        {
            float normalizedX = screenPosition.x / Screen.width;

            if (laneBoundaries != null && laneBoundaries.Length == touchZoneCount + 1)
            {
                for (int i = 0; i < touchZoneCount; i++)
                {
                    if (normalizedX < laneBoundaries[i + 1])
                        return i;
                }
                return touchZoneCount - 1;
            }

            // 폴백: 균등 분할
            int zone = Mathf.FloorToInt(normalizedX * touchZoneCount);
            return Mathf.Clamp(zone, 0, touchZoneCount - 1);
        }

        /// <summary>
        /// 가장자리 존 → 스크래치 레인 매핑
        /// 존 0 → Lane 0 (Scratch L), 존 3 → Lane 3 (Scratch R)
        /// </summary>
        private int GetScratchLane(int zone)
        {
            if (zone == 0) return 0;                      // Scratch L
            if (zone == touchZoneCount - 1) return 3;     // Scratch R
            return -1; // 스크래치 불가
        }

        private void ProcessKeyboardInput()
        {
            // 디버그용 키보드 입력 (4레인 매핑)
            // 왼쪽 스크래치: S(상), X(하)
            // 키 1-2: D, F
            // 오른쪽 스크래치: L(상), ;(하)

            // 스크래치 왼쪽 (Lane 0)
            if (Input.GetKeyDown(KeyCode.S)) OnLaneInput?.Invoke(0, InputType.Scratch);
            if (Input.GetKeyDown(KeyCode.X)) OnLaneInput?.Invoke(0, InputType.Scratch);

            // 키 1-2
            ProcessKey(KeyCode.D, 1);
            ProcessKey(KeyCode.F, 2);

            // 스크래치 오른쪽 (Lane 3)
            if (Input.GetKeyDown(KeyCode.L)) OnLaneInput?.Invoke(3, InputType.Scratch);
            if (Input.GetKeyDown(KeyCode.Semicolon)) OnLaneInput?.Invoke(3, InputType.Scratch);
        }

        private void ProcessKey(KeyCode key, int lane)
        {
            if (Input.GetKeyDown(key))
                OnLaneInput?.Invoke(lane, InputType.Down);
            else if (Input.GetKeyUp(key))
                OnLaneInput?.Invoke(lane, InputType.Up);
            // Hold 이벤트 제거: GameplayController.HandleInput에서 Hold case를
            // 처리하지 않으므로 매 프레임 불필요한 이벤트 발화 방지
        }
    }
}
