using UnityEngine;
using System;
using AIBeat.Core;

namespace AIBeat.Gameplay
{
    /// <summary>
    /// 노트 타이밍 판정 및 점수 계산
    /// </summary>
    public class JudgementSystem : MonoBehaviour
    {
        [Header("Judgement Windows (seconds)")]
        [SerializeField] private float perfectWindow = 0.050f;  // ±50ms
        [SerializeField] private float greatWindow = 0.100f;    // ±100ms
        [SerializeField] private float goodWindow = 0.200f;     // ±200ms
        [SerializeField] private float badWindow = 0.350f;      // ±350ms

        [Header("Score Settings")]
        [SerializeField] private int baseScorePerNote = 1000;
        [SerializeField] private float maxComboBonus = 0.5f;    // 최대 50% 보너스
        [SerializeField] private int comboForMaxBonus = 100;

        [Header("User Offset")]
        [SerializeField] private float userOffset = 0f;         // 사용자 오프셋 조정

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private int totalNotes;
        private int currentScore;
        private int currentCombo;
        private int maxCombo;

        // 판정 카운트
        private int perfectCount;
        private int greatCount;
        private int goodCount;
        private int badCount;
        private int missCount;

        public int CurrentScore => currentScore;
        public int CurrentCombo => currentCombo;
        public int MaxCombo => maxCombo;
        public float Accuracy => CalculateAccuracy();
        public int PerfectCount => perfectCount;
        public int GreatCount => greatCount;
        public int GoodCount => goodCount;
        public int BadCount => badCount;
        public int MissCount => missCount;

        public event Action<JudgementResult, int> OnJudgement; // (결과, 콤보)
        public event Action<JudgementResult, float> OnJudgementDetailed; // (결과, rawDiff: 양수=late, 음수=early)
        public event Action<int> OnScoreChanged;
        public event Action<int> OnComboChanged;

        public void Initialize(int noteCount)
        {
            totalNotes = noteCount;
            currentScore = 0;
            currentCombo = 0;
            maxCombo = 0;
            perfectCount = 0;
            greatCount = 0;
            goodCount = 0;
            badCount = 0;
            missCount = 0;

            // PlayerPrefs에서 저장된 오프셋 로드
            userOffset = GetUserOffset();

            // SettingsManager에서 오프셋 적용 (있으면 덮어쓰기)
            if (SettingsManager.Instance != null)
                userOffset = SettingsManager.Instance.JudgementOffset;
            SettingsManager.OnSettingChanged -= OnSettingChanged;
            SettingsManager.OnSettingChanged += OnSettingChanged;

#if UNITY_EDITOR
            if (showDebugLogs)
            {
                Debug.Log($"[JudgementSystem] Initialized with {noteCount} notes | Windows: P={perfectWindow*1000}ms, G={greatWindow*1000}ms, Good={goodWindow*1000}ms, B={badWindow*1000}ms | Offset: {userOffset*1000f}ms");
            }
#endif
        }

        /// <summary>
        /// SettingsManager에서 오프셋 변경 시 즉시 적용
        /// </summary>
        private void OnSettingChanged(string key, float value)
        {
            if (key == SettingsManager.KEY_JUDGEMENT_OFFSET)
            {
                userOffset = value;
            }
        }

        public void SetUserOffset(float offset)
        {
            userOffset = offset;
            PlayerPrefs.SetFloat("JudgementOffset", offset);
        }

        public float GetUserOffset()
        {
            return PlayerPrefs.GetFloat("JudgementOffset", 0f);
        }

