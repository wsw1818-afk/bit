using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

namespace AIBeat.Gameplay
{
    /// <summary>
    /// 배경 비주얼 이펙트: 떠다니는 파티클, 비트 반응 플래시, 레인 구분선 글로우
    /// 단조로운 배경에 생동감 추가
    /// </summary>
    public class BackgroundVFX : MonoBehaviour
    {
        private static BackgroundVFX instance;
        public static BackgroundVFX Instance => instance;

        private SortingGroup sortingGroup;

        // 떠다니는 파티클들
        private Transform[] floatingParticles;
        private Material[] particleMaterials;
        private Vector3[] particleVelocities;
        private float[] particlePhases;

        // 레인 구분선
        private GameObject[] laneDividers;
        private Material[] dividerMaterials;

        // 비트 플래시 오버레이
        private GameObject beatFlashOverlay;
        private Material beatFlashMaterial;

        // 배경 그라데이션
        private GameObject bgGradient;
        private Material bgMaterial;

        private const int PARTICLE_COUNT = 30;
        private const int LANE_COUNT = 4; // 실제 노트 레인 수와 일치

        // Music Theme 색상
        private static readonly Color[] themeColors = new Color[]
        {
            new Color(1f, 0.84f, 0f),      // Gold
            new Color(0.58f, 0.29f, 0.98f), // Purple
            new Color(0f, 0.8f, 0.82f),     // Teal
            new Color(1f, 0.55f, 0f),       // Orange
        };

        private void Awake()
        {
            instance = this;

            // SortingGroup을 추가하여 배경 전체가 노트(sortingOrder=100)보다 뒤에 렌더링되도록 함
            sortingGroup = gameObject.AddComponent<SortingGroup>();
            sortingGroup.sortingOrder = -200; // 노트(100)보다 훨씬 낮은 값
        }

        private void Start()
        {
            StartCoroutine(InitializeDelayed());
        }

        private IEnumerator InitializeDelayed()
        {
            yield return null; // 다른 컴포넌트 초기화 대기

            var judgementLine = GameObject.Find("JudgementLine");
            float judgeY = judgementLine != null ? judgementLine.transform.position.y : -5f;

            CreateBackgroundGradient(judgeY);
            CreateFloatingParticles(judgeY);
            CreateLaneDividers(judgeY);
            CreateBeatFlashOverlay(judgeY);

            StartCoroutine(AnimateParticles());
            StartCoroutine(AnimateBackground());

            Debug.Log("[BackgroundVFX] Initialized: gradient + particles + dividers + beatFlash");
        }

        /// <summary>
        /// 배경 그라데이션 (Deep Navy → Purple tint)
        /// </summary>
        private void CreateBackgroundGradient(float judgeY)
        {
            bgGradient = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgGradient.name = "BG_Gradient";
            bgGradient.transform.SetParent(transform);
            bgGradient.transform.position = new Vector3(0, judgeY + 8f, 2f); // 모든 것 뒤
            bgGradient.transform.localScale = new Vector3(12f, 22f, 1f);

            var col = bgGradient.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var tex = CreateGradientTexture(64, 128);
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            bgMaterial = new Material(shader);
            bgMaterial.mainTexture = tex;
            bgMaterial.color = Color.white;

            var bgRenderer = bgGradient.GetComponent<MeshRenderer>();
            bgRenderer.material = bgMaterial;
            bgRenderer.sortingOrder = -100; // 노트(100)보다 훨씬 뒤
        }

        /// <summary>
        /// 배경 그라데이션 텍스처: 네온 사이버펑크 스타일
        /// 중앙 빛나는 레인 + 양쪽 어두운 비네트
        /// </summary>
        private Texture2D CreateGradientTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            // 네온 색상 정의
            Color deepBlack = new Color(0.01f, 0.01f, 0.02f);        // 깊은 검정
            Color darkPurple = new Color(0.08f, 0.02f, 0.15f);       // 어두운 보라
            Color neonMagenta = new Color(0.25f, 0.05f, 0.35f);      // 네온 마젠타 (약하게)
            Color neonCyan = new Color(0.02f, 0.12f, 0.18f);         // 네온 시안 (약하게)

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);

