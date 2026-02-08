using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AIBeat.Core;
using AIBeat.Data;

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
        [SerializeField] private float spawnDistance = 15f;    // 판정선까지의 거리
        [SerializeField] private float lookAhead = 3f;         // 미리 생성할 시간(초)

        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 100;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private Queue<NoteData> noteQueue;
        private List<Note> activeNotes;
        private Dictionary<NoteType, Queue<Note>> notePools;

        // 메모리 관리: 동적 생성된 프리팹과 머티리얼 추적
        private List<GameObject> dynamicPrefabs = new List<GameObject>();
        private List<Material> managedMaterials = new List<Material>();

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

            // Note Prefabs 자동 찾기 (Resources 폴더에서)
            if (tapNotePrefab == null)
            {
                var normalNote = GameObject.Find("NormalNote");
                if (normalNote != null) tapNotePrefab = normalNote;
            }
            if (longNotePrefab == null)
            {
                var longNote = GameObject.Find("LongNote");
                if (longNote != null) longNotePrefab = longNote;
            }
            if (scratchNotePrefab == null)
            {
                var scratchNote = GameObject.Find("ScratchNote");
                if (scratchNote != null) scratchNotePrefab = scratchNote;
            }
        }

        private void InitializePools()
        {
            // 노트 프리팹이 없으면 동적 생성 (음악 테마 컬러)
            if (tapNotePrefab == null)
                tapNotePrefab = CreateNotePrefab("TapNote", new Color(0.2f, 0.7f, 1f));      // 네온 블루 (비트)
            if (longNotePrefab == null)
                longNotePrefab = CreateNotePrefab("LongNote", new Color(0.3f, 1f, 0.7f));     // 민트 그린 (멜로디)
            if (scratchNotePrefab == null)
                scratchNotePrefab = CreateNotePrefab("ScratchNote", new Color(0.8f, 0.3f, 1f)); // 바이올렛 (스크래치/DJ)

            // 각 노트 타입별 풀 생성
            notePools[NoteType.Tap] = CreatePool(tapNotePrefab, poolSize);
            notePools[NoteType.Long] = CreatePool(longNotePrefab, poolSize / 2);
            notePools[NoteType.Scratch] = CreatePool(scratchNotePrefab, poolSize / 4);

#if UNITY_EDITOR
            Debug.Log($"[NoteSpawner] Pool initialized - Tap:{notePools[NoteType.Tap].Count}, Long:{notePools[NoteType.Long].Count}, Scratch:{notePools[NoteType.Scratch].Count}");
#endif
        }

        /// <summary>
        /// 노트 프리팹 동적 생성
        /// </summary>
        private GameObject CreateNotePrefab(string name, Color color)
        {
            var noteObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            noteObj.name = name;
            noteObj.transform.localScale = new Vector3(0.82f, 0.22f, 1f);

            // Collider 제거 (불필요)
            var collider = noteObj.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // 머티리얼 설정 (Unlit 셰이더 사용 - 조명 없이도 색상 표시)
            var renderer = noteObj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                // Unlit 셰이더 찾기 (조명 영향 없이 색상만 표시)
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");

                var mat = new Material(shader);
                mat.color = color;

                // URP Unlit의 경우 _BaseColor 사용
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);

                renderer.material = mat;
                managedMaterials.Add(mat);
            }

            // Note 컴포넌트 추가
            noteObj.AddComponent<Note>();

            noteObj.SetActive(false);
            dynamicPrefabs.Add(noteObj);
#if UNITY_EDITOR
            Debug.Log($"[NoteSpawner] Created note prefab: {name}");
#endif
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
            if (mat == null || !mat.shader.name.Contains("Unlit"))
            {
                Color color = mat != null ? GetMaterialColor(mat) : Color.cyan;

                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Sprites/Default");

                var newMat = new Material(shader);
                newMat.color = color;
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

            // 최소 시작 시간 보장: lookAhead 시간보다 이른 노트는 화면 밖에서 시작할 수 없음
            // 노트가 화면 위에서 충분히 내려올 시간(lookAhead초)을 확보
            float minHitTime = lookAhead;
            if (notes.Count > 0 && notes[0].HitTime < minHitTime)
            {
                float offset = minHitTime - notes[0].HitTime;
                Debug.Log($"[NoteSpawner] Notes start too early ({notes[0].HitTime:F2}s), adding {offset:F2}s offset to all notes");
                for (int i = 0; i < notes.Count; i++)
                {
                    var n = notes[i];
                    notes[i] = new NoteData(n.HitTime + offset, n.LaneIndex, n.Type, n.Duration);
                }
            }

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
            if (!notePools.TryGetValue(data.Type, out var pool) || pool.Count == 0)
            {
                Debug.LogWarning($"[NoteSpawner] No available notes in pool for type: {data.Type}");
                return;
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
                Debug.Log($"[NoteSpawner] Spawned {data.Type} note at lane {data.LaneIndex}, hitTime: {data.HitTime:F2}s | Active: {activeNotes.Count}");
            }
#endif
        }

        private Vector3 GetSpawnPosition(int laneIndex)
        {
            // 노트 Z 위치 (배경 Z=1보다 앞에 표시되도록 -1)
            const float noteZ = -1f;

            // laneSpawnPoints 유효성 검사
            if (laneSpawnPoints == null || laneSpawnPoints.Length == 0)
            {
                // 기본 레인 위치 계산 (중앙 기준 -1.5 ~ +1.5)
                float x = -1.5f + laneIndex;
                return new Vector3(x, spawnDistance, noteZ);
            }

            if (laneIndex < 0 || laneIndex >= laneSpawnPoints.Length)
            {
                Debug.LogWarning($"[NoteSpawner] Invalid lane index: {laneIndex}");
                return new Vector3(0f, spawnDistance, noteZ);
            }

            if (laneSpawnPoints[laneIndex] == null)
            {
                float x = -1.5f + laneIndex;
                return new Vector3(x, spawnDistance, noteZ);
            }

            Vector3 pos = laneSpawnPoints[laneIndex].position;
            pos.y += spawnDistance;
            pos.z = noteZ;  // 노트가 배경 앞에 표시되도록
            return pos;
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
