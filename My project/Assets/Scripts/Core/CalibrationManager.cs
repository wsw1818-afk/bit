using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AIBeat.Core
{
    /// <summary>
    /// 오프셋 캘리브레이션 (탭 테스트)
    /// 메트로놈 비트에 맞춰 탭 → 평균 오프셋 자동 측정
    /// </summary>
    public class CalibrationManager : MonoBehaviour
    {
        [Header("Calibration Settings")]
        [SerializeField] private float calibrationBPM = 120f;
        [SerializeField] private int totalBeats = 16;            // 16비트 측정
        [SerializeField] private int discardFirstBeats = 2;      // 처음 2비트 버림 (적응 시간)
        [SerializeField] private float countInBeats = 4;         // 시작 전 4비트 카운트

        private float beatInterval;     // 비트 간격 (초)
        private float startTime;        // 캘리브레이션 시작 시간
        private int currentBeat;        // 현재 비트 번호
        private bool isRunning;
        private bool isCountIn;         // 카운트인 중
        private List<float> offsets = new List<float>();

        // AudioSource for metronome tick
        private AudioSource tickSource;

        public event Action<int, int> OnBeat;              // (현재 비트, 전체 비트)
        public event Action<float> OnCalibrationComplete;  // 측정된 오프셋 (초)
        public event Action OnCalibrationCancelled;
        public event Action<string> OnStatusChanged;       // 상태 메시지

        public bool IsRunning => isRunning;

        private void Awake()
        {
            tickSource = gameObject.AddComponent<AudioSource>();
            tickSource.playOnAwake = false;
            tickSource.volume = 0.8f;
        }

        /// <summary>
        /// 캘리브레이션 시작
        /// </summary>
        public void StartCalibration()
        {
            if (isRunning) return;
            StartCoroutine(CalibrationFlow());
        }

        /// <summary>
        /// 캘리브레이션 취소
        /// </summary>
        public void CancelCalibration()
        {
            if (!isRunning) return;
            StopAllCoroutines();
            isRunning = false;
            OnCalibrationCancelled?.Invoke();
        }

        /// <summary>
        /// 탭 입력 (화면 터치 또는 키 입력 시 호출)
        /// </summary>
        public void RegisterTap()
        {
            if (!isRunning || isCountIn) return;

            float tapTime = Time.realtimeSinceStartup;
            float elapsed = tapTime - startTime;

            // 가장 가까운 비트 시간 찾기
            float nearestBeatTime = Mathf.Round(elapsed / beatInterval) * beatInterval;
            float offset = elapsed - nearestBeatTime;

            // 첫 N비트는 버림 (적응 시간)
            int beatIndex = Mathf.RoundToInt(elapsed / beatInterval);
            if (beatIndex >= discardFirstBeats)
            {
                offsets.Add(offset);
            }
        }

        private IEnumerator CalibrationFlow()
        {
            isRunning = true;
            offsets.Clear();
            beatInterval = 60f / calibrationBPM;

            // 메트로놈 틱 생성
            AudioClip tick = CreateTickClip();

            // 카운트인
            isCountIn = true;
            OnStatusChanged?.Invoke("Listen to the beat...");

            for (int i = 0; i < (int)countInBeats; i++)
            {
                tickSource.PlayOneShot(tick);
                OnBeat?.Invoke(i + 1, (int)countInBeats);
                yield return new WaitForSecondsRealtime(beatInterval);
            }

            // 측정 시작
            isCountIn = false;
            startTime = Time.realtimeSinceStartup;
            OnStatusChanged?.Invoke("Tap along with the beat!");

            for (int beat = 0; beat < totalBeats; beat++)
            {
                currentBeat = beat;
                tickSource.PlayOneShot(tick);
                OnBeat?.Invoke(beat + 1, totalBeats);
                yield return new WaitForSecondsRealtime(beatInterval);
            }

            // 결과 계산
            isRunning = false;

            if (offsets.Count < 3)
            {
                OnStatusChanged?.Invoke("Not enough taps. Try again.");
                OnCalibrationCancelled?.Invoke();
                yield break;
            }

            // 이상치 제거 (IQR 방식)
            float medianOffset = CalculateMedian(offsets);
            float q1 = CalculatePercentile(offsets, 25f);
            float q3 = CalculatePercentile(offsets, 75f);
            float iqr = q3 - q1;
            float lowerBound = q1 - 1.5f * iqr;
            float upperBound = q3 + 1.5f * iqr;

            List<float> filtered = new List<float>();
            foreach (float o in offsets)
            {
                if (o >= lowerBound && o <= upperBound)
                    filtered.Add(o);
            }

            float avgOffset = 0f;
            foreach (float o in filtered)
                avgOffset += o;
            avgOffset = filtered.Count > 0 ? avgOffset / filtered.Count : 0f;

            // ms 단위 반올림 (1ms 단위)
            avgOffset = Mathf.Round(avgOffset * 1000f) / 1000f;

            // 결과를 SettingsManager에 적용
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.JudgementOffset = avgOffset;

            string resultMsg = $"Offset: {avgOffset * 1000f:F0}ms ({filtered.Count} taps)";
            OnStatusChanged?.Invoke(resultMsg);
            OnCalibrationComplete?.Invoke(avgOffset);

#if UNITY_EDITOR
            Debug.Log($"[CalibrationManager] Result: {avgOffset * 1000f:F1}ms (raw={offsets.Count} taps, filtered={filtered.Count})");
#endif
        }

        /// <summary>
        /// 프로시저럴 메트로놈 틱 생성
        /// </summary>
        private AudioClip CreateTickClip()
        {
            int sampleRate = 44100;
            int samples = sampleRate / 20; // 50ms
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 40f); // 빠른 감쇠
                data[i] = Mathf.Sin(2f * Mathf.PI * 1000f * t) * envelope; // 1kHz 톤
            }

            var clip = AudioClip.Create("MetronomeTick", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private float CalculateMedian(List<float> values)
        {
            var sorted = new List<float>(values);
            sorted.Sort();
            int n = sorted.Count;
            if (n == 0) return 0f;
            return n % 2 == 0 ? (sorted[n / 2 - 1] + sorted[n / 2]) / 2f : sorted[n / 2];
        }

        private float CalculatePercentile(List<float> values, float percentile)
        {
            var sorted = new List<float>(values);
            sorted.Sort();
            float index = (percentile / 100f) * (sorted.Count - 1);
            int lower = Mathf.FloorToInt(index);
            int upper = Mathf.CeilToInt(index);
            if (lower == upper) return sorted[lower];
            float frac = index - lower;
            return sorted[lower] * (1f - frac) + sorted[upper] * frac;
        }
    }
}
