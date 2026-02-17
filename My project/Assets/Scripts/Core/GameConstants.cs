namespace AIBeat.Core
{
    /// <summary>
    /// 게임 전체에서 사용되는 상수 (Magic Number 제거)
    /// </summary>
    public static class GameConstants
    {
        // 레인 설정
        public const int LaneCount = 4;
        public const float LaneWidth = 1.4f;
        public const float TotalLaneWidth = LaneCount * LaneWidth; // 5.6f

        // 노트 설정
        public const float DefaultNoteSpeed = 5f;
        public const float MinNoteSpeed = 1f;
        public const float MaxNoteSpeed = 15f;
        public const float DefaultSpawnDistance = 12f;
        public const float DefaultLookAhead = 3f;

        // 판정 윈도우 (초)
        public const float PerfectWindow = 0.050f;   // ±50ms
        public const float GreatWindow = 0.100f;     // ±100ms
        public const float GoodWindow = 0.200f;      // ±200ms
        public const float BadWindow = 0.350f;       // ±350ms

        // 점수 설정
        public const int BaseScorePerNote = 1000;
        public const float MaxComboBonus = 0.5f;       // 최대 50% 보너스
        public const int ComboForMaxBonus = 100;

        // 롱노트 홀드 보너스
        public const float HoldBonusTick = 0.1f;       // 0.1초마다 보너스
        public const int HoldBonusPerTick = 50;         // 틱당 50점

        // 오브젝트 풀링
        public const int DefaultPoolSize = 100;

        // 카운트다운
        public const float DefaultCountdownTime = 3f;

        // 카메라
        public const float CameraYPosition = 6f;
        public const float MinOrthoSize = 7f;
        public const float LanePadding = 0.3f;
    }
}
