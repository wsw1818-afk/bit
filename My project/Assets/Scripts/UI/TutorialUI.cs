using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBeat.Core;

namespace AIBeat.UI
{
    /// <summary>
    /// 튜토리얼 오버레이 UI
    /// 사이버펑크 네온 스타일로 각 단계별 콘텐츠를 동적 생성
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        // 색상 상수 (사이버펑크 네온 스타일)
        private static readonly Color BG_COLOR = new Color(0.01f, 0.01f, 0.05f, 0.90f);
        private static readonly Color CYAN = new Color(0f, 0.9f, 1f, 1f);
        private static readonly Color CYAN_DIM = new Color(0f, 0.7f, 0.85f, 0.6f);
        private static readonly Color NEON_PINK = new Color(1f, 0.2f, 0.6f, 1f);
        private static readonly Color NEON_GREEN = new Color(0.2f, 1f, 0.4f, 1f);
        private static readonly Color NEON_ORANGE = new Color(1f, 0.6f, 0.1f, 1f);
        private static readonly Color NEON_YELLOW = new Color(1f, 1f, 0.2f, 1f);
        private static readonly Color BTN_BG = new Color(0f, 0.15f, 0.2f, 0.7f);
        private static readonly Color TEXT_WHITE = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color TEXT_DIM = new Color(0.7f, 0.7f, 0.8f, 0.8f);

        // UI 요소
        private Image backgroundImage;
        private GameObject contentContainer;
        private GameObject stepIndicatorContainer;
        private Button nextButton;
        private Button skipButton;

        // 스텝 인디케이터 점들
        private Image[] stepDots;

        private void Awake()
        {
            BuildUI();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 전체 UI 구조 빌드
        /// </summary>
        private void BuildUI()
        {
            // 배경 오버레이
            backgroundImage = gameObject.AddComponent<Image>();
            backgroundImage.color = BG_COLOR;

            // CanvasGroup으로 페이드 제어
            var cg = gameObject.AddComponent<CanvasGroup>();

            // --- 콘텐츠 컨테이너 (중앙) ---
            contentContainer = new GameObject("ContentContainer");
            contentContainer.transform.SetParent(transform, false);
            var contentRect = contentContainer.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.05f, 0.15f);
            contentRect.anchorMax = new Vector2(0.95f, 0.85f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // --- 스텝 인디케이터 (상단) ---
            stepIndicatorContainer = new GameObject("StepIndicator");
            stepIndicatorContainer.transform.SetParent(transform, false);
            var stepRect = stepIndicatorContainer.AddComponent<RectTransform>();
            stepRect.anchorMin = new Vector2(0.3f, 0.88f);
            stepRect.anchorMax = new Vector2(0.7f, 0.92f);
            stepRect.offsetMin = Vector2.zero;
            stepRect.offsetMax = Vector2.zero;

            var stepLayout = stepIndicatorContainer.AddComponent<HorizontalLayoutGroup>();
            stepLayout.spacing = 12;
            stepLayout.childAlignment = TextAnchor.MiddleCenter;
            stepLayout.childControlWidth = false;
            stepLayout.childControlHeight = false;
            stepLayout.childForceExpandWidth = false;
            stepLayout.childForceExpandHeight = false;

            // 6개 점 생성
            stepDots = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                var dot = new GameObject($"Dot_{i}");
                dot.transform.SetParent(stepIndicatorContainer.transform, false);
                var dotRect = dot.AddComponent<RectTransform>();
                dotRect.sizeDelta = new Vector2(12, 12);
                var dotImage = dot.AddComponent<Image>();
                dotImage.color = CYAN_DIM;
                stepDots[i] = dotImage;
            }

            // --- 다음 버튼 (하단 우측) ---
            nextButton = CreateButton("NextButton", "NEXT >>", new Vector2(0.55f, 0.04f), new Vector2(0.85f, 0.11f));
            nextButton.onClick.AddListener(OnNextClicked);

            // --- 건너뛰기 버튼 (하단 좌측) ---
            skipButton = CreateButton("SkipButton", "SKIP", new Vector2(0.15f, 0.04f), new Vector2(0.45f, 0.11f));
            skipButton.onClick.AddListener(OnSkipClicked);
            // 건너뛰기 버튼은 약간 어두운 스타일
            var skipBg = skipButton.GetComponent<Image>();
            if (skipBg != null) skipBg.color = new Color(0.1f, 0.1f, 0.15f, 0.6f);
        }

