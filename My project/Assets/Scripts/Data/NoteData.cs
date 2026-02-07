using System;

namespace AIBeat.Data
{
    /// <summary>
    /// 개별 노트 데이터
    /// </summary>
    [Serializable]
    public struct NoteData
    {
        public float HitTime;      // 판정 시간 (곡 시작 기준, 초)
        public int LaneIndex;      // 레인 인덱스 (0-3): 0=ScratchL, 1=Key1, 2=Key2, 3=ScratchR
        public NoteType Type;      // 노트 타입
        public float Duration;     // 롱노트 지속 시간 (초)

        public NoteData(float hitTime, int lane, NoteType type = NoteType.Tap, float duration = 0f)
        {
            HitTime = hitTime;
            LaneIndex = lane;
            Type = type;
            Duration = duration;
        }
    }

    public enum NoteType
    {
        Tap,       // 일반 탭 노트
        Long,      // 롱노트 (홀드)
        Scratch    // 스크래치 노트
    }
}
