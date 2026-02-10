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
            switch (lane)
            {
                case 0: return Color.cyan;      // Lane 0: Cyan #00FFFF
                case 1: return Color.magenta;   // Lane 1: Magenta #FF00FF
                case 2: return Color.yellow;    // Lane 2: Yellow #FFFF00
                case 3: return Color.white;     // Lane 3: White #FFFFFF
                default: return Color.white;
            }
        }
    }
}
