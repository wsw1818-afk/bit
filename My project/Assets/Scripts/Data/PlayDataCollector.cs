using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using AIBeat.Audio;
using AIBeat.Core;
using AIBeat.Gameplay;

namespace AIBeat.Data
{
    /// <summary>
    /// 플레이 데이터 수집기
    /// 판정 타이밍 차이, 구간별 판정 분포를 기록하고 JSON으로 저장
    /// AdaptiveTuner가 이 데이터를 기반으로 파라미터를 보정
    /// </summary>
    public class PlayDataCollector : MonoBehaviour
    {
        public static PlayDataCollector Instance { get; private set; }

        private PlaySessionData currentSession;
        private bool isCollecting;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 수집 시작 (곡 시작 시 호출)
        /// </summary>
        public void StartCollecting(string songId, int difficulty,
            AnalysisParams analysisParams, MappingParams mappingParams,
            List<OfflineAudioAnalyzer.SectionData> sections)
        {
            currentSession = new PlaySessionData
            {
                SongId = songId,
                Difficulty = difficulty,
                PlayedAt = DateTime.UtcNow.ToString("o"),
                UsedAnalysisParams = analysisParams,
                UsedMappingParams = mappingParams,
                Judgements = new List<JudgementRecord>(),
                SectionStats = new List<SectionStat>()
            };

            // 구간별 통계 초기화
            if (sections != null)
            {
                foreach (var s in sections)
                {
                    currentSession.SectionStats.Add(new SectionStat
                    {
                        SectionType = s.Type.ToString(),
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        PerfectCount = 0,
                        GreatCount = 0,
                        GoodCount = 0,
                        BadCount = 0,
                        MissCount = 0
                    });
                }
            }

            isCollecting = true;
#if UNITY_EDITOR
            Debug.Log($"[PlayData] Started collecting for '{songId}' (diff={difficulty})");
#endif
        }

        /// <summary>
        /// 판정 기록 (JudgementSystem.OnJudgementDetailed 이벤트에서 호출)
        /// </summary>
        public void RecordJudgement(JudgementResult result, float rawDiff, float noteTime)
        {
            if (!isCollecting || currentSession == null) return;

            currentSession.Judgements.Add(new JudgementRecord
            {
                NoteTime = noteTime,
                RawDiff = rawDiff,
                Result = result.ToString()
            });

            // 구간별 통계 업데이트
            foreach (var stat in currentSession.SectionStats)
            {
                if (noteTime >= stat.StartTime && noteTime < stat.EndTime)
                {
                    switch (result)
                    {
                        case JudgementResult.Perfect: stat.PerfectCount++; break;
                        case JudgementResult.Great: stat.GreatCount++; break;
                        case JudgementResult.Good: stat.GoodCount++; break;
                        case JudgementResult.Bad: stat.BadCount++; break;
                        case JudgementResult.Miss: stat.MissCount++; break;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 수집 종료 (곡 끝 시 호출)
        /// </summary>
        public void EndCollecting(GameResult gameResult)
        {
            if (!isCollecting || currentSession == null) return;

            isCollecting = false;

            currentSession.FinalScore = gameResult.Score;
            currentSession.FinalAccuracy = gameResult.Accuracy;
            currentSession.MaxCombo = gameResult.MaxCombo;
            currentSession.TotalNotes = gameResult.TotalNotes;

            // rawDiff 통계 계산
            if (currentSession.Judgements.Count > 0)
            {
                float sumDiff = 0f, sumAbsDiff = 0f;
                foreach (var j in currentSession.Judgements)
                {
                    sumDiff += j.RawDiff;
                    sumAbsDiff += Mathf.Abs(j.RawDiff);
                }
                currentSession.MeanRawDiff = sumDiff / currentSession.Judgements.Count;
                currentSession.MeanAbsRawDiff = sumAbsDiff / currentSession.Judgements.Count;
            }

            // 히스토리에 추가하고 저장
            var history = LoadHistory(currentSession.SongId);
            history.Sessions.Add(currentSession);

            // 최근 10세션만 유지
            while (history.Sessions.Count > 10)
                history.Sessions.RemoveAt(0);

            SaveHistory(history);

#if UNITY_EDITOR
            Debug.Log($"[PlayData] Saved session for '{currentSession.SongId}': " +
                      $"score={gameResult.Score}, acc={gameResult.Accuracy:F1}%, " +
                      $"meanDiff={currentSession.MeanRawDiff:F1}ms, sessions={history.Sessions.Count}");
#endif
        }

        /// <summary>
        /// 곡별 히스토리 로드
        /// </summary>
        public static SongPlayHistory LoadHistory(string songId)
        {
            string path = GetHistoryPath(songId);
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    return JsonUtility.FromJson<SongPlayHistory>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PlayData] Failed to load history for '{songId}': {e.Message}");
                }
            }

            return new SongPlayHistory
            {
                SongId = songId,
                Sessions = new List<PlaySessionData>()
            };
        }

        /// <summary>
        /// 히스토리 저장
        /// </summary>
        private static void SaveHistory(SongPlayHistory history)
        {
            string path = GetHistoryPath(history.SongId);
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            try
            {
                string json = JsonUtility.ToJson(history, true);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayData] Failed to save history: {e.Message}");
            }
        }

        private static string GetHistoryPath(string songId)
        {
            // 파일명 안전하게 변환
            string safeName = songId.Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            return Path.Combine(Application.persistentDataPath, "PlayData", $"{safeName}.json");
        }

        public bool IsCollecting => isCollecting;
    }

    // --- 직렬화 데이터 구조체 ---

    [Serializable]
    public class SongPlayHistory
    {
        public string SongId;
        public List<PlaySessionData> Sessions;
    }

    [Serializable]
    public class PlaySessionData
    {
        public string SongId;
        public int Difficulty;
        public string PlayedAt;

        // 사용된 파라미터 (재현용)
        public AnalysisParams UsedAnalysisParams;
        public MappingParams UsedMappingParams;

        // 판정 기록
        public List<JudgementRecord> Judgements;

        // 구간별 통계
        public List<SectionStat> SectionStats;

        // 최종 결과
        public int FinalScore;
        public float FinalAccuracy;
        public int MaxCombo;
        public int TotalNotes;

        // rawDiff 통계 (ms)
        public float MeanRawDiff;
        public float MeanAbsRawDiff;
    }

    [Serializable]
    public class JudgementRecord
    {
        public float NoteTime;
        public float RawDiff;  // 양수=late, 음수=early (ms)
        public string Result;  // "Perfect", "Great", etc.
    }

    [Serializable]
    public class SectionStat
    {
        public string SectionType;
        public float StartTime;
        public float EndTime;
        public int PerfectCount;
        public int GreatCount;
        public int GoodCount;
        public int BadCount;
        public int MissCount;

        public int TotalCount => PerfectCount + GreatCount + GoodCount + BadCount + MissCount;
        public float MissRate => TotalCount > 0 ? (float)MissCount / TotalCount : 0f;
        public float PerfectRate => TotalCount > 0 ? (float)PerfectCount / TotalCount : 0f;
    }
}
