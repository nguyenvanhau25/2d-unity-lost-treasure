using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// A development-only scene picker. It is created automatically in the Editor
/// and Development Builds, and lists every enabled scene in Build Settings.
/// Press F1 to hide/show it while testing.
/// </summary>
public sealed class SceneQuickSelector : MonoBehaviour
{
    private const KeyCode ToggleKey = KeyCode.F1;
    private static SceneQuickSelector instance;

    private readonly List<string> scenePaths = new List<string>();
    private GameObject panel;
    private TMP_Dropdown dropdown;
    private bool isLoadingScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForTesting()
    {
        if (!Application.isEditor && !Debug.isDebugBuild) return;
        if (instance != null) return;

        GameObject root = new GameObject("SceneQuickSelector", typeof(RectTransform), typeof(SceneQuickSelector));
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        this.BuildUi();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += this.OnSceneLoaded;
    }

    private void Start()
    {
        this.RefreshSceneOptions();
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey) && this.panel != null)
            this.panel.SetActive(!this.panel.activeSelf);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= this.OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        this.EnsureEventSystem();
        this.RefreshSceneOptions();
    }

    private void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        this.panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
        this.panel.transform.SetParent(transform, false);
        RectTransform panelRect = this.panel.GetComponent<RectTransform>();
        // The player health UI owns the top-left corner. Keep the development
        // scene picker in the opposite corner so the two HUDs never overlap.
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-24f, -24f);
        panelRect.sizeDelta = new Vector2(390f, 132f);

        Image panelImage = this.panel.GetComponent<Image>();
        panelImage.color = new Color(0.055f, 0.024f, 0.12f, 0.94f);
        Shadow shadow = this.panel.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(4f, -5f);

        Image accent = CreateImage(this.panel.transform, "Accent", new Color(1f, 0.71f, 0.18f, 1f));
        Stretch(accent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(7f, 0f));

        TextMeshProUGUI title = CreateText(this.panel.transform, "Title", 21f, FontStyles.Bold, TextAlignmentOptions.Left);
        title.text = "SCENE TESTER";
        title.color = new Color(1f, 0.79f, 0.29f, 1f);
        Stretch(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -34f), new Vector2(-20f, -6f));

        TextMeshProUGUI hint = CreateText(this.panel.transform, "Hint", 13f, FontStyles.Normal, TextAlignmentOptions.Left);
        hint.text = "Chọn màn để test nhanh  •  F1: Ẩn/Hiện";
        hint.color = new Color(0.83f, 0.77f, 0.95f, 1f);
        Stretch(hint.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -55f), new Vector2(-20f, -31f));

        this.dropdown = this.CreateDropdown(this.panel.transform);
        RectTransform dropdownRect = this.dropdown.GetComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0f, 0f);
        dropdownRect.anchorMax = new Vector2(1f, 0f);
        dropdownRect.pivot = new Vector2(0f, 0f);
        dropdownRect.anchoredPosition = new Vector2(20f, 15f);
        dropdownRect.sizeDelta = new Vector2(-112f, 40f);
        this.dropdown.onValueChanged.AddListener(this.LoadSelectedScene);

        Button reloadButton = CreateButton(this.panel.transform, "Reload", "R", new Color(0.35f, 0.16f, 0.52f, 1f));
        RectTransform reloadRect = reloadButton.GetComponent<RectTransform>();
        reloadRect.anchorMin = new Vector2(1f, 0f);
        reloadRect.anchorMax = new Vector2(1f, 0f);
        reloadRect.pivot = new Vector2(1f, 0f);
        reloadRect.anchoredPosition = new Vector2(-20f, 15f);
        reloadRect.sizeDelta = new Vector2(62f, 40f);
        reloadButton.onClick.AddListener(this.ReloadCurrentScene);

        this.EnsureEventSystem();
    }

    private void RefreshSceneOptions()
    {
        if (this.dropdown == null) return;

        this.scenePaths.Clear();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        int selectedIndex = 0;
        string activePath = SceneManager.GetActiveScene().path;

        List<string> rawPaths = new List<string>();
        for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(index);
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                rawPaths.Add(scenePath);
            }
        }

        rawPaths.Sort((a, b) =>
        {
            string nameA = Path.GetFileNameWithoutExtension(a);
            string nameB = Path.GetFileNameWithoutExtension(b);
            return LostTreasureSceneHelper.GetStageOrder(nameA).CompareTo(LostTreasureSceneHelper.GetStageOrder(nameB));
        });

        for (int i = 0; i < rawPaths.Count; i++)
        {
            string scenePath = rawPaths[i];
            string rawName = Path.GetFileNameWithoutExtension(scenePath);
            this.scenePaths.Add(scenePath);
            options.Add(new TMP_Dropdown.OptionData(LostTreasureSceneHelper.GetMapDisplayName(rawName)));
            if (scenePath == activePath) selectedIndex = this.scenePaths.Count - 1;
        }

        this.dropdown.SetValueWithoutNotify(0);
        this.dropdown.ClearOptions();
        this.dropdown.AddOptions(options);
        this.ResizeDropdownList(options.Count);
        if (this.scenePaths.Count > 0) this.dropdown.SetValueWithoutNotify(selectedIndex);
        this.dropdown.RefreshShownValue();
    }

    private void ResizeDropdownList(int optionCount)
    {
        if (this.dropdown == null || this.dropdown.template == null) return;

        // Every scene must be inside the ScrollRect viewport. The old fixed
        // height clipped the final item (InsideCave), making it visible but
        // impossible to click.
        const float itemHeight = 36f;
        const float spacing = 3f;
        const float verticalPadding = 8f;
        const float maxListHeight = 340f;
        float requiredHeight = optionCount * itemHeight + Mathf.Max(0, optionCount - 1) * spacing + verticalPadding;
        Vector2 size = this.dropdown.template.sizeDelta;
        size.y = Mathf.Min(maxListHeight, Mathf.Max(72f, requiredHeight));
        this.dropdown.template.sizeDelta = size;
    }

    private void LoadSelectedScene(int optionIndex)
    {
        if (this.isLoadingScene || optionIndex < 0 || optionIndex >= this.scenePaths.Count) return;
        string selectedPath = this.scenePaths[optionIndex];
        if (selectedPath == SceneManager.GetActiveScene().path) return;

        this.isLoadingScene = true;
        SceneManager.LoadScene(selectedPath);
        this.isLoadingScene = false;
    }

    private void ReloadCurrentScene()
    {
        if (this.isLoadingScene) return;
        this.isLoadingScene = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        this.isLoadingScene = false;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        GameObject eventSystemObject = new GameObject("SceneQuickSelectorEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }

    private TMP_Dropdown CreateDropdown(Transform parent)
    {
        GameObject dropdownObject = new GameObject("SceneDropdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
        dropdownObject.transform.SetParent(parent, false);
        Image background = dropdownObject.GetComponent<Image>();
        background.color = new Color(0.17f, 0.075f, 0.29f, 1f);

        TMP_Dropdown result = dropdownObject.GetComponent<TMP_Dropdown>();
        result.targetGraphic = background;

        TextMeshProUGUI caption = CreateText(dropdownObject.transform, "Label", 17f, FontStyles.Bold, TextAlignmentOptions.Left);
        caption.color = Color.white;
        Stretch(caption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 0f), new Vector2(-42f, 0f));
        result.captionText = caption;

        Image arrow = CreateImage(dropdownObject.transform, "Arrow", new Color(1f, 0.75f, 0.24f, 1f));
        arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        arrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        arrow.rectTransform.anchoredPosition = new Vector2(-20f, 0f);
        arrow.rectTransform.sizeDelta = new Vector2(13f, 13f);
        arrow.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        RectTransform template = this.CreateDropdownTemplate(dropdownObject.transform, out TextMeshProUGUI itemText);
        result.template = template;
        result.itemText = itemText;
        return result;
    }

    private RectTransform CreateDropdownTemplate(Transform parent, out TextMeshProUGUI itemText)
    {
        GameObject templateObject = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        templateObject.transform.SetParent(parent, false);
        RectTransform template = templateObject.GetComponent<RectTransform>();
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, -4f);
        template.sizeDelta = new Vector2(0f, 238f);
        templateObject.GetComponent<Image>().color = new Color(0.10f, 0.035f, 0.19f, 1f);

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(template, false);
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = Color.white;
        viewportImage.enabled = false;
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(viewportObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 3f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject itemObject = new GameObject("Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle), typeof(LayoutElement));
        itemObject.transform.SetParent(content, false);
        Image itemBackground = itemObject.GetComponent<Image>();
        itemBackground.color = new Color(0.22f, 0.10f, 0.37f, 1f);
        Toggle itemToggle = itemObject.GetComponent<Toggle>();
        itemToggle.targetGraphic = itemBackground;
        itemObject.GetComponent<LayoutElement>().preferredHeight = 36f;

        Image checkmark = CreateImage(itemObject.transform, "Item Checkmark", new Color(1f, 0.72f, 0.18f, 1f));
        checkmark.rectTransform.anchorMin = checkmark.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        checkmark.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        checkmark.rectTransform.anchoredPosition = new Vector2(15f, 0f);
        checkmark.rectTransform.sizeDelta = new Vector2(9f, 18f);
        itemToggle.graphic = checkmark;

        itemText = CreateText(itemObject.transform, "Item Label", 16f, FontStyles.Normal, TextAlignmentOptions.Left);
        itemText.color = Color.white;
        Stretch(itemText.rectTransform, Vector2.zero, Vector2.one, new Vector2(31f, 0f), new Vector2(-8f, 0f));

        ScrollRect scrollRect = templateObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportObject.GetComponent<RectTransform>();
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        templateObject.SetActive(false);
        return template;
    }

    private static Button CreateButton(Transform parent, string objectName, string text, Color color)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI label = CreateText(buttonObject.transform, "Label", 22f, FontStyles.Bold, TextAlignmentOptions.Center);
        label.text = text;
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static Image CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string objectName, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
