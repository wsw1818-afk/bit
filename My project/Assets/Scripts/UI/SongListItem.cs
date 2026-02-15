using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace AIBeat.UI
{
    public class SongListItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI artistText;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private Image coverImage;
        [SerializeField] private Button selectButton;

        private System.Action<string> onSelectCallback;
        private string songId;

        public void Setup(string id, string title, string artist, string difficulty, System.Action<string> onSelect)
        {
            songId = id;
            if (titleText != null) titleText.text = title;
            if (artistText != null) artistText.text = artist;
            if (difficultyText != null) difficultyText.text = difficulty;
            
            onSelectCallback = onSelect;
            
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnClicked);
            }
        }

        private void OnClicked()
        {
            onSelectCallback?.Invoke(songId);
        }
    }
}
