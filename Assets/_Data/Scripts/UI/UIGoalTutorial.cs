using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị Bảng Mục Tiêu Hành Trình tại ForestStartScene.
/// Khắc phục triệt để lỗi chữ trắng mờ chồng lên nhau bằng cách:
/// 1. Sử dụng duy nhất 1 TextMeshProUGUI với font hỗ trợ tiếng Việt (LiberationSans SDF), xóa mọi TMP_SubMeshUI / text trùng lặp.
/// 2. Đảm bảo CanvasGroup alpha = 1 và nâng SortingOrder của bảng mục tiêu lên trên cùng.
/// 3. Tự động ẩn Notification, SceneStoryOverlay và HUD tạm thời khi mở bảng mục tiêu.
/// </summary>
public class UIGoalTutorial : UIDisplay
{
    protected static UIGoalTutorial instance;
    public static UIGoalTutorial Instance => instance;

    private TMP_Text primaryGoalText;
    private CanvasGroup modalCanvasGroup;
    private Canvas modalCanvas;

    protected override void Awake()
    {
        base.Awake();
        if (UIGoalTutorial.instance != null && UIGoalTutorial.instance != this)
        {
            Debug.LogWarning("Only 1 UIGoalTutorial allowed to exist");
        }
        UIGoalTutorial.instance = this;

        this.ConfigureModalHierarchyAndSorting();
        this.PrepareSingleCleanGoalText();
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            this.Close();
        }
    }

    public override void Open()
    {
        // Hide all overlapping notifications, story overlays, and HUDs before showing the Goal Board
        NotificationEffect.HideAllNotifications();
        SceneStoryOverlay.DismissActive();
        LostTreasureHUD.SetTemporarilyHidden(true);

        this.ConfigureModalHierarchyAndSorting();
        this.PrepareSingleCleanGoalText();

        base.Open();

        if (this.modalCanvasGroup != null)
        {
            this.modalCanvasGroup.alpha = 1f;
            this.modalCanvasGroup.interactable = true;
            this.modalCanvasGroup.blocksRaycasts = true;
        }
    }

    public override void Close()
    {
        base.Close();
        LostTreasureHUD.SetTemporarilyHidden(false);
    }

    private void OnDisable()
    {
        LostTreasureHUD.SetTemporarilyHidden(false);
    }

    private void ConfigureModalHierarchyAndSorting()
    {
        // Ensure CanvasGroup has clean alpha = 1
        this.modalCanvasGroup = GetComponent<CanvasGroup>();
        if (this.modalCanvasGroup == null)
        {
            this.modalCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        this.modalCanvasGroup.alpha = 1f;

        // Ensure dedicated top-level sorting so no HUD or overlay renders over the Goal Board
        this.modalCanvas = GetComponent<Canvas>();
        if (this.modalCanvas == null)
        {
            this.modalCanvas = gameObject.AddComponent<Canvas>();
        }
        this.modalCanvas.overrideSorting = true;
        this.modalCanvas.sortingOrder = 260;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Style the backdrop and scroll background for optimal readability
        Transform dimOverlay = transform.Find("Image");
        if (dimOverlay != null)
        {
            Image dimImg = dimOverlay.GetComponent<Image>();
            if (dimImg != null)
            {
                dimImg.color = new Color(0.03f, 0.04f, 0.07f, 0.78f);
            }
        }

        Transform scrollBg = transform.Find("BackGround");
        if (scrollBg != null)
        {
            RectTransform scrollRect = scrollBg.GetComponent<RectTransform>();
            if (scrollRect != null)
            {
                scrollRect.anchoredPosition = Vector2.zero;
                scrollRect.sizeDelta = new Vector2(476f, 460f);
            }

            Image scrollImg = scrollBg.GetComponent<Image>();
            if (scrollImg != null)
            {
                scrollImg.color = Color.white;
            }
        }
    }

    private void PrepareSingleCleanGoalText()
    {
        // Destroy any legacy UnityEngine.UI.Text that might exist in children
        Text[] legacyTexts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < legacyTexts.Length; i++)
        {
            if (legacyTexts[i] != null)
            {
                legacyTexts[i].text = string.Empty;
                legacyTexts[i].enabled = false;
            }
        }

        // Find all TMP_Text components and keep only one primary text component
        TMP_Text[] allTmpTexts = GetComponentsInChildren<TMP_Text>(true);
        if (allTmpTexts != null && allTmpTexts.Length > 0)
        {
            this.primaryGoalText = allTmpTexts[0];
            for (int i = 1; i < allTmpTexts.Length; i++)
            {
                if (allTmpTexts[i] != null && allTmpTexts[i] != this.primaryGoalText)
                {
                    allTmpTexts[i].text = string.Empty;
                    allTmpTexts[i].gameObject.SetActive(false);
                }
            }
        }

        if (this.primaryGoalText == null) return;

        // Remove any stale TMP_SubMeshUI children left over from previous non-Vietnamese pixel font fallback
        TMP_SubMeshUI[] subMeshes = this.primaryGoalText.GetComponentsInChildren<TMP_SubMeshUI>(true);
        for (int i = 0; i < subMeshes.Length; i++)
        {
            if (subMeshes[i] != null)
            {
                Destroy(subMeshes[i].gameObject);
            }
        }

        // Assign clean Vietnamese-supported SDF font (LiberationSans SDF) so no fallback submesh is needed
        if (TMP_Settings.defaultFontAsset != null)
        {
            this.primaryGoalText.font = TMP_Settings.defaultFontAsset;
            this.primaryGoalText.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
        }

        // Remove any shadow/outline components on the text object that could cause ghosting
        Shadow[] shadows = this.primaryGoalText.GetComponents<Shadow>();
        for (int i = 0; i < shadows.Length; i++)
        {
            if (shadows[i] != null) shadows[i].enabled = false;
        }

        Outline[] outlines = this.primaryGoalText.GetComponents<Outline>();
        for (int i = 0; i < outlines.Length; i++)
        {
            if (outlines[i] != null) outlines[i].enabled = false;
        }

        RectTransform textRect = this.primaryGoalText.rectTransform;
        if (textRect != null)
        {
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0f, -2f);
            textRect.sizeDelta = new Vector2(292f, 278f);
            textRect.localScale = Vector3.one;
        }

        int collected = LostTreasureManager.Instance != null ? LostTreasureManager.Instance.TotalCollectedCount : 0;

        this.primaryGoalText.fontStyle = FontStyles.Normal;
        this.primaryGoalText.fontSize = 12.8f;
        this.primaryGoalText.lineSpacing = 8f;
        this.primaryGoalText.paragraphSpacing = 6f;
        this.primaryGoalText.alignment = TextAlignmentOptions.Center;
        this.primaryGoalText.textWrappingMode = TextWrappingModes.Normal;
        this.primaryGoalText.overflowMode = TextOverflowModes.Overflow;
        // High-contrast deep sepia ink on the golden parchment scroll (zero faint white text!)
        this.primaryGoalText.color = new Color(0.15f, 0.08f, 0.02f, 1f);

        this.primaryGoalText.text =
            "<b><size=14.5><color=#6E140E>✦ BẢNG MỤC TIÊU HÀNH TRÌNH ✦</color></size></b>\n" +
            "<color=#3A1F0B>Lần theo Tấm Bản Đồ Cổ vượt qua các vùng đất bí ẩn để thu thập đủ <b>4 Mảnh Chìa Khóa Cổ Đại</b>:</color>\n" +
            "<align=left><color=#241205>" +
            "• <b>Rừng Sao Cổ:</b> Mảnh Sao Rừng (1/4)\n" +
            "• <b>Rừng Hắc Ám:</b> Vượt bẫy gai & Thủ Lĩnh (2/4)\n" +
            "• <b>Hang Sâu Cổ Đại:</b> Mảnh Hầm Mỏ (3/4)\n" +
            "• <b>Đền Cổ Bị Nguyền:</b> Giải ấn & Guardian (4/4)</color></align>\n" +
            $"<color=#5A180C><b>Tiến độ hiện tại: {collected}/4 mảnh chìa khóa</b></color>\n" +
            "<size=11><color=#4A2A10><i>[ Bước ra ngoài hoặc nhấn ESC để đóng ]</i></color></size>";

        this.primaryGoalText.ForceMeshUpdate();
    }
}
