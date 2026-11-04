using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Notification luôn hoạt động cho tới khi gọi HideNotification().
///
/// - Text chạy từng ký tự liên tục.
/// - Chạy hết câu -> bắt đầu lại từ đầu.
/// - Bell nhấp nháy liên tục.
/// - Không tự ẩn.
/// - Gọi HideNotification() từ script khác để tắt.
/// </summary>
public class NotificationEffect : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private Graphic bellIcon;

    [Header("Notification Text")]
    [TextArea(2, 5)]
    [SerializeField] private string message = "New Text";

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
        if (notificationPanel == null)
            notificationPanel = gameObject;

        if (notificationText == null)
            notificationText = GetComponentInChildren<TMP_Text>(true);

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

    private void Start()
    {
        ShowNotification();
    }

    /// <summary>
    /// Hiện notification và bắt đầu chạy hiệu ứng.
    /// Text sẽ chạy vô hạn cho tới khi HideNotification() được gọi.
    /// </summary>
    public void ShowNotification()
    {
        if (notificationPanel == null)
            return;

        notificationPanel.SetActive(true);

        StopCurrentAnimations();
        panelIntroCoroutine = StartCoroutine(PlayPanelIntro());

        // =========================
        // TEXT CHẠY VÔ HẠN
        // =========================
        if (notificationText != null)
        {
            notificationText.text = string.Empty;
            typingCoroutine = StartCoroutine(TypeTextLoop());
        }

        // =========================
        // BELL CHẠY VÔ HẠN
        // =========================
        if (bellIcon != null)
        {
            bellIcon.gameObject.SetActive(true);
            bellIcon.transform.localScale = bellOriginalScale;

            if (bellCanvasGroup != null)
                bellCanvasGroup.alpha = maxAlpha;

            blinkCoroutine = StartCoroutine(BlinkBellRoutine());
        }
    }

    /// <summary>
    /// Tắt notification ngay lập tức.
    /// Có thể gọi từ OnTriggerExit2D().
    /// </summary>
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

    /// <summary>
    /// Text chạy liên tục:
    ///
    /// New Text
    /// -> hết
    /// -> xóa
    /// -> New Text
    /// -> hết
    /// -> xóa
    /// -> lặp vô hạn
    /// </summary>
    private IEnumerator TypeTextLoop()
    {
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        while (true)
        {
            if (notificationText == null)
                yield break;

            // Xóa text trước mỗi lần chạy
            notificationText.text = string.Empty;

            // Chạy từng ký tự
            foreach (char character in message)
            {
                notificationText.text += character;

                if (typingSpeed > 0f)
                    yield return new WaitForSecondsRealtime(typingSpeed);
                else
                    yield return null;
            }

            // Chờ một chút sau khi chạy hết câu
            if (repeatDelay > 0f)
                yield return new WaitForSecondsRealtime(repeatDelay);

            // Sau đó vòng while chạy lại từ đầu
        }
    }

    /// <summary>
    /// Bell nhấp nháy liên tục.
    /// </summary>
    private IEnumerator BlinkBellRoutine()
    {
        if (bellIcon == null)
            yield break;

        float time = 0f;

        while (true)
        {
            time += Time.unscaledDeltaTime * blinkSpeed;

            float pingPong =
                (Mathf.Sin(time * Mathf.PI) + 1f) * 0.5f;

            if (bellCanvasGroup != null)
            {
                bellCanvasGroup.alpha = Mathf.Lerp(
                    minAlpha,
                    maxAlpha,
                    pingPong
                );
            }

            float scale = Mathf.Lerp(
                1f,
                pulseScale,
                pingPong
            );

            bellIcon.transform.localScale =
                bellOriginalScale * scale;

            yield return null;
        }
    }

    private IEnumerator PlayPanelIntro()
    {
        if (panelCanvasGroup == null) yield break;

        const float duration = 0.2f;
        panelCanvasGroup.alpha = 0f;
        notificationPanel.transform.localScale = panelOriginalScale * 0.92f;

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / duration;
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            panelCanvasGroup.alpha = eased;
            notificationPanel.transform.localScale = Vector3.Lerp(panelOriginalScale * 0.92f, panelOriginalScale, eased);
            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
        notificationPanel.transform.localScale = panelOriginalScale;
        panelIntroCoroutine = null;
    }

    private void StopCurrentAnimations()
    {
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
        if (panelIntroCoroutine != null)
        {
            StopCoroutine(panelIntroCoroutine);
            panelIntroCoroutine = null;
        }
    }

    private void OnDisable()
    {
        StopCurrentAnimations();

        if (bellIcon != null)
        {
            bellIcon.transform.localScale = bellOriginalScale;

            if (bellCanvasGroup != null)
                bellCanvasGroup.alpha = maxAlpha;
        }

        if (notificationPanel != null)
        {
            notificationPanel.transform.localScale = panelOriginalScale;
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
        }
    }
}
