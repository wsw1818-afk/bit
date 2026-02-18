using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace AIBeat.Core
{
    /// <summary>
    /// Android MediaStore API를 통해 로컬 음악 파일을 스캔.
    /// IS_MUSIC=1 필터 + 경로/크기/시간 기반 다중 필터로 음성 녹음 제외.
    /// </summary>
    public static class AndroidMusicScanner
    {
        [Serializable]
        public class ScannedMusic
        {
            public string Title;
            public string Artist;
            public long DurationMs;
            public string FilePath;
            public long FileSize;
        }

        // 음성 녹음 경로 제외 키워드 (소문자)
        private static readonly string[] RECORDING_PATH_KEYWORDS = {
            "recording", "recordings",
            "voice recorder", "voicerecorder",
            "call", "callrecord", "callrecording",
            "녹음", "통화녹음", "음성녹음",
            "samsung voice", "samsungvoice",
            "sound recorder", "soundrecorder",
            "recorder", "voice memo", "voicememo",
            "kakaotalk", "telegram", "whatsapp",
            "/ringtones/", "/notifications/", "/alarms/"
        };

        private const long MIN_FILE_SIZE_BYTES = 500 * 1024;   // 500KB
        private const long MIN_DURATION_MS = 30000;              // 30초
        private const long MAX_DURATION_MS = 60 * 60 * 1000;    // 60분

        /// <summary>
        /// Android 권한이 있는지 확인
        /// </summary>
        public static bool HasPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (GetApiLevel() >= 33)
                return Permission.HasUserAuthorizedPermission("android.permission.READ_MEDIA_AUDIO");
            else
                return Permission.HasUserAuthorizedPermission("android.permission.READ_EXTERNAL_STORAGE");
#else
            return true;
#endif
        }

        /// <summary>
        /// 권한 요청 (콜백 기반)
        /// </summary>
        public static void RequestPermission(Action<bool> onResult)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string permission = GetApiLevel() >= 33
                ? "android.permission.READ_MEDIA_AUDIO"
                : "android.permission.READ_EXTERNAL_STORAGE";

            if (Permission.HasUserAuthorizedPermission(permission))
            {
                onResult?.Invoke(true);
                return;
            }

            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (perm) => onResult?.Invoke(true);
            callbacks.PermissionDenied += (perm) => onResult?.Invoke(false);
            callbacks.PermissionDeniedAndDontAskAgain += (perm) => onResult?.Invoke(false);
            Permission.RequestUserPermission(permission, callbacks);
#else
            onResult?.Invoke(true);
#endif
        }

        /// <summary>
        /// 음악 파일 스캔 (MediaStore + 녹음 필터링)
        /// </summary>
        public static List<ScannedMusic> ScanMusicFiles()
        {
            var results = new List<ScannedMusic>();

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                results = QueryMediaStore();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"[AndroidMusicScanner] MediaStore 쿼리 실패: {e.Message}");
#endif
            }
#else
            results = ScanFileSystemFallback();
#endif

            results = FilterOutRecordings(results);
#if UNITY_EDITOR
            Debug.Log($"[AndroidMusicScanner] 스캔 완료: {results.Count}곡 (녹음 제외)");
