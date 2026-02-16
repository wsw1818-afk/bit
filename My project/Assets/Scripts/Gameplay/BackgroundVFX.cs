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
        private const int LANE_COUNT = 7;

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
        /// 배경 그라데이션 텍스처: 아래쪽 짙은 남색, 위쪽 보라빛
        /// + 미세한 수직 라인 패턴
        /// </summary>
        private Texture2D CreateGradientTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color bottomColor = new Color(0.02f, 0.03f, 0.08f); // 매우 어두운 남색
            Color midColor = new Color(0.05f, 0.04f, 0.12f);    // 어두운 보라+남색
            Color topColor = new Color(0.08f, 0.05f, 0.18f);    // 진한 보라

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color baseColor;
                if (t < 0.5f)
                    baseColor = Color.Lerp(bottomColor, midColor, t * 2f);
                else
                    baseColor = Color.Lerp(midColor, topColor, (t - 0.5f) * 2f);

                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    Color c = baseColor;

                    // 수직 라인 패턴 (미세)
                    float linePattern = Mathf.Abs(Mathf.Sin(nx * Mathf.PI * w / 4f));
                    c.r += linePattern * 0.01f;
                    c.g += linePattern * 0.008f;
                    c.b += linePattern * 0.02f;

                    // 중앙 약간 밝게 (비네트 역효과)
                    float centerGlow = 1f - Mathf.Abs(nx - 0.5f) * 1.5f;
                    centerGlow = Mathf.Clamp01(centerGlow);
                    c.r += centerGlow * 0.015f;
                    c.g += centerGlow * 0.01f;
                    c.b += centerGlow * 0.025f;

                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 떠다니는 파티클 생성 (배경에 별빛 같은 효과)
        /// </summary>
        private void CreateFloatingParticles(float judgeY)
        {
            floatingParticles = new Transform[PARTICLE_COUNT];
            particleMaterials = new Material[PARTICLE_COUNT];
            particleVelocities = new Vector3[PARTICLE_COUNT];
            particlePhases = new float[PARTICLE_COUNT];

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var particleTex = CreateSoftDotTexture(8, 8);

            for (int i = 0; i < PARTICLE_COUNT; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"FloatingParticle_{i}";
                go.transform.SetParent(transform);

                var c = go.GetComponent<Collider>();
                if (c != null) Destroy(c);

                // 랜덤 위치 (화면 전체에 분포)
                float x = Random.Range(-4f, 4f);
                float y = Random.Range(judgeY - 2f, judgeY + 16f);
                go.transform.position = new Vector3(x, y, 1.5f); // 배경과 노트 사이

                float size = Random.Range(0.03f, 0.12f);
                go.transform.localScale = new Vector3(size, size, 1f);

                var mat = new Material(shader);
                mat.mainTexture = particleTex;

                // 테마 색상 중 랜덤 선택
                Color color = themeColors[Random.Range(0, themeColors.Length)];
                color.a = Random.Range(0.1f, 0.4f);
                mat.color = color;

                var particleRenderer = go.GetComponent<MeshRenderer>();
                particleRenderer.material = mat;
                particleRenderer.sortingOrder = -50; // 배경 앞, 노트(100) 뒤

                floatingParticles[i] = go.transform;
                particleMaterials[i] = mat;
                particleVelocities[i] = new Vector3(
                    Random.Range(-0.1f, 0.1f),
                    Random.Range(0.2f, 0.6f), // 위로 천천히 올라감
                    0
                );
                particlePhases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
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
        /// 레인 구분선 (Gold 반투명 세로선)
        /// </summary>
        private void CreateLaneDividers(float judgeY)
        {
            float laneWidth = 1f;
            float startX = -(LANE_COUNT - 1) * laneWidth / 2f;
            int dividerCount = LANE_COUNT + 1; // 레인 양쪽 경계

            laneDividers = new GameObject[dividerCount];
            dividerMaterials = new Material[dividerCount];

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var lineTex = CreateVerticalLineTex(4, 128);

            for (int i = 0; i < dividerCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"LaneDivider_{i}";
                go.transform.SetParent(transform);

                float x = startX - laneWidth / 2f + i * laneWidth;
                go.transform.position = new Vector3(x, judgeY + 8f, 0.9f);
                go.transform.localScale = new Vector3(0.04f, 20f, 1f);

                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var mat = new Material(shader);
                mat.mainTexture = lineTex;

                // 양쪽 끝은 오렌지, 안쪽은 Gold
                Color lineColor;
                if (i == 0 || i == dividerCount - 1)
                    lineColor = new Color(1f, 0.55f, 0f, 0.3f); // Orange
                else if (i == 1 || i == dividerCount - 2)
                    lineColor = new Color(0.58f, 0.29f, 0.98f, 0.2f); // Purple
                else
                    lineColor = new Color(1f, 0.84f, 0f, 0.15f); // Gold (투명)

                mat.color = lineColor;
                var dividerRenderer = go.GetComponent<MeshRenderer>();
                dividerRenderer.material = mat;
                dividerRenderer.sortingOrder = -30; // 파티클 앞, 노트(100) 뒤

                laneDividers[i] = go;
                dividerMaterials[i] = mat;
            }
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
        /// 파티클 애니메이션 루프
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

                    // 기본 이동 + 좌우 흔들림
                    float sway = Mathf.Sin(time * 0.5f + particlePhases[i]) * 0.3f;
                    pos += particleVelocities[i] * Time.deltaTime;
                    pos.x += sway * Time.deltaTime;

                    // 화면 밖으로 나가면 아래에서 재시작
                    if (pos.y > resetY)
                    {
                        pos.y = minY;
                        pos.x = Random.Range(-4f, 4f);
                    }

                    floatingParticles[i].position = pos;

                    // 알파 반짝임
                    if (particleMaterials[i] != null)
                    {
                        var c = particleMaterials[i].color;
                        float twinkle = 0.5f + 0.5f * Mathf.Sin(time * 2f + particlePhases[i]);
                        c.a = Mathf.Lerp(0.05f, 0.35f, twinkle);
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
