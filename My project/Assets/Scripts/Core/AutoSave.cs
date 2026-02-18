using UnityEngine;
using System;

namespace AIBeat.Core
{
    /// <summary>
    /// 자동 저장 시스템
    /// 플레이 통계를 주기적으로 PlayerPrefs에 저장
    /// </summary>
    public class AutoSave : MonoBehaviour
    {
        [SerializeField] private float saveInterval = 30f;

        private static readonly string KeyLastPlayDate = "LastPlayDate";
        private static readonly string KeyTotalPlayCount = "TotalPlayCount";
        private static readonly string KeyLastSong = "LastSong";
        private static readonly string KeyTotalPlayTime = "TotalPlayTime";

        private float sessionStartTime;

        private void Start()
        {
            sessionStartTime = Time.realtimeSinceStartup;
            InvokeRepeating(nameof(SaveProgress), saveInterval, saveInterval);
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetString(KeyLastPlayDate, DateTime.Now.ToString("O"));

            // 누적 플레이 시간 (초)
            float elapsed = Time.realtimeSinceStartup - sessionStartTime;
            float totalTime = PlayerPrefs.GetFloat(KeyTotalPlayTime, 0f) + elapsed;
            PlayerPrefs.SetFloat(KeyTotalPlayTime, totalTime);
            sessionStartTime = Time.realtimeSinceStartup;

            PlayerPrefs.Save();
        }

        /// <summary>
        /// 곡 플레이 완료 시 호출 (GameplayController에서)
        /// </summary>
        public static void RecordSongPlay(string songTitle)
        {
            PlayerPrefs.SetString(KeyLastSong, songTitle ?? "");
            int count = PlayerPrefs.GetInt(KeyTotalPlayCount, 0) + 1;
            PlayerPrefs.SetInt(KeyTotalPlayCount, count);
            PlayerPrefs.SetString(KeyLastPlayDate, DateTime.Now.ToString("O"));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 마지막 플레이 날짜 조회
        /// </summary>
        public static DateTime? GetLastPlayDate()
        {
            string dateStr = PlayerPrefs.GetString(KeyLastPlayDate, "");
            if (string.IsNullOrEmpty(dateStr)) return null;
            if (DateTime.TryParse(dateStr, out var date)) return date;
            return null;
        }

        /// <summary>
        /// 총 플레이 횟수 조회
        /// </summary>
        public static int GetTotalPlayCount()
        {
            return PlayerPrefs.GetInt(KeyTotalPlayCount, 0);
        }

        private void OnDestroy()
        {
            CancelInvoke();
            // 파괴 시 마지막 저장
            SaveProgress();
        }
    }
}
