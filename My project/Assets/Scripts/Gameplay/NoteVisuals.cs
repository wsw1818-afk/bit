using UnityEngine;
using AIBeat.Core;

namespace AIBeat.Gameplay
{
    [RequireComponent(typeof(Renderer))]
    public class NoteVisuals : MonoBehaviour
    {
        // 4레인 색상 테이블 (GameConstants.LaneCount 기반)
        private static readonly Color[] LaneColors = new Color[]
        {
            new Color(0.58f, 0.29f, 0.98f),  // Lane 0: Purple
            new Color(0f, 0.8f, 0.82f),      // Lane 1: Teal
            new Color(1f, 0.84f, 0f),         // Lane 2: Gold
            new Color(1f, 0.55f, 0f),         // Lane 3: Orange
        };

        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        public void SetLaneColor(int laneIndex)
        {
            Color color = GetLaneColor(laneIndex);

            // Apply to all renderers (Head + Body)
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mat = r.material;
                mat.color = color;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
            }

            // 글로우 이펙트 색상 동기화
            var glow = GetComponent<NoteGlowEffect>();
            if (glow != null)
                glow.Initialize(color);
        }

        private Color GetLaneColor(int lane)
        {
            float intensity = 1.2f;
            Color baseColor = (lane >= 0 && lane < LaneColors.Length)
                ? LaneColors[lane]
                : Color.white;

            return new Color(
                Mathf.Min(baseColor.r * intensity, 1f),
                Mathf.Min(baseColor.g * intensity, 1f),
                Mathf.Min(baseColor.b * intensity, 1f),
                1f
            );
        }
    }
}

