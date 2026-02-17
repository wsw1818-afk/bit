using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using AIBeat.UI;

namespace AIBeat.Gameplay
{
    /// <summary>
    /// 판정 이펙트 컨트롤러 — UI Image 기반 (Screen Space Overlay Canvas 호환)
    /// SpriteRenderer는 Screen Space Overlay 캔버스 뒤에 가려지므로
    /// Image 컴포넌트를 사용하여 Canvas 내부에서 렌더링합니다.
    /// </summary>
    public class JudgementEffectController : MonoBehaviour
    {
        private Image effectImage;

        private Dictionary<string, Sprite[]> animationFrames = new Dictionary<string, Sprite[]>();
        private Coroutine activeRoutine;

        private void Awake()
        {
            // RectTransform 보장
            if (GetComponent<RectTransform>() == null)
                gameObject.AddComponent<RectTransform>();

            // Image 컴포넌트 설정
            effectImage = GetComponent<Image>();
            if (effectImage == null)
                effectImage = gameObject.AddComponent<Image>();
            effectImage.raycastTarget = false;

            // RectTransform 크기 설정 (판정 이펙트 크기)
            var rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 200f);

            // Pre-load or Generate frames
            LoadOrGenerateFrames("Perfect");
            LoadOrGenerateFrames("Great");
            LoadOrGenerateFrames("Good");
            LoadOrGenerateFrames("Bad");

            gameObject.SetActive(false);
        }

        private void LoadOrGenerateFrames(string type)
        {
            string path = $"AIBeat_Design/Judgements/{type}_Sheet";
            Texture2D tex = Resources.Load<Texture2D>(path);

            // Fallback: Generate if missing
            if (tex == null)
            {
                tex = ProceduralImageGenerator.CreateJudgementSheet(type);
            }

            if (tex != null)
            {
                // Slice 4x4
                int frameSize = tex.width / 4; // 128
                Sprite[] frames = new Sprite[16];
                for (int i = 0; i < 16; i++)
                {
                    int x = (i % 4) * frameSize;
                    int y = tex.height - ((i / 4) + 1) * frameSize; // Top to bottom

                    frames[i] = Sprite.Create(tex, new Rect(x, y, frameSize, frameSize), new Vector2(0.5f, 0.5f));
                    frames[i].name = $"{type}_{i}";
                }
                animationFrames[type] = frames;
            }
        }

        /// <summary>
        /// 이펙트 재생 (UI 기반 — 위치는 RectTransform으로 고정)
        /// </summary>
        public void Play(string type)
        {
            if (!animationFrames.ContainsKey(type)) return;

            gameObject.SetActive(true);

            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(AnimateRoutine(animationFrames[type]));
        }

        /// <summary>
        /// 하위 호환용 오버로드 (기존 position 인자 무시)
        /// </summary>
        public void Play(string type, Vector3 position)
        {
            Play(type);
        }

        private IEnumerator AnimateRoutine(Sprite[] frames)
        {
            float duration = 0.4f; // Total duration
            float frameTime = duration / frames.Length;

            // 시작 시 스케일 팝업 효과
            var rect = GetComponent<RectTransform>();
            rect.localScale = Vector3.one * 1.3f;

            for (int i = 0; i < frames.Length; i++)
            {
                effectImage.sprite = frames[i];

                // 스케일 애니메이션: 1.3 → 1.0 (처음 절반)
                float progress = (float)i / frames.Length;
                float scale = Mathf.Lerp(1.3f, 0.8f, progress);
                rect.localScale = Vector3.one * scale;

                // 페이드아웃 (후반부)
                float alpha = progress < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.5f) * 2f);
                effectImage.color = new Color(1f, 1f, 1f, alpha);

                yield return new WaitForSeconds(frameTime);
            }

            gameObject.SetActive(false);
        }
    }
}
