using UnityEngine;
using System;

namespace AIBeat.Core
{
    /// <summary>
    /// 게임 설정 관리 싱글톤
    /// PlayerPrefs 기반 설정 저장/로드
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        // 설정 변경 이벤트 (키, 값)
        public static event Action<string, float> OnSettingChanged;

        // 설정 키 상수
        public const string KEY_NOTE_SPEED = "NoteSpeed";
        public const string KEY_JUDGEMENT_OFFSET = "JudgementOffset";
        public const string KEY_BGM_VOLUME = "BGMVolume";
        public const string KEY_SFX_VOLUME = "SFXVolume";
        public const string KEY_BACKGROUND_DIM = "BackgroundDim";

        // 기본값
        private const float DEFAULT_NOTE_SPEED = 5.0f;
        private const float DEFAULT_JUDGEMENT_OFFSET = 0f;
        private const float DEFAULT_BGM_VOLUME = 0.8f;
        private const float DEFAULT_SFX_VOLUME = 0.8f;
        private const float DEFAULT_BACKGROUND_DIM = 0.5f;

        // 현재 설정값
        private float noteSpeed;
        private float judgementOffset;
        private float bgmVolume;
        private float sfxVolume;
        private float backgroundDim;

        // 프로퍼티 (값 변경 시 이벤트 발행 + 저장)
        public float NoteSpeed
        {
            get => noteSpeed;
            set
            {
                // 0.5 단위로 반올림, 1.0 ~ 10.0 범위
                noteSpeed = Mathf.Round(Mathf.Clamp(value, 1.0f, 10.0f) * 2f) / 2f;
                PlayerPrefs.SetFloat(KEY_NOTE_SPEED, noteSpeed);
                OnSettingChanged?.Invoke(KEY_NOTE_SPEED, noteSpeed);
            }
        }

        public float JudgementOffset
        {
            get => judgementOffset;
            set
            {
                // -100ms ~ +100ms, 1ms 단위 (초 단위로 저장)
                judgementOffset = Mathf.Round(Mathf.Clamp(value, -0.1f, 0.1f) * 1000f) / 1000f;
                PlayerPrefs.SetFloat(KEY_JUDGEMENT_OFFSET, judgementOffset);
                OnSettingChanged?.Invoke(KEY_JUDGEMENT_OFFSET, judgementOffset);
            }
        }

        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KEY_BGM_VOLUME, bgmVolume);
                OnSettingChanged?.Invoke(KEY_BGM_VOLUME, bgmVolume);
            }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KEY_SFX_VOLUME, sfxVolume);
                OnSettingChanged?.Invoke(KEY_SFX_VOLUME, sfxVolume);
            }
        }

        public float BackgroundDim
        {
            get => backgroundDim;
            set
            {
                backgroundDim = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KEY_BACKGROUND_DIM, backgroundDim);
                OnSettingChanged?.Invoke(KEY_BACKGROUND_DIM, backgroundDim);
            }
        }

        private void Awake()
        {
            // 단순 싱글톤 (DontDestroyOnLoad 사용하지 않음)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LoadSettings();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// PlayerPrefs에서 설정 로드
        /// </summary>
        public void LoadSettings()
        {
            noteSpeed = PlayerPrefs.GetFloat(KEY_NOTE_SPEED, DEFAULT_NOTE_SPEED);
            judgementOffset = PlayerPrefs.GetFloat(KEY_JUDGEMENT_OFFSET, DEFAULT_JUDGEMENT_OFFSET);
            bgmVolume = PlayerPrefs.GetFloat(KEY_BGM_VOLUME, DEFAULT_BGM_VOLUME);
            sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, DEFAULT_SFX_VOLUME);
            backgroundDim = PlayerPrefs.GetFloat(KEY_BACKGROUND_DIM, DEFAULT_BACKGROUND_DIM);

#if UNITY_EDITOR
            Debug.Log($"[SettingsManager] Settings loaded - Speed:{noteSpeed}, Offset:{judgementOffset*1000f}ms, BGM:{bgmVolume}, SFX:{sfxVolume}, Dim:{backgroundDim}");
#endif
        }

        /// <summary>
        /// 현재 설정을 PlayerPrefs에 저장
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_NOTE_SPEED, noteSpeed);
            PlayerPrefs.SetFloat(KEY_JUDGEMENT_OFFSET, judgementOffset);
            PlayerPrefs.SetFloat(KEY_BGM_VOLUME, bgmVolume);
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, sfxVolume);
            PlayerPrefs.SetFloat(KEY_BACKGROUND_DIM, backgroundDim);
            PlayerPrefs.Save();

#if UNITY_EDITOR
            Debug.Log("[SettingsManager] Settings saved");
#endif
        }

        /// <summary>
        /// 모든 설정을 기본값으로 초기화
        /// </summary>
        public void ResetToDefaults()
        {
            NoteSpeed = DEFAULT_NOTE_SPEED;
            JudgementOffset = DEFAULT_JUDGEMENT_OFFSET;
            BGMVolume = DEFAULT_BGM_VOLUME;
            SFXVolume = DEFAULT_SFX_VOLUME;
            BackgroundDim = DEFAULT_BACKGROUND_DIM;
            PlayerPrefs.Save();

#if UNITY_EDITOR
            Debug.Log("[SettingsManager] Settings reset to defaults");
#endif
        }
    }
}
