using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Dedicated HUD for "Lost Treasure".
/// Displays the 4 Key Fragments, Master Key crafting status, and current objective.
/// Auto-created on game scenes.
/// </summary>
public class LostTreasureHUD : MonoBehaviour
{
    private static LostTreasureHUD instance;
    public static LostTreasureHUD Instance => instance;

    private GameObject rootCanvas;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI masterKeyStatusText;
    private Image[] fragmentSlots = new Image[4];

    private readonly Color emptySlotColor = new Color(0.2f, 0.22f, 0.25f, 0.75f);
    private readonly Color activeSlotColor = new Color(1f, 0.85f, 0.25f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateOnScene()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "MainMenu" || scene == "BeginScreen") return;

        if (instance == null)
        {
            GameObject hudObj = new GameObject("LostTreasureHUD");
            instance = hudObj.AddComponent<LostTreasureHUD>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        this.BuildHUD();
    }

    private void Start()
    {
        this.UpdateHUD();
        if (LostTreasureManager.Instance != null)
        {
            LostTreasureManager.Instance.OnFragmentCollectedEvent += this.OnFragmentCollected;
            LostTreasureManager.Instance.OnMasterKeyCraftedEvent += this.OnMasterKeyCrafted;
        }
    }

    private void OnDestroy()
    {
        if (LostTreasureManager.Instance != null)
        {
            LostTreasureManager.Instance.OnFragmentCollectedEvent -= this.OnFragmentCollected;
            LostTreasureManager.Instance.OnMasterKeyCraftedEvent -= this.OnMasterKeyCrafted;
        }

        if (instance == this) instance = null;
    }

    private void Update()
    {
        // Allow toggle HUD with H key
        if (Input.GetKeyDown(KeyCode.H) && this.rootCanvas != null)
        {
            this.rootCanvas.SetActive(!this.rootCanvas.activeSelf);
        }
    }

    private void OnFragmentCollected(int index, string name)
    {
        this.UpdateHUD();
        StartCoroutine(this.PulseSlot(index));
    }

    private void OnMasterKeyCrafted()
    {
        this.UpdateHUD();
    }

    public void UpdateHUD()
    {
        if (LostTreasureManager.Instance == null) return;

        int count = LostTreasureManager.Instance.TotalCollectedCount;
        bool isMasterKey = LostTreasureManager.Instance.IsMasterKeyCrafted;

        // Update slots
        for (int i = 0; i < 4; i++)
        {
            if (this.fragmentSlots[i] != null)
            {
                bool hasIt = LostTreasureManager.Instance.HasFragment(i);
                this.fragmentSlots[i].color = hasIt ? this.activeSlotColor : this.emptySlotColor;
            }
        }

        // Master Key status
        if (this.masterKeyStatusText != null)
        {
            if (isMasterKey)
            {
                this.masterKeyStatusText.text = "★ CHÌA KHÓA HOÀN CHỈNH ★";
                this.masterKeyStatusText.color = new Color(1f, 0.9f, 0.35f);
            }
            else
            {
                this.masterKeyStatusText.text = $"MẢNH CHÌA KHÓA: {count}/4";
                this.masterKeyStatusText.color = new Color(0.85f, 0.85f, 0.85f);
            }
        }

        // Update dynamic objective text based on current scene
        if (this.objectiveText != null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            switch (sceneName)
            {
                case "ForestStartScene":
                    this.objectiveText.text = count > 0 ? "Tiến đến cổng phong ấn sang Rừng Hắc Ám" : "Khám phá rừng và thu thập Mảnh Chìa Khóa 1";
                    break;
                case "DarkForestScene":
                    this.objectiveText.text = count > 1 ? "Tiến vào Hang Động Cổ Xưa" : "Vượt qua cạm bẫy quái vật & tìm Mảnh Chìa Khóa 2";
                    break;
                case "InsideCave":
                    this.objectiveText.text = count > 2 ? "Tiến vào Chiến Trường Hộ Vệ" : "Giải mã cơ quan trong hang & tìm Mảnh Chìa Khóa 3";
                    break;
                case "LastWar":
                    this.objectiveText.text = "Đánh bại Đại Hộ Vệ rồi tiến vào Đền Cổ Bị Nguyền";
                    break;
                case "RecusePrincess":
                    this.objectiveText.text = "Khám phá Đền Cổ Bị Nguyền, vượt qua cạm bẫy và Guardian";
                    break;
                case "Special":
                    this.objectiveText.text = "Tiêu diệt hộ vệ cuối cùng và mở Rương Kho Báu Bí Ẩn";
                    break;
                default:
                    this.objectiveText.text = "Lần theo bản đồ cổ để tìm kiếm kho báu";
                    break;
            }
        }
    }

