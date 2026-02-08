using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using AIBeat.Core;

namespace AIBeat.Gameplay
{
    /// <summary>
    /// 터치/키 입력 시 레인 시각적 피드백 (Music/DJ Theme)
    /// + 하단 히트존 (2존), 판정선 글로우, 키 라벨, 스크래치 오버레이
    /// 모바일 최적화: 4 비주얼 레인 (2키 + 2스크래치) + 2 터치 존 히트존
    /// 음악 테마: 이퀄라이저 배경, 턴테이블 스크래치, 비트 반응 판정선
    /// </summary>
    public class LaneVisualFeedback : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float flashDuration = 0.2f;
        [SerializeField] private Color flashColor = new Color(0.4f, 0.8f, 1.0f, 0.85f);
        [SerializeField] private Color idleColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

        private static LaneVisualFeedback instance;
        private static Dictionary<int, MeshRenderer> laneRenderers;
        private static Dictionary<int, Material> laneMaterials;
        private static Dictionary<int, Coroutine> flashCoroutines;

        // 하단 히트존 (2존: Lane 1-2)
        private static Dictionary<int, MeshRenderer> hitZoneRenderers;
        private static Dictionary<int, Material> hitZoneMaterials;

        // 키 라벨 (2존: Lane 1-2) - 음표 스타일
        private static Dictionary<int, TextMeshPro> keyLabels;
        private static Color keyLabelIdleColor = new Color(0.5f, 0.7f, 1f, 0.75f);
        private static Color keyLabelFlashColor = new Color(0.4f, 1f, 1f, 1f);

        // 스크래치 오버레이 라벨 (턴테이블 테마)
        private static Dictionary<int, TextMeshPro> scratchOverlayLabels;
        private static Color scratchIdleColor = new Color(0.7f, 0.3f, 1f, 0.5f);
        private static Color scratchFlashColor = new Color(1f, 0.5f, 1f, 1f);

        // 판정선 글로우
        private static MeshRenderer glowRenderer;
        private static Material glowMaterial;

        // 메모리 관리: 동적 생성한 Texture2D 추적
        private List<Texture2D> managedTextures = new List<Texture2D>();

        // === 히트 이펙트 (판정 Quad + 파티클 풀) ===
        private const int PARTICLE_POOL_SIZE = 40;
        private static GameObject[] particlePool;
        private static Material[] particleMaterials;
        private static ParticleData[] particleDataArray;
        private static int particleIndex;

        private static GameObject[] judgeEffectQuads;  // 4개 (레인당 1개)
        private static Material[] judgeEffectMaterials;
        private static Coroutine[] judgeEffectCoroutines;

        private static float cachedJudgeY;
        private static float cachedStartX;
        private static float cachedLaneWidth;

        private struct ParticleData
        {
            public bool Active;
            public Vector3 Velocity;
            public float Lifetime;
            public float Elapsed;
            public Color StartColor;
        }

        // 판정별 이펙트 색상 (음악 이퀄라이저 그라데이션)
        private static readonly Color perfectEffectColor = new Color(1f, 0.85f, 0.1f, 1f);  // 골드 (최고 비트)
        private static readonly Color greatEffectColor = new Color(0.2f, 0.9f, 1f, 1f);     // 네온 시안
        private static readonly Color goodEffectColor = new Color(0.4f, 1f, 0.6f, 1f);      // 민트 그린
        private static readonly Color badEffectColor = new Color(0.8f, 0.3f, 0.6f, 1f);     // 핑크

        private const int LANE_COUNT = 4;          // 비주얼 레인: 0=ScratchL, 1=Key1, 2=Key2, 3=ScratchR
        private const int TOUCH_ZONE_COUNT = 2;    // 터치 존 (입력용 히트존/라벨): Key1, Key2

