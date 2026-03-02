using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AIBeat.Core; // For AssetGenTrigger
using AIBeat.Data;
using AIBeat.UI;


namespace AIBeat.Gameplay
{
    /// <summary>
    /// 노트 생성 및 오브젝트 풀링 관리
    /// </summary>
    public class NoteSpawner : MonoBehaviour
    {
        [Header("Note Prefabs")]
        [SerializeField] private GameObject tapNotePrefab;
        [SerializeField] private GameObject longNotePrefab;
        [SerializeField] private GameObject scratchNotePrefab;

        [Header("Lane Settings")]
        [SerializeField] private Transform[] laneSpawnPoints;  // 4개 레인
        [SerializeField] private Transform judgementLine;

        [Header("Spawn Settings")]
        [SerializeField] private float noteSpeed = 5f;         // 노트 속도 (units/sec)
        [SerializeField] private float spawnDistance = 12f;    // 화면 상단(StatsBar 바로 아래)에서 스폰
        [SerializeField] private float lookAhead = 3f;         // 미리 생성할 시간(초)

        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 100;
        [SerializeField] private int maxPoolSize = 200;
        [SerializeField] private int poolExpandAmount = 20;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private Queue<NoteData> noteQueue;
        private List<Note> activeNotes;
        private Dictionary<NoteType, Queue<Note>> notePools;

        // 메모리 관리: 동적 생성된 프리팹과 머티리얼 추적
        private List<GameObject> dynamicPrefabs = new List<GameObject>();
        private List<Material> managedMaterials = new List<Material>();

        // 풀 동적 확장: 타입별 총 생성 수 추적
        private Dictionary<NoteType, int> poolTotalCounts = new Dictionary<NoteType, int>();

        private float currentMusicTime;
        private bool isSpawning;
        private JudgementSystem judgementSystem;
        private Coroutine spawnLoopCoroutine;

        public float NoteSpeed
        {
            get => noteSpeed;
            set => noteSpeed = Mathf.Clamp(value, 1f, 15f);
        }

        private void Awake()
        {
            noteQueue = new Queue<NoteData>();
            activeNotes = new List<Note>();
            notePools = new Dictionary<NoteType, Queue<Note>>();

            // JudgementSystem 캐싱 (null이면 경고)
            judgementSystem = FindFirstObjectByType<JudgementSystem>();
            if (judgementSystem == null)
            {
                Debug.LogWarning("[NoteSpawner] JudgementSystem not found - miss registration will be disabled");
            }

            // 자동으로 참조 설정 (에디터에서 설정되지 않은 경우)
            AutoSetupReferences();
            InitializePools();

            // SettingsManager에서 노트 속도 적용
            if (SettingsManager.Instance != null)
                noteSpeed = SettingsManager.Instance.NoteSpeed;
            SettingsManager.OnSettingChanged += OnSettingChanged;
        }

        /// <summary>
        /// SettingsManager에서 노트 속도 변경 시 즉시 적용
        /// </summary>
        private void OnSettingChanged(string key, float value)
        {
            if (key == SettingsManager.KEY_NOTE_SPEED)
            {
                noteSpeed = value;
            }
        }

