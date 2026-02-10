using UnityEngine;
using System;
using System.Collections.Generic;
using AIBeat.Core;
using AIBeat.Data;
using AIBeat.Audio;
using AIBeat.UI;

namespace AIBeat.Gameplay
{
    /// <summary>
    /// 게임플레이 씬의 메인 컨트롤러
    /// </summary>
    public class GameplayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NoteSpawner noteSpawner;
        [SerializeField] private JudgementSystem judgementSystem;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private AudioAnalyzer audioAnalyzer;
        [SerializeField] private BeatMapper beatMapper;
        [SerializeField] private SmartBeatMapper smartBeatMapper;

        [Header("UI References")]
        [SerializeField] private GameplayUI gameplayUI;

        [Header("Settings")]
        [SerializeField] private float countdownTime = 3f;

        [Header("Debug")]
#if UNITY_EDITOR
        [SerializeField] private bool debugMode = true;
#else
        private bool debugMode = false; // APK에서는 항상 일반 모드
#endif
        [SerializeField] private bool autoPlay = false;
        [SerializeField] private SongData debugSongData;

        private SongData currentSong;
        private bool isPlaying;
        private bool isPaused;
        private bool isShowingResult;
        private float gameStartTime;

        // 롱노트 홀드 보너스 추적
        private const float HOLD_BONUS_TICK_INTERVAL = 0.1f;  // 0.1초마다 보너스
        private const int HOLD_BONUS_PER_TICK = 50;            // 틱당 50점 보너스
        private Dictionary<Note, float> holdingNotes = new Dictionary<Note, float>(); // 노트 → 마지막 틱 시간

        // 이벤트 핸들러 참조 (구독 해제용)
        private Action<int> scoreChangedHandler;
        private Action<int> comboChangedHandler;
        private Action<int, int> bonusScoreHandler;

        private void Start()
        {
            // timeScale 강제 복원
            Time.timeScale = 1f;
            // 포커스를 잃어도 게임 루프가 계속 실행되도록 설정
            Application.runInBackground = true;
            // 모바일 세로 모드 고정
            Screen.orientation = ScreenOrientation.Portrait;
            // 세로 모드에서 레인이 화면 너비를 꽉 채우도록 카메라 조정
            AdjustCameraForPortrait();
            // LaneBackground Cyberpunk 스타일 적용
            ApplyCyberpunkLaneBackground();
            Initialize();
            StartCoroutine(InputLoop());
            StartCoroutine(HoldBonusTickLoop());
        }