        /// <summary>
        /// 해당 단계의 UI를 표시
        /// </summary>
        public void Show(TutorialManager.TutorialStep step)
        {
            gameObject.SetActive(true);

            // 기존 콘텐츠 제거
            ClearContent();

            // 스텝 인디케이터 업데이트
            UpdateStepIndicator((int)step);

            // 단계별 콘텐츠 생성
            switch (step)
            {
                case TutorialManager.TutorialStep.Welcome:
                    BuildWelcomeContent();
                    break;
                case TutorialManager.TutorialStep.Controls:
                    BuildControlsContent();
                    break;
                case TutorialManager.TutorialStep.NoteTypes:
                    BuildNoteTypesContent();
                    break;
                case TutorialManager.TutorialStep.Judgement:
                    BuildJudgementContent();
                    break;
                case TutorialManager.TutorialStep.Practice:
                    BuildPracticeContent();
                    break;
                case TutorialManager.TutorialStep.Complete:
                    BuildCompleteContent();
                    break;
            }

            // Complete 단계에서는 "다음" 버튼 텍스트 변경
            var nextBtnText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (nextBtnText != null)
            {
                if (step == TutorialManager.TutorialStep.Complete)
                {
                    nextBtnText.text = "START!";
                    skipButton.gameObject.SetActive(false);
                }
                else
                {
                    nextBtnText.text = "NEXT >>";
                    skipButton.gameObject.SetActive(true);
                }
            }

            // 등장 애니메이션
            if (contentContainer != null)
            {
                contentContainer.transform.localScale = Vector3.one * 0.9f;
                UIAnimator.ScaleTo(this, contentContainer, Vector3.one, 0.25f);
            }
        }

