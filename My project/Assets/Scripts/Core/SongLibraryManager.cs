using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIBeat.Core
{
    /// <summary>
    /// 곡 라이브러리 관리 (생성된 곡 기록 저장/로드)
    /// PlayerPrefs + JSON 직렬화로 곡 목록을 영구 저장
    /// </summary>
    public class SongLibraryManager : MonoBehaviour
    {
        public static SongLibraryManager Instance { get; private set; }

        private const string LIBRARY_KEY = "SongLibrary";
        private const int MAX_SONGS = 100;

        private SongLibraryData libraryData;

        public event Action OnLibraryChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadLibrary();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// PlayerPrefs에서 라이브러리 데이터 로드
        /// </summary>
        private void LoadLibrary()
        {
            string json = PlayerPrefs.GetString(LIBRARY_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    libraryData = JsonUtility.FromJson<SongLibraryData>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SongLibrary] Library load failed, initialized: {e.Message}");
                    libraryData = new SongLibraryData();
                }
            }
            else
            {
                libraryData = new SongLibraryData();
            }

            // CoverImagePath가 없는 기존 레코드에 소급 적용
            MigrateCoverImages();

#if UNITY_EDITOR
            Debug.Log($"[SongLibrary] Loaded: {libraryData.songs.Count} songs");
#endif
        }

        /// <summary>
        /// 기존 SongRecord의 CoverImagePath를 최신화.
        /// - 비어 있으면: FindCoverImage로 탐색 (ID3 임베드 우선)
        /// - 이미 있지만 "id3:" 접두사가 아닌 파일 경로면: ID3 임베드가 있을 경우 "id3:" 경로로 업그레이드
        /// </summary>
        private void MigrateCoverImages()
        {
            bool changed = false;
            foreach (var record in libraryData.songs)
            {
                string fullPath = ResolveAudioPath(record.AudioFileName);
                if (string.IsNullOrEmpty(fullPath)) continue;

                // 이미 id3: 경로면 스킵
                if (!string.IsNullOrEmpty(record.CoverImagePath) &&
                    record.CoverImagePath.StartsWith("id3:")) continue;

                // 비어 있거나 파일 경로인 경우: FindCoverImage로 재탐색
                // (ID3 임베드가 있으면 "id3:" 접두사 반환, 없으면 파일 경로)
                string cover = AndroidMusicScanner.FindCoverImage(fullPath);
                if (!string.IsNullOrEmpty(cover) && cover != record.CoverImagePath)
                {
                    record.CoverImagePath = cover;
                    changed = true;
                }
            }
            if (changed) SaveLibrary();
        }

        /// <summary>
        /// AudioFileName 문자열로부터 실제 절대 파일 경로 복원.
        /// </summary>
        private static string ResolveAudioPath(string audioFileName)
        {
            if (string.IsNullOrEmpty(audioFileName)) return null;
            if (audioFileName.StartsWith("ext:"))
                return audioFileName.Substring(4);
            if (audioFileName.StartsWith("music:"))
                return System.IO.Path.Combine(
                    Application.persistentDataPath, "Music",
                    audioFileName.Substring(6));
            // StreamingAssets 상대 경로
            return System.IO.Path.Combine(Application.streamingAssetsPath, audioFileName);
        }

        /// <summary>
        /// PlayerPrefs에 라이브러리 데이터 저장
        /// </summary>
        private void SaveLibrary()
        {
            string json = JsonUtility.ToJson(libraryData);
            PlayerPrefs.SetString(LIBRARY_KEY, json);
            PlayerPrefs.Save();
            OnLibraryChanged?.Invoke();
        }

        /// <summary>
        /// 새 곡 기록 추가
        /// </summary>
        public bool AddSong(SongRecord record)
        {
            if (libraryData.songs.Count >= MAX_SONGS)
            {
                Debug.LogWarning($"[SongLibrary] Max storage limit reached ({MAX_SONGS} songs)");
                return false;
            }

            // 중복 체크 (AudioFileName 기반 — 같은 파일은 한 번만 등록)
            var existing = libraryData.songs.Find(s =>
                !string.IsNullOrEmpty(s.AudioFileName) && s.AudioFileName == record.AudioFileName);

            if (existing != null)
            {
#if UNITY_EDITOR
                Debug.Log($"[SongLibrary] Song already exists: {record.Title}");
#endif
                return false;
            }

            record.CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            libraryData.songs.Add(record);
            SaveLibrary();

#if UNITY_EDITOR
            Debug.Log($"[SongLibrary] Song added: {record.Title} (total: {libraryData.songs.Count} songs)");
#endif
            return true;
        }

        /// <summary>
        /// 모든 곡 목록 반환
        /// </summary>
        public List<SongRecord> GetAllSongs()
        {
            return new List<SongRecord>(libraryData.songs);
        }

        /// <summary>
        /// 장르별 곡 필터링
        /// </summary>
        public List<SongRecord> GetSongsByGenre(string genre)
        {
            if (string.IsNullOrEmpty(genre) || genre == "All")
                return GetAllSongs();

            return libraryData.songs
                .Where(s => s.Genre.Equals(genre, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 랭크별 곡 필터링
        /// </summary>
        public List<SongRecord> GetSongsByRank(string rank)
        {
            if (string.IsNullOrEmpty(rank) || rank == "All")
                return GetAllSongs();

            return libraryData.songs
                .Where(s => s.BestRank == rank)
                .ToList();
        }

        /// <summary>
        /// 곡 삭제
        /// </summary>
        public bool DeleteSong(string title)
        {
            var song = libraryData.songs.Find(s => s.Title == title);
            if (song == null) return false;

            libraryData.songs.Remove(song);
            SaveLibrary();

#if UNITY_EDITOR
            Debug.Log($"[SongLibrary] Song deleted: {title} (remaining: {libraryData.songs.Count})");
#endif
            return true;
        }

        /// <summary>
        /// 인덱스로 곡 삭제
        /// </summary>
        public bool DeleteSongAt(int index)
        {
            if (index < 0 || index >= libraryData.songs.Count) return false;

            string title = libraryData.songs[index].Title;
            libraryData.songs.RemoveAt(index);
            SaveLibrary();

#if UNITY_EDITOR
            Debug.Log($"[SongLibrary] Song deleted: {title} (remaining: {libraryData.songs.Count})");
#endif
            return true;
        }

        /// <summary>
        /// 플레이 결과로 최고 기록 업데이트
        /// </summary>
        public void UpdateBestRecord(string title, int score, int combo, string rank)
        {
            var song = libraryData.songs.Find(s => s.Title == title);
            if (song == null) return;

            song.PlayCount++;

            // 최고 점수 갱신
            if (score > song.BestScore)
            {
                song.BestScore = score;
                song.BestRank = rank;
            }

            // 최고 콤보 갱신
            if (combo > song.BestCombo)
            {
                song.BestCombo = combo;
            }

            SaveLibrary();
#if UNITY_EDITOR
            Debug.Log($"[SongLibrary] Record updated: {title} - Score:{score}, Rank:{rank}, Play#{song.PlayCount}");
#endif
        }

        /// <summary>
        /// 정렬: 최신순
        /// </summary>
        public List<SongRecord> GetSongsSortedByDate()
        {
            return libraryData.songs
                .OrderByDescending(s => s.CreatedDate)
                .ToList();
        }

        /// <summary>
        /// 정렬: 점수순
        /// </summary>
        public List<SongRecord> GetSongsSortedByScore()
        {
            return libraryData.songs
                .OrderByDescending(s => s.BestScore)
                .ToList();
        }

        /// <summary>
        /// 정렬: 플레이 횟수순
        /// </summary>
        public List<SongRecord> GetSongsSortedByPlayCount()
        {
            return libraryData.songs
                .OrderByDescending(s => s.PlayCount)
                .ToList();
        }

        /// <summary>
        /// 총 곡 수
        /// </summary>
        public int SongCount => libraryData.songs.Count;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    /// <summary>
    /// 곡 기록 데이터 (JSON 직렬화용)
    /// </summary>
    [Serializable]
    public class SongRecord
    {
        public string Title;
        public string Artist;
        public string Genre;
        public string Mood;
        public int BPM;
        public int DifficultyLevel;
        public float Duration;
        public string BestRank;      // S+, S, A, B, C, D
        public int BestScore;
        public int BestCombo;
        public int PlayCount;
        public string CreatedDate;
        public int Seed;             // SmartBeatMapper 재생성을 위한 시드
        public string AudioFileName; // StreamingAssets 내 MP3 파일명 (예: "jpop_energetic.mp3")
        public string CoverImagePath; // 커버 이미지 절대 경로 (없으면 기본 앨범 아트 사용)
    }

    /// <summary>
    /// 라이브러리 전체 데이터 (JSON 직렬화 래퍼)
    /// </summary>
    [Serializable]
    public class SongLibraryData
    {
        public List<SongRecord> songs = new List<SongRecord>();
    }
}