                // 아래쪽: 시안 힌트, 위쪽: 마젠타 힌트
                Color baseColor;
                if (t < 0.3f)
                    baseColor = Color.Lerp(deepBlack, neonCyan, t / 0.3f * 0.5f);
                else if (t < 0.7f)
                    baseColor = Color.Lerp(neonCyan * 0.5f, darkPurple, (t - 0.3f) / 0.4f);
                else
                    baseColor = Color.Lerp(darkPurple, neonMagenta * 0.6f, (t - 0.7f) / 0.3f);

                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    Color c = baseColor;

                    // 중앙 레인 글로우 (밝은 수직 빛줄기)
                    float centerDist = Mathf.Abs(nx - 0.5f);
                    float laneGlow = Mathf.Exp(-centerDist * centerDist * 12f); // 가우시안 글로우
                    c.r += laneGlow * 0.06f;
                    c.g += laneGlow * 0.08f;
                    c.b += laneGlow * 0.12f;

                    // 양쪽 비네트 (어둡게)
                    float vignette = 1f - Mathf.Pow(centerDist * 1.8f, 2f);
                    vignette = Mathf.Clamp01(vignette);
                    c.r *= vignette * 0.9f + 0.1f;
                    c.g *= vignette * 0.9f + 0.1f;
                    c.b *= vignette * 0.9f + 0.1f;

                    // 미세한 노이즈/그레인 효과
                    float noise = (Mathf.PerlinNoise(x * 0.5f, y * 0.5f) - 0.5f) * 0.015f;
                    c.r += noise;
                    c.g += noise;
                    c.b += noise;

