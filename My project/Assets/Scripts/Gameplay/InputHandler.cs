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
        [SerializeField] private int touchZoneCount = 2;       // 2개 균등 터치 존 (Key1, Key2)
        [SerializeField] private float touchAreaRatio = 0.85f;  // 하단 85% 입력 영역 (상단 UI만 제외)
        [SerializeField] private float scratchEdgeRatio = 0.08f; // 좌우 가장자리 8%를 스크래치 전용 존으로

        [Header("Debug")]
        [SerializeField] private bool showTouchDebug = true;

        [Header("Scratch Swipe Settings")]
        [SerializeField] private float scratchThresholdMM = 7f;  // 스크래치 인식 최소 거리(mm)
        [SerializeField] private float fallbackDPI = 160f;       // DPI 미지원 기기 폴백값

        private Dictionary<int, TouchData> activeTouches = new Dictionary<int, TouchData>();
        private bool isEnabled = true;
        private float cachedScratchThreshold; // 픽셀 단위 캐시

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
            public int TouchZone;          // 0-1 (2개 존)
            public int MappedLane;         // 1-2 (키 레인)
            public Vector2 StartPosition;
            public bool IsEdgeZone;        // 스와이프 스크래치 가능 존 여부
            public bool IsScratchOnly;     // 스크래치 전용 존 (가장자리 터치 → Key Down 미발생)
            public bool ScratchTriggered;  // 스크래치 이미 발동 여부
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        private void Start()
        {
            CacheScratchThreshold();
            StartCoroutine(InputLoop());
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
            while (true)
            {
                yield return null;
                if (!isEnabled) continue;
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
            bool isScratchOnly = normalizedX < scratchEdgeRatio || normalizedX > (1f - scratchEdgeRatio);

            int zone = GetTouchZone(position);
            int lane = TouchZoneToLane(zone);
            bool isEdge = zone == 0 || zone == touchZoneCount - 1;

            activeTouches[touchId] = new TouchData
            {
                TouchZone = zone,
                MappedLane = lane,
                StartPosition = position,
                IsEdgeZone = isEdge,
                IsScratchOnly = isScratchOnly,
                ScratchTriggered = false
            };

            if (isScratchOnly)
            {
                // 스크래치 전용 존: 스크래치 + 키 Down 동시 발행
                // 스크래치 노트와 일반 노트 모두 처리 가능하도록
                int scratchLane = normalizedX < 0.5f ? 0 : 3;
                int keyLane = normalizedX < 0.5f ? 1 : 2; // 가장 가까운 키 레인
                OnLaneInput?.Invoke(scratchLane, InputType.Scratch);
                OnLaneInput?.Invoke(keyLane, InputType.Down);
                var data = activeTouches[touchId];
                data.ScratchTriggered = true;
                data.MappedLane = keyLane; // Up 이벤트 시 키 레인으로 발행
                activeTouches[touchId] = data;
                if (showTouchDebug)
                    Debug.Log($"[Touch] SCRATCH+KEY lane=SC{scratchLane}/K{keyLane} x={normalizedX:F2} pos=({position.x:F0},{position.y:F0})");
            }
            else
            {
                // 키 존: 즉시 Key Down 발행 (리듬게임 반응성 우선)
                OnLaneInput?.Invoke(lane, InputType.Down);
                if (showTouchDebug)
                    Debug.Log($"[Touch] KEY DOWN lane={lane} zone={zone} x={normalizedX:F2} pos=({position.x:F0},{position.y:F0})");
            }
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
        /// 화면 X 좌표 → 터치 존 (0~1) 매핑
        /// 2개 균등 분할 (각 50%)
        /// </summary>
        private int GetTouchZone(Vector2 screenPosition)
        {
            float normalizedX = screenPosition.x / Screen.width;
            int zone = Mathf.FloorToInt(normalizedX * touchZoneCount);
            return Mathf.Clamp(zone, 0, touchZoneCount - 1);
        }

        /// <summary>
        /// 터치 존 → 키 레인 매핑
        /// 존 0 → Lane 1, 존 1 → Lane 2
        /// </summary>
        private int TouchZoneToLane(int zone)
        {
            // 안전한 범위로 클램핑 (0-1 → 1-2)
            int clampedZone = Mathf.Clamp(zone, 0, touchZoneCount - 1);
            return clampedZone + 1;
        }

        /// <summary>
        /// 가장자리 존 → 스크래치 레인 매핑
        /// 존 0 → Lane 0 (Scratch L), 존 1 → Lane 3 (Scratch R)
        /// </summary>
        private int GetScratchLane(int zone)
        {
            // 범위 검증
            if (zone < 0 || zone >= touchZoneCount) return -1;

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