        // 4레인 색상 (음악 테마: 턴테이블+이퀄라이저)
        private static readonly Color[] laneColors = new Color[]
        {
            new Color(0.6f, 0.1f, 0.8f, 0.28f),     // Lane 0: Scratch L (바이올렛 - 턴테이블)
            new Color(0.0f, 0.5f, 1.0f, 0.24f),     // Lane 1: Key 1 (네온 블루 - 이퀄라이저)
            new Color(0.0f, 0.8f, 0.6f, 0.22f),     // Lane 2: Key 2 (시안 그린 - 이퀄라이저)
            new Color(0.6f, 0.1f, 0.8f, 0.28f),     // Lane 3: Scratch R (바이올렛 - 턴테이블)
        };

        // 모바일 터치 존 키 라벨 (2개: Lane 1-2)
        private static readonly string[] touchZoneKeyNames = { "1", "2" };

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
            scratchOverlayLabels = new Dictionary<int, TextMeshPro>();
        }

        private void Start()
        {
            InitializeLanes();
            InitializeHitEffects();
            StartCoroutine(ParticleUpdateCoroutine());
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

            // 히트 이펙트용 좌표 캐시
            cachedJudgeY = judgeY;
            cachedStartX = startX;
            cachedLaneWidth = laneWidth;

            // === 1. 레인 배경 (4개 비주얼 레인) ===
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

            // === 3. 하단 히트존 (2존: Lane 1-2만) ===
            CreateHitZones(judgeY, startX, laneWidth);

            // === 4. 키 라벨 (2존: Lane 1-2만) ===
            CreateKeyLabels(judgeY, startX, laneWidth);

            // === 5. 스크래치 오버레이 (가장자리 존) ===
            CreateScratchOverlays(judgeY, startX, laneWidth);

#if UNITY_EDITOR
            Debug.Log($"[LaneVisualFeedback] Initialized: {LANE_COUNT} visual lanes + {TOUCH_ZONE_COUNT} touch zones + glow + scratch overlays");
#endif
        }

        /// <summary>
        /// 판정선 글로우 효과 (음파 스타일 - 중앙 밝고 위아래 페이드)
        /// </summary>
        private void CreateJudgementLineGlow(float judgeY, float startX, float laneWidth)
        {
            float glowWidth = LANE_COUNT * laneWidth + 0.5f;
            float glowHeight = 1.2f;

            var glowGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glowGo.name = "JudgementLineGlow";
            glowGo.transform.SetParent(transform);
            glowGo.transform.position = new Vector3(0, judgeY, -0.5f);
            glowGo.transform.localScale = new Vector3(glowWidth, glowHeight, 1f);

            var collider = glowGo.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var glowTex = CreateGlowTexture(4, 64);
            managedTextures.Add(glowTex);
            var glowShader = Shader.Find("Sprites/Default");
            if (glowShader == null) glowShader = Shader.Find("Unlit/Transparent");
            glowMaterial = new Material(glowShader);
            glowMaterial.mainTexture = glowTex;
            glowMaterial.color = new Color(0.3f, 0.6f, 1f, 0.65f); // 블루-시안 음파 글로우

            glowRenderer = glowGo.GetComponent<MeshRenderer>();
            glowRenderer.material = glowMaterial;

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
                float t = (float)y / (height - 1);
                float dist = Mathf.Abs(t - 0.5f) * 2f;
                float alpha = Mathf.Exp(-dist * dist * 4f);

                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 글로우 pulse 애니메이션 (비트에 맞춘 색상 시프트)
        /// </summary>
        private IEnumerator GlowPulseCoroutine()
        {
            float time = 0f;
            while (true)
            {
                yield return null;
                time += Time.deltaTime;
                // 비트감 있는 색상 변화 (블루↔시안↔퍼플)
                float pulse = 0.65f + 0.12f * Mathf.Sin(time * 3f);
                float r = 0.2f + 0.15f * Mathf.Sin(time * 1.3f);
                float g = 0.5f + 0.2f * Mathf.Sin(time * 2.1f);
                if (glowMaterial != null)
                    glowMaterial.color = new Color(r, g, 1f, pulse);
            }
        }

        /// <summary>
        /// 하단 히트존 생성 (2존: Lane 1-2 위치에만)
        /// </summary>
        private void CreateHitZones(float judgeY, float startX, float laneWidth)
        {
            float hitZoneHeight = 3.5f;  // 엄지 터치 영역 확대
            float hitZoneTop = judgeY;
            float hitZoneCenterY = hitZoneTop - hitZoneHeight / 2f;

            for (int i = 0; i < TOUCH_ZONE_COUNT; i++)
            {
                int visualLane = i + 1; // 존 0-4 → Lane 1-5
                Color zoneColor = laneColors[visualLane];

                float widthMult = 1.0f;

                var hitZoneTex = CreateHitZoneTextureForLane(8, 64, zoneColor);
                managedTextures.Add(hitZoneTex);

                var hzGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                hzGo.name = $"HitZone_{visualLane}";
                hzGo.transform.SetParent(transform);
                hzGo.transform.position = new Vector3(startX + visualLane * laneWidth, hitZoneCenterY, 0.8f);
                hzGo.transform.localScale = new Vector3(laneWidth * widthMult, hitZoneHeight, 1f);

                var collider = hzGo.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                var renderer = hzGo.GetComponent<MeshRenderer>();
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Unlit/Transparent");
                var mat = new Material(shader);
                mat.mainTexture = hitZoneTex;
                mat.color = Color.white;
                renderer.material = mat;

                hitZoneRenderers[visualLane] = renderer;
                hitZoneMaterials[visualLane] = mat;
            }
        }

        /// <summary>
        /// 히트존 텍스처: 색상+알파를 텍스처에 직접 베이크
        /// 상단이 밝고 하단이 어두운 그라디언트
        /// </summary>
        private Texture2D CreateHitZoneTextureForLane(int width, int height, Color laneColor)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                float gradientAlpha = t * t;
                float finalAlpha = gradientAlpha * laneColor.a;

                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, new Color(laneColor.r, laneColor.g, laneColor.b, finalAlpha));
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 키 라벨 생성 (2존: Lane 1-2 위치에만)
        /// </summary>
        private void CreateKeyLabels(float judgeY, float startX, float laneWidth)
        {
            float labelY = judgeY - 2.5f;  // 히트존 확대에 맞춰 하향

            for (int i = 0; i < TOUCH_ZONE_COUNT; i++)
            {
                int visualLane = i + 1; // 존 0-4 → Lane 1-5

                var labelGo = new GameObject($"KeyLabel_{visualLane}");
                labelGo.transform.SetParent(transform);
                labelGo.transform.position = new Vector3(startX + visualLane * laneWidth, labelY, -1f);

                var tmp = labelGo.AddComponent<TextMeshPro>();
                tmp.text = touchZoneKeyNames[i];
                tmp.fontSize = 5f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = keyLabelIdleColor;
                tmp.fontStyle = FontStyles.Bold;
                var korFont = KoreanFontManager.KoreanFont;
                if (korFont != null) tmp.font = korFont;
                tmp.outlineWidth = 0.2f;
                tmp.outlineColor = new Color32(0, 0, 0, 180);

                var rect = tmp.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1f, 1f);

                keyLabels[visualLane] = tmp;
            }
        }

        /// <summary>
        /// 스크래치 오버레이 생성 (가장자리 존: Lane 0, Lane 3 위치)
        /// 세로 스와이프 아이콘 표시
        /// </summary>
        private void CreateScratchOverlays(float judgeY, float startX, float laneWidth)
        {
            // 좌측 스크래치 오버레이 (Lane 0 위치) - 턴테이블 디스크
            CreateSingleScratchOverlay(judgeY, startX, laneWidth, 0, "<< DJ");
            // 우측 스크래치 오버레이 (Lane 3 위치) - 턴테이블 디스크
            CreateSingleScratchOverlay(judgeY, startX, laneWidth, 3, "DJ >>");
        }

        private void CreateSingleScratchOverlay(float judgeY, float startX, float laneWidth, int laneIndex, string text)
        {
            float overlayY = judgeY - 4.2f; // 히트존 하단 부근 (확대 대응)

            var overlayGo = new GameObject($"ScratchOverlay_{laneIndex}");
            overlayGo.transform.SetParent(transform);
            overlayGo.transform.position = new Vector3(startX + laneIndex * laneWidth, overlayY, -1f);

            var tmp = overlayGo.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 3.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = scratchIdleColor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.15f;
            tmp.outlineColor = new Color32(0, 0, 0, 150);
            var korFont = KoreanFontManager.KoreanFont;
            if (korFont != null) tmp.font = korFont;

            var rect = tmp.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1.2f, 0.6f);

            scratchOverlayLabels[laneIndex] = tmp;
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

            // 히트존 플래시 (Lane 1-2에만 존재)
            Color hzFlash = new Color(1.5f, 1.5f, 1.5f, 1.2f);
            Color hzIdle = Color.white;
            Material hzMat = null;
            if (hitZoneMaterials.TryGetValue(laneIndex, out hzMat))
            {
                hzMat.color = hzFlash;
            }

            // 키 라벨 플래시 (Lane 1-2에만 존재)
            TextMeshPro label = null;
            if (keyLabels.TryGetValue(laneIndex, out label))
            {
                label.color = keyLabelFlashColor;
            }

            // 스크래치 오버레이 플래시 (Lane 0/3에만 존재)
            TextMeshPro scratchLabel = null;
            if (scratchOverlayLabels.TryGetValue(laneIndex, out scratchLabel))
            {
                scratchLabel.color = scratchFlashColor;
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

                if (scratchLabel != null)
                {
                    scratchLabel.color = Color.Lerp(scratchFlashColor, scratchIdleColor, t);
                }

                yield return null;
            }

            mat.color = idleColor;
            if (hzMat != null) hzMat.color = hzIdle;
            if (label != null) label.color = keyLabelIdleColor;
            if (scratchLabel != null) scratchLabel.color = scratchIdleColor;
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
                mat.color = new Color(0.3f, 0.4f, 1.0f, 0.45f); // 음파 바이올렛-블루
            }
            else
            {
                mat.color = instance.idleColor;
            }
        }

        // ====================================================================
        // 히트 이펙트 시스템 (판정 Quad 확대 + 파티클 풀)
        // ====================================================================

        /// <summary>
        /// 히트 이펙트 초기화: 판정 Quad 7개 + 파티클 Quad 40개 사전 할당
        /// </summary>
        private void InitializeHitEffects()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");

            // === 판정 Quad (레인당 1개, 판정선 위치에서 확대+페이드) ===
            judgeEffectQuads = new GameObject[LANE_COUNT];
            judgeEffectMaterials = new Material[LANE_COUNT];
            judgeEffectCoroutines = new Coroutine[LANE_COUNT];

            for (int i = 0; i < LANE_COUNT; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"JudgeEffect_{i}";
                go.transform.SetParent(transform);
                go.transform.position = new Vector3(cachedStartX + i * cachedLaneWidth, cachedJudgeY, -0.3f);
                go.transform.localScale = Vector3.zero; // 초기 비활성

                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var mat = new Material(shader);
                mat.color = new Color(1f, 1f, 1f, 0f);
                go.GetComponent<MeshRenderer>().material = mat;

                judgeEffectQuads[i] = go;
                judgeEffectMaterials[i] = mat;
            }

            // === 파티클 Quad 풀 ===
            particlePool = new GameObject[PARTICLE_POOL_SIZE];
            particleMaterials = new Material[PARTICLE_POOL_SIZE];
            particleDataArray = new ParticleData[PARTICLE_POOL_SIZE];
            particleIndex = 0;

            for (int i = 0; i < PARTICLE_POOL_SIZE; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"HitParticle_{i}";
                go.transform.SetParent(transform);
                go.transform.localScale = Vector3.zero; // 초기 비활성

                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var mat = new Material(shader);
                mat.color = new Color(1f, 1f, 1f, 0f);
                go.GetComponent<MeshRenderer>().material = mat;

                particlePool[i] = go;
                particleMaterials[i] = mat;
                particleDataArray[i] = new ParticleData { Active = false };
            }
        }

        /// <summary>
        /// 판정 이펙트 외부 호출용 (GameplayController에서 호출)
        /// </summary>
        public static void PlayJudgementEffect(int laneIndex, JudgementResult result)
        {
            if (instance == null || laneIndex < 0 || laneIndex >= LANE_COUNT) return;
            if (result == JudgementResult.Miss) return; // Miss는 이펙트 없음

            instance.PlayJudgeEffectInternal(laneIndex, result);
        }

        private void PlayJudgeEffectInternal(int laneIndex, JudgementResult result)
        {
            Color effectColor = result switch
            {
                JudgementResult.Perfect => perfectEffectColor,
                JudgementResult.Great => greatEffectColor,
                JudgementResult.Good => goodEffectColor,
                JudgementResult.Bad => badEffectColor,
                _ => Color.clear
            };

            int particleCount = result switch
            {
                JudgementResult.Perfect => 8,
                JudgementResult.Great => 6,
                JudgementResult.Good => 4,
                JudgementResult.Bad => 2,
                _ => 0
            };

            float particleSpeed = result switch
            {
                JudgementResult.Perfect => 5f,
                JudgementResult.Great => 4f,
                JudgementResult.Good => 3f,
                _ => 2f
            };

            float particleSize = result switch
            {
                JudgementResult.Perfect => 0.12f,
                JudgementResult.Great => 0.10f,
                JudgementResult.Good => 0.08f,
                _ => 0.06f
            };

            // 1. 판정 Quad 확대 + 페이드
            if (judgeEffectCoroutines[laneIndex] != null)
                StopCoroutine(judgeEffectCoroutines[laneIndex]);

            judgeEffectCoroutines[laneIndex] = StartCoroutine(JudgeEffectCoroutine(laneIndex, effectColor));

            // 2. 파티클 발사
            Vector3 emitPos = new Vector3(cachedStartX + laneIndex * cachedLaneWidth, cachedJudgeY, -0.4f);
            EmitParticles(emitPos, particleCount, effectColor, particleSpeed, particleSize);

            // 3. 판정선 글로우 강화 (Perfect/Great만)
            if (result == JudgementResult.Perfect || result == JudgementResult.Great)
            {
                float glowAlpha = result == JudgementResult.Perfect ? 1.0f : 0.9f;
                StartCoroutine(GlowBoostCoroutine(glowAlpha));
            }
        }

        /// <summary>
        /// 판정 Quad: 확대 + 페이드아웃 (0.3초)
        /// </summary>
        private IEnumerator JudgeEffectCoroutine(int laneIndex, Color color)
        {
            var go = judgeEffectQuads[laneIndex];
            var mat = judgeEffectMaterials[laneIndex];

            go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            mat.color = color;

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 스케일: 0.8 → 2.0 (EaseOut)
                float scale = Mathf.Lerp(0.8f, 2.0f, t * (2f - t));
                go.transform.localScale = new Vector3(scale, scale, 1f);

                // 알파: 1.0 → 0.0 (빠른 페이드)
                float alpha = 1f - (t * t);
                mat.color = new Color(color.r, color.g, color.b, alpha);

                yield return null;
            }

            go.transform.localScale = Vector3.zero;
            mat.color = new Color(color.r, color.g, color.b, 0f);
            judgeEffectCoroutines[laneIndex] = null;
        }

        /// <summary>
        /// 파티클 발사: 부채꼴 상향 확산
        /// </summary>
        private void EmitParticles(Vector3 pos, int count, Color color, float speed, float size)
        {
            for (int i = 0; i < count; i++)
            {
                int idx = particleIndex % PARTICLE_POOL_SIZE;
                particleIndex++;

                var go = particlePool[idx];
                var mat = particleMaterials[idx];

                go.transform.position = pos;
                go.transform.localScale = new Vector3(size, size, 1f);
                mat.color = color;

                // 부채꼴 방향: -70° ~ +70° (상향 편향)
                float angle = Random.Range(-70f, 70f) * Mathf.Deg2Rad;
                float upBias = Random.Range(0.5f, 1.0f);
                Vector3 velocity = new Vector3(Mathf.Sin(angle) * speed, Mathf.Cos(angle) * speed * upBias, 0f);

                float lifetime = Random.Range(0.3f, 0.5f);

                particleDataArray[idx] = new ParticleData
                {
                    Active = true,
                    Velocity = velocity,
                    Lifetime = lifetime,
                    Elapsed = 0f,
                    StartColor = color
                };
            }
        }

        /// <summary>
        /// 파티클 업데이트 코루틴 (매 프레임)
        /// </summary>
        private IEnumerator ParticleUpdateCoroutine()
        {
            while (true)
            {
                yield return null;
                float dt = Time.deltaTime;

                for (int i = 0; i < PARTICLE_POOL_SIZE; i++)
                {
                    var data = particleDataArray[i];
                    if (!data.Active) continue;

                    data.Elapsed += dt;

                    if (data.Elapsed >= data.Lifetime)
                    {
                        data.Active = false;
                        particleDataArray[i] = data;
                        particlePool[i].transform.localScale = Vector3.zero;
                        particleMaterials[i].color = new Color(0, 0, 0, 0);
                        continue;
                    }

                    float t = data.Elapsed / data.Lifetime;

                    // 중력 적용
                    var vel = data.Velocity;
                    vel.y -= 3.0f * dt;
                    data.Velocity = vel;

                    // struct를 배열에 다시 기록
                    particleDataArray[i] = data;

                    // 위치 업데이트
                    var pos = particlePool[i].transform.position;
                    pos += vel * dt;
                    particlePool[i].transform.position = pos;

                    // 회전
                    var rot = particlePool[i].transform.eulerAngles;
                    rot.z += 360f * dt;
                    particlePool[i].transform.eulerAngles = rot;

                    // 알파 감소
                    float alpha = 1f - t;
                    particleMaterials[i].color = new Color(
                        data.StartColor.r, data.StartColor.g, data.StartColor.b, alpha);

                    // 크기 축소
                    float scaleT = 1f - (t * 0.5f);
                    float shrink = scaleT * scaleT;
                    var s = particlePool[i].transform.localScale;
                    particlePool[i].transform.localScale = new Vector3(s.x * shrink, s.y * shrink, 1f);
                }
            }
        }

        /// <summary>
        /// 판정선 글로우 일시 강화
        /// </summary>
        private IEnumerator GlowBoostCoroutine(float targetAlpha)
        {
            if (glowMaterial == null) yield break;

            float duration = 0.25f;
            float elapsed = 0f;
            float baseAlpha = 0.65f;

            // 비트 히트 시 밝은 화이트-시안 플래시
            glowMaterial.color = new Color(0.5f, 0.8f, 1f, targetAlpha);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float alpha = Mathf.Lerp(targetAlpha, baseAlpha, t);
                float r = Mathf.Lerp(0.5f, 0.2f, t);
                float g = Mathf.Lerp(0.8f, 0.5f, t);
                glowMaterial.color = new Color(r, g, 1f, alpha);
                yield return null;
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();

            // Material 정리
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

            // 히트 이펙트 Material 정리
            if (judgeEffectMaterials != null)
            {
                foreach (var mat in judgeEffectMaterials)
                {
                    if (mat != null) Destroy(mat);
                }
            }
            if (particleMaterials != null)
            {
                foreach (var mat in particleMaterials)
                {
                    if (mat != null) Destroy(mat);
                }
            }

            // Texture2D 정리 (메모리 누수 방지)
            if (managedTextures != null)
            {
                foreach (var tex in managedTextures)
                {
                    if (tex != null) Destroy(tex);
                }
                managedTextures.Clear();
            }

            // static 필드 정리 (에디터 Play Mode 재진입 대응)
            if (instance == this)
            {
                instance = null;
                laneRenderers = null;
                laneMaterials = null;
                flashCoroutines = null;
                hitZoneRenderers = null;
                hitZoneMaterials = null;
                keyLabels = null;
                scratchOverlayLabels = null;
                glowRenderer = null;
                glowMaterial = null;
                judgeEffectQuads = null;
                judgeEffectMaterials = null;
                judgeEffectCoroutines = null;
                particlePool = null;
                particleMaterials = null;
                particleDataArray = null;
            }
        }
    }
}
