using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

namespace AIBeat.Core
{
    /// <summary>
    /// BGM 및 SFX 재생을 관리하는 싱글톤 매니저
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("Hit Sounds")]
        [SerializeField] private AudioClip perfectSound;
        [SerializeField] private AudioClip greatSound;
        [SerializeField] private AudioClip goodSound;
        [SerializeField] private AudioClip badSound;
        [SerializeField] private AudioClip scratchSound;

        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = Mathf.Clamp01(value);
                if (bgmSource != null) bgmSource.volume = bgmVolume;
                PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
            }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                if (sfxSource != null) sfxSource.volume = sfxVolume;
                PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            }
        }

        public bool IsPlaying => isDebugMode ? isDebugPlaying : (bgmSource != null && bgmSource.isPlaying);
        public float CurrentTime => isDebugMode ? debugTime : (bgmSource != null ? bgmSource.time : 0f);
        public float Duration => bgmSource != null && bgmSource.clip != null ? bgmSource.clip.length : debugDuration;

            // 디버그 모드 (오디오 없이 시간만 진행)
        private bool isDebugMode;
        private bool isDebugPlaying;
        private float debugTime;
        private float debugDuration = 60f;
        private float lastLogTime;
        private Coroutine debugTimeCoroutine;
        private Coroutine checkBGMEndCoroutine;
        private bool isBgmPaused;

        public event Action OnBGMLoaded;
        public event Action OnBGMStarted;
        public event Action OnBGMEnded;
        public event Action<string> OnBGMLoadFailed;

        private void Awake()
        {
            // 단순 싱글톤 (DontDestroyOnLoad 사용하지 않음)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            SettingsManager.OnSettingChanged -= OnSettingChanged;
            if (Instance == this)
                Instance = null;
        }

        private void Initialize()
        {
            // PlayerPrefs에서 볼륨 설정 로드
            bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
            }
            bgmSource.volume = bgmVolume;

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            sfxSource.volume = sfxVolume;

            // SettingsManager 이벤트 구독 (설정 변경 시 즉시 반영)
            SettingsManager.OnSettingChanged += OnSettingChanged;
        }

        /// <summary>
        /// SettingsManager에서 볼륨 설정 변경 시 즉시 적용
        /// </summary>
        private void OnSettingChanged(string key, float value)
        {
            if (key == SettingsManager.KEY_BGM_VOLUME)
            {
                bgmVolume = value;
                if (bgmSource != null) bgmSource.volume = bgmVolume;
            }
            else if (key == SettingsManager.KEY_SFX_VOLUME)
            {
                sfxVolume = value;
                if (sfxSource != null) sfxSource.volume = sfxVolume;
            }
        }

        /// <summary>
        /// URL에서 오디오를 스트리밍 로드
        /// </summary>
        public void LoadBGMFromUrl(string url, Action onComplete = null)
        {
            StartCoroutine(LoadAudioFromUrl(url, onComplete));
        }

        private IEnumerator LoadAudioFromUrl(string url, Action onComplete)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    bgmSource.clip = clip;
                    OnBGMLoaded?.Invoke();
                    onComplete?.Invoke();
#if UNITY_EDITOR
                    Debug.Log($"[AudioManager] BGM loaded: {url}");
