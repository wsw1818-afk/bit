using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using AIBeat.Data;
using AIBeat.Audio;

namespace AIBeat.Network
{
    /// <summary>
    /// MVP 테스트용 가짜 곡 생성기
    /// 실제 AI API 없이 사전 준비된 곡 사용
    /// </summary>
    public class FakeSongGenerator : MonoBehaviour, ISongGenerator
    {
        [Header("Settings")]
        [SerializeField] private float fakeGenerationTime = 3f;  // 가짜 생성 대기 시간
        [SerializeField] private AudioClip[] preloadedSongs;     // Resources에서 로드할 곡들
        [SerializeField] private TextAsset songDataJson;         // 사전 정의된 곡 데이터

        [Header("Demo Songs")]
        [SerializeField] private List<DemoSongData> demoSongs = new List<DemoSongData>();

        private BeatMapper beatMapper;

        public event Action<float> OnGenerationProgress;
        public event Action<SongData> OnGenerationComplete;
        public event Action<string> OnGenerationError;

        private void Awake()
        {
            beatMapper = GetComponent<BeatMapper>();
            if (beatMapper == null)
            {
                beatMapper = gameObject.AddComponent<BeatMapper>();
            }

            // 데모 곡 초기화
            InitializeDemoSongs();
        }

        private void InitializeDemoSongs()
        {
            // 데모 곡이 없으면 기본값 추가
            if (demoSongs.Count == 0)
            {
                demoSongs.Add(new DemoSongData
                {
                    id = "demo_edm_fast",
                    title = "Neon Rush",
                    genre = "EDM",
                    mood = "Aggressive",
                    bpm = 140,
                    duration = 90,
                    difficulty = 5
                });

                demoSongs.Add(new DemoSongData
                {
                    id = "demo_house_medium",
                    title = "Midnight Groove",
                    genre = "House",
                    mood = "Chill",
                    bpm = 120,
                    duration = 120,
                    difficulty = 3
                });

                demoSongs.Add(new DemoSongData
                {
                    id = "demo_cyberpunk_fast",
                    title = "Digital Warfare",
                    genre = "Cyberpunk",
                    mood = "Dark",
                    bpm = 160,
                    duration = 100,
                    difficulty = 7
                });

                demoSongs.Add(new DemoSongData
                {
                    id = "demo_synthwave_slow",
                    title = "Retro Dreams",
                    genre = "Synthwave",
                    mood = "Melancholic",
                    bpm = 100,
                    duration = 110,
                    difficulty = 4
                });
            }
        }

        /// <summary>
        /// ISongGenerator 인터페이스 구현
        /// </summary>
        public void Generate(PromptOptions options) => GenerateSong(options);

        /// <summary>
        /// 프롬프트 옵션에 맞는 곡 "생성" (실제로는 매칭)
        /// </summary>
        public void GenerateSong(PromptOptions options)
        {
            StartCoroutine(FakeGenerationProcess(options));
        }

        private IEnumerator FakeGenerationProcess(PromptOptions options)
        {
            float elapsed = 0f;

            // 가짜 진행률 표시
            while (elapsed < fakeGenerationTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / fakeGenerationTime);
                OnGenerationProgress?.Invoke(progress);
                yield return null;
            }

            // 프롬프트에 맞는 곡 찾기
            DemoSongData matchedSong = FindMatchingSong(options);

            if (matchedSong == null)
            {
                OnGenerationError?.Invoke("No matching song found");
                yield break;
            }

            // SongData 생성
            SongData songData = CreateSongData(matchedSong, options);

            OnGenerationComplete?.Invoke(songData);
        }

        private DemoSongData FindMatchingSong(PromptOptions options)
        {
            // 장르와 BPM 범위로 매칭
            DemoSongData bestMatch = null;
            int bestScore = 0;

            foreach (var demo in demoSongs)
            {
                int score = 0;

                // 장르 매칭
                if (demo.genre.Equals(options.Genre, StringComparison.OrdinalIgnoreCase))
                    score += 10;

                // BPM 범위 매칭 (±20 BPM 이내)
                if (Mathf.Abs(demo.bpm - options.BPM) <= 20)
                    score += 5;

                // 분위기 매칭
                if (demo.mood.Equals(options.Mood, StringComparison.OrdinalIgnoreCase))
                    score += 3;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = demo;
                }
            }

            // 매칭되는 곡이 없으면 랜덤
            if (bestMatch == null && demoSongs.Count > 0)
            {
                bestMatch = demoSongs[UnityEngine.Random.Range(0, demoSongs.Count)];
            }

            return bestMatch;
        }

        private SongData CreateSongData(DemoSongData demo, PromptOptions options)
        {
            // 구간 생성
            var sections = BeatMapper.CreateDefaultSections(demo.duration);

            // 노트 생성
            var notes = beatMapper.GenerateNotesFromBPM(demo.bpm, demo.duration, sections);

            return new SongData
            {
                Id = demo.id,
                Title = demo.title,
                Artist = "AI Generated",
                BPM = demo.bpm,
                Duration = demo.duration,
                AudioUrl = GetAudioUrl(demo.id),
                Sections = sections,
                Notes = notes.ToArray(),
                Difficulty = demo.difficulty,
                Genre = demo.genre,
                Mood = demo.mood,
                CreatedAt = DateTime.Now
            };
        }

        private string GetAudioUrl(string songId)
        {
            // 실제 구현에서는 서버 URL 또는 로컬 경로 반환
            // MVP에서는 Resources 폴더 경로
            return $"Audio/BGM/{songId}";
        }

        /// <summary>
        /// 모든 데모 곡 목록 반환 (라이브러리용)
        /// </summary>
        public List<SongData> GetAllDemoSongs()
        {
            var result = new List<SongData>();

            foreach (var demo in demoSongs)
            {
                var sections = BeatMapper.CreateDefaultSections(demo.duration);
                var notes = beatMapper.GenerateNotesFromBPM(demo.bpm, demo.duration, sections);

                result.Add(new SongData
                {
                    Id = demo.id,
                    Title = demo.title,
                    Artist = "AI Generated",
                    BPM = demo.bpm,
                    Duration = demo.duration,
                    AudioUrl = GetAudioUrl(demo.id),
                    Sections = sections,
                    Notes = notes.ToArray(),
                    Difficulty = demo.difficulty,
                    Genre = demo.genre,
                    Mood = demo.mood
                });
            }

            return result;
        }

        /// <summary>
        /// 커스텀 데모 곡 추가
        /// </summary>
        public void AddDemoSong(DemoSongData songData)
        {
            demoSongs.Add(songData);
        }
    }

    [Serializable]
    public class DemoSongData
    {
        public string id;
        public string title;
        public string genre;
        public string mood;
        public int bpm;
        public float duration;
        public int difficulty;
        public AudioClip audioClip;  // 직접 할당용
    }
}
