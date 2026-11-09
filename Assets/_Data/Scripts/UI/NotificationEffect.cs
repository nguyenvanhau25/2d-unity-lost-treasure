using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Notification luôn hoạt động cho tới khi gọi HideNotification().
/// Styled with an attractive golden RPG dialogue frame.
/// </summary>
public class NotificationEffect : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private Graphic bellIcon;

    [Header("Notification Text")]
    [TextArea(2, 5)]
    [SerializeField] private string message = "Bản đồ cổ đã thức tỉnh! Hãy khám phá rừng, né cạm bẫy và tìm Mảnh Chìa Khóa Cổ để mở đường!";

    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float startDelay = 0.1f;
    [SerializeField] private float repeatDelay = 0.5f;

    [Header("Bell Blink")]
    [SerializeField] private float blinkSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.3f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;
    [SerializeField] private float pulseScale = 1.08f;

    private Coroutine typingCoroutine;
    private Coroutine blinkCoroutine;
    private Coroutine panelIntroCoroutine;

    private CanvasGroup bellCanvasGroup;
    private CanvasGroup panelCanvasGroup;
    private Vector3 bellOriginalScale;
    private Vector3 panelOriginalScale;

    private void Awake()
    {
        if (this.message == "có chuyện gì" || string.IsNullOrEmpty(this.message))
        {
            this.message = "Bản đồ cổ đã thức tỉnh! Hãy khám phá rừng, né cạm bẫy và tìm Mảnh Chìa Khóa Cổ để mở đường!";
        }

        if (notificationPanel == null)
            notificationPanel = gameObject;

        if (notificationText == null)
            notificationText = GetComponentInChildren<TMP_Text>(true);

        this.StyleNotificationFrame();

        panelCanvasGroup = notificationPanel.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = notificationPanel.AddComponent<CanvasGroup>();
        panelOriginalScale = notificationPanel.transform.localScale;

        if (bellIcon != null)
        {
            bellOriginalScale = bellIcon.transform.localScale;
            bellCanvasGroup = bellIcon.GetComponent<CanvasGroup>();
            if (bellCanvasGroup == null)
                bellCanvasGroup = bellIcon.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void StyleNotificationFrame()
    {
        if (this.notificationPanel == null) return;

        // Ensure background image frame exists
        Image bg = this.notificationPanel.GetComponent<Image>();
        if (bg == null)
        {
            bg = this.notificationPanel.AddComponent<Image>();
        }
        bg.color = new Color(0.08f, 0.1f, 0.14f, 0.94f);

        // Ensure gold border exists
        Outline border = this.notificationPanel.GetComponent<Outline>();
        if (border == null)
        {
            border = this.notificationPanel.AddComponent<Outline>();
        }
        border.effectColor = new Color(1f, 0.82f, 0.28f, 0.9f);
        border.effectDistance = new Vector2(2.5f, -2.5f);

        // Adjust dimensions and padding
        RectTransform rect = this.notificationPanel.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 560f), Mathf.Max(rect.sizeDelta.y, 68f));
        }

        if (this.notificationText != null)
        {
            this.notificationText.fontSize = 13.5f;
            this.notificationText.color = new Color(1f, 0.95f, 0.75f, 1f);
            this.notificationText.textWrappingMode = TextWrappingModes.Normal;
            this.notificationText.alignment = TextAlignmentOptions.MidlineLeft;

            RectTransform textRect = this.notificationText.rectTransform;
            if (textRect != null)
            {
                textRect.anchorMin = new Vector2(0.12f, 0.05f);
                textRect.anchorMax = new Vector2(0.96f, 0.95f);
                textRect.anchoredPosition = Vector2.zero;
                textRect.sizeDelta = Vector2.zero;
            }
        }
    }

    private void Start()
    {
        ShowNotification();
    }

    public void ShowNotification()
    {
        if (this.message == "có chuyện gì" || string.IsNullOrEmpty(this.message))
        {
            this.message = "Bản đồ cổ đã thức tỉnh! Hãy khám phá rừng, né cạm bẫy và tìm Mảnh Chìa Khóa Cổ để mở đường!";
        }

        if (notificationPanel == null)
            return;

        notificationPanel.SetActive(true);

        StopCurrentAnimations();

        panelIntroCoroutine = StartCoroutine(PlayPanelIntro());
        typingCoroutine = StartCoroutine(TypeTextLoop());

        if (bellIcon != null)
            blinkCoroutine = StartCoroutine(BlinkBellLoop());
    }

    public void HideNotification()
    {
        StopCurrentAnimations();

        if (notificationText != null)
            notificationText.text = string.Empty;

        if (bellIcon != null)
        {
            bellIcon.transform.localScale = bellOriginalScale;

            if (bellCanvasGroup != null)
                bellCanvasGroup.alpha = maxAlpha;
        }

        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    private IEnumerator TypeTextLoop()
    {
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        while (true)
        {
            if (notificationText == null)
                yield break;

            notificationText.text = string.Empty;

            foreach (char character in message)
            {
                notificationText.text += character;

                if (typingSpeed > 0f)
                    yield return new WaitForSecondsRealtime(typingSpeed);
                else
                    yield return null;
            }

            if (repeatDelay > 0f)
                yield return new WaitForSecondsRealtime(repeatDelay);
        }
    }

    private IEnumerator BlinkBellLoop()
    {
        float timer = 0f;

        while (true)
        {
            timer += Time.unscaledDeltaTime * blinkSpeed;
            float wave = (Mathf.Sin(timer) + 1f) * 0.5f;

            if (bellCanvasGroup != null)
                bellCanvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);

            float currentScale = Mathf.Lerp(1f, pulseScale, wave);
            bellIcon.transform.localScale = bellOriginalScale * currentScale;

            yield return null;
        }
    }

    private IEnumerator PlayPanelIntro()
    {
        float duration = 0.2f;
        float elapsed = 0f;

        Vector3 startScale = panelOriginalScale * 0.9f;

        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;

        notificationPanel.transform.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = t;

            notificationPanel.transform.localScale = Vector3.Lerp(startScale, panelOriginalScale, t);
            yield return null;
        }

        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 1f;

        notificationPanel.transform.localScale = panelOriginalScale;
    }

    private void StopCurrentAnimations()
    {
        if (panelIntroCoroutine != null)
        {
            StopCoroutine(panelIntroCoroutine);
            panelIntroCoroutine = null;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }
}
