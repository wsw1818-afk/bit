using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace AIBeat.Gameplay
{
    /// <summary>
    /// 터치/키 입력 시 레인 시각적 피드백 (Music Theme)
    /// + 하단 히트존, 판정선 글로우, 키 라벨
    /// </summary>
    public class LaneVisualFeedback : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float flashDuration = 0.2f;
        [SerializeField] private Color flashColor = new Color(1f, 0.84f, 0f, 0.8f); // Gold flash
        [SerializeField] private Color idleColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

        private static LaneVisualFeedback instance;
        private static Dictionary<int, MeshRenderer> laneRenderers;
        private static Dictionary<int, Material> laneMaterials;
        private static Dictionary<int, Coroutine> flashCoroutines;

        // 하단 히트존
        private static Dictionary<int, MeshRenderer> hitZoneRenderers;
        private static Dictionary<int, Material> hitZoneMaterials;

        // 키 라벨
        private static Dictionary<int, TextMeshPro> keyLabels;
        private static Color keyLabelIdleColor = new Color(1f, 0.84f, 0f, 0.5f); // Gold idle
        private static Color keyLabelFlashColor = new Color(1f, 0.95f, 0.6f, 1f); // Bright gold flash

        // 판정선 글로우
        private static MeshRenderer glowRenderer;
        private static Material glowMaterial;

        private const int LANE_COUNT = 4;

        // Music Theme 레인 색상 (어두운 음악 테마 톤) - 4레인 구조
        private static readonly Color[] laneColors = new Color[]
        {
            new Color(1f, 0.55f, 0f, 0.22f),      // Lane 0: Scratch L (어두운 오렌지)
            new Color(0.58f, 0.29f, 0.98f, 0.18f), // Lane 1: Purple Key
            new Color(0f, 0.8f, 0.82f, 0.20f),    // Lane 2: Teal Key
            new Color(1f, 0.55f, 0f, 0.22f),      // Lane 3: Scratch R (어두운 오렌지)
        };

        private static readonly string[] keyNames = { "S", "D", "F", "L" }; // 4레인: Scratch L, Key1, Key2, Scratch R

        public static LaneVisualFeedback Instance => instance;

        private void Awake()
        {
            instance = this;
            laneRenderers = new Dictionary<int, MeshRenderer>();
            laneMaterials = new Dictionary<int, Material>();
            flashCoroutines = new Dictionary<int, Coroutine>();
            hitZoneRenderers = new Dictionary<int, MeshRenderer>();
            hitZoneMaterials = new Dictionary<int, Material>();
            keyLabels = new Dictionary<int, TextMeshPro>();
        }

        private void Start()
        {
            InitializeLanes();
        }

        private void InitializeLanes()
        {
            var judgementLine = GameObject.Find("JudgementLine");
            if (judgementLine == null)
            {
                Debug.LogWarning("[LaneVisualFeedback] JudgementLine not found, skipping initialization");
                return;
            }

            float judgeY = judgementLine.transform.position.y;
            float laneWidth = 1f;
            float laneHeight = 20f;
            float startX = -(LANE_COUNT - 1) * laneWidth / 2f;

            // === 1. 레인 배경 (기존) ===
            for (int i = 0; i < LANE_COUNT; i++)
            {
                var laneGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                laneGo.name = $"LaneBackground_{i}";
                laneGo.transform.SetParent(transform);
                laneGo.transform.position = new Vector3(startX + i * laneWidth, judgeY + laneHeight / 2f, 1.0f);
                laneGo.transform.localScale = new Vector3(laneWidth, laneHeight, 1f);

                var collider = laneGo.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                var renderer = laneGo.GetComponent<MeshRenderer>();
                var laneShader = Shader.Find("Sprites/Default");
                if (laneShader == null) laneShader = Shader.Find("Unlit/Transparent");
                var mat = new Material(laneShader);
                mat.color = idleColor;
                renderer.material = mat;

                laneRenderers[i] = renderer;
                laneMaterials[i] = mat;
                flashCoroutines[i] = null;
            }

            // === 2. 판정선 글로우 ===
            CreateJudgementLineGlow(judgeY, startX, laneWidth);

            // === 3. 하단 히트존 ===
            CreateHitZones(judgeY, startX, laneWidth);

            // === 4. 키 라벨 ===
            CreateKeyLabels(judgeY, startX, laneWidth);

            Debug.Log($"[LaneVisualFeedback] Initialized: {LANE_COUNT} lanes + glow + hitZones + keyLabels");
        }

        /// <summary>
        /// 판정선 글로우 효과 (가로 전체, 중앙 밝고 위아래 페이드)
        /// </summary>
        private void CreateJudgementLineGlow(float judgeY, float startX, float laneWidth)
        {
            float glowWidth = LANE_COUNT * laneWidth + 0.5f;
            float glowHeight = 1.0f; // 얇은 글로우 (노트 가리지 않도록)

            var glowGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glowGo.name = "JudgementLineGlow";
            glowGo.transform.SetParent(transform);
            glowGo.transform.position = new Vector3(0, judgeY, -0.5f); // 노트(z=0)보다 앞, 카메라(-10)에 가까운 쪽
            glowGo.transform.localScale = new Vector3(glowWidth, glowHeight, 1f);

            var collider = glowGo.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // 글로우 텍스처 생성 (중앙 밝고 위아래 페이드, Gold 색상 베이크)
            var glowTex = CreateGlowTexture(4, 64);
            var glowShader = Shader.Find("Sprites/Default");
            if (glowShader == null) glowShader = Shader.Find("Unlit/Transparent");
            glowMaterial = new Material(glowShader);
            glowMaterial.mainTexture = glowTex;
            glowMaterial.color = new Color(1f, 0.84f, 0f, 0.8f); // Gold 글로우 (강한 발광)

            glowRenderer = glowGo.GetComponent<MeshRenderer>();
            glowRenderer.material = glowMaterial;

            // 미세 pulse 코루틴
            StartCoroutine(GlowPulseCoroutine());
        }

        /// <summary>
        /// 글로우 텍스처: 세로 중앙이 밝고 위아래로 페이드
        /// </summary>
        private Texture2D CreateGlowTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1); // 0 (bottom) ~ 1 (top)
                // 가우시안: 중앙(t=0.5)에서 최대
                float dist = Mathf.Abs(t - 0.5f) * 2f; // 0 (center) ~ 1 (edge)
                float alpha = Mathf.Exp(-dist * dist * 4f); // 가우시안 감쇄

                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 글로우 pulse 애니메이션 (밝기 0.5~0.8 반복 - Gold)
        /// </summary>
        private IEnumerator GlowPulseCoroutine()
        {
            float time = 0f;
            while (true)
            {
                yield return null;
                time += Time.deltaTime;
                float alpha = 0.65f + 0.15f * Mathf.Sin(time * 2.5f); // 0.5~0.8
                if (glowMaterial != null)
                    glowMaterial.color = new Color(1f, 0.84f, 0f, alpha);
            }
        }

        /// <summary>
        /// 하단 히트존 생성 (판정선 아래, 레인별 색상)
        /// </summary>
        private void CreateHitZones(float judgeY, float startX, float laneWidth)
        {
            float hitZoneHeight = 2.8f;
            float hitZoneTop = judgeY;
            float hitZoneCenterY = hitZoneTop - hitZoneHeight / 2f;

            for (int i = 0; i < LANE_COUNT; i++)
            {
                // 레인별 전용 텍스처 생성 (색상+알파를 텍스처에 직접 베이크)
                var hitZoneTex = CreateHitZoneTextureForLane(8, 64, laneColors[i]);

                var hzGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                hzGo.name = $"HitZone_{i}";
                hzGo.transform.SetParent(transform);
                hzGo.transform.position = new Vector3(startX + i * laneWidth, hitZoneCenterY, 0.8f);
                hzGo.transform.localScale = new Vector3(laneWidth * 0.92f, hitZoneHeight, 1f);

                var collider = hzGo.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                var renderer = hzGo.GetComponent<MeshRenderer>();
                // Sprites/Default: _Color와 텍스처 알파를 올바르게 곱함
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Unlit/Transparent");
                var mat = new Material(shader);
                mat.mainTexture = hitZoneTex;
                mat.color = Color.white; // 텍스처에 이미 색상 포함
                renderer.material = mat;

                hitZoneRenderers[i] = renderer;
                hitZoneMaterials[i] = mat;
            }
        }

        /// <summary>
        /// 히트존 텍스처: 위에서 밝고 아래로 어두움
        /// </summary>
        /// <summary>
        /// 레인별 히트존 텍스처: 색상+알파를 텍스처에 직접 베이크
        /// 상단이 밝고 하단이 어두운 그라디언트
        /// </summary>
        private Texture2D CreateHitZoneTextureForLane(int width, int height, Color laneColor)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1); // 0 (bottom) ~ 1 (top)
                // 위(top)가 밝고 아래(bottom)가 어두움
                float gradientAlpha = t * t; // 제곱 커브
                float finalAlpha = gradientAlpha * laneColor.a; // 레인 색상 알파 적용

                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, new Color(laneColor.r, laneColor.g, laneColor.b, finalAlpha));
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 키 라벨 생성 (World Space TextMeshPro)
        /// </summary>
        private void CreateKeyLabels(float judgeY, float startX, float laneWidth)
        {
            float labelY = judgeY - 2.0f; // 히트존 중앙 부근

            for (int i = 0; i < LANE_COUNT; i++)
            {
                var labelGo = new GameObject($"KeyLabel_{i}");
                labelGo.transform.SetParent(transform);
                labelGo.transform.position = new Vector3(startX + i * laneWidth, labelY, -1f); // 모든 것 앞에 표시

                var tmp = labelGo.AddComponent<TextMeshPro>();
                tmp.text = keyNames[i];
                tmp.fontSize = 5f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = keyLabelIdleColor;
                tmp.fontStyle = FontStyles.Bold;
                tmp.outlineWidth = 0.2f;
                tmp.outlineColor = new Color32(0, 0, 0, 180);

                // RectTransform 크기 설정
                var rect = tmp.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1f, 1f);

                keyLabels[i] = tmp;
            }
        }

        /// <summary>
        /// 레인 플래시 효과 (외부 호출용)
        /// </summary>
        public static void Flash(int laneIndex)
        {
            if (instance == null || laneIndex < 0 || laneIndex >= LANE_COUNT) return;
            instance.FlashLane(laneIndex);
        }

        private void FlashLane(int laneIndex)
        {
            if (flashCoroutines.TryGetValue(laneIndex, out var existingCoroutine) && existingCoroutine != null)
            {
                StopCoroutine(existingCoroutine);
            }

            flashCoroutines[laneIndex] = StartCoroutine(FlashCoroutine(laneIndex));
        }

        private IEnumerator FlashCoroutine(int laneIndex)
        {
            if (!laneMaterials.TryGetValue(laneIndex, out var mat)) yield break;

            // 레인 배경 플래시
            mat.color = flashColor;

            // 히트존 밝게 (Sprites/Default: color가 텍스처와 곱해짐)
            Color hzFlash = new Color(2f, 2f, 2f, 1.5f); // 밝게 증폭
            Color hzIdle = Color.white; // 기본 = 텍스처 원본
            if (hitZoneMaterials.TryGetValue(laneIndex, out var hzMat))
            {
                hzMat.color = hzFlash;
            }

            // 키 라벨 밝게
            if (keyLabels.TryGetValue(laneIndex, out var label))
            {
                label.color = keyLabelFlashColor;
            }

            // 서서히 원래 색상으로
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashDuration;

                mat.color = Color.Lerp(flashColor, idleColor, t);

                if (hzMat != null)
                {
                    hzMat.color = Color.Lerp(hzFlash, hzIdle, t);
                }

                if (label != null)
                {
                    label.color = Color.Lerp(keyLabelFlashColor, keyLabelIdleColor, t);
                }

                yield return null;
            }

            mat.color = idleColor;
            if (hzMat != null) hzMat.color = hzIdle;
            if (label != null) label.color = keyLabelIdleColor;
            flashCoroutines[laneIndex] = null;
        }

        /// <summary>
        /// 레인 지속 하이라이트 (롱노트 홀드 중)
        /// </summary>
        public static void SetHighlight(int laneIndex, bool highlighted)
        {
            if (instance == null || !laneMaterials.TryGetValue(laneIndex, out var mat)) return;

            if (highlighted)
            {
                mat.color = new Color(0.0f, 0.5f, 1.0f, 0.5f);
            }
            else
            {
                mat.color = instance.idleColor;
            }
        }

        /// <summary>
        /// 판정 이펙트 (GameplayController에서 호출)
        /// 파티클 + 레인 플래시 동시 재생
        /// </summary>
        public static void PlayJudgementEffect(int laneIndex, AIBeat.Core.JudgementResult result)
        {
            if (instance == null || laneIndex < 0 || laneIndex >= LANE_COUNT) return;
            if (result == AIBeat.Core.JudgementResult.Miss) return;
            instance.FlashLane(laneIndex);

            // 히트 파티클 이펙트 (판정선 위치에서 발생)
            var judgementLine = GameObject.Find("JudgementLine");
            if (judgementLine != null)
            {
                float laneWidth = 1f;
                float startX = -(LANE_COUNT - 1) * laneWidth / 2f;
                float x = startX + laneIndex * laneWidth;
                float y = judgementLine.transform.position.y;
                Vector3 hitPos = new Vector3(x, y, -0.3f);
                HitParticleEffect.PlayForJudgement(hitPos, laneIndex, result);

                // 충격파 링 + 라이트빔 이펙트
                Color laneColor = laneColors[laneIndex];
                HitImpactEffect.Play(hitPos, laneColor, result);
            }
        }

        private void OnDestroy()
        {
            if (laneMaterials != null)
            {
                foreach (var mat in laneMaterials.Values)
                {
                    if (mat != null) Destroy(mat);
                }
            }
            if (hitZoneMaterials != null)
            {
                foreach (var mat in hitZoneMaterials.Values)
                {
                    if (mat != null) Destroy(mat);
                }
            }
            if (glowMaterial != null) Destroy(glowMaterial);
        }
    }
}