        /// <summary>
        /// 세로 모드에서 4레인이 화면 너비를 꽉 채우도록 카메라 orthographicSize 조정
        /// 레인 범위: -1.5 ~ +2.5 (4유닛), 중심: 0.5
        /// </summary>
        private void AdjustCameraForPortrait()
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic) return;

            float laneWidth = 4f; // 4레인 x 1유닛
            float padding = 0.3f; // 좌우 약간의 여백
            float targetWorldWidth = laneWidth + padding;

            // orthographicSize = (targetWorldWidth / aspect) / 2
            float aspect = (float)Screen.width / Screen.height;
            float requiredOrthoSize = (targetWorldWidth / aspect) / 2f;

            // 최소 orthoSize 보장 (노트 스폰 위치까지 보이도록)
            // 노트는 Y=12에서 스폰, 판정선 Y=0 → 카메라 Y=6, ortho=7이면 -1~13 범위
            cam.orthographicSize = Mathf.Max(requiredOrthoSize, 7f);

            // 카메라 위치 조정 (X=레인 중심, Y=화면 중앙)
            var pos = cam.transform.position;
            pos.x = 0.5f;  // 레인 중심
            pos.y = 6f;    // 판정선(Y=0)과 스폰점(Y=12) 중간
            cam.transform.position = pos;
        }

        /// <summary>
        /// 코루틴 기반 입력 루프 (Update() 미호출 문제 우회)
        /// Android 뒤로가기 = KeyCode.Escape
        /// </summary>
        private System.Collections.IEnumerator InputLoop()
        {
            while (true)
            {
                yield return null;
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (isShowingResult)
                    {
                        // 결과 화면에서 뒤로가기 → 메뉴로 복귀
                        Time.timeScale = 1f;
                        GameManager.Instance?.ReturnToMenu();
                    }
                    else if (isPaused)
                        ResumeGame();
                    else if (isPlaying)
                        PauseGame();
                }
            }
        }

        /// <summary>
        /// 롱노트 홀드 중 보너스 점수 틱 루프
        /// 0.1초마다 홀드 중인 노트에 보너스 점수 부여
        /// </summary>
        private System.Collections.IEnumerator HoldBonusTickLoop()
        {
            var notesToRemove = new List<Note>();
            var notesToUpdate = new List<KeyValuePair<Note, float>>();

            while (true)
            {
                yield return null;
                if (!isPlaying || isPaused || holdingNotes.Count == 0) continue;

                float currentTime = AudioManager.Instance?.CurrentTime ?? 0f;
                notesToRemove.Clear();
                notesToUpdate.Clear();

                foreach (var kvp in holdingNotes)
                {
                    Note note = kvp.Key;
                    float lastTickTime = kvp.Value;

                    // 노트가 null이거나 더 이상 홀드 중이 아니면 제거 예약
                    if (note == null || !note.IsHolding)
                    {
                        notesToRemove.Add(note);
                        continue;
                    }

                    // 틱 간격마다 보너스 점수 추가
                    if (currentTime - lastTickTime >= HOLD_BONUS_TICK_INTERVAL)
                    {
                        judgementSystem?.AddBonusScore(HOLD_BONUS_PER_TICK);
                        notesToUpdate.Add(new KeyValuePair<Note, float>(note, currentTime));
                    }
                }

                // 순회 완료 후 업데이트/제거 (Collection modified 방지)
                foreach (var kvp in notesToUpdate)
                    holdingNotes[kvp.Key] = kvp.Value;
                foreach (var note in notesToRemove)
                    holdingNotes.Remove(note);
            }
        }

        /// <summary>
        /// 홀드 보너스 추적 시작
        /// </summary>
        private void RegisterHoldBonus(Note note)
        {
            float currentTime = AudioManager.Instance?.CurrentTime ?? 0f;
            holdingNotes[note] = currentTime;
        }

        /// <summary>
        /// 홀드 보너스 추적 종료
        /// </summary>
        private void UnregisterHoldBonus(Note note)
        {
            holdingNotes.Remove(note);
        }

        private void Initialize()
        {
            Debug.Log($"[GameplayController] Initialize - debugMode={debugMode}");

            // 디버그 모드: 테스트 곡 데이터 사용
            if (debugMode && debugSongData != null)
            {
                currentSong = debugSongData;
                Debug.Log($"[GameplayController] Debug mode: Using {debugSongData.Title}");
            }
            else
            {
                currentSong = GameManager.Instance?.CurrentSongData;
                Debug.Log($"[GameplayController] Normal mode: song={(currentSong != null ? currentSong.Title : "NULL")}");
            }

            if (currentSong == null)
            {
                currentSong = Resources.Load<SongData>("Songs/SimpleTest");
                if (currentSong == null)
                {
                    Debug.LogError("[GameplayController] No song data found!");
                    return;
                }
                Debug.Log("[GameplayController] Loaded test song from Resources");
            }

            // 컴포넌트 참조 자동 연결
            if (noteSpawner == null)
                noteSpawner = FindFirstObjectByType<NoteSpawner>();
            if (judgementSystem == null)
                judgementSystem = FindFirstObjectByType<JudgementSystem>();
            if (inputHandler == null)
                inputHandler = FindFirstObjectByType<InputHandler>();
            if (smartBeatMapper == null)
                smartBeatMapper = FindFirstObjectByType<SmartBeatMapper>();

            if (noteSpawner == null || judgementSystem == null || inputHandler == null)
            {
                string missing = "";
                if (noteSpawner == null) missing += "NoteSpawner ";
                if (judgementSystem == null) missing += "JudgementSystem ";
                if (inputHandler == null) missing += "InputHandler ";
                Debug.LogError($"[GameplayController] Missing critical components: {missing}. Returning to menu.");
                StartCoroutine(SafeReturnToMenu(2f));
                return;
            }

            // LaneVisualFeedback 자동 생성 (씬에 없으면)
            if (FindFirstObjectByType<LaneVisualFeedback>() == null)
            {
                var feedbackGo = new GameObject("LaneVisualFeedback");
                feedbackGo.AddComponent<LaneVisualFeedback>();
            }

            // 이벤트 연결
            if (inputHandler != null)
                inputHandler.OnLaneInput += HandleInput;
            if (judgementSystem != null)
            {
                judgementSystem.OnJudgement += HandleJudgement;
                judgementSystem.OnJudgementDetailed += HandleJudgementDetailed;
                scoreChangedHandler = (score) => gameplayUI?.UpdateScore(score);
                comboChangedHandler = (combo) => gameplayUI?.UpdateCombo(combo);
                bonusScoreHandler = (tick, total) => gameplayUI?.ShowBonusScore(tick, total);
                judgementSystem.OnScoreChanged += scoreChangedHandler;
                judgementSystem.OnComboChanged += comboChangedHandler;
                judgementSystem.OnBonusScore += bonusScoreHandler;
            }

            // 디버그 모드: 즉시 시작
            if (debugMode)
            {
                StartDebugGame();
                return;
            }

            // 일반 모드: 오디오 로드 → 게임 시작
            Debug.Log($"[GameplayController] Normal mode: AudioClip={(currentSong.AudioClip != null)}, AudioUrl={currentSong.AudioUrl ?? "null"}");

            AudioManager.Instance.OnBGMLoaded -= OnAudioLoaded;
            AudioManager.Instance.OnBGMEnded -= OnSongEnd;
            AudioManager.Instance.OnBGMLoadFailed -= OnAudioLoadFailed;
            AudioManager.Instance.OnBGMLoaded += OnAudioLoaded;
            AudioManager.Instance.OnBGMEnded += OnSongEnd;
            AudioManager.Instance.OnBGMLoadFailed += OnAudioLoadFailed;

            if (currentSong.AudioClip != null)
            {
                Debug.Log("[GameplayController] AudioClip already loaded, starting game");
                OnAudioLoaded();
            }
            else if (!string.IsNullOrEmpty(currentSong.AudioUrl))
            {
                Debug.Log($"[GameplayController] Loading audio from URL: {currentSong.AudioUrl}");
                AudioManager.Instance.LoadBGMFromUrl(currentSong.AudioUrl);
            }
            else
            {
                Debug.LogWarning("[GameplayController] No audio source, starting without music");
                OnAudioLoaded();
            }

            gameplayUI?.Initialize(currentSong);
        }

        /// <summary>
        /// 디버그 모드: Start()에서 직접 호출하여 카운트다운 후 게임 시작
        /// </summary>
        private void StartDebugGame()
        {
#if UNITY_EDITOR
            Debug.Log("[GameplayController] === Starting Debug Game ===");
#endif

            // AudioClip이 있으면 오프라인 분석으로 노트 생성
            if (currentSong.AudioClip != null && smartBeatMapper != null &&
                (currentSong.Notes == null || currentSong.Notes.Length == 0))
            {
                StartCoroutine(DebugAnalyzeAndStart());
                return;
            }

            // 노트 데이터 로드
            List<NoteData> notes;
            if (currentSong.Notes != null && currentSong.Notes.Length > 0)
            {
                notes = new List<NoteData>(currentSong.Notes);
            }
            else
            {
                notes = GenerateDebugFallbackNotes();
#if UNITY_EDITOR
                Debug.Log($"[GameplayController] Debug fallback notes generated: {notes.Count}");
#endif
            }

            noteSpawner?.LoadNotes(notes);
            judgementSystem?.Initialize(notes.Count);
            gameplayUI?.Initialize(currentSong);

            // 카운트다운 후 시작
            StartCoroutine(DebugCountdownAndStart());
        }

        /// <summary>
        /// 디버그 모드: 카운트다운 표시 후 게임 시작
        /// </summary>
        private System.Collections.IEnumerator DebugCountdownAndStart()
        {
            // 영상 정지 보장 + 카운트다운 표시
            gameplayUI?.ShowLoadingVideo(false);
            gameplayUI?.ShowCountdown(true);
            for (int i = 3; i > 0; i--)
            {
                gameplayUI?.UpdateCountdown(i.ToString());
                yield return new WaitForSeconds(1f);
            }
            gameplayUI?.UpdateCountdown("GO!");
            yield return new WaitForSeconds(0.5f);
            gameplayUI?.ShowCountdown(false);

            // AudioManager 디버그 모드 설정 및 시작
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnBGMEnded -= OnSongEnd;
                AudioManager.Instance.OnBGMEnded += OnSongEnd;
                AudioManager.Instance.EnableDebugMode(currentSong?.Duration ?? 60f);
                AudioManager.Instance.StartDebugPlayback();
#if UNITY_EDITOR
                Debug.Log($"[GameplayController] AudioManager debug started, CurrentTime={AudioManager.Instance.CurrentTime}");
#endif
            }
            else
            {
                Debug.LogWarning("[GameplayController] AudioManager.Instance is null!");
            }

            // 노트 스폰 시작
            if (noteSpawner != null)
            {
                noteSpawner.StartSpawning();
#if UNITY_EDITOR
                Debug.Log("[GameplayController] NoteSpawner started");
#endif
            }
            else
            {
                Debug.LogError("[GameplayController] noteSpawner is NULL!");
            }

            isPlaying = true;
            gameStartTime = Time.time;

            if (autoPlay)
            {
                StartCoroutine(AutoPlayLoop());
#if UNITY_EDITOR
                Debug.Log("[GameplayController] === Auto Play ENABLED ===");
#endif
            }

