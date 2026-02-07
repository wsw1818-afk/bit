using UnityEngine;
using System.Collections;
using AIBeat.Core;
using AIBeat.Data;

namespace AIBeat.Gameplay
{
    /// <summary>
    /// 개별 노트 오브젝트
    /// </summary>
    public class Note : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private MeshRenderer meshRenderer;

        private NoteData noteData;
        private float speed;
        private float judgementLineY;
        private Vector3 originalScale = new Vector3(0.8f, 0.3f, 1f);  // 기본 스케일
        private bool scaleInitialized;

        public NoteType NoteType => noteData.Type;
        public int LaneIndex => noteData.LaneIndex;
        public float HitTime => noteData.HitTime;
        public float Duration => noteData.Duration;
        public bool IsExpired
        {
            get
            {
                if (hasBeenJudged) return true;
                // 롱노트가 홀드 중이면 만료되지 않음
                if (isHolding) return false;

                // AudioManager null 체크
                if (AudioManager.Instance == null)
                {
                    Debug.LogWarning("[Note] AudioManager.Instance is null in IsExpired");
                    return false; // AudioManager가 없으면 만료 안됨 (안전)
                }

                float currentTime = AudioManager.Instance.CurrentTime;
                // 롱노트는 HitTime + Duration + 여유시간 이후에 만료
                float expireAfter = noteData.Type == NoteType.Long
                    ? noteData.HitTime + noteData.Duration + 0.5f
                    : noteData.HitTime + 0.5f;
                return currentTime > expireAfter;
            }
        }
        public bool HasBeenJudged => hasBeenJudged;

        public void MarkAsJudged() { hasBeenJudged = true; }

        private bool hasBeenJudged = false;
        private bool isHolding;
        private float holdStartTime;

        private void Awake()
        {
            CacheComponents();
        }

        private Coroutine moveCoroutine;

        private void OnEnable()
        {
            CacheComponents();
            moveCoroutine = StartCoroutine(MoveCoroutine());
        }

        private void OnDisable()
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
        }

        private void CacheComponents()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            // 스케일 초기화 (한 번만)
            if (!scaleInitialized && transform.localScale.sqrMagnitude > 0.01f)
            {
                originalScale = transform.localScale;
                scaleInitialized = true;
            }
        }

        public void Initialize(NoteData data, float noteSpeed, float judgeY)
        {
            noteData = data;
            speed = noteSpeed;
            judgementLineY = judgeY;
            isHolding = false;
            hasBeenJudged = false;

            // 스케일이 0이면 기본값 복원
            if (transform.localScale.sqrMagnitude < 0.01f)
                transform.localScale = originalScale;

            // 롱노트는 길이 조절
            if (data.Type == NoteType.Long)
            {
                float length = data.Duration * speed;
                transform.localScale = new Vector3(originalScale.x, length, originalScale.z);
            }
            else
            {
                transform.localScale = originalScale;
            }
        }

        /// <summary>
        /// 코루틴 기반 노트 이동 (시간 기반 절대 위치)
        /// x좌표는 Move 시작 시 이미 설정되어 있으므로 y만 변경
        /// </summary>
        private IEnumerator MoveCoroutine()
        {
            while (true)
            {
                yield return null;
                if (AudioManager.Instance == null) continue;

                float currentTime = AudioManager.Instance.CurrentTime;
                var pos = transform.position;
                pos.y = judgementLineY + (noteData.HitTime - currentTime) * speed;
                transform.position = pos;
            }
        }

        public void Reset()
        {
            noteData = default;
            isHolding = false;
            hasBeenJudged = false;
            transform.localScale = originalScale;
        }

        /// <summary>
        /// 현재 시간과 노트 시간의 차이
        /// </summary>
        public float GetTimeDifference(float currentTime)
        {
            return currentTime - noteData.HitTime;
        }

        /// <summary>
        /// 롱노트 홀드 시작
        /// </summary>
        public void StartHold(float time)
        {
            if (noteData.Type != NoteType.Long) return;
            isHolding = true;
            holdStartTime = time;
        }

        /// <summary>
        /// 롱노트 홀드 종료
        /// </summary>
        public bool EndHold(float time)
        {
            if (!isHolding) return false;

            isHolding = false;
            float holdDuration = time - holdStartTime;
            float targetDuration = noteData.Duration;

            // Duration이 0 이하인 경우 방어 (잘못된 데이터)
            if (targetDuration <= 0f)
            {
                Debug.LogWarning($"[Note] Invalid duration: {targetDuration}");
                return true; // 기본 성공 처리
            }

            // 홀드 시간이 목표의 80% 이상이면 성공
            return holdDuration >= targetDuration * 0.8f;
        }

        public bool IsHolding => isHolding;
    }
}
