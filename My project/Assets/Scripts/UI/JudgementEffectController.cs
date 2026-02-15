using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AIBeat.UI;

namespace AIBeat.Gameplay
{
    public class JudgementEffectController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        
        private Dictionary<string, Sprite[]> animationFrames = new Dictionary<string, Sprite[]>();
        private Coroutine activeRoutine;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            
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

        public void Play(string type, Vector3 position)
        {
            if (!animationFrames.ContainsKey(type)) return;

            transform.position = position;
            gameObject.SetActive(true);
            
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(AnimateRoutine(animationFrames[type]));
        }

        private IEnumerator AnimateRoutine(Sprite[] frames)
        {
            float duration = 0.4f; // Total duration
            float frameTime = duration / frames.Length;

            for (int i = 0; i < frames.Length; i++)
            {
                spriteRenderer.sprite = frames[i];
                yield return new WaitForSeconds(frameTime);
            }

            gameObject.SetActive(false);
        }
    }
}
