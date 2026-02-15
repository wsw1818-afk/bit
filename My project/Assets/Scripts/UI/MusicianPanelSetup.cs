using UnityEngine;
using UnityEngine.UI;

namespace AIBeat.UI
{
    /// <summary>
    /// MusicianBackground 패널의 자식 Image들에 스프라이트를 런타임에 할당
    /// </summary>
    public class MusicianPanelSetup : MonoBehaviour
    {
        private void Start()
        {
            SetupMusicianSprites();
        }

        private void SetupMusicianSprites()
        {
            // Resources에서 스프라이트 로드
            var spriteMap = new (string childName, string spritePath)[]
            {
                ("Drummer", "Sprites/Instruments/drum_perform"),
                ("Pianist", "Sprites/Instruments/piano_perform"),
                ("Guitarist", "Sprites/Instruments/guitar_perform"),
                ("DJ", "Sprites/Instruments/dj_perform")
            };

            foreach (var (childName, spritePath) in spriteMap)
            {
                var child = transform.Find(childName);
                if (child == null)
                {
                    Debug.LogWarning($"[MusicianPanelSetup] Child '{childName}' not found");
                    continue;
                }

                var image = child.GetComponent<Image>();
                if (image == null)
                {
                    Debug.LogWarning($"[MusicianPanelSetup] Image component not found on '{childName}'");
                    continue;
                }

                var sprite = Resources.Load<Sprite>(spritePath);
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.preserveAspect = true;
                    Debug.Log($"[MusicianPanelSetup] Loaded sprite for '{childName}'");
                }
                else
                {
                    Debug.LogWarning($"[MusicianPanelSetup] Sprite not found at '{spritePath}'");
                }
            }
        }
    }
}
