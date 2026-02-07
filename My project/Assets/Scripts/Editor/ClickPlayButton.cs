using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class ClickPlayButton
{
    [MenuItem("Tools/A.I. BEAT/Click Play Button")]
    public static void Click()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ClickPlayButton] Play 모드에서만 실행 가능합니다");
            return;
        }

        var btn = GameObject.Find("PlayButton")?.GetComponent<Button>();
        if (btn != null)
        {
            Debug.Log("[ClickPlayButton] PlayButton 클릭 실행");
            btn.onClick.Invoke();
        }
        else
        {
            Debug.LogError("[ClickPlayButton] PlayButton을 찾을 수 없습니다");
        }
    }
}
