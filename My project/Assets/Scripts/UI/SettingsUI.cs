using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBeat.Core;

namespace AIBeat.UI
{
    /// <summary>
    /// 설정 화면 UI
    /// MainMenuUI의 settingsPanel 안에 동적으로 UI 요소 생성
    /// 사이버펑크/네온 스타일
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        // 디자인 에셋 기반 색상 팔레트
        private static readonly Color BG_OVERLAY = new Color(0f, 0f, 0.02f, 0.85f);  // 반투명 오버레이
        private static readonly Color CARD_BG_COLOR = new Color(0.02f, 0.02f, 0.08f, 0.92f);  // 카드 배경
        private static readonly Color BORDER_CYAN = UIColorPalette.BORDER_CYAN;  // 시안 테두리
        private static readonly Color LABEL_COLOR = UIColorPalette.NEON_CYAN_BRIGHT;  // 밝은 시안
        private static readonly Color VALUE_COLOR = Color.white;
        private static readonly Color TITLE_COLOR = UIColorPalette.NEON_CYAN_BRIGHT;
        private static readonly Color SLIDER_BG_COLOR = new Color(0.05f, 0.05f, 0.12f, 0.9f);
        private static readonly Color SLIDER_FILL_COLOR = UIColorPalette.NEON_MAGENTA;  // 마젠타 필
        private static readonly Color SLIDER_HANDLE_COLOR = Color.white;

        // 슬라이더 참조 (기본값 복원 시 사용)
        private Slider noteSpeedSlider;
        private Slider judgementOffsetSlider;
        private Slider bgmVolumeSlider;
        private Slider sfxVolumeSlider;
        private Slider backgroundDimSlider;

        // 캘리브레이션
        private CalibrationManager calibrationManager;
        private TMP_Text calibrationStatusText;
        private GameObject calibrationPanel;

        // 값 표시 텍스트 참조
        private TMP_Text noteSpeedValueText;
        private TMP_Text judgementOffsetValueText;
        private TMP_Text bgmVolumeValueText;
        private TMP_Text sfxVolumeValueText;
        private TMP_Text backgroundDimValueText;

        private MainMenuUI mainMenuUI;

        private void Awake()
        {
            mainMenuUI = GetComponentInParent<MainMenuUI>();
            if (mainMenuUI == null)
                mainMenuUI = FindFirstObjectByType<MainMenuUI>();

            // RectTransform 확인 - 없으면 Transform을 RectTransform으로 변환
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                // Canvas 하위에서 RectTransform이 필요함
                // gameObject를 재생성하지 않고 직접 추가는 불가능하므로 로그 출력
                Debug.LogWarning("[SettingsUI] RectTransform이 없습니다. UI가 올바르게 표시되지 않을 수 있습니다.");
            }

            // UI 레이어 설정
            gameObject.layer = LayerMask.NameToLayer("UI");

            // CalibrationManager 확보
            calibrationManager = FindFirstObjectByType<CalibrationManager>();
            if (calibrationManager == null)
            {
                var calGo = new GameObject("CalibrationManager");
                calGo.transform.SetParent(transform.root);
                calibrationManager = calGo.AddComponent<CalibrationManager>();
            }

