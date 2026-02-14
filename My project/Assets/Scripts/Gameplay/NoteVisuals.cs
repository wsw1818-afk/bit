using UnityEngine;

namespace AIBeat.Gameplay
{
    [RequireComponent(typeof(Renderer))]
    public class NoteVisuals : MonoBehaviour
    {
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        public void SetLaneColor(int laneIndex)
        {
            if (_renderer == null) return;

            Color color = GetLaneColor(laneIndex);

            // Material.color를 직접 변경 (Sprites/Default 셰이더 호환)
            _renderer.material.color = color;
            if (_renderer.material.HasProperty("_BaseColor"))
                _renderer.material.SetColor("_BaseColor", color);
        }

        private Color GetLaneColor(int lane)
        {
            // Music Theme 7-Lane Colors
            float intensity = 1.2f;

            switch (lane)
            {
                case 0: return new Color(1f, 0.55f, 0f) * intensity;      // Orange (Scratch L)
                case 1: return new Color(0.58f, 0.29f, 0.98f) * intensity; // Purple
                case 2: return new Color(0f, 0.8f, 0.82f) * intensity;    // Teal
                case 3: return new Color(1f, 0.84f, 0f) * intensity;      // Gold (Center)
                case 4: return new Color(0f, 0.8f, 0.82f) * intensity;    // Teal
                case 5: return new Color(0.58f, 0.29f, 0.98f) * intensity; // Purple
                case 6: return new Color(1f, 0.55f, 0f) * intensity;      // Orange (Scratch R)
                default: return Color.white;
            }
        }
    }
}
