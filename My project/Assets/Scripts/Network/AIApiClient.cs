using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using AIBeat.Data;

namespace AIBeat.Network
{
    /// <summary>
    /// AI 음악 생성 API 통신 클라이언트
    /// </summary>
    public class AIApiClient : MonoBehaviour, ISongGenerator
    {
        [Header("API Settings")]
        [SerializeField] private string apiBaseUrl = "https://api.example.com";
        [SerializeField] private float pollInterval = 3f;      // 상태 확인 간격
        [SerializeField] private float maxWaitTime = 60f;      // 최대 대기 시간

        private string apiKey;

        public event Action<float> OnGenerationProgress;       // 0~1 진행률
        public event Action<SongData> OnGenerationComplete;
        public event Action<string> OnGenerationError;

        private void Start()
        {
            // API 키는 환경변수 또는 보안 저장소에서 로드
            // 절대 하드코딩하지 않음
            apiKey = LoadApiKey();
        }

        private string LoadApiKey()
        {
            // 실제 구현에서는 PlayerPrefs 대신 안전한 저장소 사용
            // 예: Unity Secure PlayerPrefs, Keychain, etc.
            return PlayerPrefs.GetString("AI_API_KEY", "");
        }

        /// <summary>
        /// ISongGenerator 인터페이스 구현
        /// </summary>
        public void Generate(PromptOptions options) => RequestGeneration(options);

        /// <summary>
        /// AI 음악 생성 요청
        /// </summary>
        public void RequestGeneration(PromptOptions options)
        {
            StartCoroutine(GenerationFlow(options));
        }

        private IEnumerator GenerationFlow(PromptOptions options)
        {
            // 1. 생성 요청
            string taskId = null;
            yield return StartCoroutine(SendGenerationRequest(options, (id) => taskId = id));

            if (string.IsNullOrEmpty(taskId))
            {
                OnGenerationError?.Invoke("Failed to start generation");
                yield break;
            }

            // 2. 상태 폴링
            float elapsed = 0f;
            SongData result = null;

            while (elapsed < maxWaitTime)
            {
                yield return new WaitForSeconds(pollInterval);
                elapsed += pollInterval;

                OnGenerationProgress?.Invoke(Mathf.Clamp01(elapsed / maxWaitTime));

                yield return StartCoroutine(CheckStatus(taskId, (data) => result = data));

                if (result != null)
                {
                    OnGenerationComplete?.Invoke(result);
                    yield break;
                }
            }

            OnGenerationError?.Invoke("Generation timeout");
        }

        private IEnumerator SendGenerationRequest(PromptOptions options, Action<string> onTaskId)
        {
            string url = $"{apiBaseUrl}/api/generate";
            string jsonBody = JsonUtility.ToJson(new GenerationRequest
            {
                genre = options.Genre,
                bpm = options.BPM,
                mood = options.Mood,
                duration = options.Duration,
                structure = options.Structure
            });

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<GenerationResponse>(request.downloadHandler.text);
                    onTaskId?.Invoke(response.taskId);
                }
                else
                {
                    Debug.LogError($"[AIApiClient] Request failed: {request.error}");
                    onTaskId?.Invoke(null);
                }
            }
        }

        private IEnumerator CheckStatus(string taskId, Action<SongData> onComplete)
        {
            string url = $"{apiBaseUrl}/api/status/{taskId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<StatusResponse>(request.downloadHandler.text);

                    if (response.status == "completed")
                    {
                        SongData songData = ParseSongData(response);
                        onComplete?.Invoke(songData);
                    }
                }
            }
        }

        private SongData ParseSongData(StatusResponse response)
        {
            var sections = new SongSection[response.sections.Length];
            for (int i = 0; i < response.sections.Length; i++)
            {
                var s = response.sections[i];
                sections[i] = new SongSection(s.name, s.start, s.end, GetDensityForSection(s.name));
            }

            return new SongData
            {
                Id = response.id,
                Title = $"{response.genre} Beat",
                BPM = response.bpm,
                Duration = response.duration,
                AudioUrl = response.songUrl,
                Sections = sections,
                Genre = response.genre,
                CreatedAt = DateTime.Now
            };
        }

        private float GetDensityForSection(string name)
        {
            return name.ToLower() switch
            {
                "intro" => 0.3f,
                "build" => 0.5f,
                "drop" => 1.0f,
                "outro" => 0.4f,
                _ => 0.5f
            };
        }

        // API 요청/응답 구조체
        [Serializable]
        private class GenerationRequest
        {
            public string genre;
            public int bpm;
            public string mood;
            public int duration;
            public string structure;
        }

        [Serializable]
        private class GenerationResponse
        {
            public string taskId;
        }

        [Serializable]
        private class StatusResponse
        {
            public string status;
            public string id;
            public string songUrl;
            public float bpm;
            public float duration;
            public string genre;
            public SectionData[] sections;
        }

        [Serializable]
        private class SectionData
        {
            public string name;
            public float start;
            public float end;
        }
    }
}
