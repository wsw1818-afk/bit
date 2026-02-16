using UnityEngine;

namespace AIBeat.Gameplay
{
    [RequireComponent(typeof(Renderer))]
    public class NoteVisuals : MonoBehaviour
    {
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
            // Music Theme 7-Lane Colors
            // intensity를 RGB에만 적용 (Alpha는 1.0 유지)
            float intensity = 1.2f;
            Color baseColor;

            switch (lane)
            {
                case 0: baseColor = new Color(1f, 0.55f, 0f);      break; // Orange (Scratch L)
                case 1: baseColor = new Color(0.58f, 0.29f, 0.98f); break; // Purple
                case 2: baseColor = new Color(0f, 0.8f, 0.82f);    break; // Teal
                case 3: baseColor = new Color(1f, 0.84f, 0f);      break; // Gold (Center)
                case 4: baseColor = new Color(0f, 0.8f, 0.82f);    break; // Teal
                case 5: baseColor = new Color(0.58f, 0.29f, 0.98f); break; // Purple
                case 6: baseColor = new Color(1f, 0.55f, 0f);      break; // Orange (Scratch R)
                default: return Color.white;
            }

            // RGB에만 intensity 적용, Alpha는 1.0 유지
            return new Color(
                Mathf.Min(baseColor.r * intensity, 1f),
                Mathf.Min(baseColor.g * intensity, 1f),
                Mathf.Min(baseColor.b * intensity, 1f),
                1f
            );
        }
    }
}

