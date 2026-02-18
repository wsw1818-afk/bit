using UnityEngine;
using System.Collections;

namespace AIBeat.Audio
{
    /// <summary>
    /// 오디오 버퍼링 관리
    /// 오디오 로딩 중 끊김 방지 및 프리로드 지원
    /// </summary>
    public class AudioBuffer : MonoBehaviour
    {
        [Header("Buffer Settings")]
        [SerializeField] private float preloadTimeSeconds = 2f;
        [SerializeField] private int prioritySampleRate = 44100;

        /// <summary>
        /// AudioClip이 완전히 로드되었는지 확인
        /// 스트리밍 클립의 경우 loadState 체크
        /// </summary>
        public static bool IsClipReady(AudioClip clip)
        {
            if (clip == null) return false;
            return clip.loadState == AudioDataLoadState.Loaded;
        }

        /// <summary>
        /// AudioClip을 메모리에 프리로드 (스트리밍 클립 대비)
        /// </summary>
        public static void PreloadClip(AudioClip clip)
        {
            if (clip == null) return;
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
            }
        }

        /// <summary>
        /// AudioClip 로드 완료까지 대기하는 코루틴
        /// </summary>
        public static IEnumerator WaitForClipReady(AudioClip clip, float timeout = 10f)
        {
            if (clip == null) yield break;

            PreloadClip(clip);

            float elapsed = 0f;
            while (clip.loadState != AudioDataLoadState.Loaded && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogWarning($"[AudioBuffer] Clip '{clip.name}' failed to load within {timeout}s");
            }
        }

        /// <summary>
        /// AudioSource에 클립을 설정하고 프리로드
        /// 재생 준비가 완료되면 콜백 호출
        /// </summary>
        public IEnumerator PrepareAudioSource(AudioSource source, AudioClip clip, System.Action onReady = null)
        {
            if (source == null || clip == null)
            {
                onReady?.Invoke();
                yield break;
            }

            source.clip = clip;
            yield return WaitForClipReady(clip);

            // 버퍼 워밍업: 볼륨 0으로 잠시 재생 후 정지
            float originalVolume = source.volume;
            source.volume = 0f;
            source.Play();
            yield return new WaitForSecondsRealtime(0.05f);
            source.Stop();
            source.volume = originalVolume;
            source.time = 0f;

            onReady?.Invoke();
        }

        /// <summary>
        /// 오디오 클립의 샘플 레이트 정보 로그
        /// </summary>
        public static void LogClipInfo(AudioClip clip)
        {
#if UNITY_EDITOR
            if (clip == null)
            {
                Debug.Log("[AudioBuffer] Clip is null");
                return;
            }
            Debug.Log($"[AudioBuffer] Clip: {clip.name}, " +
                      $"Length: {clip.length:F2}s, " +
                      $"Channels: {clip.channels}, " +
                      $"Frequency: {clip.frequency}Hz, " +
                      $"LoadState: {clip.loadState}, " +
                      $"Samples: {clip.samples}");
#endif
        }
    }
}
