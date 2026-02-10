using UnityEngine;

namespace AIBeat.UI
{
    /// <summary>
    /// LaneBackground(3D Quad)를 Cyberpunk 테마로 스타일링
    /// 어두운 보라색 그라데이션 + 네온 그리드
    /// </summary>
    [ExecuteAlways]
    public class LaneBackgroundStyler : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color topColor = new Color(0.02f, 0f, 0.1f, 1f);      // 진한 보라
        [SerializeField] private Color bottomColor = new Color(0.1f, 0f, 0.2f, 1f);    // 보라
        [SerializeField] private Color gridColor = new Color(1f, 0f, 0.8f, 0.15f);     // 네온 마젠타

        [Header("Grid Settings")]
        [SerializeField] private int gridSize = 64;
        [SerializeField] private int textureSize = 512;

        private MeshRenderer meshRenderer;
        private Material instanceMaterial;

        private void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                Debug.LogWarning("[LaneBackgroundStyler] No MeshRenderer found!");
                return;
            }

            // Material 인스턴스 생성 (원본 보존)
            instanceMaterial = new Material(meshRenderer.sharedMaterial);
            meshRenderer.material = instanceMaterial;

            // Cyberpunk 텍스처 생성 및 적용
            Texture2D texture = GenerateCyberpunkTexture();
            instanceMaterial.mainTexture = texture;

            Debug.Log("[LaneBackgroundStyler] Cyberpunk texture applied to LaneBackground");
        }

        private Texture2D GenerateCyberpunkTexture()
        {
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < textureSize; y++)
            {
                float vy = (float)y / textureSize;
                Color bgColor = Color.Lerp(bottomColor, topColor, vy);

                for (int x = 0; x < textureSize; x++)
                {
                    Color finalColor = bgColor;

                    // 세로 그리드 라인
                    if (x % gridSize == 0)
                    {
                        finalColor += gridColor;
                    }

                    // 가로 그리드 라인
                    if (y % gridSize == 0)
                    {
                        finalColor += gridColor;
                    }

                    // 노이즈 추가 (Cyberpunk 느낌)
                    float noise = Random.Range(-0.02f, 0.02f);
                    finalColor.r += noise;
                    finalColor.g += noise;
                    finalColor.b += noise;

                    texture.SetPixel(x, y, finalColor);
                }
            }

            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            // 인스턴스 Material 정리
            if (instanceMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(instanceMaterial);
                else
                    DestroyImmediate(instanceMaterial);
            }
        }
    }
}
