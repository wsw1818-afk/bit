using UnityEngine;

namespace AIBeat.Utils
{
    /// <summary>
    /// Resources.Load 헬퍼 — Sprite 로드 실패 시 Texture2D에서 Sprite.Create() 폴백
    /// Unity에서 텍스처의 Import Type이 "Sprite (2D and UI)"가 아니면
    /// Resources.Load<Sprite>()가 null을 반환하므로, Texture2D로 폴백합니다.
    /// </summary>
    public static class ResourceHelper
    {
        /// <summary>
        /// Resources 폴더에서 Sprite를 로드합니다.
        /// 1차: Resources.Load&lt;Sprite&gt; 시도
        /// 2차: Resources.Load&lt;Texture2D&gt; → Sprite.Create() 폴백
        /// </summary>
        /// <param name="path">Resources 폴더 기준 경로 (확장자 제외)</param>
        /// <returns>로드된 Sprite, 실패 시 null</returns>
        public static Sprite LoadSpriteFromResources(string path)
        {
            // 1차 시도: Sprite로 직접 로드
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
                return sprite;

            // 2차 시도: Texture2D로 로드 후 Sprite.Create() 폴백
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                Debug.Log($"[ResourceHelper] '{path}' Texture2D→Sprite 폴백 성공 ({tex.width}x{tex.height})");
                return sprite;
            }

            Debug.LogWarning($"[ResourceHelper] '{path}' 로드 실패 (Sprite/Texture2D 모두 없음)");
            return null;
        }
    }
}