            BuildUI();
        }

        /// <summary>
        /// 설정 패널 내부 UI를 동적으로 구성
        /// </summary>
        private void BuildUI()
        {
            // 기존 자식 오브젝트 정리 (중복 생성 방지)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // RectTransform 설정 (전체 화면 덮기)
            var panelRect = GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // 설정창을 최상위로 이동 (다른 UI 요소 위에 표시)
            transform.SetAsLastSibling();

            // 패널 배경 설정 (완전 불투명)
            var panelImage = GetComponent<Image>();
            if (panelImage == null)
                panelImage = gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.02f, 0.06f, 1f);  // 완전 불투명 어두운 배경
            panelImage.raycastTarget = true;  // 클릭 차단

            // 네온 테두리
            var outline = GetComponent<Outline>();
            if (outline == null)
                outline = gameObject.AddComponent<Outline>();
            outline.effectColor = BORDER_CYAN;
            outline.effectDistance = new Vector2(3, -3);

            // 스크롤 가능한 콘텐츠 영역
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(20, 90); // 하단 버튼 영역 확보 (더 넓게)
            contentRect.offsetMax = new Vector2(-20, -30);

            // VerticalLayoutGroup으로 항목 정렬
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 12;  // 8→12 (카드 간 여백 확대)
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // ContentSizeFitter 추가
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 타이틀
            CreateTitle(contentGo.transform, "설정");

            // 구분선
            CreateSeparator(contentGo.transform);

            // SettingsManager에서 현재 값 로드
            var settings = SettingsManager.Instance;
            float currentNoteSpeed = settings != null ? settings.NoteSpeed : 5.0f;
            float currentOffset = settings != null ? settings.JudgementOffset : 0f;
            float currentBGM = settings != null ? settings.BGMVolume : 0.8f;
            float currentSFX = settings != null ? settings.SFXVolume : 0.8f;
            float currentDim = settings != null ? settings.BackgroundDim : 0.5f;

            // 게임플레이 섹션 헤더
            CreateSectionHeader(contentGo.transform, "▶ 게임플레이");

            // 노트 속도 슬라이더 (1.0 ~ 10.0, 0.5 단위)
            noteSpeedSlider = CreateSliderRow(contentGo.transform, "노트 속도",
                1.0f, 10.0f, currentNoteSpeed, out noteSpeedValueText,
                (val) =>
                {
                    // 0.5 단위로 스냅
                    float snapped = Mathf.Round(val * 2f) / 2f;
                    noteSpeedSlider.SetValueWithoutNotify(snapped);
                    noteSpeedValueText.text = snapped.ToString("F1");
                    if (SettingsManager.Instance != null)
                        SettingsManager.Instance.NoteSpeed = snapped;
                },
                "F1");

            // 판정 오프셋 슬라이더 (-100ms ~ +100ms)
            // 슬라이더는 -100 ~ 100 (정수ms), 저장은 초 단위
            judgementOffsetSlider = CreateSliderRow(contentGo.transform, "판정 오프셋",
                -100f, 100f, currentOffset * 1000f, out judgementOffsetValueText,
                (val) =>
                {
                    float rounded = Mathf.Round(val);
                    judgementOffsetSlider.SetValueWithoutNotify(rounded);
                    judgementOffsetValueText.text = $"{rounded:F0}ms";
                    if (SettingsManager.Instance != null)
                        SettingsManager.Instance.JudgementOffset = rounded / 1000f;
                },
                "ms", true);

            // 캘리브레이션 카드 (판정 오프셋 바로 아래)
            CreateCalibrationButton(contentGo.transform);

            // 구분선
            CreateSeparator(contentGo.transform);

            // 오디오 섹션 헤더
            CreateSectionHeader(contentGo.transform, "♪ 오디오");

            // BGM 볼륨 슬라이더
            bgmVolumeSlider = CreateSliderRow(contentGo.transform, "배경음악 볼륨",
                0f, 1f, currentBGM, out bgmVolumeValueText,
                (val) =>
                {
                    bgmVolumeValueText.text = Mathf.RoundToInt(val * 100f) + "%";
                    if (SettingsManager.Instance != null)
                        SettingsManager.Instance.BGMVolume = val;
                },
                "%");

            // SFX 볼륨 슬라이더
            sfxVolumeSlider = CreateSliderRow(contentGo.transform, "효과음 볼륨",
                0f, 1f, currentSFX, out sfxVolumeValueText,
                (val) =>
                {
                    sfxVolumeValueText.text = Mathf.RoundToInt(val * 100f) + "%";
                    if (SettingsManager.Instance != null)
                        SettingsManager.Instance.SFXVolume = val;
                },
                "%");

            // 구분선
            CreateSeparator(contentGo.transform);

            // 비주얼 섹션 헤더
            CreateSectionHeader(contentGo.transform, "◆ 비주얼");

            // 배경 밝기 슬라이더
            backgroundDimSlider = CreateSliderRow(contentGo.transform, "배경 어둡게",
                0f, 1f, currentDim, out backgroundDimValueText,
                (val) =>
                {
                    backgroundDimValueText.text = Mathf.RoundToInt(val * 100f) + "%";
                    if (SettingsManager.Instance != null)
                        SettingsManager.Instance.BackgroundDim = val;
                },
                "%");

            // 최종 구분선
            CreateSeparator(contentGo.transform);

            // 초기 값 표시 업데이트
            noteSpeedValueText.text = currentNoteSpeed.ToString("F1");
            judgementOffsetValueText.text = $"{currentOffset * 1000f:F0}ms";
            bgmVolumeValueText.text = Mathf.RoundToInt(currentBGM * 100f) + "%";
            sfxVolumeValueText.text = Mathf.RoundToInt(currentSFX * 100f) + "%";
            backgroundDimValueText.text = Mathf.RoundToInt(currentDim * 100f) + "%";

            // 하단 버튼 영역
            CreateButtons();

            // 한국어 폰트 적용 (□□□ 방지)
            KoreanFontManager.ApplyFontToAll(gameObject);
        }

        /// <summary>
        /// 타이틀 텍스트 생성 (디자인 에셋 스타일)
        /// </summary>
        private void CreateTitle(Transform parent, string text)
        {
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(parent, false);

            var rect = titleGo.AddComponent<RectTransform>();
            var layoutElem = titleGo.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 60;

            var tmp = titleGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 42;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = TITLE_COLOR;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // 글로우 효과
            var outline = titleGo.AddComponent<Outline>();
            outline.effectColor = UIColorPalette.NEON_CYAN.WithAlpha(0.7f);
            outline.effectDistance = new Vector2(3, -3);

            // 섀도우 효과
            var shadow = titleGo.AddComponent<Shadow>();
            shadow.effectColor = UIColorPalette.NEON_MAGENTA.WithAlpha(0.4f);
            shadow.effectDistance = new Vector2(4, -4);
        }

        /// <summary>
        /// 섹션 헤더 생성 (게임플레이/오디오/비주얼 구분)
        /// </summary>
        private void CreateSectionHeader(Transform parent, string text)
        {
            var headerGo = new GameObject($"SectionHeader_{text}");
            headerGo.transform.SetParent(parent, false);

            var rect = headerGo.AddComponent<RectTransform>();
            var layoutElem = headerGo.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 36;

            var tmp = headerGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = UIColorPalette.NEON_MAGENTA;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;

            // 글로우 효과
            var outline = headerGo.AddComponent<Outline>();
            outline.effectColor = UIColorPalette.NEON_MAGENTA.WithAlpha(0.5f);
            outline.effectDistance = new Vector2(2, -2);
        }

        /// <summary>
        /// 구분선 생성 (그라데이션 네온)
        /// </summary>
        private void CreateSeparator(Transform parent)
        {
            var sepGo = new GameObject("Separator");
            sepGo.transform.SetParent(parent, false);

            var rect = sepGo.AddComponent<RectTransform>();
            var layoutElem = sepGo.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 4;

            var img = sepGo.AddComponent<Image>();
            img.color = UIColorPalette.BORDER_CYAN.WithAlpha(0.6f);
            img.raycastTarget = false;

            // 글로우 효과
            var outline = sepGo.AddComponent<Outline>();
            outline.effectColor = UIColorPalette.NEON_CYAN.WithAlpha(0.4f);
            outline.effectDistance = new Vector2(0, 2);
        }

        /// <summary>
        /// 슬라이더 행 생성 (카드 스타일, 라벨 + 슬라이더 + 값 통합)
        /// </summary>
        private Slider CreateSliderRow(Transform parent, string label,
            float min, float max, float currentValue,
            out TMP_Text valueText, UnityEngine.Events.UnityAction<float> onChanged,
            string suffix = "", bool wholeNumbers = false)
        {
            // 카드 컨테이너 (배경 + 테두리)
            var cardGo = new GameObject($"Card_{label.Replace(" ", "")}");
            cardGo.transform.SetParent(parent, false);

            var cardRect = cardGo.AddComponent<RectTransform>();
            var cardLayout = cardGo.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = 85;  // 70→85 (카드 스타일로 높이 확대)

            // 카드 배경 + 테두리
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = CARD_BG_COLOR;

            var cardOutline = cardGo.AddComponent<Outline>();
            cardOutline.effectColor = BORDER_CYAN;
            cardOutline.effectDistance = new Vector2(2, -2);

            // 카드 내부 레이아웃
            var vertLayout = cardGo.AddComponent<VerticalLayoutGroup>();
            vertLayout.padding = new RectOffset(12, 12, 8, 8);  // 패딩 확대
            vertLayout.spacing = 4;
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = true;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childForceExpandHeight = false;

            // 상단: 라벨 + 값 텍스트 (가로 배치)
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(cardGo.transform, false);

            var headerRect = headerGo.AddComponent<RectTransform>();
            var headerLayout = headerGo.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 26;  // 24→26 (라벨 높이 확대)

            var hLayout = headerGo.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            // 라벨 (더 눈에 띄게)
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(headerGo.transform, false);
            labelGo.AddComponent<RectTransform>();
            var labelLayout2 = labelGo.AddComponent<LayoutElement>();
            labelLayout2.flexibleWidth = 1;
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 20;  // 18→20 (라벨 크기 확대)
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.color = LABEL_COLOR;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // 값 표시 텍스트 (더 강조)
            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(headerGo.transform, false);
            valueGo.AddComponent<RectTransform>();
            var valueLayout2 = valueGo.AddComponent<LayoutElement>();
            valueLayout2.preferredWidth = 90;  // 80→90 (값 영역 확대)
            var valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
            valueTmp.fontSize = 22;  // 18→22 (값 크기 확대)
            valueTmp.fontStyle = FontStyles.Bold;
            valueTmp.color = VALUE_COLOR;
            valueTmp.alignment = TextAlignmentOptions.MidlineRight;
            valueText = valueTmp;

            // 슬라이더 (개선된 디자인)
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(cardGo.transform, false);

            var sliderRect = sliderGo.AddComponent<RectTransform>();
            var sliderLayout = sliderGo.AddComponent<LayoutElement>();
            sliderLayout.preferredHeight = 40;  // 48→40 (슬라이더 높이 최적화)

            // 슬라이더 배경 (둥근 모서리 시뮬레이션)
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.2f);
            bgRect.anchorMax = new Vector2(1, 0.8f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = SLIDER_BG_COLOR;

            // 슬라이더 필 영역 (밝은 네온 시안)
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.2f);
            fillAreaRect.anchorMax = new Vector2(1, 0.8f);
            fillAreaRect.offsetMin = new Vector2(4, 0);
            fillAreaRect.offsetMax = new Vector2(-4, 0);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = SLIDER_FILL_COLOR;

            // 슬라이더 핸들 영역
            var handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRect = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0, 0);
            handleAreaRect.anchorMax = new Vector2(1, 1);
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(48, 48);  // 44→48 (핸들 크기 최대화)
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = SLIDER_HANDLE_COLOR;

            // 핸들 네온 효과 (Outline)
            var handleOutline = handleGo.AddComponent<Outline>();
            handleOutline.effectColor = SLIDER_FILL_COLOR;
            handleOutline.effectDistance = new Vector2(2, -2);

            // Slider 컴포넌트 설정
            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.value = currentValue;
            slider.onValueChanged.AddListener(onChanged);

            // 슬라이더 색상 전환 설정 (개선)
            var colors = slider.colors;
            colors.normalColor = SLIDER_HANDLE_COLOR;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);  // 밝은 흰색
            colors.pressedColor = SLIDER_FILL_COLOR;  // 눌렀을 때 네온 시안
            colors.selectedColor = SLIDER_FILL_COLOR;
            colors.colorMultiplier = 1f;
            slider.colors = colors;

            return slider;
        }

        /// <summary>
        /// 캘리브레이션 카드 (버튼 + 상태 텍스트)
        /// </summary>
        private void CreateCalibrationButton(Transform parent)
        {
            // 카드 컨테이너
            var cardGo = new GameObject("CalibrationCard");
            cardGo.transform.SetParent(parent, false);

            var cardLayout = cardGo.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = 80;

            // 카드 배경 + 테두리
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = new Color(0.15f, 0.1f, 0.02f, 0.9f);  // 어두운 오렌지 톤

            var cardOutline = cardGo.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 0.7f, 0.2f, 0.7f);  // 밝은 오렌지 테두리
            cardOutline.effectDistance = new Vector2(2, -2);

            // 카드 내부 레이아웃
            var vertLayout = cardGo.AddComponent<VerticalLayoutGroup>();
            vertLayout.padding = new RectOffset(15, 15, 10, 10);
            vertLayout.spacing = 8;
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = true;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childForceExpandHeight = false;

            // 버튼 직접 생성 (UIButtonStyleHelper 대신)
            var btnGo = new GameObject("CalibrateBtn");
            btnGo.transform.SetParent(cardGo.transform, false);

            var btnLayout = btnGo.AddComponent<LayoutElement>();
            btnLayout.preferredHeight = 40;

            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.8f, 0.5f, 0f, 0.8f);  // 오렌지 배경

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(OnCalibrateClicked);

            var btnColors = btn.colors;
            btnColors.normalColor = new Color(0.8f, 0.5f, 0f, 0.8f);
            btnColors.highlightedColor = new Color(1f, 0.7f, 0.2f, 1f);
            btnColors.pressedColor = new Color(1f, 0.8f, 0.4f, 1f);
            btn.colors = btnColors;

            // 버튼 텍스트
            var btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRect = btnTextGo.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            var btnTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
            btnTmp.text = "● 자동 조정";
            btnTmp.fontSize = 20;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.color = Color.white;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.raycastTarget = false;

            // 상태 텍스트
            var statusGo = new GameObject("CalibrationStatus");
            statusGo.transform.SetParent(cardGo.transform, false);
            statusGo.AddComponent<RectTransform>();
            var statusLayout = statusGo.AddComponent<LayoutElement>();
            statusLayout.preferredHeight = 22;

            calibrationStatusText = statusGo.AddComponent<TextMeshProUGUI>();
            calibrationStatusText.fontSize = 14;
            calibrationStatusText.color = new Color(0.7f, 0.7f, 0.8f, 1f);  // 연한 회색
            calibrationStatusText.alignment = TextAlignmentOptions.Center;
            calibrationStatusText.text = "탭 테스트로 오프셋 자동 감지";
        }

        /// <summary>
        /// 인라인 버튼 생성 (카드 내부용, 작은 버튼)
        /// </summary>
        private void CreateInlineButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            // UIButtonStyleHelper의 인라인 버튼 사용
            var btn = UIButtonStyleHelper.CreateInlineButton(
                parent,
                $"InlineBtn_{text}",
                text,
                textColor: new Color(1f, 0.8f, 0.4f, 1f),  // 밝은 오렌지
                bgColor: new Color(0.6f, 0.4f, 0f, 0.3f),  // 오렌지 톤
                fontSize: 16f
            );
            btn.onClick.AddListener(onClick);
        }

        private void OnCalibrateClicked()
        {
            if (calibrationManager == null || calibrationManager.IsRunning) return;

            // 이벤트 연결
            calibrationManager.OnStatusChanged -= UpdateCalibrationStatus;
            calibrationManager.OnStatusChanged += UpdateCalibrationStatus;
            calibrationManager.OnCalibrationComplete -= OnCalibrationDone;
            calibrationManager.OnCalibrationComplete += OnCalibrationDone;

            calibrationManager.StartCalibration();
            if (calibrationStatusText != null)
                calibrationStatusText.text = "시작 중...";
        }

        private void Update()
        {
            // 캘리브레이션 중 탭 입력 감지
            if (calibrationManager != null && calibrationManager.IsRunning)
            {
                bool tapped = Input.GetKeyDown(KeyCode.Space);
                if (!tapped)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        if (Input.GetTouch(i).phase == TouchPhase.Began)
                        {
                            tapped = true;
                            break;
                        }
                    }
                }
                if (tapped)
                    calibrationManager.RegisterTap();
            }
        }

        private void UpdateCalibrationStatus(string msg)
        {
            if (calibrationStatusText != null)
                calibrationStatusText.text = msg;
        }

        private void OnCalibrationDone(float offset)
        {
            // 슬라이더에 반영
            if (judgementOffsetSlider != null)
            {
                judgementOffsetSlider.value = offset * 1000f;
                if (judgementOffsetValueText != null)
                    judgementOffsetValueText.text = $"{offset * 1000f:F0}ms";
            }

            // 이벤트 해제
            calibrationManager.OnStatusChanged -= UpdateCalibrationStatus;
            calibrationManager.OnCalibrationComplete -= OnCalibrationDone;
        }

        /// <summary>
        /// 하단 버튼 영역 (기본값 복원 + 닫기)
        /// </summary>
        private void CreateButtons()
        {
            var buttonArea = new GameObject("ButtonArea");
            buttonArea.transform.SetParent(transform, false);

            var areaRect = buttonArea.AddComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0, 0);
            areaRect.anchorMax = new Vector2(1, 0);
            areaRect.pivot = new Vector2(0.5f, 0);
            areaRect.anchoredPosition = new Vector2(0, 20);
            areaRect.sizeDelta = new Vector2(-40, 60);

            var hLayout = buttonArea.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 20;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            // 기본값 복원 버튼
            CreateButton(buttonArea.transform, "초기화", OnResetClicked);

            // 닫기 버튼
            CreateButton(buttonArea.transform, "닫기", OnCloseClicked);
        }

        /// <summary>
        /// 네온 스타일 버튼 생성 (디자인 에셋 사용)
        /// </summary>
        private void CreateButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            // UIButtonStyleHelper를 사용하여 버튼 생성
            var btn = UIButtonStyleHelper.CreateStyledButton(parent, $"Btn_{text}", text,
                preferredHeight: 56f, fontSize: 22f);
            btn.onClick.AddListener(onClick);
        }

        /// <summary>
        /// 기본값 복원 버튼 클릭
        /// </summary>
        private void OnResetClicked()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ResetToDefaults();

                // 슬라이더 값 갱신
                var s = SettingsManager.Instance;
                if (noteSpeedSlider != null) noteSpeedSlider.value = s.NoteSpeed;
                if (judgementOffsetSlider != null) judgementOffsetSlider.value = s.JudgementOffset * 1000f;
                if (bgmVolumeSlider != null) bgmVolumeSlider.value = s.BGMVolume;
                if (sfxVolumeSlider != null) sfxVolumeSlider.value = s.SFXVolume;
                if (backgroundDimSlider != null) backgroundDimSlider.value = s.BackgroundDim;
            }
        }

        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void OnCloseClicked()
        {
            // 설정 저장
            SettingsManager.Instance?.SaveSettings();

            // MainMenuUI의 CloseSettings 호출
            if (mainMenuUI != null)
            {
                mainMenuUI.CloseSettings();
            }
            else
            {
                // MainMenuUI가 없으면 직접 비활성화
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 패널이 활성화될 때 최신 설정값으로 갱신
        /// </summary>
        private void OnEnable()
        {
            // 설정창을 최상위로 이동 (다른 UI 요소 위에 표시)
            transform.SetAsLastSibling();
            RefreshValues();
        }

        /// <summary>
        /// 슬라이더 값을 SettingsManager의 현재 값으로 갱신
        /// </summary>
        public void RefreshValues()
        {
            var s = SettingsManager.Instance;
            if (s == null) return;

            if (noteSpeedSlider != null)
            {
                noteSpeedSlider.SetValueWithoutNotify(s.NoteSpeed);
                if (noteSpeedValueText != null) noteSpeedValueText.text = s.NoteSpeed.ToString("F1");
            }
            if (judgementOffsetSlider != null)
            {
                judgementOffsetSlider.SetValueWithoutNotify(s.JudgementOffset * 1000f);
                if (judgementOffsetValueText != null) judgementOffsetValueText.text = $"{s.JudgementOffset * 1000f:F0}ms";
            }
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(s.BGMVolume);
                if (bgmVolumeValueText != null) bgmVolumeValueText.text = Mathf.RoundToInt(s.BGMVolume * 100f) + "%";
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(s.SFXVolume);
                if (sfxVolumeValueText != null) sfxVolumeValueText.text = Mathf.RoundToInt(s.SFXVolume * 100f) + "%";
            }
            if (backgroundDimSlider != null)
            {
                backgroundDimSlider.SetValueWithoutNotify(s.BackgroundDim);
                if (backgroundDimValueText != null) backgroundDimValueText.text = Mathf.RoundToInt(s.BackgroundDim * 100f) + "%";
            }
        }

        private void OnDestroy()
        {
            // 슬라이더 리스너 정리
            if (noteSpeedSlider != null) noteSpeedSlider.onValueChanged.RemoveAllListeners();
            if (judgementOffsetSlider != null) judgementOffsetSlider.onValueChanged.RemoveAllListeners();
            if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.RemoveAllListeners();
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            if (backgroundDimSlider != null) backgroundDimSlider.onValueChanged.RemoveAllListeners();

            // 캘리브레이션 이벤트 정리
            if (calibrationManager != null)
            {
                calibrationManager.OnStatusChanged -= UpdateCalibrationStatus;
                calibrationManager.OnCalibrationComplete -= OnCalibrationDone;
            }
        }
    }
}
