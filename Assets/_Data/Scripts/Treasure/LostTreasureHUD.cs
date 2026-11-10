using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Dedicated HUD for "Lost Treasure".
/// Displays the 4 Key Fragments and Master Key status right next to the Player's HP Bar (top-left).
/// Persists across all scenes via DontDestroyOnLoad + SceneManager.sceneLoaded.
/// </summary>
public class LostTreasureHUD : MonoBehaviour
{
    private static LostTreasureHUD instance;
    private static bool sceneHookRegistered;
    public static LostTreasureHUD Instance => instance;

    private bool temporarilyHiddenByModal;
    private GameObject rootCanvas;
    private TextMeshProUGUI masterKeyStatusText;
    private TextMeshProUGUI objectiveText;
    private readonly Image[] fragmentSlotBackgrounds = new Image[4];
    private readonly Image[] fragmentKeyIcons = new Image[4];
    private readonly TextMeshProUGUI[] fragmentSlotLabels = new TextMeshProUGUI[4];

    private readonly Color emptySlotBgColor = new Color(0.14f, 0.16f, 0.22f, 0.92f);
    private readonly Color activeSlotBgColor = new Color(0.42f, 0.30f, 0.06f, 0.98f);
    private readonly Color emptyKeyIconColor = new Color(0.45f, 0.48f, 0.55f, 0.40f);
    private readonly Color activeKeyIconColor = new Color(1f, 0.88f, 0.22f, 1f);