        /// <summary>
        /// 런타임에서 자동으로 Transform 참조를 찾아 연결
        /// </summary>
        private void AutoSetupReferences()
        {
            // JudgementLine 자동 찾기
            if (judgementLine == null)
            {
                var jLine = GameObject.Find("NoteArea/JudgementLine");
                if (jLine != null)
                {
                    judgementLine = jLine.transform;
#if UNITY_EDITOR
                    Debug.Log("[NoteSpawner] JudgementLine auto-connected");
#endif
                }
                else
                {
                    Debug.LogWarning("[NoteSpawner] JudgementLine not found");
                }
            }

            // Lane Spawn Points 자동 찾기
            if (laneSpawnPoints == null || laneSpawnPoints.Length == 0)
            {
                var noteArea = GameObject.Find("NoteArea");
                if (noteArea != null)
                {
                    laneSpawnPoints = new Transform[4];
                    for (int i = 0; i < 4; i++)
                    {
                        var lane = noteArea.transform.Find($"Lane{i}");
                        if (lane != null)
                        {
                            laneSpawnPoints[i] = lane;
                        }
                        else
                        {
                            Debug.LogWarning($"[NoteSpawner] Lane{i} not found");
                        }
                    }
#if UNITY_EDITOR
                    Debug.Log("[NoteSpawner] LaneSpawnPoints auto-connected (4 lanes)");
#endif
                }
                else
                {
                    Debug.LogWarning("[NoteSpawner] NoteArea not found");
                }
            }

            // Music Theme: 씬 프리팹 사용하지 않고 항상 동적 생성 또는 리소스 로드
            // (기존 NormalNote/LongNote/ScratchNote 프리팹은 NoteVisuals 미포함)

            // AssetGenTrigger는 static 클래스이므로 별도 인스턴스 생성 불필요
            // 에셋 생성은 RuntimeInitializeOnLoadMethod로 자동 실행됨 
            // For now, let's just try to load sprites.
            tapNotePrefab = null;
            longNotePrefab = null;
            scratchNotePrefab = null;

        }

        private void InitializePools()
        {
            // 노트 프리팹이 없으면 동적 생성 (Music Theme)
            if (tapNotePrefab == null)
                tapNotePrefab = CreateNotePrefab("TapNote", new Color(1f, 0.84f, 0f));        // Gold (메인 비트)
            if (longNotePrefab == null)
                longNotePrefab = CreateNotePrefab("LongNote", new Color(0.58f, 0.29f, 0.98f)); // Purple (멜로디)
            if (scratchNotePrefab == null)
                scratchNotePrefab = CreateNotePrefab("ScratchNote", new Color(1f, 0.55f, 0f));  // Orange (스크래치)

            // 각 노트 타입별 풀 생성
            notePools[NoteType.Tap] = CreatePool(tapNotePrefab, poolSize);
            notePools[NoteType.Long] = CreatePool(longNotePrefab, poolSize / 2);
            notePools[NoteType.Scratch] = CreatePool(scratchNotePrefab, poolSize / 4);

            // 풀 총 개수 추적 초기화
            poolTotalCounts[NoteType.Tap] = poolSize;
            poolTotalCounts[NoteType.Long] = poolSize / 2;
            poolTotalCounts[NoteType.Scratch] = poolSize / 4;

#if UNITY_EDITOR
            Debug.Log($"[NoteSpawner] Pool initialized - Tap:{notePools[NoteType.Tap].Count}, Long:{notePools[NoteType.Long].Count}, Scratch:{notePools[NoteType.Scratch].Count}");
#endif
        }

        /// <summary>
        /// 노트 프리팹 동적 생성 (MeshRenderer + Quad 방식 - URP 호환)
        /// </summary>
        private GameObject CreateNotePrefab(string name, Color color)
        {
            var noteObj = new GameObject(name);

            // MeshRenderer + Quad 방식 (URP에서 확실히 렌더링됨)
            var filter = noteObj.AddComponent<MeshFilter>();
            var tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            filter.sharedMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;

            // Collider 제거 (Quad에 기본 Collider가 있음)
            var col = tempQuad.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Destroy(tempQuad);

            var renderer = noteObj.AddComponent<MeshRenderer>();

            // URP 호환 셰이더 사용
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            mat.color = color;

            // AI 디자인 노트 텍스처 우선 로드, 없으면 ProceduralImageGenerator 폴백
            Texture2D tex = null;
            if (name == "TapNote")
            {
                tex = Resources.Load<Texture2D>("AIBeat_Design/Notes/NormalNote");
                if (tex == null) tex = ProceduralImageGenerator.CreateNoteTexture(NoteType.Tap);
            }
            else if (name == "LongNote")
            {
                tex = Resources.Load<Texture2D>("AIBeat_Design/Notes/LongNote");
                if (tex == null) tex = ProceduralImageGenerator.CreateNoteTexture(NoteType.Long);
            }
            else if (name == "ScratchNote")
            {
                tex = Resources.Load<Texture2D>("AIBeat_Design/Notes/ScratchNote");
                if (tex == null) tex = ProceduralImageGenerator.CreateNoteTexture(NoteType.Scratch);
            }

            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
            }