    private IEnumerator PulseSlot(int index)
    {
        if (index < 0 || index >= this.fragmentSlots.Length) yield break;
        Image slot = this.fragmentSlots[index];
        if (slot == null) yield break;

        Vector3 orig = slot.transform.localScale;
        for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            float s = 1f + Mathf.Sin(t / 0.4f * Mathf.PI) * 0.45f;
            slot.transform.localScale = orig * s;
            yield return null;
        }
        slot.transform.localScale = orig;
    }

    private void BuildHUD()
    {
        // Canvas
        GameObject canvasObj = new GameObject("LostTreasureCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform);
        this.rootCanvas = canvasObj;

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Panel Container (Top-Right)
        GameObject panelObj = new GameObject("TreasurePanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-25f, -25f);
        panelRect.sizeDelta = new Vector2(340f, 95f);

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.65f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = Vector2.zero;

        this.titleText = titleObj.GetComponent<TextMeshProUGUI>();
        this.titleText.text = "✦ LOST TREASURE ✦";
        this.titleText.fontSize = 17;
        this.titleText.fontStyle = FontStyles.Bold;
        this.titleText.alignment = TextAlignmentOptions.Center;
        this.titleText.color = new Color(1f, 0.86f, 0.35f);

        // Slots container
        GameObject slotsContainer = new GameObject("Slots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        slotsContainer.transform.SetParent(panelObj.transform, false);
        RectTransform slotsRect = slotsContainer.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.1f, 0.32f);
        slotsRect.anchorMax = new Vector2(0.9f, 0.65f);
        slotsRect.anchoredPosition = Vector2.zero;
        slotsRect.sizeDelta = Vector2.zero;

        HorizontalLayoutGroup layout = slotsContainer.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 15;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Sprite slotSprite = CreateKeySlotSprite();
        for (int i = 0; i < 4; i++)
        {
            GameObject slotObj = new GameObject($"Slot_{i + 1}", typeof(RectTransform), typeof(Image));
            slotObj.transform.SetParent(slotsContainer.transform, false);
            RectTransform sRect = slotObj.GetComponent<RectTransform>();
            sRect.sizeDelta = new Vector2(24, 24);

            Image sImg = slotObj.GetComponent<Image>();
            sImg.sprite = slotSprite;
            sImg.color = this.emptySlotColor;
            this.fragmentSlots[i] = sImg;
        }

        // Master Key & Objective Text
        GameObject subObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(panelObj.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0.32f);
        subRect.anchoredPosition = Vector2.zero;
        subRect.sizeDelta = Vector2.zero;

        this.masterKeyStatusText = subObj.GetComponent<TextMeshProUGUI>();
        this.masterKeyStatusText.fontSize = 12;
        this.masterKeyStatusText.alignment = TextAlignmentOptions.Center;
        this.masterKeyStatusText.color = Color.white;

        // Objective text below panel
        GameObject objObj = new GameObject("ObjectiveText", typeof(RectTransform), typeof(TextMeshProUGUI));
        objObj.transform.SetParent(panelObj.transform, false);
        RectTransform objRect = objObj.GetComponent<RectTransform>();
        objRect.anchorMin = new Vector2(0f, -0.45f);
        objRect.anchorMax = new Vector2(1f, 0f);
        objRect.anchoredPosition = Vector2.zero;
        objRect.sizeDelta = Vector2.zero;

        this.objectiveText = objObj.GetComponent<TextMeshProUGUI>();
        this.objectiveText.fontSize = 11;
        this.objectiveText.alignment = TextAlignmentOptions.Center;
        this.objectiveText.color = new Color(0.85f, 0.85f, 0.7f, 0.9f);
    }

    private static Sprite CreateKeySlotSprite()
    {
        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);
        Color fill = Color.white;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int dist = Mathf.Abs(x - 8) + Mathf.Abs(y - 8);
                tex.SetPixel(x, y, dist <= 6 ? fill : clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }
}
