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
            
            _renderer.GetPropertyBlock(_propBlock);
            // Support both URP and Built-in shaders common property names
            _propBlock.SetColor("_BaseColor", color);
            _propBlock.SetColor("_Color", color);
            _renderer.SetPropertyBlock(_propBlock);
        }

        private Color GetLaneColor(int lane)
        {
            // Neon / Sensation Style Colors
            // Using HDR intensity (values > 1) for bloom effect if supported
            float intensity = 1.2f; 
            
            switch (lane)
            {
                case 0: return new Color(0f, 1f, 1f) * intensity;      // Cyan (Electric Blue)
                case 1: return new Color(1f, 0f, 0.8f) * intensity;    // Hot Pink (Magenta-ish)
                case 2: return new Color(1f, 0.9f, 0.1f) * intensity;  // Neon Yellow
                case 3: return new Color(0.9f, 0.95f, 1f) * intensity; // Cool White
                default: return Color.white;
            }
        }
    }
}
