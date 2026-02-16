using UnityEngine;

namespace AIBeat.UI
{
    /// <summary>
    /// Canvas에 붙이면 자동으로 Safe Area 패널을 생성하고,
    /// 기존 자식 UI 요소들을 Safe Area 안으로 이동시킴.
    /// 노치/펀치홀/다이나믹 아일랜드 영역과 UI가 겹치지 않도록 함.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class SafeAreaApplier : MonoBehaviour
    {
        private RectTransform safeAreaPanel;
        private Rect lastSafeArea;

        private void Awake()
        {
            CreateSafeAreaPanel();
            ApplySafeArea();
        }

        private void Update()
        {
            // 화면 회전/Safe Area 변경 시 실시간 대응
            if (Screen.safeArea != lastSafeArea)
            {
                ApplySafeArea();
            }
        }

        private void CreateSafeAreaPanel()
        {
            // 이미 SafeAreaPanel이 있으면 재사용
            var existing = transform.Find("SafeAreaPanel");
            if (existing != null)
            {
                safeAreaPanel = existing.GetComponent<RectTransform>();
                return;
            }

            // SafeAreaPanel 생성
            var panelGo = new GameObject("SafeAreaPanel");
            safeAreaPanel = panelGo.AddComponent<RectTransform>();
            safeAreaPanel.SetParent(transform, false);

            // 전체 화면 크기로 초기화
            safeAreaPanel.anchorMin = Vector2.zero;
            safeAreaPanel.anchorMax = Vector2.one;
            safeAreaPanel.offsetMin = Vector2.zero;
            safeAreaPanel.offsetMax = Vector2.zero;

            // 맨 처음에 위치 (Sibling 0)
            safeAreaPanel.SetAsFirstSibling();

            // 기존 자식들을 SafeAreaPanel 안으로 이동
            // (역순으로 수집 후 이동해야 인덱스 꼬이지 않음)
            // Fullscreen으로 시작하는 오브젝트는 제외 (전체 화면 배경용)
            var children = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child != safeAreaPanel.transform && !child.name.StartsWith("Fullscreen"))
                    children.Add(child);
            }

            foreach (var child in children)
            {
                child.SetParent(safeAreaPanel, false);
            }
        }

        private void ApplySafeArea()
        {
            if (safeAreaPanel == null) return;

            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;

            // Safe Area를 Canvas 기준 앵커로 변환
            var canvas = GetComponent<Canvas>();
            if (canvas == null) return;

            var canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            if (screenSize.x <= 0 || screenSize.y <= 0) return;

            Vector2 anchorMin = safeArea.position / screenSize;
            Vector2 anchorMax = (safeArea.position + safeArea.size) / screenSize;

            // 값 클램핑
            anchorMin.x = Mathf.Clamp01(anchorMin.x);
            anchorMin.y = Mathf.Clamp01(anchorMin.y);
            anchorMax.x = Mathf.Clamp01(anchorMax.x);
            anchorMax.y = Mathf.Clamp01(anchorMax.y);

            safeAreaPanel.anchorMin = anchorMin;
            safeAreaPanel.anchorMax = anchorMax;
            safeAreaPanel.offsetMin = Vector2.zero;
            safeAreaPanel.offsetMax = Vector2.zero;
        }
    }
}
