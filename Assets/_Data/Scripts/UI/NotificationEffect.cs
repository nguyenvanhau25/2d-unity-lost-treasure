using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hệ thống khung thông báo phiêu lưu (Notification Box) với hiệu ứng Typewriter.
/// Hiển thị ở vùng HUD phía dưới màn hình để không bao giờ đè lên nhân vật hay che khuất gameplay,
/// đồng thời đồng bộ biểu tượng chuông báo phía trên bảng chỉ dẫn trong thế giới.
/// </summary>
public class NotificationEffect : MonoBehaviour
{
    private static readonly List<NotificationEffect> ActiveInstances = new List<NotificationEffect>();

    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private Graphic bellIcon;

    [Header("Notification Text")]
    [TextArea(2, 5)]
    [SerializeField] private string message = "Bản đồ cổ đã thức tỉnh! Hãy đến Bảng Chỉ Dẫn phía trước để xem mục tiêu hành trình và tìm Mảnh Chìa Khóa Sao Rừng!";

    [SerializeField] private float typingSpeed = 0.032f;
    [SerializeField] private float startDelay = 0.15f;
    [SerializeField] private float repeatDelay = 2.8f;

    [Header("Bell Blink")]
    [SerializeField] private float blinkSpeed = 2.4f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.45f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;
    [SerializeField] private float pulseScale = 1.12f;

    private Coroutine typingCoroutine;
    private Coroutine blinkCoroutine;
    private Coroutine panelIntroCoroutine;

    private CanvasGroup bellCanvasGroup;
    private CanvasGroup panelCanvasGroup;
    private Vector3 bellOriginalScale;
    private Vector3 panelOriginalScale = Vector3.one;

    // Dedicated ScreenSpace Notification Box so text is crisp and never overlaps the player in world space
    private GameObject screenBoxCanvasObj;
    private RectTransform screenBoxFrame;
    private TMP_Text screenNotificationText;
    private RectTransform screenBellBadge;
    private CanvasGroup screenBellCanvasGroup;

    public static void HideAllNotifications()
    {
        for (int i = ActiveInstances.Count - 1; i >= 0; i--)
        {
            if (ActiveInstances[i] != null)
            {
                ActiveInstances[i].HideNotification();
            }
        }
    }

