using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using AIBeat.Core;
using AIBeat.Utils;

namespace AIBeat.UI
{
    public class SplashController : MonoBehaviour
    {
        [SerializeField] private float autoTransitionDelay = 2f;
        private float timer;

        private void Start()
        {
            Application.runInBackground = true;

            // 배경 이미지 설정
            SetupSplashUI();

            Debug.Log("[SplashController] Start() - 자동 전환 대기 중");
        }

        private void SetupSplashUI()
        {
            // Canvas 찾기
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            if (canvas == null) return;

            // CanvasScaler 설정
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // 배경 이미지 (Splash_BG.png)
            var existingBg = canvas.transform.Find("SplashBackground");
            if (existingBg == null)
            {
                var bgGo = new GameObject("SplashBackground");
                bgGo.transform.SetParent(canvas.transform, false);
                bgGo.transform.SetAsFirstSibling();

                var bgRect = bgGo.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                var bgImg = bgGo.AddComponent<Image>();
                bgImg.raycastTarget = false;

                Sprite bgSprite = ResourceHelper.LoadSpriteFromResources("AIBeat_Design/UI/Backgrounds/Splash_BG");
                if (bgSprite != null)
                {
                    bgImg.sprite = bgSprite;
                    bgImg.type = Image.Type.Simple;
                    bgImg.preserveAspect = false;
                    Debug.Log("[SplashController] Loaded Splash_BG");
                }
                else
                {
                    bgImg.color = new Color(0.02f, 0.01f, 0.05f, 1f);
                }
            }

            // 로고 이미지 (MainLogo.png)
            var existingLogo = canvas.transform.Find("SplashLogo");
            if (existingLogo == null)
            {
                Sprite logoSprite = ResourceHelper.LoadSpriteFromResources("AIBeat_Design/UI/Logo/MainLogo");
                if (logoSprite != null)
                {
                    var logoGo = new GameObject("SplashLogo");
                    logoGo.transform.SetParent(canvas.transform, false);

                    var logoRect = logoGo.AddComponent<RectTransform>();
                    logoRect.anchorMin = new Vector2(0.1f, 0.35f);
                    logoRect.anchorMax = new Vector2(0.9f, 0.65f);
                    logoRect.offsetMin = Vector2.zero;
                    logoRect.offsetMax = Vector2.zero;

                    var logoImg = logoGo.AddComponent<Image>();
                    logoImg.sprite = logoSprite;
                    logoImg.preserveAspect = true;
                    logoImg.raycastTarget = false;
                    Debug.Log("[SplashController] Loaded MainLogo");
                }
            }

            // "Touch to Start" 텍스트
            var existingTouch = canvas.transform.Find("TouchToStartText");
            if (existingTouch == null)
            {
                var touchGo = new GameObject("TouchToStartText");
                touchGo.transform.SetParent(canvas.transform, false);

                var touchRect = touchGo.AddComponent<RectTransform>();
                touchRect.anchorMin = new Vector2(0.1f, 0.12f);
                touchRect.anchorMax = new Vector2(0.9f, 0.18f);
                touchRect.offsetMin = Vector2.zero;
                touchRect.offsetMax = Vector2.zero;

                var touchText = touchGo.AddComponent<TextMeshProUGUI>();
                touchText.text = "화면을 터치하세요";
                touchText.fontSize = 24;
                touchText.color = new Color(0.6f, 0.65f, 0.8f, 0.7f);
                touchText.alignment = TextAlignmentOptions.Center;
                touchText.raycastTarget = false;

                var korFont = KoreanFontManager.KoreanFont;
                if (korFont != null) touchText.font = korFont;
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;

            // 자동 전환 또는 아무 키/터치로 메인 메뉴 이동
            if (timer >= autoTransitionDelay || Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                Debug.Log("[SplashController] MainMenuScene으로 전환");
                SceneManager.LoadScene("MainMenuScene");
            }
        }
    }
}
