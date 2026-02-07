using System;
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
    }
}
