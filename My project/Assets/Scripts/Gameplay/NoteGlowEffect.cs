using UnityEngine;
using System.Collections;

namespace AIBeat.Gameplay
{
    /// <summary>
    /// 노트에 글로우 외곽선 + 펄스 애니메이션을 추가하는 컴포넌트
    /// 노트 뒤에 살짝 큰 반투명 Quad를 배치하여 발광 효과 연출
    /// </summary>
    public class NoteGlowEffect : MonoBehaviour
    {
        private GameObject glowQuad;
        private MeshRenderer glowRenderer;
        private Material glowMaterial;
        private Coroutine pulseCoroutine;
        private Color baseGlowColor;
        private float pulseSpeed;
        private bool initialized;

        // 글로우 크기 배율 (노트보다 살짝 큼)
        private const float GLOW_SCALE_MULT = 1.5f;
        private const float GLOW_ALPHA = 0.45f;

        public void Initialize(Color noteColor)
        {
            if (initialized)
            {
                // 이미 초기화된 경우 색상만 갱신
                UpdateGlowColor(noteColor);
                return;
            }

            baseGlowColor = noteColor;
            baseGlowColor.a = GLOW_ALPHA;
            pulseSpeed = Random.Range(3f, 5f); // 개별 노트마다 약간 다른 속도

            CreateGlowQuad();
            initialized = true;
        }

        private void CreateGlowQuad()
        {
            glowQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glowQuad.name = "NoteGlow";
            glowQuad.transform.SetParent(transform);
            glowQuad.transform.localPosition = new Vector3(0, 0, 0.01f); // 노트 바로 뒤
            glowQuad.transform.localScale = new Vector3(GLOW_SCALE_MULT, GLOW_SCALE_MULT, 1f);
            glowQuad.transform.localRotation = Quaternion.identity;

            // Collider 제거
            var col = glowQuad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            glowRenderer = glowQuad.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            glowMaterial = new Material(shader);

            // 글로우 텍스처 생성 (원형 페이드)
            glowMaterial.mainTexture = CreateGlowTexture(32, 32);
            glowMaterial.color = baseGlowColor;
            glowRenderer.material = glowMaterial;
        }

        /// <summary>
        /// 부드러운 원형 글로우 텍스처
        /// </summary>
        private Texture2D CreateGlowTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float cx = width / 2f;
            float cy = height / 2f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // 부드러운 감쇄
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha; // 더 부드러운 페이드
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        public void UpdateGlowColor(Color noteColor)
        {
            baseGlowColor = noteColor;
            baseGlowColor.a = GLOW_ALPHA;
            if (glowMaterial != null)
                glowMaterial.color = baseGlowColor;
        }

        private void OnEnable()
        {
            if (initialized && glowQuad != null)
                glowQuad.SetActive(true);
            pulseCoroutine = StartCoroutine(PulseCoroutine());
        }

        private void OnDisable()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
            if (glowQuad != null)
                glowQuad.SetActive(false);
        }

        /// <summary>
        /// 펄스 애니메이션: 글로우 크기와 투명도가 미세하게 변화
        /// </summary>
        private IEnumerator PulseCoroutine()
        {
            float time = Random.Range(0f, Mathf.PI * 2f); // 랜덤 시작 위상
            while (true)
            {
                yield return null;
                if (glowQuad == null || glowMaterial == null) continue;

                time += Time.deltaTime * pulseSpeed;
                float pulse = 0.5f + 0.5f * Mathf.Sin(time);

                // 크기 펄스 (1.4~1.6배)
                float scale = GLOW_SCALE_MULT + pulse * 0.2f;
                glowQuad.transform.localScale = new Vector3(scale, scale, 1f);

                // 알파 펄스 (0.3~0.5)
                var c = baseGlowColor;
                c.a = GLOW_ALPHA - 0.15f + pulse * 0.15f;
                glowMaterial.color = c;
            }
        }

        private void OnDestroy()
        {
            if (glowMaterial != null) Destroy(glowMaterial);
            if (glowQuad != null) Destroy(glowQuad);
        }
    }
}
