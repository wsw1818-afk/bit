using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AIBeat.Gameplay
{
    /// <summary>
    /// 노트 히트 시 충격파 링 + 세로 라이트빔 이펙트
    /// 판정선 위치에서 원형으로 퍼지는 링과 위로 솟는 빛기둥
    /// </summary>
    public class HitImpactEffect : MonoBehaviour
    {
        private static HitImpactEffect instance;
        public static HitImpactEffect Instance => instance;

        // 충격파 링 풀
        private Queue<RingEffect> ringPool = new Queue<RingEffect>();
        private List<RingEffect> activeRings = new List<RingEffect>();

        // 라이트빔 풀
        private Queue<LightBeam> beamPool = new Queue<LightBeam>();
        private List<LightBeam> activeBeams = new List<LightBeam>();

        private const int RING_POOL_SIZE = 10;
        private const int BEAM_POOL_SIZE = 8;

        private Material ringMaterial;
        private Material beamMaterial;
        private Mesh quadMesh;

        private void Awake()
        {
            instance = this;
            CreateSharedResources();
            PrewarmPools();
        }

        private void CreateSharedResources()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            ringMaterial = new Material(shader);
            ringMaterial.mainTexture = CreateRingTexture(64, 64);

            beamMaterial = new Material(shader);
            beamMaterial.mainTexture = CreateBeamTexture(16, 64);

            var tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempQuad);
        }

        /// <summary>
        /// 링 텍스처: 중앙이 빈 원형 (도넛 모양)
        /// </summary>
        private Texture2D CreateRingTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float cx = w / 2f, cy = h / 2f;
            float outerRadius = 0.95f;
            float innerRadius = 0.65f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 0f;
                    if (dist >= innerRadius && dist <= outerRadius)
                    {
                        // 링 영역: 중앙이 가장 밝음
                        float ringCenter = (innerRadius + outerRadius) / 2f;
                        float ringDist = Mathf.Abs(dist - ringCenter) / ((outerRadius - innerRadius) / 2f);
                        alpha = 1f - ringDist;
                        alpha = alpha * alpha; // 더 부드럽게
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 빔 텍스처: 아래 밝고 위로 페이드, 좌우 가장자리 페이드
        /// </summary>
        private Texture2D CreateBeamTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                // 아래(y=0)가 밝고 위로 페이드
                float fadeY = 1f - ny * ny;

                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    // 중앙이 밝고 가장자리 페이드
                    float fadeX = 1f - Mathf.Abs(nx - 0.5f) * 2f;
                    fadeX = fadeX * fadeX;

                    float alpha = fadeY * fadeX;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        private void PrewarmPools()
        {
            for (int i = 0; i < RING_POOL_SIZE; i++)
            {
                var ring = new RingEffect(ringMaterial, quadMesh, transform);
                ring.Deactivate();
                ringPool.Enqueue(ring);
            }

            for (int i = 0; i < BEAM_POOL_SIZE; i++)
            {
                var beam = new LightBeam(beamMaterial, quadMesh, transform);
                beam.Deactivate();
                beamPool.Enqueue(beam);
            }
        }

        /// <summary>
        /// 히트 이펙트 재생 (판정 결과별)
        /// </summary>
        public static void Play(Vector3 position, Color color, AIBeat.Core.JudgementResult result)
        {
            if (instance == null) return;

            float scale;
            bool showBeam;
            switch (result)
            {
                case AIBeat.Core.JudgementResult.Perfect:
                    scale = 2.0f;
                    showBeam = true;
                    break;
                case AIBeat.Core.JudgementResult.Great:
                    scale = 1.5f;
                    showBeam = true;
                    break;
                case AIBeat.Core.JudgementResult.Good:
                    scale = 1.0f;
                    showBeam = false;
                    break;
                default:
                    scale = 0.7f;
                    showBeam = false;
                    break;
            }

            // 충격파 링
            instance.SpawnRing(position, color, scale);

            // Perfect/Great만 라이트빔
            if (showBeam)
                instance.SpawnBeam(position, color, scale);
        }

        private void SpawnRing(Vector3 position, Color color, float scale)
        {
            RingEffect ring;
            if (ringPool.Count > 0)
            {
                ring = ringPool.Dequeue();
            }
            else if (activeRings.Count > 0)
            {
                ring = activeRings[0];
                activeRings.RemoveAt(0);
            }
            else return;

            ring.Activate(position, color, scale);
            activeRings.Add(ring);
            StartCoroutine(AnimateRing(ring));
        }

        private void SpawnBeam(Vector3 position, Color color, float scale)
        {
            LightBeam beam;
            if (beamPool.Count > 0)
            {
                beam = beamPool.Dequeue();
            }
            else if (activeBeams.Count > 0)
            {
                beam = activeBeams[0];
                activeBeams.RemoveAt(0);
            }
            else return;

            beam.Activate(position, color, scale);
            activeBeams.Add(beam);
            StartCoroutine(AnimateBeam(beam));
        }

        private IEnumerator AnimateRing(RingEffect ring)
        {
            float duration = 0.35f;
            float elapsed = 0f;
            float startScale = 0.3f;
            float endScale = ring.maxScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 빠르게 확대 후 서서히 사라짐
                float easeOut = 1f - (1f - t) * (1f - t);
                float currentScale = Mathf.Lerp(startScale, endScale, easeOut);
                float alpha = 1f - t * t; // 점점 투명

                ring.Update(currentScale, alpha);
                yield return null;
            }

            ring.Deactivate();
            activeRings.Remove(ring);
            ringPool.Enqueue(ring);
        }

        private IEnumerator AnimateBeam(LightBeam beam)
        {
            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 빠르게 위로 솟았다 사라짐
                float heightT = t < 0.3f ? t / 0.3f : 1f; // 처음 30%에서 최대 높이
                float alpha = t < 0.3f ? 1f : 1f - ((t - 0.3f) / 0.7f); // 30% 이후 페이드
                alpha = Mathf.Clamp01(alpha);

                beam.Update(heightT, alpha);
                yield return null;
            }

            beam.Deactivate();
            activeBeams.Remove(beam);
            beamPool.Enqueue(beam);
        }

        private void OnDestroy()
        {
            if (ringMaterial != null) Destroy(ringMaterial);
            if (beamMaterial != null) Destroy(beamMaterial);
        }

        /// <summary>
        /// 충격파 링: 원형으로 확대되면서 사라짐
        /// </summary>
        private class RingEffect
        {
            public GameObject go;
            public Material mat;
            public float maxScale;
            private Color baseColor;

            public RingEffect(Material sharedMat, Mesh mesh, Transform parent)
            {
                go = new GameObject("HitRing");
                go.transform.SetParent(parent);

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                var mr = go.AddComponent<MeshRenderer>();
                mat = new Material(sharedMat);
                mr.material = mat;
            }

            public void Activate(Vector3 pos, Color color, float scale)
            {
                go.SetActive(true);
                go.transform.position = new Vector3(pos.x, pos.y, -0.4f); // 노트 앞
                maxScale = scale * 1.5f;
                baseColor = color;
                mat.color = color;
            }

            public void Update(float scale, float alpha)
            {
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var c = baseColor;
                c.a = alpha * 0.8f;
                mat.color = c;
            }

            public void Deactivate()
            {
                go.SetActive(false);
            }
        }

        /// <summary>
        /// 라이트빔: 판정선에서 위로 솟는 빛기둥
        /// </summary>
        private class LightBeam
        {
            public GameObject go;
            public Material mat;
            private Color baseColor;
            private float maxHeight;
            private Vector3 basePos;

            public LightBeam(Material sharedMat, Mesh mesh, Transform parent)
            {
                go = new GameObject("LightBeam");
                go.transform.SetParent(parent);

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                var mr = go.AddComponent<MeshRenderer>();
                mat = new Material(sharedMat);
                mr.material = mat;
            }

            public void Activate(Vector3 pos, Color color, float scale)
            {
                go.SetActive(true);
                basePos = pos;
                basePos.z = -0.2f;
                maxHeight = 4f * scale;
                baseColor = color;
                baseColor.a = 0.5f;
                mat.color = baseColor;
            }

            public void Update(float heightT, float alpha)
            {
                float height = maxHeight * heightT;
                go.transform.position = new Vector3(basePos.x, basePos.y + height / 2f, basePos.z);
                go.transform.localScale = new Vector3(0.4f, height, 1f);

                var c = baseColor;
                c.a = alpha * 0.45f;
                mat.color = c;
            }

            public void Deactivate()
            {
                go.SetActive(false);
            }
        }
    }
}