                    // 수평 스캔라인 (CRT 효과)
                    if (y % 4 == 0)
                    {
                        c.r *= 0.97f;
                        c.g *= 0.97f;
                        c.b *= 0.97f;
                    }

                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 떠다니는 파티클 생성 (네온 별빛 + 빛줄기 효과)
        /// </summary>
        private void CreateFloatingParticles(float judgeY)
        {
            floatingParticles = new Transform[PARTICLE_COUNT];
            particleMaterials = new Material[PARTICLE_COUNT];
            particleVelocities = new Vector3[PARTICLE_COUNT];
            particlePhases = new float[PARTICLE_COUNT];

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var particleTex = CreateGlowingDotTexture(16, 16);

            // 네온 파티클 색상 (더 밝고 선명하게)
            Color[] neonColors = new Color[]
            {
                new Color(0f, 1f, 1f),       // 시안
                new Color(1f, 0.3f, 0.9f),   // 마젠타
                new Color(1f, 0.9f, 0.2f),   // 골드
                new Color(0.4f, 0.6f, 1f),   // 스카이 블루
                new Color(0.2f, 1f, 0.5f),   // 네온 그린
            };

            for (int i = 0; i < PARTICLE_COUNT; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"FloatingParticle_{i}";
                go.transform.SetParent(transform);

                var c = go.GetComponent<Collider>();
                if (c != null) Destroy(c);

                // 랜덤 위치 (화면 전체에 분포, 중앙에 더 집중)
                float xBias = Random.Range(-1f, 1f);
                float x = xBias * xBias * Mathf.Sign(xBias) * 4f; // 중앙 집중 분포
                float y = Random.Range(judgeY - 2f, judgeY + 16f);
                go.transform.position = new Vector3(x, y, 1.5f);

                // 크기 다양화 (작은 것은 멀리, 큰 것은 가까이 느낌)
                float size = Random.Range(0.04f, 0.18f);
                go.transform.localScale = new Vector3(size, size, 1f);

                var mat = new Material(shader);
                mat.mainTexture = particleTex;

                // 네온 색상 랜덤 선택 (더 밝게)
                Color color = neonColors[Random.Range(0, neonColors.Length)];
                color.a = Random.Range(0.2f, 0.6f); // 더 불투명하게
                mat.color = color;

                var particleRenderer = go.GetComponent<MeshRenderer>();
                particleRenderer.material = mat;
                particleRenderer.sortingOrder = -50;

                floatingParticles[i] = go.transform;
                particleMaterials[i] = mat;
                particleVelocities[i] = new Vector3(
                    Random.Range(-0.15f, 0.15f),
                    Random.Range(0.3f, 0.8f), // 위로 더 빠르게
                    0
                );
                particlePhases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        /// <summary>
        /// 네온 글로우 도트 텍스처 (부드러운 발광 효과)
        /// </summary>
        private Texture2D CreateGlowingDotTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float cx = w / 2f, cy = h / 2f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // 코어 (밝은 중심)
                    float core = Mathf.Exp(-dist * dist * 8f);
                    // 글로우 (부드러운 외곽)
                    float glow = Mathf.Exp(-dist * dist * 2f) * 0.5f;

                    float alpha = Mathf.Clamp01(core + glow);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D CreateSoftDotTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float cx = w / 2f, cy = h / 2f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 레인 구분선 (네온 글로우 세로선 + 하단 판정 가이드)
        /// </summary>
        private void CreateLaneDividers(float judgeY)
        {
            // 기존 구분선 정리
            if (laneDividers != null)
            {
                foreach (var go in laneDividers)
                    if (go != null) Destroy(go);
            }
            // 씬에 남아있는 기존 LaneDivider 오브젝트도 정리
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("LaneDivider_"))
                    Destroy(child.gameObject);
            }

            float laneWidth = 1.4f;  // 레인 간격 (넓게 조정)
            float startX = -(LANE_COUNT - 1) * laneWidth / 2f;
            int dividerCount = LANE_COUNT + 1; // 레인 양쪽 경계 (4레인 = 5개 구분선)

            laneDividers = new GameObject[dividerCount];
            dividerMaterials = new Material[dividerCount];

            Debug.Log($"[BackgroundVFX] Creating {dividerCount} lane dividers (LANE_COUNT={LANE_COUNT})");

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var lineTex = CreateNeonLineTex(8, 256);

            for (int i = 0; i < dividerCount; i++)
            {
                // 양쪽 끝 구분선 건너뛰기 (화면 밖으로 나가서 반만 보임)
                if (i == 0 || i == dividerCount - 1)
                    continue;

                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"LaneDivider_{i}";
                go.transform.SetParent(transform);

                float x = startX - laneWidth / 2f + i * laneWidth;
                go.transform.position = new Vector3(x, judgeY + 8f, 0.9f);
                go.transform.localScale = new Vector3(0.12f, 20f, 1f);

                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var mat = new Material(shader);
                mat.mainTexture = lineTex;

                // 네온 색상: 마젠타/골드
                Color lineColor;
                if (i == 1 || i == dividerCount - 2)
                    lineColor = new Color(0.95f, 0.25f, 0.95f, 0.6f); // 네온 마젠타
                else if (i == dividerCount / 2)
                    lineColor = new Color(1f, 0.92f, 0.25f, 0.7f); // 중앙 골드
                else
                    lineColor = new Color(0.6f, 0.35f, 0.95f, 0.45f); // 보라

                mat.color = lineColor;
                var dividerRenderer = go.GetComponent<MeshRenderer>();
                dividerRenderer.material = mat;
                dividerRenderer.sortingOrder = -30;

                laneDividers[i] = go;
                dividerMaterials[i] = mat;
            }

            // 판정선 위 글로우 바 생성
            CreateJudgementLineGlow(judgeY);
        }