        /// <summary>
        /// 입력 타이밍과 노트 타이밍을 비교하여 판정
        /// </summary>
        public JudgementResult Judge(float inputTime, float noteTime)
        {
            float adjustedNoteTime = noteTime + userOffset;
            float rawDiff = inputTime - adjustedNoteTime; // 양수=late, 음수=early
            float diff = Mathf.Abs(rawDiff);

            JudgementResult result;

            if (diff <= perfectWindow)
            {
                result = JudgementResult.Perfect;
                perfectCount++;
                AddCombo();
            }
            else if (diff <= greatWindow)
            {
                result = JudgementResult.Great;
                greatCount++;
                AddCombo();
            }
            else if (diff <= goodWindow)
            {
                result = JudgementResult.Good;
                goodCount++;
                AddCombo();
            }
            else if (diff <= badWindow)
            {
                result = JudgementResult.Bad;
                badCount++;
                ResetCombo();
            }
            else
            {
                result = JudgementResult.Miss;
                missCount++;
                ResetCombo();
            }

            int scoreGained = CalculateScore(result);
            currentScore += scoreGained;

            // 디버그 로그 (에디터에서만 - 매 노트마다 호출되어 성능 영향)
#if UNITY_EDITOR
            if (showDebugLogs)
            {
                Debug.Log($"[Judge] {result} | diff: {diff*1000:F1}ms | combo: {currentCombo} | score: +{scoreGained} (total: {currentScore})");
            }
#endif

            OnJudgement?.Invoke(result, currentCombo);
            OnJudgementDetailed?.Invoke(result, rawDiff);
            OnScoreChanged?.Invoke(currentScore);

            // 히트사운드 재생
            AudioManager.Instance?.PlayHitSound(result);

            return result;
        }

        /// <summary>
        /// 노트를 놓친 경우 (Miss)
        /// </summary>
        public void RegisterMiss()
        {
            missCount++;
            ResetCombo();

#if UNITY_EDITOR
            if (showDebugLogs)
            {
                Debug.Log($"[Judge] MISS (note passed) | total misses: {missCount}");
            }
#endif

            OnJudgement?.Invoke(JudgementResult.Miss, 0);
            OnJudgementDetailed?.Invoke(JudgementResult.Miss, 0f);
        }

        private void AddCombo()
        {
            currentCombo++;
            if (currentCombo > maxCombo)
            {
                maxCombo = currentCombo;
            }
            OnComboChanged?.Invoke(currentCombo);
        }

        private void ResetCombo()
        {
            currentCombo = 0;
            OnComboChanged?.Invoke(currentCombo);
        }

        private int CalculateScore(JudgementResult result)
        {
            float scoreMultiplier = result switch
            {
                JudgementResult.Perfect => 1.0f,
                JudgementResult.Great => 0.8f,
                JudgementResult.Good => 0.5f,
                JudgementResult.Bad => 0.2f,
                _ => 0f
            };

            // 콤보 보너스 계산
            float comboRatio = Mathf.Min(1f, (float)currentCombo / comboForMaxBonus);
            float comboBonus = 1f + (comboRatio * maxComboBonus);

            return Mathf.RoundToInt(baseScorePerNote * scoreMultiplier * comboBonus);
        }

        private float CalculateAccuracy()
        {
            int totalHits = perfectCount + greatCount + goodCount + badCount + missCount;
            if (totalHits == 0) return 100f;

            float weightedSum = (perfectCount * 100f) +
                               (greatCount * 80f) +
                               (goodCount * 50f) +
                               (badCount * 20f);

            return weightedSum / totalHits;
        }

        /// <summary>
        /// 최종 결과 데이터 반환
        /// </summary>
        public GameResult GetResult()
        {
            return new GameResult
            {
                Score = currentScore,
                MaxCombo = maxCombo,
                Accuracy = Accuracy,
                PerfectCount = perfectCount,
                GreatCount = greatCount,
                GoodCount = goodCount,
                BadCount = badCount,
                MissCount = missCount,
                TotalNotes = totalNotes,
                Rank = CalculateRank()
            };
        }

        private string CalculateRank()
        {
            float acc = Accuracy;

            if (acc >= 98 && missCount == 0) return "S+";
            if (acc >= 95) return "S";
            if (acc >= 90) return "A";
            if (acc >= 80) return "B";
            if (acc >= 70) return "C";
            return "D";
        }

        private void OnDestroy()
        {
            SettingsManager.OnSettingChanged -= OnSettingChanged;
        }
    }

    [Serializable]
    public struct GameResult
    {
        public int Score;
        public int MaxCombo;
        public float Accuracy;
        public int PerfectCount;
        public int GreatCount;
        public int GoodCount;
        public int BadCount;
        public int MissCount;
        public int TotalNotes;
        public string Rank;
    }
}