    public static void SetTemporarilyHidden(bool hidden)
    {
        if (instance == null) return;
        instance.temporarilyHiddenByModal = hidden;
        instance.RefreshVisibilityForScene(SceneManager.GetActiveScene().name);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateOnScene()
    {
        if (!sceneHookRegistered)
        {
            sceneHookRegistered = true;
            SceneManager.sceneLoaded += OnSceneLoadedStatic;
        }

        EnsureHUDExists();
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
    {
        EnsureHUDExists();
        if (instance != null)
        {
            instance.temporarilyHiddenByModal = false;
            instance.RefreshVisibilityForScene(scene.name);
            instance.UpdateHUD();
        }
    }

    private static void EnsureHUDExists()
    {
        if (instance == null)
        {
            GameObject hudObj = new GameObject("LostTreasureHUD");
            instance = hudObj.AddComponent<LostTreasureHUD>();
            DontDestroyOnLoad(hudObj);
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
        DontDestroyOnLoad(gameObject);
        this.BuildHUD();
        this.RefreshVisibilityForScene(SceneManager.GetActiveScene().name);
    }

    private void Start()
    {
        this.UpdateHUD();
        if (LostTreasureManager.Instance != null)
        {
            LostTreasureManager.Instance.OnFragmentCollectedEvent -= this.OnFragmentCollected;
            LostTreasureManager.Instance.OnFragmentCollectedEvent += this.OnFragmentCollected;
            LostTreasureManager.Instance.OnMasterKeyCraftedEvent -= this.OnMasterKeyCrafted;
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

    private void RefreshVisibilityForScene(string sceneName)
    {
        if (this.rootCanvas == null) return;
        bool isMenu = sceneName == "MainMenu" || sceneName == "BeginScreen";
        this.rootCanvas.SetActive(!isMenu && !this.temporarilyHiddenByModal);
    }

    private void OnFragmentCollected(int index, string fragmentName)
    {
        this.UpdateHUD();
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(this.PulseSlot(index));
        }
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

        for (int i = 0; i < 4; i++)
        {
            bool hasIt = LostTreasureManager.Instance.HasFragment(i);
            if (this.fragmentSlotBackgrounds[i] != null)
            {
                this.fragmentSlotBackgrounds[i].color = hasIt ? this.activeSlotBgColor : this.emptySlotBgColor;
            }
            if (this.fragmentKeyIcons[i] != null)
            {
                this.fragmentKeyIcons[i].color = hasIt ? this.activeKeyIconColor : this.emptyKeyIconColor;
            }
            if (this.fragmentSlotLabels[i] != null)
            {
                this.fragmentSlotLabels[i].text = hasIt ? $"✓{i + 1}" : $"{i + 1}";
                this.fragmentSlotLabels[i].color = hasIt
                    ? new Color(0.45f, 1f, 0.55f, 1f)
                    : new Color(0.68f, 0.72f, 0.78f, 0.75f);
            }
        }

        if (this.masterKeyStatusText != null)
        {
            if (isMasterKey)
            {
                this.masterKeyStatusText.text = "🔑 ĐỦ 4/4\n<size=8><color=#66FF88>ĐÃ GHÉP</color></size>";
                this.masterKeyStatusText.color = new Color(1f, 0.92f, 0.32f, 1f);
            }
            else
            {
                this.masterKeyStatusText.text = $"🔑 CHÌA KHÓA\n<size=11><b>{count} / 4</b></size>";
                this.masterKeyStatusText.color = new Color(1f, 0.90f, 0.55f, 1f);
            }
        }

        if (this.objectiveText != null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (LostTreasureSceneHelper.IsForestStart(sceneName))
            {
                this.objectiveText.text = LostTreasureManager.Instance.HasFragment(0)
                    ? "✔ [Rừng Sao] Đã có Mảnh 1/4 — Vào cổng sang Rừng Hắc Ám"
                    : "➤ [Màn 1: Rừng Sao] Nhặt Mảnh Chìa Khóa 1/4 trước cổng";
            }
            else if (LostTreasureSceneHelper.IsDarkForest(sceneName))
            {
                this.objectiveText.text = LostTreasureManager.Instance.HasFragment(1)
                    ? "✔ [Rừng Hắc Ám] Đã có Mảnh 2/4 — Bước vào cửa Hang Động"
                    : "➤ [Màn 2: Rừng Hắc Ám] Vượt bẫy lên đồi nhặt Mảnh 2/4";
            }
            else if (LostTreasureSceneHelper.IsAncientCave(sceneName))
            {
                this.objectiveText.text = LostTreasureManager.Instance.HasFragment(2)
                    ? "✔ [Hang Động Cổ] Đã có Mảnh 3/4 — Tiến sang Chiến Trường"
                    : "➤ [Màn 3: Hang Động Cổ] Lên tầng 3 nhặt Mảnh Chìa Khóa 3/4";
            }
            else if (LostTreasureSceneHelper.IsGuardianBattlefield(sceneName))
            {
                this.objectiveText.text = "➤ [Màn 4: Chiến Trường Cổ] Vượt Đại Hộ Vệ & vào cổng tới Đền Cổ";
            }
            else if (LostTreasureSceneHelper.IsCursedTemple(sceneName))
            {
                this.objectiveText.text = LostTreasureManager.Instance.HasFragment(3)
                    ? "✔ [Đền Cổ] Đủ 4/4 Mảnh — Bước lên Bệ Thờ sang Tháp Kho Báu"
                    : "➤ [Màn 5: Đền Cổ] Giải 3 Câu Đố mở Cửa Phong Ấn & hạ Guardian";
            }
            else if (LostTreasureSceneHelper.IsTreasureSanctum(sceneName))
            {
                this.objectiveText.text = "➤ [Màn 6: Tháp Kho Báu] Hạ Hộ Vệ & mở Rương Kho Báu trên Tháp";
            }
            else
            {
                this.objectiveText.text = "➤ Thu thập đủ 4 Mảnh Chìa Khóa Cổ Đại";
            }
        }
    }

    private IEnumerator PulseSlot(int index)
    {
        if (index < 0 || index >= this.fragmentSlotBackgrounds.Length) yield break;
        Image slot = this.fragmentSlotBackgrounds[index];
        if (slot == null) yield break;

        Vector3 orig = Vector3.one;
        for (float t = 0; t < 0.45f; t += Time.unscaledDeltaTime)
        {
            if (slot == null) yield break;
            float s = 1f + Mathf.Sin(t / 0.45f * Mathf.PI) * 0.38f;
            slot.transform.localScale = orig * s;
            yield return null;
        }
        if (slot != null) slot.transform.localScale = orig;
    }

    private void BuildHUD()
    {
        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

        // Match HpBarPlayer's CanvasScaler (800x600, matchWidthOrHeight = 0) so our panel sits right next to the HP bar!
        GameObject canvasObj = new GameObject("LostTreasureCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        this.rootCanvas = canvasObj;

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0f;

        // Main bar positioned directly to the right of HpBarPlayer (HpBarPlayer slider ends at x = 195, y = -22)
        GameObject borderObj = new GameObject("KeyFragmentsBarBorder", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(canvasObj.transform, false);
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0f, 1f);
        borderRect.anchorMax = new Vector2(0f, 1f);
        borderRect.pivot = new Vector2(0f, 0.5f);
        borderRect.anchoredPosition = new Vector2(202f, -23f);
        borderRect.sizeDelta = new Vector2(218f, 36f);

        Image borderImg = borderObj.GetComponent<Image>();
        borderImg.color = new Color(0.88f, 0.70f, 0.24f, 0.96f);
        borderImg.raycastTarget = false;

        GameObject panelObj = new GameObject("KeyFragmentsBarInner", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(borderObj.transform, false);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(1.5f, 1.5f);
        panelRect.offsetMax = new Vector2(-1.5f, -1.5f);

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.08f, 0.10f, 0.15f, 0.94f);
        panelImg.raycastTarget = false;

        // Left status text ("🔑 CHÌA KHÓA 0/4")
        GameObject statusObj = new GameObject("KeyStatusLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusObj.transform.SetParent(panelObj.transform, false);
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(0.40f, 1f);
        statusRect.offsetMin = new Vector2(4f, 1f);
        statusRect.offsetMax = new Vector2(-2f, -1f);

        this.masterKeyStatusText = statusObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) this.masterKeyStatusText.font = defaultFont;
        this.masterKeyStatusText.fontSize = 8.5f;
        this.masterKeyStatusText.fontStyle = FontStyles.Bold;
        this.masterKeyStatusText.alignment = TextAlignmentOptions.Center;
        this.masterKeyStatusText.color = new Color(1f, 0.90f, 0.55f, 1f);
        this.masterKeyStatusText.raycastTarget = false;

        // 4 Key Fragment Slots on the right side of the bar
        GameObject slotsContainer = new GameObject("KeySlotsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        slotsContainer.transform.SetParent(panelObj.transform, false);
        RectTransform slotsRect = slotsContainer.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.41f, 0.06f);
        slotsRect.anchorMax = new Vector2(0.98f, 0.94f);
        slotsRect.offsetMin = Vector2.zero;
        slotsRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layout = slotsContainer.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 4f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Sprite keySprite = KeyFragmentPickup.CreateProceduralKeySprite();

        for (int i = 0; i < 4; i++)
        {
            GameObject slotBorder = new GameObject($"KeySlot_{i + 1}", typeof(RectTransform), typeof(Image));
            slotBorder.transform.SetParent(slotsContainer.transform, false);
            RectTransform sbRect = slotBorder.GetComponent<RectTransform>();
            sbRect.sizeDelta = new Vector2(26f, 26f);

            Image sbImg = slotBorder.GetComponent<Image>();
            sbImg.color = this.emptySlotBgColor;
            sbImg.raycastTarget = false;
            this.fragmentSlotBackgrounds[i] = sbImg;

            Outline slotOutline = slotBorder.AddComponent<Outline>();
            slotOutline.effectColor = new Color(0.85f, 0.68f, 0.25f, 0.65f);
            slotOutline.effectDistance = new Vector2(1f, -1f);

            // Key Icon inside slot
            GameObject iconObj = new GameObject("KeyIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(slotBorder.transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.12f, 0.22f);
            iconRect.anchorMax = new Vector2(0.88f, 0.96f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = keySprite;
            iconImg.preserveAspect = true;
            iconImg.color = this.emptyKeyIconColor;
            iconImg.raycastTarget = false;
            this.fragmentKeyIcons[i] = iconImg;

            // Small index number at bottom of slot
            GameObject numObj = new GameObject("SlotNum", typeof(RectTransform), typeof(TextMeshProUGUI));
            numObj.transform.SetParent(slotBorder.transform, false);
            RectTransform numRect = numObj.GetComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0f, 0f);
            numRect.anchorMax = new Vector2(1f, 0.36f);
            numRect.offsetMin = Vector2.zero;
            numRect.offsetMax = Vector2.zero;

            TextMeshProUGUI numTmp = numObj.GetComponent<TextMeshProUGUI>();
            if (defaultFont != null) numTmp.font = defaultFont;
            numTmp.text = $"{i + 1}";
            numTmp.fontSize = 6.5f;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = new Color(0.7f, 0.74f, 0.8f, 0.8f);
            numTmp.raycastTarget = false;
            this.fragmentSlotLabels[i] = numTmp;
        }

        // Compact Objective Strip right below the Key Fragments bar
        GameObject objBg = new GameObject("ObjectiveStrip", typeof(RectTransform), typeof(Image));
        objBg.transform.SetParent(borderObj.transform, false);
        RectTransform objBgRect = objBg.GetComponent<RectTransform>();
        objBgRect.anchorMin = new Vector2(0f, 0f);
        objBgRect.anchorMax = new Vector2(1f, 0f);
        objBgRect.pivot = new Vector2(0.5f, 1f);
        objBgRect.anchoredPosition = new Vector2(0f, -2f);
        objBgRect.sizeDelta = new Vector2(0f, 15f);

        Image objBgImg = objBg.GetComponent<Image>();
        objBgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.82f);
        objBgImg.raycastTarget = false;

        GameObject objTextGO = new GameObject("ObjectiveText", typeof(RectTransform), typeof(TextMeshProUGUI));
        objTextGO.transform.SetParent(objBg.transform, false);
        RectTransform objTextRect = objTextGO.GetComponent<RectTransform>();
        objTextRect.anchorMin = Vector2.zero;
        objTextRect.anchorMax = Vector2.one;
        objTextRect.offsetMin = new Vector2(4f, 0f);
        objTextRect.offsetMax = new Vector2(-4f, 0f);

        this.objectiveText = objTextGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) this.objectiveText.font = defaultFont;
        this.objectiveText.fontSize = 6.8f;
        this.objectiveText.alignment = TextAlignmentOptions.Center;
        this.objectiveText.color = new Color(0.95f, 0.90f, 0.68f, 0.96f);
        this.objectiveText.raycastTarget = false;
    }
}
