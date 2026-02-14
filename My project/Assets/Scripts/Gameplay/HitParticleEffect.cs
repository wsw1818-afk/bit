using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AIBeat.Gameplay
{
    /// <summary>
    /// 노트 히트 시 파티클 폭발 이펙트
    /// 판정별로 다른 색상/크기의 파티클 스프레이
    /// 오브젝트 풀링으로 가비지 최소화
    /// </summary>
    public class HitParticleEffect : MonoBehaviour
    {
        private static HitParticleEffect instance;
        public static HitParticleEffect Instance => instance;

        // 파티클 풀
        private Queue<ParticleGroup> pool = new Queue<ParticleGroup>();
        private List<ParticleGroup> active = new List<ParticleGroup>();

        private const int POOL_SIZE = 20;
        private const int PARTICLES_PER_HIT = 12;
        private const float PARTICLE_LIFETIME = 0.6f;

        private Material particleMaterial;
        private Mesh quadMesh;

        private void Awake()
        {
            instance = this;
            CreateSharedResources();
            PrewarmPool();
        }

        private void CreateSharedResources()
        {
            // 공유 머티리얼
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            particleMaterial = new Material(shader);
            particleMaterial.mainTexture = CreateParticleTexture(16, 16);

            // Quad 메시 참조
            var tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempQuad);
        }

        private Texture2D CreateParticleTexture(int w, int h)
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
                    alpha = alpha * alpha * alpha; // 매우 부드러운 원
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private void PrewarmPool()
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                var group = new ParticleGroup(PARTICLES_PER_HIT, particleMaterial, quadMesh, transform);
                group.Deactivate();
                pool.Enqueue(group);
            }
        }

        /// <summary>
        /// 히트 이펙트 재생 (외부 호출용)
        /// </summary>
        public static void Play(Vector3 position, Color color, float scale = 1f)
        {
            if (instance == null) return;
            instance.SpawnParticles(position, color, scale);
        }

        /// <summary>
        /// 판정 결과별 이펙트 재생
        /// </summary>
        public static void PlayForJudgement(Vector3 position, int laneIndex, AIBeat.Core.JudgementResult result)
        {
            if (instance == null) return;

            Color color;
            float scale;
            switch (result)
            {
                case AIBeat.Core.JudgementResult.Perfect:
                    color = new Color(1f, 0.84f, 0f); // Gold
                    scale = 1.5f;
                    break;
                case AIBeat.Core.JudgementResult.Great:
                    color = new Color(0f, 0.8f, 0.82f); // Teal
                    scale = 1.2f;
                    break;
                case AIBeat.Core.JudgementResult.Good:
                    color = new Color(0.4f, 0.8f, 0.2f); // Green
                    scale = 1.0f;
                    break;
                case AIBeat.Core.JudgementResult.Bad:
                    color = new Color(1f, 0.55f, 0f); // Orange
                    scale = 0.7f;
                    break;
                default:
                    return; // Miss는 이펙트 없음
            }

            instance.SpawnParticles(position, color, scale);
        }

        private void SpawnParticles(Vector3 position, Color color, float scale)
        {
            ParticleGroup group;
            if (pool.Count > 0)
            {
                group = pool.Dequeue();
            }
            else
            {
                // 풀 부족 시 가장 오래된 활성 그룹 재활용
                if (active.Count > 0)
                {
                    group = active[0];
                    active.RemoveAt(0);
                }
                else return;
            }

            group.Activate(position, color, scale);
            active.Add(group);
            StartCoroutine(ReturnAfterDelay(group));
        }

        private IEnumerator ReturnAfterDelay(ParticleGroup group)
        {
            float elapsed = 0f;
            while (elapsed < PARTICLE_LIFETIME)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / PARTICLE_LIFETIME;
                group.UpdateParticles(t);
                yield return null;
            }

            group.Deactivate();
            active.Remove(group);
            pool.Enqueue(group);
        }

        private void OnDestroy()
        {
            if (particleMaterial != null) Destroy(particleMaterial);
        }

        /// <summary>
        /// 파티클 그룹: 여러 작은 Quad가 방사형으로 퍼져나감
        /// </summary>
        private class ParticleGroup
        {
            private GameObject root;
            private Transform[] particles;
            private Material[] materials;
            private Vector3[] velocities;
            private float[] rotations;
            private int count;

            public ParticleGroup(int particleCount, Material sharedMat, Mesh mesh, Transform parent)
            {
                count = particleCount;
                root = new GameObject("HitParticles");
                root.transform.SetParent(parent);

                particles = new Transform[count];
                materials = new Material[count];
                velocities = new Vector3[count];
                rotations = new float[count];

                for (int i = 0; i < count; i++)
                {
                    var go = new GameObject($"p_{i}");
                    go.transform.SetParent(root.transform);

                    var mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = mesh;

                    var mr = go.AddComponent<MeshRenderer>();
                    materials[i] = new Material(sharedMat);
                    mr.material = materials[i];

                    particles[i] = go.transform;
                }
            }

            public void Activate(Vector3 position, Color color, float scale)
            {
                root.SetActive(true);
                root.transform.position = position;

                for (int i = 0; i < count; i++)
                {
                    // 방사형으로 퍼지는 속도
                    float angle = (360f / count) * i + Random.Range(-15f, 15f);
                    float speed = Random.Range(3f, 7f) * scale;
                    float rad = angle * Mathf.Deg2Rad;
                    velocities[i] = new Vector3(Mathf.Cos(rad) * speed, Mathf.Sin(rad) * speed, 0);
                    rotations[i] = Random.Range(-360f, 360f);

                    particles[i].localPosition = Vector3.zero;
                    float particleScale = Random.Range(0.08f, 0.18f) * scale;
                    particles[i].localScale = new Vector3(particleScale, particleScale, 1f);
                    particles[i].localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

                    // 색상에 약간의 변화
                    Color c = color;
                    c.r += Random.Range(-0.1f, 0.1f);
                    c.g += Random.Range(-0.1f, 0.1f);
                    c.b += Random.Range(-0.1f, 0.1f);
                    c.a = 1f;
                    materials[i].color = c;
                }
            }

            public void UpdateParticles(float t)
            {
                for (int i = 0; i < count; i++)
                {
                    if (particles[i] == null) continue;

                    // 위치: 속도 기반 이동 + 감속
                    float decel = 1f - t * 0.7f;
                    particles[i].localPosition += velocities[i] * Time.deltaTime * decel;

                    // 회전
                    particles[i].Rotate(0, 0, rotations[i] * Time.deltaTime);

                    // 스케일 축소
                    float scaleMult = 1f - t;
                    var s = particles[i].localScale;
                    particles[i].localScale = s * (1f - Time.deltaTime * 2f);

                    // 알파 페이드
                    var c = materials[i].color;
                    c.a = Mathf.Clamp01(1f - t * t);
                    materials[i].color = c;
                }
            }

            public void Deactivate()
            {
                root.SetActive(false);
            }
        }
    }
}
