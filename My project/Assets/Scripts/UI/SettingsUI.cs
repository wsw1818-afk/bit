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
        // 네온 스타일 색상
        private static readonly Color BG_COLOR = new Color(0.02f, 0.02f, 0.08f, 0.95f);
        private static readonly Color BORDER_COLOR = new Color(0f, 0.8f, 1f, 0.5f);
        private static readonly Color LABEL_COLOR = new Color(0f, 0.9f, 1f, 1f);
        private static readonly Color VALUE_COLOR = Color.white;
        private static readonly Color TITLE_COLOR = new Color(0f, 1f, 1f, 1f);
        private static readonly Color BUTTON_BG = new Color(0.05f, 0.05f, 0.15f, 0.9f);
        private static readonly Color BUTTON_TEXT_COLOR = new Color(0f, 0.9f, 1f, 1f);
        private static readonly Color SLIDER_BG_COLOR = new Color(0.1f, 0.1f, 0.2f, 0.8f);
        private static readonly Color SLIDER_FILL_COLOR = new Color(0f, 0.7f, 1f, 0.8f);
        private static readonly Color SLIDER_HANDLE_COLOR = new Color(0f, 1f, 1f, 1f);

        // 슬라이더 참조 (기본값 복원 시 사용)
        private Slider noteSpeedSlider;
        private Slider judgementOffsetSlider;
        private Slider bgmVolumeSlider;
        private Slider sfxVolumeSlider;
        private Slider backgroundDimSlider;

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

            // 패널 배경 설정
            var panelImage = GetComponent<Image>();
            if (panelImage == null)
                panelImage = gameObject.AddComponent<Image>();
            panelImage.color = BG_COLOR;

            // 네온 테두리
            var outline = GetComponent<Outline>();
            if (outline == null)
                outline = gameObject.AddComponent<Outline>();
            outline.effectColor = BORDER_COLOR;
            outline.effectDistance = new Vector2(2, -2);

            // RectTransform 설정 (화면 중앙, 적절한 크기)
            var panelRect = GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.1f);
            panelRect.anchorMax = new Vector2(0.95f, 0.9f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;

            // 스크롤 가능한 콘텐츠 영역
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(20, 70); // 하단 버튼 영역 확보
            contentRect.offsetMax = new Vector2(-20, -20);

            // VerticalLayoutGroup으로 항목 정렬
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // ContentSizeFitter 추가
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 타이틀
            CreateTitle(contentGo.transform, "SETTINGS");

            // 구분선
            CreateSeparator(contentGo.transform);

            // SettingsManager에서 현재 값 로드
            var settings = SettingsManager.Instance;
            float currentNoteSpeed = settings != null ? settings.NoteSpeed : 5.0f;
            float currentOffset = settings != null ? settings.JudgementOffset : 0f;
            float currentBGM = settings != null ? settings.BGMVolume : 0.8f;
            float currentSFX = settings != null ? settings.SFXVolume : 0.8f;
            float currentDim = settings != null ? settings.BackgroundDim : 0.5f;

            // 노트 속도 슬라이더 (1.0 ~ 10.0, 0.5 단위)
            noteSpeedSlider = CreateSliderRow(contentGo.transform, "NOTE SPEED",
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
            judgementOffsetSlider = CreateSliderRow(contentGo.transform, "JUDGEMENT OFFSET",
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

            // BGM 볼륨 슬라이더
            bgmVolumeSlider = CreateSliderRow(contentGo.transform, "BGM VOLUME",
                0f, 1f, currentBGM, out bgmVolumeValueText,
                (val) =>
                {
                    bgmVolumeValueText.text = Mathf.RoundToInt(val * 100f) + "%";
                    if (SettingsManager.Instance != null)
                        SettingsManager.Instance.BGMVolume = val;
                },
                "%");

            // SFX 볼륨 슬라이더
            sfxVolumeSlider = CreateSliderRow(contentGo.transform, "SFX VOLUME",
                0f, 1f, currentSFX, out sfxVolumeValueText,
                (val) =>
                {
                    sfxVolumeValueText.text = Mathf.RoundToInt(val * 100f) + "%";
                    if (SettingsManager.Instance != null)
                        SettingsManager.Instance.SFXVolume = val;
                },
                "%");

            // 배경 밝기 슬라이더
            backgroundDimSlider = CreateSliderRow(contentGo.transform, "BACKGROUND DIM",
                0f, 1f, currentDim, out backgroundDimValueText,
                (val) =>
                {
                    backgroundDimValueText.text = Mathf.RoundToInt(val * 100f) + "%";
                    if (SettingsManager.Instance != null)
                        SettingsManager.Instance.BackgroundDim = val;
                },
                "%");

            // 초기 값 표시 업데이트
            noteSpeedValueText.text = currentNoteSpeed.ToString("F1");
            judgementOffsetValueText.text = $"{currentOffset * 1000f:F0}ms";
            bgmVolumeValueText.text = Mathf.RoundToInt(currentBGM * 100f) + "%";
            sfxVolumeValueText.text = Mathf.RoundToInt(currentSFX * 100f) + "%";
            backgroundDimValueText.text = Mathf.RoundToInt(currentDim * 100f) + "%";

            // 하단 버튼 영역
            CreateButtons();
        }

        /// <summary>
        /// 타이틀 텍스트 생성
        /// </summary>
        private void CreateTitle(Transform parent, string text)
        {
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(parent, false);

            var rect = titleGo.AddComponent<RectTransform>();
            var layoutElem = titleGo.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 40;

            var tmp = titleGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = TITLE_COLOR;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// 구분선 생성
        /// </summary>
        private void CreateSeparator(Transform parent)
        {
            var sepGo = new GameObject("Separator");
            sepGo.transform.SetParent(parent, false);

            var rect = sepGo.AddComponent<RectTransform>();
            var layoutElem = sepGo.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 2;

            var img = sepGo.AddComponent<Image>();
            img.color = new Color(0f, 0.8f, 1f, 0.3f);
        }

        /// <summary>
        /// 슬라이더 행 생성 (라벨 + 슬라이더 + 값 표시)
        /// </summary>
        private Slider CreateSliderRow(Transform parent, string label,
            float min, float max, float currentValue,
            out TMP_Text valueText, UnityEngine.Events.UnityAction<float> onChanged,
            string suffix = "", bool wholeNumbers = false)
        {
            // 행 컨테이너
            var rowGo = new GameObject($"Setting_{label.Replace(" ", "")}");
            rowGo.transform.SetParent(parent, false);

            var rowRect = rowGo.AddComponent<RectTransform>();
            var rowLayout = rowGo.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 60;

            var vertLayout = rowGo.AddComponent<VerticalLayoutGroup>();
            vertLayout.padding = new RectOffset(5, 5, 2, 2);
            vertLayout.spacing = 2;
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = true;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childForceExpandHeight = false;

            // 상단: 라벨 + 값 텍스트 (가로 배치)
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(rowGo.transform, false);

            var headerRect = headerGo.AddComponent<RectTransform>();
            var headerLayout = headerGo.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 24;

            var hLayout = headerGo.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            // 라벨
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(headerGo.transform, false);
            labelGo.AddComponent<RectTransform>();
            var labelLayout2 = labelGo.AddComponent<LayoutElement>();
            labelLayout2.flexibleWidth = 1;
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 18;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.color = LABEL_COLOR;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // 값 표시 텍스트
            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(headerGo.transform, false);
            valueGo.AddComponent<RectTransform>();
            var valueLayout2 = valueGo.AddComponent<LayoutElement>();
            valueLayout2.preferredWidth = 80;
            var valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
            valueTmp.fontSize = 18;
            valueTmp.fontStyle = FontStyles.Bold;
            valueTmp.color = VALUE_COLOR;
            valueTmp.alignment = TextAlignmentOptions.MidlineRight;
            valueText = valueTmp;

            // 슬라이더
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(rowGo.transform, false);

            var sliderRect = sliderGo.AddComponent<RectTransform>();
            var sliderLayout = sliderGo.AddComponent<LayoutElement>();
            sliderLayout.preferredHeight = 36;

            // 슬라이더 배경
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = SLIDER_BG_COLOR;

            // 슬라이더 필 영역
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

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
            handleRect.sizeDelta = new Vector2(32, 32);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = SLIDER_HANDLE_COLOR;

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

            // 슬라이더 색상 전환 설정
            var colors = slider.colors;
            colors.normalColor = SLIDER_HANDLE_COLOR;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0f, 0.6f, 0.8f, 1f);
            slider.colors = colors;

            return slider;
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
            areaRect.anchoredPosition = new Vector2(0, 15);
            areaRect.sizeDelta = new Vector2(-40, 56);

            var hLayout = buttonArea.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 20;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            // 기본값 복원 버튼
            CreateButton(buttonArea.transform, "RESET", OnResetClicked);

            // 닫기 버튼
            CreateButton(buttonArea.transform, "CLOSE", OnCloseClicked);
        }

        /// <summary>
        /// 네온 스타일 버튼 생성
        /// </summary>
        private void CreateButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject($"Btn_{text}");
            btnGo.transform.SetParent(parent, false);

            var btnRect = btnGo.AddComponent<RectTransform>();

            // 버튼 배경
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = BUTTON_BG;

            // 네온 테두리
            var btnOutline = btnGo.AddComponent<Outline>();
            btnOutline.effectColor = BORDER_COLOR;
            btnOutline.effectDistance = new Vector2(1, -1);

            // Button 컴포넌트
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(onClick);

            var colors = btn.colors;
            colors.normalColor = BUTTON_BG;
            colors.highlightedColor = new Color(0.1f, 0.1f, 0.25f, 0.9f);
            colors.pressedColor = new Color(0f, 0.3f, 0.5f, 0.9f);
            btn.colors = colors;

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
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = BUTTON_TEXT_COLOR;
            tmp.alignment = TextAlignmentOptions.Center;
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
        }
    }
}