            renderer.material = mat;
            renderer.sortingOrder = 500; // 높은 sortingOrder
            managedMaterials.Add(mat);

            // 노트 크기: 1.1 x 0.3
            noteObj.transform.localScale = new Vector3(1.1f, 0.3f, 1f);

#if UNITY_EDITOR
            Debug.Log($"[NoteSpawner] Note {name} created: MeshRenderer, shader={shader?.name}, color={color}, scale={noteObj.transform.localScale}");
#endif

            // Note 컴포넌트 추가
            noteObj.AddComponent<Note>();
            // NoteVisuals 컴포넌트 추가
            noteObj.AddComponent<NoteVisuals>();
            // 글로우 이펙트
            noteObj.AddComponent<NoteGlowEffect>();

            noteObj.SetActive(false);
            dynamicPrefabs.Add(noteObj);

            return noteObj;
        }

        private Queue<Note> CreatePool(GameObject prefab, int size)
        {
            var pool = new Queue<Note>();

            if (prefab == null)
            {
                Debug.LogWarning("[NoteSpawner] Prefab is null, creating empty pool");
                return pool;
            }

            for (int i = 0; i < size; i++)
            {
                var obj = Instantiate(prefab, transform);
                obj.SetActive(false);

                // Note 컴포넌트가 없으면 추가
                var note = obj.GetComponent<Note>();
                if (note == null)
                {
                    note = obj.AddComponent<Note>();
                }

                // SpriteRenderer가 있으면 MeshFilter/MeshRenderer 추가하지 않음
                var sr = obj.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    // MeshFilter에 Quad 메시 보장
                    var mf = obj.GetComponent<MeshFilter>();
                    if (mf == null) mf = obj.AddComponent<MeshFilter>();
                    if (mf.sharedMesh == null)
                    {
                        mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                        if (mf.sharedMesh == null)
                        {
                            var tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            mf.sharedMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
                            Destroy(tempQuad);
                        }
                    }

                    // MeshRenderer + Unlit 머티리얼 보장
                    var mr = obj.GetComponent<MeshRenderer>();
                    if (mr == null) mr = obj.AddComponent<MeshRenderer>();
                    EnsureUnlitMaterial(mr);
                }

                pool.Enqueue(note);
            }
            return pool;
        }

        /// <summary>
        /// 렌더러에 Unlit 머티리얼이 적용되어 있지 않으면 강제로 적용
        /// </summary>
        private void EnsureUnlitMaterial(MeshRenderer renderer)
        {
            if (renderer == null) return;

            var mat = renderer.sharedMaterial;
            // Unlit이 아닌 셰이더를 사용하고 있으면 교체
            if (mat == null || (!mat.shader.name.Contains("Unlit") && !mat.shader.name.Contains("Sprites")))
            {
                Color color = mat != null ? GetMaterialColor(mat) : Color.cyan;
                Texture texture = mat != null ? mat.mainTexture : null;

                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Texture"); // 텍스처 지원 우선
                if (shader == null) shader = Shader.Find("Sprites/Default");

                var newMat = new Material(shader);
                newMat.color = color;
                
                if (texture != null)
                    newMat.mainTexture = texture;
                    
                if (newMat.HasProperty("_BaseColor"))
                    newMat.SetColor("_BaseColor", color);

                renderer.material = newMat;
                managedMaterials.Add(newMat);
            }
        }

        private Color GetMaterialColor(Material mat)
        {
            if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
            if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
            return Color.cyan;
        }

        /// <summary>
        /// 노트 데이터 로드 및 준비
        /// </summary>
        public void LoadNotes(List<NoteData> notes)
        {
            noteQueue.Clear();

            // 시간순 정렬
            notes.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));

            // Note 이동은 시간 기반 절대 위치: Y = judgeY + (hitTime - currentTime) * speed
            // lookAhead로 미리 스폰하므로 별도 오프셋 불필요
            // (이전 minHitTime 오프셋은 lookAhead와 중복되어 초반 노트가 지연되는 버그 유발)
