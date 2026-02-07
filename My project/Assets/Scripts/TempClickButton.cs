using UnityEngine;
using UnityEngine.UI;

public class TempClickButton : MonoBehaviour
{
    void Start()
    {
        var btn = GameObject.Find("PlayButton")?.GetComponent<Button>();
        if (btn != null)
        {
            Debug.Log("[TempClickButton] PlayButton 클릭 실행");
            btn.onClick.Invoke();
            Destroy(gameObject, 0.5f);
        }
        else
        {
            Debug.LogError("[TempClickButton] PlayButton을 찾을 수 없습니다");
        }
    }
}
