using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIBeat.Data
{
    /// <summary>
    /// 곡 전체 데이터 (ScriptableObject)
    /// </summary>
    [CreateAssetMenu(fileName = "NewSong", menuName = "A.I. BEAT/Song Data")]
    public class SongData : ScriptableObject
    {
        public string Id;
        public string Title;
        public string Artist;
        public float BPM;
        public float Duration;
        public string AudioUrl;
        public AudioClip AudioClip;    // 직접 오디오 클립 참조
        public SongSection[] Sections;
        public NoteData[] Notes;
        public int Difficulty;     // 1-10

        // 프롬프트 정보
        public string Genre;
        public string Mood;
        public string CreatorId;
        public DateTime CreatedAt;
    }

    [Serializable]
    public struct SongSection
    {
        public string Name;        // intro, build, drop, outro
        public float StartTime;
        public float EndTime;
        public float DensityMultiplier;  // 노트 밀도 배율

        public SongSection(string name, float start, float end, float density = 1f)
        {
            Name = name;
            StartTime = start;
            EndTime = end;
            DensityMultiplier = density;
        }
    }

    /// <summary>
    /// 프롬프트 옵션
    /// </summary>
    [Serializable]
    public class PromptOptions
    {
        public string Genre;
        public int BPM;
        public string Mood;
        public int Duration;       // 초 단위
        public string Structure;   // "intro-build-drop-outro"

        public static readonly string[] Genres = {
            "EDM", "House", "Cyberpunk", "Synthwave",
            "Chiptune", "Dubstep", "Trance", "Techno"
        };

        public static readonly string[] Moods = {
            "Aggressive", "Chill", "Epic", "Dark",
            "Happy", "Melancholic", "Energetic", "Mysterious"
        };

        public static readonly int[] BPMOptions = {
            80, 100, 120, 140, 160, 180
        };

        // 한국어 표시명 매핑 (내부값은 영어 유지)
        public static readonly Dictionary<string, string> GenreDisplayNames = new Dictionary<string, string>
        {
            {"EDM", "EDM"},
            {"House", "하우스"},
            {"Cyberpunk", "사이버펑크"},
            {"Synthwave", "신스웨이브"},
            {"Chiptune", "칩튠"},
            {"Dubstep", "덥스텝"},
            {"Trance", "트랜스"},
            {"Techno", "테크노"}
        };

        public static readonly Dictionary<string, string> MoodDisplayNames = new Dictionary<string, string>
        {
            {"Aggressive", "공격적"},
            {"Chill", "차분한"},
            {"Epic", "웅장한"},
            {"Dark", "어두운"},
            {"Happy", "신나는"},
            {"Melancholic", "감성적"},
            {"Energetic", "에너제틱"},
            {"Mysterious", "신비로운"}
        };

        /// <summary>
        /// 영어 키에 대한 한국어 표시명 반환 (없으면 원본 반환)
        /// </summary>
        public static string GetGenreDisplay(string key)
        {
            return GenreDisplayNames.TryGetValue(key, out var display) ? display : key;
        }

        public static string GetMoodDisplay(string key)
        {
            return MoodDisplayNames.TryGetValue(key, out var display) ? display : key;
        }
    }
}