#endif
                }
                else
                {
                    Debug.LogError($"[AudioManager] Failed to load audio: {www.error}");
                    OnBGMLoadFailed?.Invoke(www.error);
                }
            }
        }

        /// <summary>
        /// 로컬 AudioClip을 BGM으로 설정
        /// </summary>
        public void SetBGM(AudioClip clip)
        {
            if (clip == null) return;
            bgmSource.clip = clip;
            OnBGMLoaded?.Invoke();
        }

        public void PlayBGM(float startTime = 0f)
        {
            if (bgmSource.clip == null)
            {
                Debug.LogWarning("[AudioManager] No BGM clip loaded");
                return;
            }

            bgmSource.time = startTime;
            bgmSource.Play();
            OnBGMStarted?.Invoke();

            if (checkBGMEndCoroutine != null)
                StopCoroutine(checkBGMEndCoroutine);
            checkBGMEndCoroutine = StartCoroutine(CheckBGMEnd());
        }

        private IEnumerator CheckBGMEnd()
        {
            while (true)
            {
                yield return null;
                if (!isBgmPaused && !bgmSource.isPlaying)
                    break;
            }
            OnBGMEnded?.Invoke();
        }

        public void StopBGM()
        {
            isBgmPaused = false;
            if (bgmSource != null) bgmSource.Stop();
        }

        public void PauseBGM()
        {
            isBgmPaused = true;
            if (bgmSource != null) bgmSource.Pause();
        }

        public void ResumeBGM()
        {
            isBgmPaused = false;
            if (bgmSource != null) bgmSource.UnPause();
        }

        /// <summary>
        /// 판정에 따른 히트사운드 재생
        /// </summary>
        public void PlayHitSound(JudgementResult result)
        {
            AudioClip clip = result switch
            {
                JudgementResult.Perfect => perfectSound,
                JudgementResult.Great => greatSound,
                JudgementResult.Good => goodSound,
                JudgementResult.Bad => badSound,
                _ => null
            };

            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }

        public void PlayScratchSound()
        {
            if (scratchSound != null)
            {
                sfxSource.PlayOneShot(scratchSound, sfxVolume);
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }

        /// <summary>
        /// 현재 로드된 AudioClip 반환 (오프라인 분석용)
        /// </summary>
        public AudioClip GetAudioClip()
        {
            return bgmSource?.clip;
        }

        /// <summary>
        /// Resources 폴더에서 AudioClip 로드 후 BGM으로 설정
        /// </summary>
        public AudioClip LoadBGMFromResources(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null)
            {
                bgmSource.clip = clip;
#if UNITY_EDITOR
                Debug.Log($"[AudioManager] BGM loaded from Resources: {path}");
#endif
                OnBGMLoaded?.Invoke();
            }
            else
            {
                Debug.LogError($"[AudioManager] Failed to load from Resources: {path}");
            }
            return clip;
        }

        /// <summary>
        /// StreamingAssets에서 AudioClip 로드
        /// </summary>
        public void LoadBGMFromStreamingAssets(string fileName, Action onComplete = null)
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
            AudioType audioType = AudioType.MPEG;
            if (fileName.EndsWith(".wav")) audioType = AudioType.WAV;
            else if (fileName.EndsWith(".ogg")) audioType = AudioType.OGGVORBIS;

            StartCoroutine(LoadFromStreamingAssets(path, audioType, onComplete));
        }

        private IEnumerator LoadFromStreamingAssets(string path, AudioType audioType, Action onComplete)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, audioType))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    bgmSource.clip = clip;
                    OnBGMLoaded?.Invoke();
                    onComplete?.Invoke();
#if UNITY_EDITOR
                    Debug.Log($"[AudioManager] BGM loaded from StreamingAssets: {path}");
#endif
                }
                else
                {
                    Debug.LogError($"[AudioManager] Failed to load: {www.error}");
                }
            }
        }

        /// <summary>
        /// 디버그 모드 활성화 (오디오 없이 시간만 진행)
        /// </summary>
        public void EnableDebugMode(float duration = 60f)
        {
            isDebugMode = true;
            isDebugPlaying = false;
            debugDuration = duration;
            debugTime = 0f;
            lastLogTime = -5f;
#if UNITY_EDITOR
            Debug.Log($"[AudioManager] Debug mode enabled, duration: {duration}s");
#endif
        }

        /// <summary>
        /// 디버그 모드에서 재생 시작 (코루틴 기반)
        /// </summary>
        public void StartDebugPlayback()
        {
            if (!isDebugMode)
            {
                Debug.LogWarning("[AudioManager] StartDebugPlayback called but isDebugMode=false!");
                return;
            }
            isDebugPlaying = true;
            debugTime = 0f;
            lastLogTime = -5f;
#if UNITY_EDITOR
            Debug.Log("[AudioManager] Debug playback started");
#endif
            OnBGMStarted?.Invoke();

            // 기존 코루틴 정지 후 새로 시작 (중복 방지)
            if (debugTimeCoroutine != null)
                StopCoroutine(debugTimeCoroutine);
            debugTimeCoroutine = StartCoroutine(DebugTimeCoroutine());
        }

        /// <summary>
        /// 코루틴 기반 디버그 시간 진행 (Update() 미호출 문제 우회)
        /// </summary>
        private IEnumerator DebugTimeCoroutine()
        {
            // 디버그 시간 코루틴 시작
            while (isDebugPlaying && debugTime < debugDuration)
            {
                yield return null; // 매 프레임 대기

                // 일시정지 중에는 시간 진행 중지
                if (Time.timeScale < 0.01f)
                    continue;

                float dt = Time.deltaTime;
                debugTime += dt;

                // 5초 간격으로 로그 출력 (에디터에서만)
                if (debugTime - lastLogTime >= 5f)
                {
                    lastLogTime = debugTime;
#if UNITY_EDITOR
                    Debug.Log($"[AudioManager] DebugTime={debugTime:F1}s / {debugDuration}s, dt={dt:F4}, timeScale={Time.timeScale}");
#endif
                }
            }

            if (debugTime >= debugDuration)
            {
                isDebugPlaying = false;
#if UNITY_EDITOR
                Debug.Log("[AudioManager] Debug playback ended");
#endif
                OnBGMEnded?.Invoke();
            }
        }
    }

    public enum JudgementResult
    {
        Perfect,
        Great,
        Good,
        Bad,
        Miss
    }
}