        /// <summary>
        /// UI 숨기기
        /// </summary>
        public void Hide()
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                UIAnimator.FadeCanvasGroup(this, cg, 0f, 0.3f, () =>
                {
                    gameObject.SetActive(false);
                    cg.alpha = 1f;
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        // === 단계별 콘텐츠 빌드 ===

        /// <summary>
        /// 1단계: 환영 메시지
        /// </summary>
        private void BuildWelcomeContent()
        {
            CreateTitle("Welcome to\nA.I. BEAT!", CYAN);

            CreateBodyText(
                "Play rhythm game with\n" +
                "<color=#00E5FF>AI-generated music</color>.\n\n" +
                "Beatmania-style\n" +
                "<color=#00E5FF>2 Keys + 2 Scratches</color> controls.\n" +
                "Hit notes in time!",
                0.35f
            );

            // 게임 아이콘/로고 영역 (간단한 네온 라인)
            CreateDecorativeLine(new Vector2(0.2f, 0.65f), new Vector2(0.8f, 0.65f));
            CreateDecorativeLine(new Vector2(0.2f, 0.33f), new Vector2(0.8f, 0.33f));
        }

        /// <summary>
        /// 2단계: 조작법 안내 (키보드 다이어그램)
        /// </summary>
        private void BuildControlsContent()
        {
            CreateTitle("Controls", CYAN);

            CreateBodyText(
                "Use <color=#FFA000>Scratch</color> and <color=#00E5FF>2 Keys</color> to play.",
                0.62f
            );

            // 키보드 다이어그램 생성
            BuildKeyboardDiagram();
        }

        /// <summary>
        /// 3단계: 노트 종류 설명
        /// </summary>
        private void BuildNoteTypesContent()
        {
            CreateTitle("Note Types", CYAN);

            // Tap Note
            CreateNoteTypeRow(0.65f, "\u25cf  Tap Note", "Press key once", CYAN);

            // Long Note
            CreateNoteTypeRow(0.48f, "\u2503  Long Note", "Hold the key", NEON_GREEN);

            // Scratch Note
            CreateNoteTypeRow(0.31f, "\u2248  Scratch Note", "Swipe S or L zone", NEON_ORANGE);
        }

        /// <summary>
        /// 4단계: 판정 설명
        /// </summary>
        private void BuildJudgementContent()
        {
            CreateTitle("Judgement System", CYAN);

            float startY = 0.68f;
            float gap = 0.11f;

            CreateJudgementRow(startY, "PERFECT!", "\u00b150ms", NEON_YELLOW);
            CreateJudgementRow(startY - gap, "GREAT!", "\u00b1100ms", NEON_GREEN);
            CreateJudgementRow(startY - gap * 2, "GOOD", "\u00b1200ms", CYAN);
            CreateJudgementRow(startY - gap * 3, "BAD", "\u00b1350ms", new Color(1f, 0.3f, 0.3f, 1f));
            CreateJudgementRow(startY - gap * 4, "MISS", "Missed", new Color(0.5f, 0.5f, 0.5f, 1f));

            CreateBodyText(
                "Maintain <color=#FFE040>COMBO</color> for score bonus!",
                0.15f
            );
        }

        /// <summary>
        /// 5단계: 연습 안내
        /// </summary>
        private void BuildPracticeContent()
        {
            CreateTitle("Ready!", NEON_GREEN);

            CreateBodyText(
                "When notes reach the\n" +
                "<color=#00E5FF>judgement line</color>,\n" +
                "press the key in time!\n\n" +
                "<color=#FFA000>TIPS</color>\n" +
                "\u2022 Notes fall from top to bottom\n" +
                "\u2022 Hit precisely at the judgement line!\n" +
                "\u2022 Hold long notes until the end",
                0.35f
            );
        }

        /// <summary>
        /// 6단계: 완료
        /// </summary>
        private void BuildCompleteContent()
        {
            CreateTitle("Tutorial Complete!", NEON_YELLOW);

            CreateBodyText(
                "You're all set!\n\n" +
                "Press <color=#00E5FF>Play</color> button\n" +
                "to start your first song!\n\n" +
                "<color=#808090>You can replay the tutorial in Settings</color>",
                0.35f
            );
        }

        // === UI 유틸리티 메서드 ===

        /// <summary>
        /// 제목 텍스트 생성
        /// </summary>
        private TextMeshProUGUI CreateTitle(string text, Color color)
        {
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(contentContainer.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.78f);
            titleRect.anchorMax = new Vector2(1f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var tmp = titleGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 36;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            return tmp;
        }

        /// <summary>
        /// 본문 텍스트 생성 (anchorY 위치 지정)
        /// </summary>
        private TextMeshProUGUI CreateBodyText(string text, float anchorY)
        {
            var bodyGo = new GameObject("BodyText");
            bodyGo.transform.SetParent(contentContainer.transform, false);
            var bodyRect = bodyGo.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.05f, anchorY - 0.25f);
            bodyRect.anchorMax = new Vector2(0.95f, anchorY + 0.25f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;

            var tmp = bodyGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.color = TEXT_WHITE;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            tmp.lineSpacing = 8;

            return tmp;
        }

        /// <summary>
        /// 키보드 다이어그램 생성
        /// [S] [D] [F] [L]
        /// </summary>
        private void BuildKeyboardDiagram()
        {
            var diagramGo = new GameObject("KeyboardDiagram");
            diagramGo.transform.SetParent(contentContainer.transform, false);
            var diagramRect = diagramGo.AddComponent<RectTransform>();
            diagramRect.anchorMin = new Vector2(0.02f, 0.18f);
            diagramRect.anchorMax = new Vector2(0.98f, 0.58f);
            diagramRect.offsetMin = Vector2.zero;
            diagramRect.offsetMax = Vector2.zero;

            // 키 정보 배열 (4레인: ScratchL, Key1, Key2, ScratchR)
            var keys = new (string key, string label, Color color)[]
            {
                ("S", "Scratch L", NEON_ORANGE),
                ("D", "Key 1", CYAN),
                ("F", "Key 2", CYAN),
                ("L", "Scratch R", NEON_ORANGE)
            };

            float totalWidth = 1f;
            float keyGap = 0.01f;
            float keyWidth = (totalWidth - keyGap * (keys.Length - 1)) / keys.Length;

            for (int i = 0; i < keys.Length; i++)
            {
                float xStart = i * (keyWidth + keyGap);
                CreateKeyVisual(diagramGo.transform, keys[i].key, keys[i].label, keys[i].color,
                    xStart, keyWidth);
            }
        }

        /// <summary>
        /// 개별 키 시각화 (키캡 + 레이블)
        /// </summary>
        private void CreateKeyVisual(Transform parent, string keyText, string label, Color color,
            float xPos, float width)
        {
            var keyGo = new GameObject($"Key_{keyText}");
            keyGo.transform.SetParent(parent, false);
            var keyRect = keyGo.AddComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(xPos, 0.1f);
            keyRect.anchorMax = new Vector2(xPos + width, 0.9f);
            keyRect.offsetMin = Vector2.zero;
            keyRect.offsetMax = Vector2.zero;

            // 키캡 배경
            var bgImage = keyGo.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.12f, 0.8f);

            // 네온 테두리
            var outline = keyGo.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(2, -2);

            // 키 텍스트 (상단)
            var keyLabelGo = new GameObject("KeyLabel");
            keyLabelGo.transform.SetParent(keyGo.transform, false);
            var klRect = keyLabelGo.AddComponent<RectTransform>();
            klRect.anchorMin = new Vector2(0, 0.45f);
            klRect.anchorMax = new Vector2(1, 0.95f);
            klRect.offsetMin = Vector2.zero;
            klRect.offsetMax = Vector2.zero;

            var klText = keyLabelGo.AddComponent<TextMeshProUGUI>();
            klText.text = keyText;
            klText.fontSize = 22;
            klText.color = color;
            klText.alignment = TextAlignmentOptions.Center;
            klText.fontStyle = FontStyles.Bold;

            // 레인 이름 (하단)
            var laneGo = new GameObject("LaneLabel");
            laneGo.transform.SetParent(keyGo.transform, false);
            var lnRect = laneGo.AddComponent<RectTransform>();
            lnRect.anchorMin = new Vector2(0, 0.05f);
            lnRect.anchorMax = new Vector2(1, 0.4f);
            lnRect.offsetMin = Vector2.zero;
            lnRect.offsetMax = Vector2.zero;

            var lnText = laneGo.AddComponent<TextMeshProUGUI>();
            lnText.text = label;
            lnText.fontSize = 11;
            lnText.color = TEXT_DIM;
            lnText.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// 노트 종류 행 생성
        /// </summary>
        private void CreateNoteTypeRow(float anchorY, string title, string desc, Color color)
        {
            var rowGo = new GameObject($"NoteRow_{title}");
            rowGo.transform.SetParent(contentContainer.transform, false);
            var rowRect = rowGo.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.05f, anchorY - 0.06f);
            rowRect.anchorMax = new Vector2(0.95f, anchorY + 0.06f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            // 배경
            var bgImage = rowGo.AddComponent<Image>();
            bgImage.color = new Color(0.02f, 0.02f, 0.08f, 0.5f);

            var outline = rowGo.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.3f);
            outline.effectDistance = new Vector2(1, -1);

            // 제목 (좌측)
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(rowGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0f);
            titleRect.anchorMax = new Vector2(0.45f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = title;
            titleTmp.fontSize = 20;
            titleTmp.color = color;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.fontStyle = FontStyles.Bold;

            // 설명 (우측)
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(rowGo.transform, false);
            var descRect = descGo.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.5f, 0f);
            descRect.anchorMax = new Vector2(0.95f, 1f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;

            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = desc;
            descTmp.fontSize = 18;
            descTmp.color = TEXT_WHITE;
            descTmp.alignment = TextAlignmentOptions.MidlineRight;
        }

        /// <summary>
        /// 판정 행 생성
        /// </summary>
        private void CreateJudgementRow(float anchorY, string judgement, string timing, Color color)
        {
            var rowGo = new GameObject($"JudgeRow_{judgement}");
            rowGo.transform.SetParent(contentContainer.transform, false);
            var rowRect = rowGo.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.1f, anchorY - 0.04f);
            rowRect.anchorMax = new Vector2(0.9f, anchorY + 0.04f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            // 판정 이름
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(rowGo.transform, false);
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = judgement;
            nameTmp.fontSize = 24;
            nameTmp.color = color;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.fontStyle = FontStyles.Bold;

            // 타이밍
            var timingGo = new GameObject("Timing");
            timingGo.transform.SetParent(rowGo.transform, false);
            var timingRect = timingGo.AddComponent<RectTransform>();
            timingRect.anchorMin = new Vector2(0.5f, 0f);
            timingRect.anchorMax = new Vector2(1f, 1f);
            timingRect.offsetMin = Vector2.zero;
            timingRect.offsetMax = Vector2.zero;

            var timingTmp = timingGo.AddComponent<TextMeshProUGUI>();
            timingTmp.text = timing;
            timingTmp.fontSize = 20;
            timingTmp.color = TEXT_DIM;
            timingTmp.alignment = TextAlignmentOptions.MidlineRight;
        }

        /// <summary>
        /// 장식용 가로선 생성
        /// </summary>
        private void CreateDecorativeLine(Vector2 startAnchor, Vector2 endAnchor)
        {
            var lineGo = new GameObject("DecorLine");
            lineGo.transform.SetParent(contentContainer.transform, false);
            var lineRect = lineGo.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(startAnchor.x, startAnchor.y);
            lineRect.anchorMax = new Vector2(endAnchor.x, endAnchor.y + 0.003f);
            lineRect.offsetMin = Vector2.zero;
            lineRect.offsetMax = Vector2.zero;

            var lineImage = lineGo.AddComponent<Image>();
            lineImage.color = CYAN_DIM;
        }

        /// <summary>
        /// 버튼 생성 유틸리티
        /// </summary>
        private Button CreateButton(string name, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = anchorMin;
            btnRect.anchorMax = anchorMax;
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            // 배경
            var bgImage = btnGo.AddComponent<Image>();
            bgImage.color = BTN_BG;

            // 네온 테두리
            var outline = btnGo.AddComponent<Outline>();
            outline.effectColor = CYAN;
            outline.effectDistance = new Vector2(2, -2);

            // 버튼 컴포넌트
            var button = btnGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            button.colors = colors;

            // 텍스트
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.color = CYAN;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            return button;
        }

        /// <summary>
        /// 스텝 인디케이터 업데이트
        /// </summary>
        private void UpdateStepIndicator(int currentIndex)
        {
            if (stepDots == null) return;

            for (int i = 0; i < stepDots.Length; i++)
            {
                if (stepDots[i] == null) continue;

                if (i == currentIndex)
                {
                    stepDots[i].color = CYAN;
                    stepDots[i].rectTransform.sizeDelta = new Vector2(16, 16);
                }
                else if (i < currentIndex)
                {
                    stepDots[i].color = NEON_GREEN;
                    stepDots[i].rectTransform.sizeDelta = new Vector2(12, 12);
                }
                else
                {
                    stepDots[i].color = CYAN_DIM;
                    stepDots[i].rectTransform.sizeDelta = new Vector2(10, 10);
                }
            }
        }

        /// <summary>
        /// 콘텐츠 컨테이너 자식 제거
        /// </summary>
        private void ClearContent()
        {
            if (contentContainer == null) return;

            for (int i = contentContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(contentContainer.transform.GetChild(i).gameObject);
            }
        }

        // === 이벤트 핸들러 ===

        private void OnNextClicked()
        {
            TutorialManager.Instance?.NextStep();
        }

        private void OnSkipClicked()
        {
            TutorialManager.Instance?.SkipTutorial();
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveAllListeners();
            if (skipButton != null) skipButton.onClick.RemoveAllListeners();
        }
    }
}