#if UNITY_EDITOR
            if (notes.Count > 0)
                Debug.Log($"[NoteSpawner] First note at {notes[0].HitTime:F2}s, last at {notes[notes.Count-1].HitTime:F2}s, total={notes.Count}");
#endif

            // 필터링: 같은 시간+레인 중복 + 롱노트 구간 내 겹침 제거
            int filteredCount = 0;
            var seen = new HashSet<long>();
            // 각 레인별 롱노트 종료 시점 추적
            var longNoteEndTime = new float[4];

            foreach (var note in notes)
            {
                // 1) 같은 시간 + 같은 레인 중복 필터
                long key = ((long)(note.HitTime * 1000)) * 10 + note.LaneIndex;
                if (!seen.Add(key))
                {
                    filteredCount++;
                    continue;
                }

                // 2) 롱노트 구간 내 같은 레인에 다른 노트 겹침 필터
                int lane = note.LaneIndex;
                if (lane >= 0 && lane < 4 && note.Type != NoteType.Long && longNoteEndTime[lane] > note.HitTime + 0.05f)
                {
                    filteredCount++;
                    continue;
                }

                // 롱노트면 종료 시점 기록
                if (note.Type == NoteType.Long && lane >= 0 && lane < 4)
                {
                    longNoteEndTime[lane] = note.HitTime + note.Duration;
                }

                noteQueue.Enqueue(note);
            }

#if UNITY_EDITOR
            if (filteredCount > 0)
                Debug.LogWarning($"[NoteSpawner] Filtered {filteredCount} overlapping notes");
            Debug.Log($"[NoteSpawner] Loaded {noteQueue.Count} notes (from {notes.Count} total)");