#if UNITY_EDITOR
            Debug.Log($"[GameplayController] === Debug Game RUNNING === isPlaying={isPlaying}");
#endif
        }

        /// <summary>
        /// 오토 플레이: 노트 타이밍에 자동으로 판정
        /// 다양한 판정이 나오도록 의도적 타이밍 오차 추가
        /// </summary>
        private System.Collections.IEnumerator AutoPlayLoop()
        {
            while (true)
            {
                yield return null;

                // 게임 종료 시 루프 탈출
                if (!isPlaying && !isPaused) break;

                // 일시정지 중에는 처리 스킵
                if (isPaused) continue;

                // AudioManager null 체크
                if (AudioManager.Instance == null || noteSpawner == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("[GameplayController] AutoPlay: Missing required components");
#endif
                    break;
                }

                float currentTime = AudioManager.Instance.CurrentTime;

                for (int lane = 0; lane <= 3; lane++)
                {
                    Note note = noteSpawner.GetNearestNote(lane);
                    if (note == null) continue;

                    // 홀드 중인 롱노트: release 타이밍 체크
                    if (note.IsHolding)
                    {
                        float releaseTime = note.HitTime + note.Duration;
                        if (currentTime >= releaseTime - 0.040f)
                        {
                            UnregisterHoldBonus(note);
                            note.EndHold(currentTime);
                            var releaseResult = judgementSystem.Judge(currentTime, releaseTime);
                            if (releaseResult != JudgementResult.Miss)
                                LaneVisualFeedback.PlayJudgementEffect(lane, releaseResult);
                            note.MarkAsJudged();
                            LaneVisualFeedback.SetHighlight(lane, false);
                            noteSpawner.RemoveNote(note);

                            // release 후 같은 레인의 다음 노트 즉시 처리
                            note = noteSpawner.GetNearestNote(lane);
                            if (note == null) continue;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    // AutoPlay 판정 다양화: 의도적 타이밍 오차 추가
                    float autoPlayWindow = 0.200f; // Good 범위까지 처리
                    float diff = Mathf.Abs(currentTime - note.HitTime);
                    if (diff <= autoPlayWindow)
                    {
                        // 판정 분포: Perfect 55%, Great 25%, Good 15%, Bad 5%
                        float inputTime = currentTime;
                        float roll = UnityEngine.Random.value;
                        if (roll < 0.55f)
                        {
                            // Perfect: ±30ms 범위
                            inputTime = note.HitTime + UnityEngine.Random.Range(-0.030f, 0.030f);
                        }
                        else if (roll < 0.80f)
                        {
                            // Great: ±60~90ms 범위
                            float offset = UnityEngine.Random.Range(0.060f, 0.090f);
                            inputTime = note.HitTime + offset * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
                        }
                        else if (roll < 0.95f)
                        {
                            // Good: ±110~180ms 범위
                            float offset = UnityEngine.Random.Range(0.110f, 0.180f);
                            inputTime = note.HitTime + offset * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
                        }
                        else
                        {
                            // Bad: ±210~300ms 범위
                            float offset = UnityEngine.Random.Range(0.210f, 0.300f);
                            inputTime = note.HitTime + offset * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
                        }

                        LaneVisualFeedback.Flash(lane);

                        var result = judgementSystem.Judge(inputTime, note.HitTime);
                        if (result != JudgementResult.Miss)
                        {
                            if (note.NoteType == NoteType.Long)
                            {
                                note.StartHold(currentTime);
                                RegisterHoldBonus(note);
                                LaneVisualFeedback.SetHighlight(lane, true);
                                LaneVisualFeedback.PlayJudgementEffect(lane, result);
                            }
                            else
                            {
                                note.MarkAsJudged();
                                noteSpawner.RemoveNote(note);
                                LaneVisualFeedback.PlayJudgementEffect(lane, result);
                            }
                        }
                    }
                }
            }
        }

        private void OnAudioLoaded()
        {
            // AudioClip이 있으면 오프라인 분석으로 노트 생성 시도
            var clip = AudioManager.Instance?.GetAudioClip() ?? currentSong.AudioClip;
            if (clip != null && smartBeatMapper != null &&
                (currentSong.Notes == null || currentSong.Notes.Length == 0))
            {
                StartCoroutine(AnalyzeAndGenerateNotes(clip));
                return;
            }

            // 기존 노트 데이터 또는 BPM 기반 생성
            List<NoteData> notes;
            if (currentSong.Notes != null && currentSong.Notes.Length > 0)
            {
                notes = new List<NoteData>(currentSong.Notes);
            }
            else
            {
                var sections = currentSong.Sections ?? BeatMapper.CreateDefaultSections(currentSong.Duration);
                notes = beatMapper.GenerateNotesFromBPM(currentSong.BPM, currentSong.Duration, sections);
            }

            StartGameWithNotes(notes);
        }

        /// <summary>
        /// 오프라인 오디오 분석 → 노트 생성 → 게임 시작
        /// </summary>
        private System.Collections.IEnumerator AnalyzeAndGenerateNotes(AudioClip clip)
        {
#if UNITY_EDITOR
            Debug.Log($"[GameplayController] Starting offline audio analysis: {clip.name}");
#endif
            gameplayUI?.Initialize(currentSong);
            gameplayUI?.ShowLoadingVideo(true);
            gameplayUI?.ShowCountdown(true);
            gameplayUI?.UpdateCountdown("분석 중...");

            // 1프레임 대기 (UI 갱신)
            yield return null;

            // 비동기 분석 (청크 단위, 메인 스레드 블로킹 없음)
            var analyzer = new OfflineAudioAnalyzer();
            OfflineAudioAnalyzer.AnalysisResult result = null;

            yield return analyzer.AnalyzeAsync(clip,
                progress => gameplayUI?.UpdateCountdown($"분석 중... {Mathf.RoundToInt(progress * 100)}%"),
                r => result = r);

            // 분석 완료 → 영상 정지
            gameplayUI?.ShowLoadingVideo(false);

            if (result == null)
            {
                Debug.LogWarning("[GameplayController] Audio analysis failed, using BPM fallback");
                var fallbackSections = currentSong.Sections ?? BeatMapper.CreateDefaultSections(currentSong.Duration);
                var fallbackNotes = beatMapper.GenerateNotesFromBPM(currentSong.BPM, currentSong.Duration, fallbackSections);
                StartGameWithNotes(fallbackNotes);
                yield break;
            }

            // 분석 결과로 노트 생성
            var notes = smartBeatMapper.GenerateNotes(result);

            // SongData에 분석 결과 반영
            if (currentSong.BPM <= 0) currentSong.BPM = result.BPM;
            currentSong.Sections = smartBeatMapper.ConvertSections(result);

#if UNITY_EDITOR
            Debug.Log($"[GameplayController] Analysis complete: BPM={result.BPM}, notes={notes.Count}, sections={result.Sections.Count}");
#endif

            StartGameWithNotes(notes);
        }

        /// <summary>
        /// 노트 데이터로 게임 시작 준비 (카운트다운 → 시작)
        /// </summary>
        private void StartGameWithNotes(List<NoteData> notes)
        {
            noteSpawner.LoadNotes(notes);
            judgementSystem.Initialize(notes.Count);
            gameplayUI?.Initialize(currentSong);

            var audioSource = AudioManager.Instance?.GetComponent<AudioSource>();
            if (audioAnalyzer != null && audioSource != null)
                audioAnalyzer.Initialize(audioSource);

            StartCoroutine(StartCountdown());
        }

        private System.Collections.IEnumerator StartCountdown()
        {
            gameplayUI?.ShowLoadingVideo(false); // 영상 정지 보장
            gameplayUI?.ShowCountdown(true);

            for (int i = (int)countdownTime; i > 0; i--)
            {
                gameplayUI?.UpdateCountdown(i.ToString());
                yield return new WaitForSeconds(1f);
            }

            gameplayUI?.UpdateCountdown("GO!");
            yield return new WaitForSeconds(0.5f);

            gameplayUI?.ShowCountdown(false);
            StartGame();
        }

        private void StartGame()
        {
            isPlaying = true;
            gameStartTime = Time.time;

            GameManager.Instance?.ChangeState(GameManager.GameState.Gameplay);
            noteSpawner.StartSpawning();

            // AudioClip이 있으면 AudioManager에 설정 후 재생
            if (currentSong.AudioClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGM(currentSong.AudioClip);
            }
            AudioManager.Instance.PlayBGM();

#if UNITY_EDITOR
            Debug.Log("[GameplayController] Game started!");
#endif
        }

        private void HandleInput(int lane, InputHandler.InputType inputType)
        {
            if (!isPlaying) return;

            float currentTime = AudioManager.Instance?.CurrentTime ?? 0f;

            switch (inputType)
            {
                case InputHandler.InputType.Down:
                    LaneVisualFeedback.Flash(lane);
                    ProcessNoteHit(lane, currentTime);
                    break;

                case InputHandler.InputType.Up:
                    LaneVisualFeedback.SetHighlight(lane, false);
                    ProcessNoteRelease(lane, currentTime);
                    break;

                case InputHandler.InputType.Scratch:
                    LaneVisualFeedback.Flash(lane);
                    ProcessScratch(lane, currentTime);
                    break;
            }
        }

        private void ProcessNoteHit(int lane, float currentTime)
        {
            Note nearestNote = noteSpawner.GetNearestNote(lane);
            if (nearestNote == null) return;

            if (nearestNote.NoteType == NoteType.Long)
            {
                nearestNote.StartHold(currentTime);
                var holdResult = judgementSystem.Judge(currentTime, nearestNote.HitTime);
                if (holdResult != JudgementResult.Miss)
                {
                    // 롱노트 press 판정만 표시, MarkAsJudged는 release에서 처리
                    LaneVisualFeedback.PlayJudgementEffect(lane, holdResult);
                }
                LaneVisualFeedback.SetHighlight(lane, true);
                RegisterHoldBonus(nearestNote);
                return;
            }

            // 스크래치 노트도 일반 터치(Down)로 판정 가능
            if (nearestNote.NoteType == NoteType.Scratch)
            {
                var scratchResult = judgementSystem.Judge(currentTime, nearestNote.HitTime);
                if (scratchResult != JudgementResult.Miss)
                {
                    nearestNote.MarkAsJudged();
                    AudioManager.Instance?.PlayScratchSound();
                    noteSpawner.RemoveNote(nearestNote);
                    LaneVisualFeedback.PlayJudgementEffect(lane, scratchResult);
                }
                return;
            }

            var result = judgementSystem.Judge(currentTime, nearestNote.HitTime);
            if (result != JudgementResult.Miss)
            {
                nearestNote.MarkAsJudged();
                noteSpawner.RemoveNote(nearestNote);
                LaneVisualFeedback.PlayJudgementEffect(lane, result);
            }
        }

        private void ProcessNoteRelease(int lane, float currentTime)
        {
            // 홀드 중인 롱노트를 activeNotes에서 직접 검색
            Note holdingNote = FindHoldingNote(lane);
            if (holdingNote == null) return;

            UnregisterHoldBonus(holdingNote);

            bool success = holdingNote.EndHold(currentTime);
            if (success)
            {
                float longNoteEndTime = holdingNote.HitTime + holdingNote.Duration;
                var releaseResult = judgementSystem.Judge(currentTime, longNoteEndTime);
                if (releaseResult != JudgementResult.Miss)
                    LaneVisualFeedback.PlayJudgementEffect(lane, releaseResult);
            }
            else
            {
                judgementSystem.RegisterMiss();
            }

            holdingNote.MarkAsJudged();
            LaneVisualFeedback.SetHighlight(lane, false);
            noteSpawner.RemoveNote(holdingNote);
        }

        /// <summary>
        /// 특정 레인에서 홀드 중인 롱노트를 찾음
        /// </summary>
        private Note FindHoldingNote(int lane)
        {
            // NoteSpawner의 GetNearestNote는 시간 기반이므로 홀드 중인 노트를 못 찾을 수 있음
            // 직접 activeNotes를 순회하여 IsHolding인 노트를 찾음
            Note nearest = noteSpawner.GetNearestNote(lane);
            if (nearest != null && nearest.IsHolding) return nearest;

            // GetNearestNote가 실패하면 null 반환 (노트가 이미 제거된 경우)
            return null;
        }

        private void ProcessScratch(int lane, float currentTime)
        {
            if (lane != 0 && lane != 3) return;

            Note nearestNote = noteSpawner.GetNearestNote(lane);
            if (nearestNote == null || nearestNote.NoteType != NoteType.Scratch) return;

            var result = judgementSystem.Judge(currentTime, nearestNote.HitTime);
            if (result != JudgementResult.Miss)
            {
                nearestNote.MarkAsJudged();
                AudioManager.Instance?.PlayScratchSound();
                noteSpawner.RemoveNote(nearestNote);
                LaneVisualFeedback.PlayJudgementEffect(lane, result);
            }
        }

        private void HandleJudgement(JudgementResult result, int combo)
        {
            // Early/Late 정보 없는 기존 호출 (Miss 등)
            // ShowJudgementDetailed가 OnJudgementDetailed에서 호출되므로 중복 방지
        }

        private void HandleJudgementDetailed(JudgementResult result, float rawDiff)
        {
            gameplayUI?.ShowJudgementDetailed(result, rawDiff);
        }

        /// <summary>
        /// 디버그 모드: AudioClip 분석 → 실제 오디오로 게임 시작
        /// </summary>
        private System.Collections.IEnumerator DebugAnalyzeAndStart()
        {
            gameplayUI?.Initialize(currentSong);
            gameplayUI?.ShowLoadingVideo(true);
            gameplayUI?.ShowCountdown(true);
            gameplayUI?.UpdateCountdown("분석 중...");
            yield return null;

            // 비동기 분석 (청크 단위)
            var analyzer = new OfflineAudioAnalyzer();
            OfflineAudioAnalyzer.AnalysisResult result = null;

            yield return analyzer.AnalyzeAsync(currentSong.AudioClip,
                progress => gameplayUI?.UpdateCountdown($"분석 중... {Mathf.RoundToInt(progress * 100)}%"),
                r => result = r);

            List<NoteData> notes;
            if (result != null)
            {
                notes = smartBeatMapper.GenerateNotes(result);
                if (currentSong.BPM <= 0) currentSong.BPM = result.BPM;
                currentSong.Sections = smartBeatMapper.ConvertSections(result);
#if UNITY_EDITOR
                Debug.Log($"[GameplayController] Debug analysis: BPM={result.BPM}, notes={notes.Count}");
#endif
            }
            else
            {
                notes = GenerateDebugFallbackNotes();
#if UNITY_EDITOR
                Debug.Log($"[GameplayController] Debug fallback notes generated: {notes.Count}");
#endif
            }

            noteSpawner?.LoadNotes(notes);
            judgementSystem?.Initialize(notes.Count);

            // 분석 완료 → 영상 정지
            gameplayUI?.ShowLoadingVideo(false);

            // 카운트다운 표시 (노트 스폰/음악 재생 전에 완료)
            for (int i = 3; i > 0; i--)
            {
                gameplayUI?.UpdateCountdown(i.ToString());
                yield return new WaitForSeconds(1f);
            }
            gameplayUI?.UpdateCountdown("GO!");
            yield return new WaitForSeconds(0.5f);
            gameplayUI?.ShowCountdown(false);

            // 카운트다운 완료 후 실제 오디오 재생 및 노트 스폰 시작
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnBGMEnded -= OnSongEnd;
                AudioManager.Instance.OnBGMEnded += OnSongEnd;
                AudioManager.Instance.SetBGM(currentSong.AudioClip);
                AudioManager.Instance.PlayBGM();
#if UNITY_EDITOR
                Debug.Log("[GameplayController] Playing actual audio");
#endif
            }

            noteSpawner?.StartSpawning();
            isPlaying = true;
            gameStartTime = Time.time;

            if (autoPlay)
            {
                StartCoroutine(AutoPlayLoop());
#if UNITY_EDITOR
                Debug.Log("[GameplayController] === Auto Play ENABLED ===");
#endif
            }
        }

        private void OnSongEnd()
        {
            if (!isPlaying && !isPaused) return;

            isPlaying = false;
            isPaused = false;

            // 남은 활성 노트를 MISS 처리 후 정리
            if (noteSpawner != null && judgementSystem != null)
            {
                noteSpawner.FlushRemainingAsMiss(judgementSystem);
                noteSpawner.StopSpawning();
            }
            audioAnalyzer?.StopAnalysis();

            if (judgementSystem != null)
            {
                var gameResult = judgementSystem.GetResult();
#if UNITY_EDITOR
                Debug.Log($"[GameplayController] Game ended - Score:{gameResult.Score}, Rank:{gameResult.Rank}, P:{gameResult.PerfectCount} G:{gameResult.GreatCount} Good:{gameResult.GoodCount} B:{gameResult.BadCount} M:{gameResult.MissCount}");
#endif
                ShowResult(gameResult);
            }
            else
            {
                Debug.LogError("[GameplayController] JudgementSystem is null at game end");
                StartCoroutine(SafeReturnToMenu(2f));
            }
        }

        private void ShowResult(GameResult result)
        {
            isShowingResult = true;
            GameManager.Instance?.ChangeState(GameManager.GameState.Result);
            gameplayUI?.ShowResult(result);

            // SongLibraryManager에 최고 기록 업데이트
            if (currentSong != null && SongLibraryManager.Instance != null)
            {
                SongLibraryManager.Instance.UpdateBestRecord(
                    currentSong.Title, result.Score, result.MaxCombo, result.Rank);
            }
        }

        public void PauseGame()
        {
            if (!isPlaying || isPaused) return;

            isPaused = true;
            isPlaying = false;

            if (GameManager.Instance != null)
                GameManager.Instance.PauseGame();
            else
                Time.timeScale = 0f;

            AudioManager.Instance?.PauseBGM();
            if (noteSpawner != null) noteSpawner.enabled = false;
            gameplayUI?.ShowPauseMenu(true);
            Debug.Log($"[GameplayController] Game paused, isPaused={isPaused}, isPlaying={isPlaying}");
        }

        public void ResumeGame()
        {
            if (!isPaused) return;

            isPaused = false;
            isPlaying = true;

            if (GameManager.Instance != null)
                GameManager.Instance.ResumeGame();
            else
                Time.timeScale = 1f;

            AudioManager.Instance?.ResumeBGM();
            if (noteSpawner != null) noteSpawner.enabled = true;
            gameplayUI?.ShowPauseMenu(false);
            Debug.Log($"[GameplayController] Game resumed, isPaused={isPaused}, isPlaying={isPlaying}");
        }

        /// <summary>
        /// 디버그 모드 폴백: BPM 100 기반 다양한 노트 패턴 생성 (30초 곡 기준, 35개)
        /// 구간별 밀도 변화: Intro(sparse) → Build(normal) → Climax(dense) → Outro(sparse)
        /// 타입 비율: Tap 70%, Long 20%, Scratch 10%
        /// </summary>
        private List<NoteData> GenerateDebugFallbackNotes()
        {
            var notes = new List<NoteData>();
            var rng = new System.Random(42); // seed 고정으로 재현성 보장

            // BPM 100 기준: 8분음표 = 0.3초, 16분음표 = 0.15초
            const float bpm = 100f;
            const float beatInterval = 60f / bpm;          // 0.6초 (4분음표)
            const float eighthNote = beatInterval / 2f;     // 0.3초 (8분음표)
            const float sixteenthNote = beatInterval / 4f;  // 0.15초 (16분음표)

            // 구간 정의: (시작시간, 종료시간, 노트간격, 구간이름)
            // Intro: sparse (8분음표 간격)
            // Build: normal (8분음표 + 가끔 16분음표)
            // Climax: dense (8분음표 + 빈번한 16분음표)
            // Outro: sparse (8분음표 간격)
            float t = 2.0f; // 시작 오프셋 (2초 여유)

            // === Intro (2.0s ~ 8.0s): sparse, Tap 위주 ===
            while (t < 8.0f)
            {
                int lane = rng.Next(1, 3); // 1~2 레인
                notes.Add(new NoteData(t, lane, NoteType.Tap));
                t += eighthNote + (float)(rng.NextDouble() * 0.1); // 0.3s + 약간의 랜덤
            }

            // === Build (8.0s ~ 16.0s): normal, Long 노트 등장 ===
            while (t < 16.0f)
            {
                int lane = rng.Next(1, 3);
                float roll = (float)rng.NextDouble();

                if (roll < 0.20f) // 20% Long
                {
                    float duration = 0.5f + (float)(rng.NextDouble() * 0.5f); // 0.5~1.0초
                    notes.Add(new NoteData(t, lane, NoteType.Long, duration));
                    t += duration + eighthNote; // 롱노트 끝난 후 8분음표 간격 보장
                }
                else if (roll < 0.28f) // 8% Scratch
                {
                    int scratchLane = rng.Next(0, 2) == 0 ? 0 : 3; // 스크래치 레인
                    notes.Add(new NoteData(t, scratchLane, NoteType.Scratch));
                    t += eighthNote;
                }
                else // 72% Tap
                {
                    notes.Add(new NoteData(t, lane, NoteType.Tap));
                    // 가끔 16분음표 간격 (15% 확률, 엄지로 반응 가능한 빈도)
                    t += rng.NextDouble() < 0.15 ? sixteenthNote : eighthNote;
                }
            }

            // === Climax (16.0s ~ 24.0s): dense, 다양한 타입 (모바일 엄지 조작 기준) ===
            while (t < 24.0f)
            {
                int lane = rng.Next(1, 3);
                float roll = (float)rng.NextDouble();

                if (roll < 0.18f) // 18% Long
                {
                    float duration = 0.5f + (float)(rng.NextDouble() * 0.5f);
                    notes.Add(new NoteData(t, lane, NoteType.Long, duration));
                    t += duration + eighthNote; // 롱노트 release 후 충분한 간격 (엄지 복귀 시간)
                }
                else if (roll < 0.30f) // 12% Scratch
                {
                    int scratchLane = rng.Next(0, 2) == 0 ? 0 : 3;
                    notes.Add(new NoteData(t, scratchLane, NoteType.Scratch));
                    t += eighthNote; // 스크래치 후 엄지가 키 존으로 돌아올 시간 보장
                }
                else // 70% Tap
                {
                    notes.Add(new NoteData(t, lane, NoteType.Tap));
                    // 20% 확률로 16분음표 간격 (고밀도, 엄지로 가능한 수준)
                    t += rng.NextDouble() < 0.2 ? sixteenthNote : eighthNote;
                }
            }

            // === Outro (24.0s ~ 29.0s): sparse, Tap 위주 ===
            while (t < 29.0f)
            {
                int lane = rng.Next(1, 3);
                float roll = (float)rng.NextDouble();

                if (roll < 0.10f) // 10% Scratch (마무리 악센트)
                {
                    int scratchLane = rng.Next(0, 2) == 0 ? 0 : 3;
                    notes.Add(new NoteData(t, scratchLane, NoteType.Scratch));
                }
                else
                {
                    notes.Add(new NoteData(t, lane, NoteType.Tap));
                }
                t += eighthNote + (float)(rng.NextDouble() * 0.15); // 느슨한 간격
            }

            // 시간순 정렬 (이미 순서대로 생성하지만 안전장치)
            notes.Sort((a, b) => a.HitTime.CompareTo(b.HitTime));

            return notes;
        }

        private void OnAudioLoadFailed(string error)
        {
            Debug.LogError($"[GameplayController] Audio load failed: {error}");
            gameplayUI?.ShowCountdown(true);
            gameplayUI?.UpdateCountdown("Load Failed\nReturning...");
            StartCoroutine(SafeReturnToMenu(3f));
        }

        private System.Collections.IEnumerator SafeReturnToMenu(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Time.timeScale = 1f;
            GameManager.Instance?.ReturnToMenu();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();

            // timeScale 복원 (일시정지 상태에서 파괴될 수 있음)
            Time.timeScale = 1f;

            if (inputHandler != null)
                inputHandler.OnLaneInput -= HandleInput;

            if (judgementSystem != null)
            {
                judgementSystem.OnJudgement -= HandleJudgement;
                judgementSystem.OnJudgementDetailed -= HandleJudgementDetailed;
                if (scoreChangedHandler != null)
                    judgementSystem.OnScoreChanged -= scoreChangedHandler;
                if (comboChangedHandler != null)
                    judgementSystem.OnComboChanged -= comboChangedHandler;
                if (bonusScoreHandler != null)
                    judgementSystem.OnBonusScore -= bonusScoreHandler;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnBGMLoaded -= OnAudioLoaded;
                AudioManager.Instance.OnBGMEnded -= OnSongEnd;
                AudioManager.Instance.OnBGMLoadFailed -= OnAudioLoadFailed;
            }
        }

        /// <summary>
        /// LaneBackground(3D Quad)에 Cyberpunk 테마 텍스처 적용
        /// "NanoBanana" 에셋 사용 (Resources/Skins/NanoBanana/Background)
        /// </summary>
        private void ApplyCyberpunkLaneBackground()
        {
            var laneBackground = GameObject.Find("LaneBackground");
            if (laneBackground == null)
            {
                Debug.LogWarning("[GameplayController] LaneBackground not found");
                return;
            }

            var meshRenderer = laneBackground.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                Debug.LogWarning("[GameplayController] LaneBackground has no MeshRenderer");
                return;
            }

            // Material 인스턴스 생성
            Material instanceMaterial = new Material(meshRenderer.sharedMaterial);
            meshRenderer.material = instanceMaterial;

            // NanoBanana 에셋 로드 (Background.png)
            Texture2D texture = Resources.Load<Texture2D>("Skins/NanoBanana/Background");
            
            if (texture != null)
            {
                instanceMaterial.mainTexture = texture;
                
                // Unlit 셰이더인 경우 색상 초기화 (텍스처 본연의 색 사용)
                if (instanceMaterial.HasProperty("_BaseColor"))
                    instanceMaterial.SetColor("_BaseColor", Color.white);
                else if (instanceMaterial.HasProperty("_Color"))
                    instanceMaterial.SetColor("_Color", Color.white);
                    
                Debug.Log("[GameplayController] NanoBanana background asset applied");
            }
            else
            {
                Debug.LogWarning("[GameplayController] Failed to load 'Skins/NanoBanana/Background' asset");
                // 로드 실패 시 기존 절차적 생성으로 폴백? (일단 경고만)
            }
        }
    }
}
