using System;
using AIBeat.Data;

namespace AIBeat.Network
{
    /// <summary>
    /// 곡 생성기 공통 인터페이스
    /// FakeSongGenerator와 AIApiClient 모두 이 인터페이스를 구현
    /// </summary>
    public interface ISongGenerator
    {
        event Action<float> OnGenerationProgress;
        event Action<SongData> OnGenerationComplete;
        event Action<string> OnGenerationError;

        void Generate(PromptOptions options);
    }
}
