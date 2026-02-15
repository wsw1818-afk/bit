using UnityEngine;
using System.Collections;
using AIBeat.Core;
using AIBeat.Data;
using AIBeat.UI;

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
        private SpriteRenderer bodyRenderer; // 롱노트 바디용


        private NoteData noteData;
        private float speed;
        private float judgementLineY;
        private Vector3 originalScale = new Vector3(0.8f, 0.45f, 1f);  // 기본 스케일 (높이 1.5배)
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
        public bool IsHolding => isHolding;

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

            // 롱노트는 길이 조절 (Head + Body 구조)
            if (data.Type == NoteType.Long)
            {
                // 1. Main Renderer is Head (don't stretch)
                transform.localScale = originalScale;
                
                // 2. Setup Body
                SetupLongNoteBody(data.Duration * speed);
            }
            else
            {
                transform.localScale = originalScale;
                if (bodyRenderer != null) bodyRenderer.gameObject.SetActive(false);
            }


            // Lane Color 적용 (NoteVisuals 컴포넌트 사용)
            var visuals = GetComponent<NoteVisuals>();
            if (visuals != null)
            {
                visuals.SetLaneColor(data.LaneIndex);
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

        private void SetupLongNoteBody(float length)
        {
            if (bodyRenderer == null)
            {
                var bodyGo = new GameObject("BodyVisual");
                bodyGo.transform.SetParent(transform, false);
                bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
                
                // Load Body Sprite
                var bodySprite = Resources.Load<Sprite>("AIBeat_Design/Notes/LongNoteBody");
                if (bodySprite == null)
                {
                    // Fallback runtime generation if missing
                    var tex = ProceduralImageGenerator.CreateLongNoteBodyTexture();
                    bodySprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f, 0.5f));
                }
                bodyRenderer.sprite = bodySprite;
                bodyRenderer.drawMode = SpriteDrawMode.Sliced; // Or Tiled if configured 
            }

            bodyRenderer.gameObject.SetActive(true);
            
            // Transform settings
            // Body should start from center (Head) and go UP.
            // Adjust Y position to be length/2
            // Length is in World Units.
            // Note scale (originalScale) affects child? Yes.
            // If Note scale is (0.8, 0.3, 1), child (1, 1, 1) is (0.8, 0.3, 1).
            // We want Body to be long.
            
            // Reset local scale of body to counteract parent Y scale if needed, 
            // OR just set Y scale to length / parent.y
            
            float parentY = transform.localScale.y;
            float targetBodyScaleY = length / (parentY > 0 ? parentY : 1f);
            
            // bodyRenderer.size = new Vector2(1f, targetBodyScaleY); // If Sliced/Tiled
            // Or just simple scale
            bodyRenderer.transform.localScale = new Vector3(1f, targetBodyScaleY, 1f);
            
            // Position: Center of body is at length/2
            // We want bottom of body at 0.
            // Sprite pivot is Center (0.5).
            // So shift Y by targetBodyScaleY / 2 ?
            // Wait, scaling is from center.
            // If we simply move it up:
            bodyRenderer.transform.localPosition = new Vector3(0f, targetBodyScaleY * 0.5f, 0f);
            
            // Sorting Order: Body behind Head
            if (spriteRenderer != null)
            {
                bodyRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
                bodyRenderer.color = spriteRenderer.color; // Match color (Purple)
            }
        }
    }
}

