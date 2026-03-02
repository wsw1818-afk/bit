using UnityEditor;
using UnityEngine;

/// <summary>
/// "Packages with Errors" 다이얼로그를 영구적으로 닫습니다.
/// Unity는 이 다이얼로그를 EditorPrefs에 저장합니다.
/// </summary>
[InitializeOnLoad]
public static class DismissPackageErrorDialog
{
    // Unity 6에서 "Dismiss Forever" 버튼이 사용하는 키
    private const string kDismissKey = "PackageManager.HidePackageErrors";
    // 추가 후보 키들
    private const string kDismissKey2 = "PackageManagerAlertsDismissed";
    private const string kDismissKey3 = "PackageManager.DismissErrors";

    static DismissPackageErrorDialog()
    {
        // 에디터 시작 시 자동으로 다이얼로그 억제
        EditorApplication.delayCall += DismissDialog;
    }

    [MenuItem("Tools/A.I. BEAT/패키지 에러 다이얼로그 영구 닫기")]
    public static void DismissDialog()
    {
        EditorPrefs.SetBool(kDismissKey, true);
        EditorPrefs.SetBool(kDismissKey2, true);
        EditorPrefs.SetBool(kDismissKey3, true);
        EditorPrefs.SetInt(kDismissKey, 1);

        // Unity 내부 PackageManager 관련 알려진 키들
        EditorPrefs.SetBool("PackageManager.showPackageManagerAlerts", false);
        EditorPrefs.SetBool("UnityEditor.PackageManager.UI.hideAlerts", true);

        Debug.Log("[DismissPackageErrorDialog] 패키지 에러 다이얼로그 닫기 설정 완료");
    }
}