        /// <summary>
        /// 판정선 위 네온 글로우 바
        /// </summary>
        private void CreateJudgementLineGlow(float judgeY)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "JudgementLineGlow";
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(0, judgeY, 0.5f);
            go.transform.localScale = new Vector3(8f, 0.15f, 1f); // 얇고 넓은 바

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var glowTex = CreateHorizontalGlowTex(256, 16);
            var mat = new Material(shader);
            mat.mainTexture = glowTex;
            mat.color = new Color(1f, 1f, 1f, 0.9f);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.material = mat;
            renderer.sortingOrder = 50; // 노트(100) 아래, 배경 위
        }

        /// <summary>
        /// 네온 글로우 수직선 텍스처
        /// </summary>
        private Texture2D CreateNeonLineTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                // 하단에서 시작해서 위로 페이드
                float baseAlpha = Mathf.Lerp(0.9f, 0.05f, ny * ny);

                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    // 중앙 밝고 가장자리 글로우
                    float cx = 1f - Mathf.Abs(nx - 0.5f) * 2f;
                    float glow = Mathf.Pow(cx, 0.5f); // 부드러운 글로우

                    // 코어 (밝은 중앙)
                    float core = Mathf.Exp(-Mathf.Pow((nx - 0.5f) * 4f, 2f));
                    float finalAlpha = baseAlpha * (glow * 0.6f + core * 0.4f);

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, finalAlpha));
                }
            }
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 판정선 수평 글로우 텍스처 (시안-마젠타 그라데이션)
        /// </summary>
        private Texture2D CreateHorizontalGlowTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color cyan = new Color(0f, 0.9f, 1f);
            Color magenta = new Color(1f, 0.2f, 0.8f);
            Color gold = new Color(1f, 0.85f, 0.2f);

            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / (w - 1);

                // 양쪽 시안, 중앙 골드, 사이 마젠타
                Color c;
                if (nx < 0.2f)
                    c = Color.Lerp(cyan, magenta, nx / 0.2f);
                else if (nx < 0.5f)
                    c = Color.Lerp(magenta, gold, (nx - 0.2f) / 0.3f);
                else if (nx < 0.8f)
                    c = Color.Lerp(gold, magenta, (nx - 0.5f) / 0.3f);
                else
                    c = Color.Lerp(magenta, cyan, (nx - 0.8f) / 0.2f);

                // 양쪽 끝 페이드아웃
                float edgeFade = 1f - Mathf.Pow(Mathf.Abs(nx - 0.5f) * 2f, 3f);
                edgeFade = Mathf.Clamp01(edgeFade);

                for (int y = 0; y < h; y++)
                {
                    float ny = (float)y / (h - 1);
                    // 수직 중앙 밝고 위아래 페이드
                    float vertGlow = 1f - Mathf.Pow(Mathf.Abs(ny - 0.5f) * 2f, 2f);

                    Color pixel = c;
                    pixel.a = edgeFade * vertGlow * 0.85f;
                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D CreateVerticalLineTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                // 아래쪽 밝고 위로 페이드
                float alpha = Mathf.Lerp(0.8f, 0.1f, ny);

                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    // 중앙 밝고 가장자리 어둡게
                    float cx = 1f - Mathf.Abs(nx - 0.5f) * 2f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * cx));
                }
            }
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 비트 플래시 오버레이 (히트 시 화면 전체 살짝 밝아짐)
        /// </summary>
        private void CreateBeatFlashOverlay(float judgeY)
        {
            beatFlashOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            beatFlashOverlay.name = "BeatFlashOverlay";
            beatFlashOverlay.transform.SetParent(transform);
            beatFlashOverlay.transform.position = new Vector3(0, judgeY + 8f, -0.8f); // 노트 앞, UI 뒤
            beatFlashOverlay.transform.localScale = new Vector3(12f, 22f, 1f);

            var col = beatFlashOverlay.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            beatFlashMaterial = new Material(shader);
            beatFlashMaterial.color = new Color(1f, 0.84f, 0f, 0f); // 투명 시작
            var flashRenderer = beatFlashOverlay.GetComponent<MeshRenderer>();
            flashRenderer.material = beatFlashMaterial;
            flashRenderer.sortingOrder = 200; // 노트(100) 앞, 히트 시 플래시 효과
        }

        /// <summary>
        /// 비트 플래시 트리거 (Perfect/Great 히트 시 호출)
        /// </summary>
        public static void TriggerBeatFlash(Color color, float intensity = 0.08f)
        {
            if (instance == null || instance.beatFlashMaterial == null) return;
            instance.StartCoroutine(instance.BeatFlashCoroutine(color, intensity));
        }

        private IEnumerator BeatFlashCoroutine(Color color, float intensity)
        {
            color.a = intensity;
            beatFlashMaterial.color = color;

            float elapsed = 0f;
            float duration = 0.15f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                color.a = Mathf.Lerp(intensity, 0f, t * t);
                beatFlashMaterial.color = color;
                yield return null;
            }

            color.a = 0f;
            beatFlashMaterial.color = color;
        }

        /// <summary>
        /// 파티클 애니메이션 루프 (더 다이나믹한 움직임)
        /// </summary>
        private IEnumerator AnimateParticles()
        {
            var judgementLine = GameObject.Find("JudgementLine");
            float judgeY = judgementLine != null ? judgementLine.transform.position.y : -5f;
            float resetY = judgeY + 18f;
            float minY = judgeY - 3f;

            while (true)
            {
                yield return null;
                float time = Time.time;

                for (int i = 0; i < PARTICLE_COUNT; i++)
                {
                    if (floatingParticles[i] == null) continue;

                    var pos = floatingParticles[i].position;

                    // 기본 이동 + 좌우 흔들림 (더 부드럽게)
                    float sway = Mathf.Sin(time * 0.8f + particlePhases[i]) * 0.4f;
                    float drift = Mathf.Cos(time * 0.3f + particlePhases[i] * 2f) * 0.1f;
                    pos += particleVelocities[i] * Time.deltaTime;
                    pos.x += (sway + drift) * Time.deltaTime;

                    // 화면 밖으로 나가면 아래에서 재시작
                    if (pos.y > resetY)
                    {
                        pos.y = minY;
                        // 중앙 집중 분포로 재배치
                        float xBias = Random.Range(-1f, 1f);
                        pos.x = xBias * xBias * Mathf.Sign(xBias) * 4f;
                    }

                    floatingParticles[i].position = pos;

                    // 네온 펄스 반짝임 (더 화려하게)
                    if (particleMaterials[i] != null)
                    {
                        var c = particleMaterials[i].color;

                        // 다중 주파수 반짝임 (별빛 느낌)
                        float twinkle1 = Mathf.Sin(time * 3f + particlePhases[i]);
                        float twinkle2 = Mathf.Sin(time * 5f + particlePhases[i] * 1.5f);
                        float twinkle = 0.5f + 0.3f * twinkle1 + 0.2f * twinkle2;

                        c.a = Mathf.Lerp(0.1f, 0.55f, twinkle);

                        // 크기도 약간 펄스
                        float scale = floatingParticles[i].localScale.x;
                        float baseScale = scale / (1f + 0.1f * Mathf.Sin(time * 2f + particlePhases[i]));
                        float newScale = baseScale * (1f + 0.15f * Mathf.Sin(time * 2.5f + particlePhases[i]));
                        floatingParticles[i].localScale = new Vector3(newScale, newScale, 1f);

                        particleMaterials[i].color = c;
                    }
                }
            }
        }

        /// <summary>
        /// 배경 색상 미세 변화 (시간에 따라 보라↔남색 천천히 변화)
        /// </summary>
        private IEnumerator AnimateBackground()
        {
            while (true)
            {
                yield return null;
                if (bgMaterial == null) continue;

                float time = Time.time;
                // 매우 미세한 색조 변화
                float hueShift = Mathf.Sin(time * 0.1f) * 0.02f;
                Color tint = new Color(1f + hueShift, 1f, 1f - hueShift, 1f);
                bgMaterial.color = tint;
            }
        }

        private void OnDestroy()
        {
            if (bgMaterial != null) Destroy(bgMaterial);
            if (beatFlashMaterial != null) Destroy(beatFlashMaterial);
            if (particleMaterials != null)
            {
                foreach (var m in particleMaterials)
                    if (m != null) Destroy(m);
            }
            if (dividerMaterials != null)
            {
                foreach (var m in dividerMaterials)
                    if (m != null) Destroy(m);
            }
        }
    }
}