#endif
        }

        /// <summary>
        /// 노트 스폰 시작 (코루틴 기반)
        /// </summary>
        public void StartSpawning()
        {
            isSpawning = true;
            currentMusicTime = 0f;
            if (spawnLoopCoroutine != null)
                StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = StartCoroutine(SpawnLoop());
        }

        /// <summary>
        /// 노트 스폰 중지
        /// </summary>
        public void StopSpawning()
        {
            isSpawning = false;
            if (spawnLoopCoroutine != null)
            {
                StopCoroutine(spawnLoopCoroutine);
                spawnLoopCoroutine = null;
            }

            // 활성 노트 모두 반환
            foreach (var note in activeNotes)
            {
                ReturnToPool(note);
            }
            activeNotes.Clear();
        }

        /// <summary>
        /// 남은 활성 노트 + 대기열 노트를 모두 MISS 처리
        /// 곡 종료 시 호출하여 정확한 결과 산출
        /// </summary>
        public void FlushRemainingAsMiss(JudgementSystem js)
        {
            if (js == null) return;

            // 활성 노트 MISS 처리
            int flushed = 0;
            foreach (var note in activeNotes)
            {
                js.RegisterMiss();
                flushed++;
            }

            // 대기열에 남은 노트도 MISS 처리
            while (noteQueue != null && noteQueue.Count > 0)
            {
                noteQueue.Dequeue();
                js.RegisterMiss();
                flushed++;
            }

#if UNITY_EDITOR
            if (flushed > 0)
                Debug.Log($"[NoteSpawner] Flushed {flushed} remaining notes as MISS");
#endif
        }

        /// <summary>
        /// 코루틴 기반 스폰 루프 (Update() 미호출 문제 우회)
        /// </summary>
        private IEnumerator SpawnLoop()
        {
#if UNITY_EDITOR
            Debug.Log("[NoteSpawner] SpawnLoop coroutine started");
#endif
            while (isSpawning)
            {
                yield return null; // 매 프레임 대기

                if (noteQueue == null || activeNotes == null) continue;

                // 현재 음악 시간 업데이트 (AudioManager에서 가져옴)
                currentMusicTime = Core.AudioManager.Instance?.CurrentTime ?? 0f;

                SpawnUpcomingNotes();
                UpdateActiveNotes();
            }
#if UNITY_EDITOR
            Debug.Log("[NoteSpawner] SpawnLoop coroutine ended");
#endif
        }

        private void SpawnUpcomingNotes()
        {
            if (noteQueue == null) return;

            // lookAhead 시간 내에 도달할 노트들 생성
            float spawnTime = currentMusicTime + lookAhead;

            while (noteQueue.Count > 0 && noteQueue.Peek().HitTime <= spawnTime)
            {
                var noteData = noteQueue.Dequeue();
                SpawnNote(noteData);
            }
        }

        private void SpawnNote(NoteData data)
        {
            if (!notePools.TryGetValue(data.Type, out var pool))
            {
                Debug.LogWarning($"[NoteSpawner] No pool for type: {data.Type}");
                return;
            }

            // 풀이 비었으면 동적 확장 시도
            if (pool.Count == 0)
            {
                if (!ExpandPool(data.Type))
                {
                    Debug.LogWarning($"[NoteSpawner] Pool exhausted for {data.Type} (max={maxPoolSize})");
                    return;
                }
            }

            var note = pool.Dequeue();

            // 판정선 Y 위치 (judgementLine이 없으면 기본값 -5)
            float judgeY = judgementLine != null ? judgementLine.position.y : -5f;

            // Initialize를 SetActive 전에 호출 (OnEnable의 MoveCoroutine이 올바른 값 사용하도록)
            note.Initialize(data, noteSpeed, judgeY);

            // 스폰 위치 계산
            Vector3 spawnPos = GetSpawnPosition(data.LaneIndex);
            note.transform.position = spawnPos;

            note.gameObject.SetActive(true);

            activeNotes.Add(note);

#if UNITY_EDITOR
            if (showDebugLogs)
            {
                var pos = note.transform.position;
                var scl = note.transform.localScale;
                var mr = note.GetComponent<MeshRenderer>();
                Debug.Log($"[NoteSpawner] Spawned {data.Type} note at lane {data.LaneIndex}, hitTime: {data.HitTime:F2}s | Active: {activeNotes.Count} | pos=({pos.x:F1},{pos.y:F1},{pos.z:F1}) scale=({scl.x:F2},{scl.y:F2},{scl.z:F2}) | MeshRenderer: enabled={mr?.enabled}, visible={mr?.isVisible}");
            }
#endif
        }

        private Vector3 GetSpawnPosition(int laneIndex)
        {
            // 노트 Z 위치: 배경(Z=2)보다 카메라(Z=-10)에 가깝게 → Z=-1이 배경 앞
            const float noteZ = -1f;

            // 항상 중앙 기준으로 레인 위치 계산 (화면 중앙 = X:0)
            // 레인 간격 (넓게 조정)
            const float laneWidth = 1.4f;
            float x = (laneIndex - 1.5f) * laneWidth;  // 레인 0~3이 중앙 기준 배치
            return new Vector3(x, spawnDistance, noteZ);
        }

        private void UpdateActiveNotes()
        {
            if (activeNotes == null) return;
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                var note = activeNotes[i];

                if (note.IsExpired)
                {
                    // Miss 처리 (캐싱된 참조 사용)
                    judgementSystem?.RegisterMiss();

#if UNITY_EDITOR
                    if (showDebugLogs)
                    {
                        Debug.Log($"[NoteSpawner] Note expired (lane {note.LaneIndex}, type={note.NoteType}, hitTime={note.HitTime:F2}s, dur={note.Duration:F2}s, holding={note.IsHolding}) - returned to pool");
                    }
#endif

                    ReturnToPool(note);
                    activeNotes.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 풀 동적 확장: maxPoolSize까지 poolExpandAmount개씩 추가
        /// </summary>
        private bool ExpandPool(NoteType type)
        {
            if (!poolTotalCounts.TryGetValue(type, out int currentTotal) || currentTotal >= maxPoolSize)
                return false;

            GameObject prefab = type switch
            {
                NoteType.Tap => tapNotePrefab,
                NoteType.Long => longNotePrefab,
                NoteType.Scratch => scratchNotePrefab,
                _ => null
            };

            if (prefab == null) return false;

            int toAdd = Mathf.Min(poolExpandAmount, maxPoolSize - currentTotal);
            var pool = notePools[type];

            for (int i = 0; i < toAdd; i++)
            {
                var obj = Instantiate(prefab, transform);
                obj.SetActive(false);

                var note = obj.GetComponent<Note>();
                if (note == null) note = obj.AddComponent<Note>();

                var sr = obj.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    var mf = obj.GetComponent<MeshFilter>();
                    if (mf == null) mf = obj.AddComponent<MeshFilter>();
                    if (mf.sharedMesh == null)
                    {
                        mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                        if (mf.sharedMesh == null)
                        {
                            var tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            mf.sharedMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
                            Destroy(tempQuad);
                        }
                    }

                    var mr = obj.GetComponent<MeshRenderer>();
                    if (mr == null) mr = obj.AddComponent<MeshRenderer>();
                    EnsureUnlitMaterial(mr);
                }

                pool.Enqueue(note);
            }

            poolTotalCounts[type] = currentTotal + toAdd;

#if UNITY_EDITOR
            Debug.Log($"[NoteSpawner] Pool expanded: {type} +{toAdd} (total={poolTotalCounts[type]})");
#endif
            return true;
        }

        private void ReturnToPool(Note note)
        {
            // Reset 전에 타입을 저장 (Reset이 noteData를 default로 초기화하므로)
            var noteType = note.NoteType;
            note.gameObject.SetActive(false);
            note.Reset();

            if (notePools.TryGetValue(noteType, out var pool))
            {
                pool.Enqueue(note);
            }
        }

        /// <summary>
        /// 특정 레인의 가장 가까운 활성 노트 가져오기
        /// 판정 가능한 범위(badWindow 기반) 내의 노트만 반환
        /// </summary>
        public Note GetNearestNote(int laneIndex)
        {
            // 레인 인덱스 유효성 검사 (0-3)
            if (laneIndex < 0 || laneIndex > 3)
            {
                Debug.LogWarning($"[NoteSpawner] Invalid lane index: {laneIndex}");
                return null;
            }

            // AudioManager null 체크
            if (Core.AudioManager.Instance == null)
            {
                Debug.LogWarning("[NoteSpawner] AudioManager.Instance is null");
                return null;
            }

            Note nearest = null;
            float minTimeDiff = float.MaxValue;
            float currentTime = Core.AudioManager.Instance.CurrentTime;

            // 판정 가능 최대 윈도우 (350ms = badWindow)
            const float maxJudgeWindow = 0.350f;

            foreach (var note in activeNotes)
            {
                if (note.LaneIndex == laneIndex)
                {
                    // 홀드 중인 롱노트는 항상 우선 반환
                    if (note.IsHolding)
                        return note;

                    float timeDiff = Mathf.Abs(currentTime - note.HitTime);
                    if (timeDiff <= maxJudgeWindow && timeDiff < minTimeDiff)
                    {
                        minTimeDiff = timeDiff;
                        nearest = note;
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// 노트 제거 (판정 후)
        /// </summary>
        public void RemoveNote(Note note)
        {
            if (activeNotes.Remove(note))
            {
                ReturnToPool(note);
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();

            SettingsManager.OnSettingChanged -= OnSettingChanged;

            // 동적 생성 머티리얼 정리
            if (managedMaterials != null)
            {
                foreach (var mat in managedMaterials)
                {
                    if (mat != null) Destroy(mat);
                }
                managedMaterials.Clear();
            }

            // 동적 생성 프리팹 정리
            if (dynamicPrefabs != null)
            {
                foreach (var go in dynamicPrefabs)
                {
                    if (go != null) Destroy(go);
                }
                dynamicPrefabs.Clear();
            }
        }
    }
}