#endif
            return results;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static List<ScannedMusic> QueryMediaStore()
        {
            var list = new List<ScannedMusic>();

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var contentResolver = activity.Call<AndroidJavaObject>("getContentResolver"))
            using (var mediaStoreAudio = new AndroidJavaClass("android.provider.MediaStore$Audio$Media"))
            {
                var uri = mediaStoreAudio.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");

                string[] projection = {
                    "_display_name",
                    "title",
                    "artist",
                    "duration",
                    "_data",
                    "_size"
                };

                // IS_MUSIC=1 -> 음성 녹음 자동 제외
                string selection = "is_music = 1";
                string sortOrder = "title ASC";

                using (var cursor = contentResolver.Call<AndroidJavaObject>(
                    "query", uri, projection, selection, null, sortOrder))
                {
                    if (cursor == null)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning("[AndroidMusicScanner] Cursor is null");
#endif
                        return list;
                    }

                    int idxDisplayName = cursor.Call<int>("getColumnIndex", "_display_name");
                    int idxTitle = cursor.Call<int>("getColumnIndex", "title");
                    int idxArtist = cursor.Call<int>("getColumnIndex", "artist");
                    int idxDuration = cursor.Call<int>("getColumnIndex", "duration");
                    int idxData = cursor.Call<int>("getColumnIndex", "_data");
                    int idxSize = cursor.Call<int>("getColumnIndex", "_size");

                    while (cursor.Call<bool>("moveToNext"))
                    {
                        try
                        {
                            var music = new ScannedMusic();

                            music.Title = idxTitle >= 0
                                ? cursor.Call<string>("getString", idxTitle) ?? ""
                                : "";
                            music.Artist = idxArtist >= 0
                                ? cursor.Call<string>("getString", idxArtist) ?? "Unknown"
                                : "Unknown";
                            music.DurationMs = idxDuration >= 0
                                ? cursor.Call<long>("getLong", idxDuration)
                                : 0;
                            music.FilePath = idxData >= 0
                                ? cursor.Call<string>("getString", idxData) ?? ""
                                : "";
                            music.FileSize = idxSize >= 0
                                ? cursor.Call<long>("getLong", idxSize)
                                : 0;

                            // 타이틀이 없으면 파일명에서 추출
                            if (string.IsNullOrEmpty(music.Title) && idxDisplayName >= 0)
                            {
                                string displayName = cursor.Call<string>("getString", idxDisplayName) ?? "";
                                music.Title = System.IO.Path.GetFileNameWithoutExtension(displayName);
                            }

                            if (!string.IsNullOrEmpty(music.FilePath))
                                list.Add(music);
                        }
                        catch (Exception e)
                        {
#if UNITY_EDITOR
                            Debug.LogWarning($"[AndroidMusicScanner] Row 읽기 실패: {e.Message}");
#endif
                        }
                    }

                    cursor.Call("close");
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[AndroidMusicScanner] MediaStore 결과: {list.Count}개");
#endif
            return list;
        }

        private static int GetApiLevel()
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }
#endif

        /// <summary>
        /// 에디터/PC 폴백: 파일 시스템 직접 스캔
        /// </summary>
        private static List<ScannedMusic> ScanFileSystemFallback()
        {
            var list = new List<ScannedMusic>();
            var searchPaths = new List<string>();

            searchPaths.Add(Application.streamingAssetsPath);

            string musicDir = System.IO.Path.Combine(Application.persistentDataPath, "Music");
            searchPaths.Add(musicDir);

            string userMusic = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (!string.IsNullOrEmpty(userMusic)) searchPaths.Add(userMusic);

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
                searchPaths.Add(System.IO.Path.Combine(userProfile, "Downloads"));

            string[] extensions = { "*.mp3", "*.wav", "*.ogg", "*.m4a", "*.flac" };

            foreach (var basePath in searchPaths)
            {
                if (string.IsNullOrEmpty(basePath) || !System.IO.Directory.Exists(basePath))
                    continue;

                foreach (var ext in extensions)
                {
                    try
                    {
                        var files = System.IO.Directory.GetFiles(basePath, ext);
                        foreach (var filePath in files)
                        {
                            var info = new System.IO.FileInfo(filePath);
                            list.Add(new ScannedMusic
                            {
                                Title = System.IO.Path.GetFileNameWithoutExtension(filePath)
                                    .Replace("_", " "),
                                Artist = "Unknown",
                                DurationMs = 0,
                                FilePath = filePath,
                                FileSize = info.Length
                            });
                        }
                    }
                    catch (Exception) { }
                }
            }

            return list;
        }

        /// <summary>
        /// 음성 녹음 파일 필터링 (다중 조건)
        /// </summary>
        private static List<ScannedMusic> FilterOutRecordings(List<ScannedMusic> input)
        {
            var filtered = new List<ScannedMusic>();

            foreach (var music in input)
            {
                // 1. 파일 크기 필터 (500KB 미만 제외)
                if (music.FileSize > 0 && music.FileSize < MIN_FILE_SIZE_BYTES)
                    continue;

                // 2. 재생 시간 필터 (30초 미만 또는 60분 초과 제외)
                if (music.DurationMs > 0)
                {
                    if (music.DurationMs < MIN_DURATION_MS) continue;
                    if (music.DurationMs > MAX_DURATION_MS) continue;
                }

                // 3. 경로 기반 제외 (녹음 관련 폴더)
                if (!string.IsNullOrEmpty(music.FilePath))
                {
                    string lowerPath = music.FilePath.ToLower();
                    bool isRecording = false;

                    foreach (var keyword in RECORDING_PATH_KEYWORDS)
                    {
                        if (lowerPath.Contains(keyword))
                        {
                            isRecording = true;
                            break;
                        }
                    }
                    if (isRecording) continue;
                }

                // 4. 지원 포맷 확인
                if (!string.IsNullOrEmpty(music.FilePath))
                {
                    string ext = System.IO.Path.GetExtension(music.FilePath).ToLower();
                    if (ext != ".mp3" && ext != ".wav" && ext != ".ogg" &&
                        ext != ".m4a" && ext != ".flac")
                        continue;
                }

                filtered.Add(music);
            }

            return filtered;
        }

        /// <summary>
        /// ScannedMusic -> SongRecord 변환
        /// </summary>
        public static SongRecord ToSongRecord(ScannedMusic music)
        {
            return new SongRecord
            {
                Title = !string.IsNullOrEmpty(music.Title)
                    ? music.Title
                    : System.IO.Path.GetFileNameWithoutExtension(music.FilePath ?? "Unknown"),
                Artist = music.Artist ?? "Unknown",
                Genre = "Local",
                Mood = "",
                BPM = 0,
                DifficultyLevel = 5,
                Duration = music.DurationMs / 1000f,
                AudioFileName = "ext:" + music.FilePath,
            };
        }
    }
}