    private void OnEnable()
    {
        if (!ActiveInstances.Contains(this))
        {
            ActiveInstances.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveInstances.Remove(this);
    }

    private void OnDestroy()
    {
        ActiveInstances.Remove(this);
        if (this.screenBoxCanvasObj != null)
        {
            Destroy(this.screenBoxCanvasObj);
        }
    }

    private void Awake()
    {
        if (this.message == "có chuyện gì" || string.IsNullOrWhiteSpace(this.message))
        {
            this.message = "Bản đồ cổ đã thức tỉnh! Hãy đến Bảng Chỉ Dẫn phía trước để xem mục tiêu hành trình và tìm Mảnh Chìa Khóa Sao Rừng!";
        }

        if (this.typingSpeed <= 0f || this.typingSpeed > 0.08f)
        {
            this.typingSpeed = 0.032f;
        }

        if (this.repeatDelay < 1.5f)
        {
            this.repeatDelay = 2.8f;
        }

        if (this.notificationPanel == null)
        {
            this.notificationPanel = gameObject;
        }

        if (this.notificationText == null)
        {
            this.notificationText = GetComponentInChildren<TMP_Text>(true);
        }

        if (this.bellIcon != null)
        {
            this.bellOriginalScale = this.bellIcon.transform.localScale;
            this.bellCanvasGroup = this.bellIcon.GetComponent<CanvasGroup>();
            if (this.bellCanvasGroup == null)
            {
                this.bellCanvasGroup = this.bellIcon.gameObject.AddComponent<CanvasGroup>();
            }
        }

        this.BuildAdventureNotificationBox();
    }

    private void BuildAdventureNotificationBox()
    {
        // Clean up the world-space panel so it doesn't draw a box over the player path
        if (this.notificationPanel != null)
        {
            Image worldBg = this.notificationPanel.GetComponent<Image>();
            if (worldBg != null)
            {
                worldBg.enabled = false;
            }

            Outline worldOutline = this.notificationPanel.GetComponent<Outline>();
            if (worldOutline != null)
            {
                worldOutline.enabled = false;
            }
        }

        // Hide the old world-space floating text so it never overlaps the character or duplicates text
        TMP_FontAsset cleanFont = TMP_Settings.defaultFontAsset;
        if (this.notificationText != null)
        {
            if (this.notificationText.font != null && !this.notificationText.font.name.ToLowerInvariant().Contains("fibberish"))
            {
                cleanFont = this.notificationText.font;
            }
            this.notificationText.text = string.Empty;
            this.notificationText.gameObject.SetActive(false);
        }

        // Build a dedicated ScreenSpace-Overlay Notification Box docked neatly at bottom-center
        this.screenBoxCanvasObj = new GameObject("AdventureNotificationBoxCanvas");
        Canvas canvas = this.screenBoxCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        CanvasScaler scaler = this.screenBoxCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        // Outer frame at bottom-center (never obscures player or center gameplay)
        GameObject outerFrameObj = new GameObject("NotificationOuterFrame");
        outerFrameObj.transform.SetParent(this.screenBoxCanvasObj.transform, false);
        this.screenBoxFrame = outerFrameObj.AddComponent<RectTransform>();
        this.screenBoxFrame.anchorMin = new Vector2(0.5f, 0f);
        this.screenBoxFrame.anchorMax = new Vector2(0.5f, 0f);
        this.screenBoxFrame.pivot = new Vector2(0.5f, 0f);
        this.screenBoxFrame.anchoredPosition = new Vector2(0f, 20f);
        this.screenBoxFrame.sizeDelta = new Vector2(700f, 88f);

        this.panelCanvasGroup = outerFrameObj.AddComponent<CanvasGroup>();
        this.panelCanvasGroup.blocksRaycasts = false;
        this.panelCanvasGroup.interactable = false;
        this.panelOriginalScale = Vector3.one;

        Image outerBorderImg = outerFrameObj.AddComponent<Image>();
        outerBorderImg.color = new Color(0.85f, 0.66f, 0.24f, 0.96f); // Antique Gold Border
        outerBorderImg.raycastTarget = false;

        Shadow dropShadow = outerFrameObj.AddComponent<Shadow>();
        dropShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        dropShadow.effectDistance = new Vector2(0f, -4f);

        // Dark slate-parchment inner box
        GameObject innerBgObj = new GameObject("NotificationInnerBg");
        innerBgObj.transform.SetParent(outerFrameObj.transform, false);
        RectTransform innerRect = innerBgObj.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(3f, 3f);
        innerRect.offsetMax = new Vector2(-3f, -3f);

        Image innerBgImg = innerBgObj.AddComponent<Image>();
        innerBgImg.color = new Color(0.07f, 0.09f, 0.14f, 0.95f);
        innerBgImg.raycastTarget = false;

        // Decorative inner gold trim line
        Outline innerTrim = innerBgObj.AddComponent<Outline>();
        innerTrim.effectColor = new Color(1f, 0.84f, 0.38f, 0.35f);
        innerTrim.effectDistance = new Vector2(1f, -1f);

        // Left accent bar
        GameObject leftAccentObj = new GameObject("LeftGoldAccent");
        leftAccentObj.transform.SetParent(innerBgObj.transform, false);
        RectTransform leftAccentRect = leftAccentObj.AddComponent<RectTransform>();
        leftAccentRect.anchorMin = new Vector2(0f, 0f);
        leftAccentRect.anchorMax = new Vector2(0f, 1f);
        leftAccentRect.pivot = new Vector2(0f, 0.5f);
        leftAccentRect.anchoredPosition = Vector2.zero;
        leftAccentRect.sizeDelta = new Vector2(6f, 0f);
        Image leftAccentImg = leftAccentObj.AddComponent<Image>();
        leftAccentImg.color = new Color(0.96f, 0.76f, 0.24f, 1f);
        leftAccentImg.raycastTarget = false;

        // Icon badge holder on the left
        GameObject badgeBgObj = new GameObject("BellBadgeBg");
        badgeBgObj.transform.SetParent(innerBgObj.transform, false);
        RectTransform badgeBgRect = badgeBgObj.AddComponent<RectTransform>();
        badgeBgRect.anchorMin = new Vector2(0f, 0.5f);
        badgeBgRect.anchorMax = new Vector2(0f, 0.5f);
        badgeBgRect.pivot = new Vector2(0.5f, 0.5f);
        badgeBgRect.anchoredPosition = new Vector2(44f, 0f);
        badgeBgRect.sizeDelta = new Vector2(54f, 54f);

        Image badgeBgImg = badgeBgObj.AddComponent<Image>();
        badgeBgImg.color = new Color(0.14f, 0.18f, 0.26f, 1f);
        badgeBgImg.raycastTarget = false;

        Outline badgeOutline = badgeBgObj.AddComponent<Outline>();
        badgeOutline.effectColor = new Color(0.95f, 0.76f, 0.26f, 0.85f);
        badgeOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject bellBadgeIconObj = new GameObject("BellBadgeIcon");
        bellBadgeIconObj.transform.SetParent(badgeBgObj.transform, false);
        this.screenBellBadge = bellBadgeIconObj.AddComponent<RectTransform>();
        this.screenBellBadge.anchorMin = new Vector2(0.5f, 0.5f);
        this.screenBellBadge.anchorMax = new Vector2(0.5f, 0.5f);
        this.screenBellBadge.pivot = new Vector2(0.5f, 0.5f);
        this.screenBellBadge.anchoredPosition = Vector2.zero;
        this.screenBellBadge.sizeDelta = new Vector2(36f, 36f);
        this.screenBellCanvasGroup = bellBadgeIconObj.AddComponent<CanvasGroup>();

        Image sourceBellImg = this.bellIcon as Image;
        if (sourceBellImg != null && sourceBellImg.sprite != null)
        {
            Image badgeIconImg = bellBadgeIconObj.AddComponent<Image>();
            badgeIconImg.sprite = sourceBellImg.sprite;
            badgeIconImg.preserveAspect = true;
            badgeIconImg.color = new Color(1f, 0.88f, 0.38f, 1f);
            badgeIconImg.raycastTarget = false;
        }
        else
        {
            TextMeshProUGUI badgeSymbol = bellBadgeIconObj.AddComponent<TextMeshProUGUI>();
            badgeSymbol.font = cleanFont;
            badgeSymbol.text = "!";
            badgeSymbol.fontSize = 26f;
            badgeSymbol.fontStyle = FontStyles.Bold;
            badgeSymbol.color = new Color(1f, 0.86f, 0.32f, 1f);
            badgeSymbol.alignment = TextAlignmentOptions.Center;
            badgeSymbol.raycastTarget = false;
        }

        // Header title inside the Notification Box
        GameObject headerObj = new GameObject("NotificationHeader");
        headerObj.transform.SetParent(innerBgObj.transform, false);
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(84f, -28f);
        headerRect.offsetMax = new Vector2(-18f, -6f);

        TextMeshProUGUI headerTmp = headerObj.AddComponent<TextMeshProUGUI>();
        headerTmp.font = cleanFont;
        headerTmp.text = "✦ CHỈ DẪN THÁM HIỂM — BẢNG MỤC TIÊU CỔ ✦";
        headerTmp.fontSize = 13.5f;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.characterSpacing = 1.5f;
        headerTmp.color = new Color(1f, 0.84f, 0.35f, 1f);
        headerTmp.alignment = TextAlignmentOptions.MidlineLeft;
        headerTmp.raycastTarget = false;

        // Typewriter body text inside the Notification Box
        GameObject bodyObj = new GameObject("NotificationBodyText");
        bodyObj.transform.SetParent(innerBgObj.transform, false);
        RectTransform bodyRect = bodyObj.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(84f, 8f);
        bodyRect.offsetMax = new Vector2(-18f, -28f);

        TextMeshProUGUI bodyTmp = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyTmp.font = cleanFont;
        bodyTmp.text = string.Empty;
        bodyTmp.fontSize = 15.5f;
        bodyTmp.lineSpacing = 4f;
        bodyTmp.color = new Color(0.98f, 0.95f, 0.86f, 1f);
        bodyTmp.textWrappingMode = TextWrappingModes.Normal;
        bodyTmp.overflowMode = TextOverflowModes.Overflow;
        bodyTmp.alignment = TextAlignmentOptions.TopLeft;
        bodyTmp.raycastTarget = false;

        this.screenNotificationText = bodyTmp;
    }

    private void Start()
    {
        ShowNotification();
    }

    public void ShowNotification()
    {
        if (this.message == "có chuyện gì" || string.IsNullOrWhiteSpace(this.message))
        {
            this.message = "Bản đồ cổ đã thức tỉnh! Hãy đến Bảng Chỉ Dẫn phía trước để xem mục tiêu hành trình và tìm Mảnh Chìa Khóa Sao Rừng!";
        }

        if (this.notificationPanel != null)
        {
            this.notificationPanel.SetActive(true);
        }

        if (this.screenBoxCanvasObj != null)
        {
            this.screenBoxCanvasObj.SetActive(true);
        }

        StopCurrentAnimations();

        this.panelIntroCoroutine = StartCoroutine(PlayPanelIntro());
        this.typingCoroutine = StartCoroutine(TypeTextLoop());
        this.blinkCoroutine = StartCoroutine(BlinkBellLoop());
    }

    public void HideNotification()
    {
        StopCurrentAnimations();

        if (this.screenNotificationText != null)
        {
            this.screenNotificationText.text = string.Empty;
        }

        if (this.notificationText != null)
        {
            this.notificationText.text = string.Empty;
        }

        if (this.bellIcon != null)
        {
            this.bellIcon.transform.localScale = this.bellOriginalScale;
            if (this.bellCanvasGroup != null)
            {
                this.bellCanvasGroup.alpha = this.maxAlpha;
            }
        }

        if (this.screenBellBadge != null)
        {
            this.screenBellBadge.localScale = Vector3.one;
            if (this.screenBellCanvasGroup != null)
            {
                this.screenBellCanvasGroup.alpha = 1f;
            }
        }

        if (this.screenBoxCanvasObj != null)
        {
            this.screenBoxCanvasObj.SetActive(false);
        }

        if (this.notificationPanel != null)
        {
            this.notificationPanel.SetActive(false);
        }
    }

    private IEnumerator TypeTextLoop()
    {
        if (this.startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(this.startDelay);
        }

        while (true)
        {
            TMP_Text targetText = this.screenNotificationText != null ? this.screenNotificationText : this.notificationText;
            if (targetText == null)
            {
                yield break;
            }

            targetText.text = string.Empty;
            targetText.maxVisibleCharacters = int.MaxValue;

            for (int i = 0; i < this.message.Length; i++)
            {
                targetText.text = this.message.Substring(0, i + 1);

                if (this.typingSpeed > 0f)
                {
                    yield return new WaitForSecondsRealtime(this.typingSpeed);
                }
                else
                {
                    yield return null;
                }
            }

            if (this.repeatDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(this.repeatDelay);
            }
        }
    }

    private IEnumerator BlinkBellLoop()
    {
        float timer = 0f;

        while (true)
        {
            timer += Time.unscaledDeltaTime * this.blinkSpeed;
            float wave = (Mathf.Sin(timer) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(this.minAlpha, this.maxAlpha, wave);
            float currentScale = Mathf.Lerp(1f, this.pulseScale, wave);

            if (this.bellIcon != null)
            {
                if (this.bellCanvasGroup != null)
                {
                    this.bellCanvasGroup.alpha = alpha;
                }
                this.bellIcon.transform.localScale = this.bellOriginalScale * currentScale;
            }

            if (this.screenBellBadge != null)
            {
                if (this.screenBellCanvasGroup != null)
                {
                    this.screenBellCanvasGroup.alpha = alpha;
                }
                this.screenBellBadge.localScale = Vector3.one * currentScale;
            }

            yield return null;
        }
    }

    private IEnumerator PlayPanelIntro()
    {
        float duration = 0.22f;
        float elapsed = 0f;

        Transform targetTransform = this.screenBoxFrame != null ? (Transform)this.screenBoxFrame : this.notificationPanel.transform;
        Vector3 startScale = this.panelOriginalScale * 0.92f;

        if (this.panelCanvasGroup != null)
        {
            this.panelCanvasGroup.alpha = 0f;
        }

        targetTransform.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (this.panelCanvasGroup != null)
            {
                this.panelCanvasGroup.alpha = t;
            }

            targetTransform.localScale = Vector3.Lerp(startScale, this.panelOriginalScale, t);
            yield return null;
        }

        if (this.panelCanvasGroup != null)
        {
            this.panelCanvasGroup.alpha = 1f;
        }

        targetTransform.localScale = this.panelOriginalScale;
    }

    private void StopCurrentAnimations()
    {
        if (this.panelIntroCoroutine != null)
        {
            StopCoroutine(this.panelIntroCoroutine);
            this.panelIntroCoroutine = null;
        }

        if (this.typingCoroutine != null)
        {
            StopCoroutine(this.typingCoroutine);
            this.typingCoroutine = null;
        }

        if (this.blinkCoroutine != null)
        {
            StopCoroutine(this.blinkCoroutine);
            this.blinkCoroutine = null;
        }
    }
}
