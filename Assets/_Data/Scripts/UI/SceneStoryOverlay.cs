using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A small scene-local message panel used by the rescue story and the Special
/// battle flow. It is built under the active HUD canvas at runtime, so no
/// prefab is required in every scene.
/// </summary>
public class SceneStoryOverlay : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private RectTransform panel;
    private Image accentBar;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private bool built;

    public static SceneStoryOverlay GetOrCreate()
    {
        SceneStoryOverlay existing = FindFirstObjectByType<SceneStoryOverlay>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        Canvas canvas = FindHudCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("SceneStoryOverlay needs an active Canvas to display messages.");
            return null;
        }

        GameObject overlayObject = new GameObject("SceneStoryOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(SceneStoryOverlay));
        overlayObject.transform.SetParent(canvas.transform, false);
        return overlayObject.GetComponent<SceneStoryOverlay>();
    }

    public IEnumerator ShowMessage(string title, string message, float duration, Color accent)
    {
        this.EnsureBuilt();
        gameObject.SetActive(true);

        this.titleText.text = title;
        this.titleText.color = accent;
        this.bodyText.text = message;
        this.accentBar.color = accent;

        this.canvasGroup.alpha = 0f;
        this.panel.localScale = Vector3.one * 0.88f;

        const float fadeInDuration = 0.22f;
        for (float time = 0f; time < fadeInDuration; time += Time.unscaledDeltaTime)
        {
            float progress = time / fadeInDuration;
            this.canvasGroup.alpha = progress;
            this.panel.localScale = Vector3.Lerp(Vector3.one * 0.88f, Vector3.one, progress);
            yield return null;
        }

        this.canvasGroup.alpha = 1f;
        this.panel.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration));

        const float fadeOutDuration = 0.28f;
        for (float time = 0f; time < fadeOutDuration; time += Time.unscaledDeltaTime)
        {
            float progress = time / fadeOutDuration;
            this.canvasGroup.alpha = 1f - progress;
            this.panel.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.96f, progress);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private static Canvas FindHudCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return canvas;

        return canvases.Length > 0 ? canvases[0] : null;
    }

    private void Awake()
    {
        this.EnsureBuilt();
        gameObject.SetActive(false);
    }

    private void EnsureBuilt()
    {
        if (this.built) return;
        this.built = true;

        RectTransform root = GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        this.canvasGroup = GetComponent<CanvasGroup>();
        this.canvasGroup.blocksRaycasts = false;
        this.canvasGroup.interactable = false;

        Image backdrop = CreateImage(transform, "Backdrop", new Color(0.015f, 0.008f, 0.035f, 0.5f));
        Stretch(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image panelImage = CreateImage(transform, "Panel", new Color(0.075f, 0.025f, 0.14f, 0.95f));
        this.panel = panelImage.rectTransform;
        this.panel.anchorMin = this.panel.anchorMax = new Vector2(0.5f, 0.58f);
        this.panel.pivot = new Vector2(0.5f, 0.5f);
        this.panel.anchoredPosition = Vector2.zero;
        this.panel.sizeDelta = new Vector2(700f, 170f);

        this.accentBar = CreateImage(this.panel, "Accent", new Color(1f, 0.78f, 0.23f, 1f));
        Stretch(this.accentBar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(9f, 0f));

        TMP_FontAsset font = GetDefaultFont();
        this.titleText = CreateText(this.panel, "Title", 34f, TextAlignmentOptions.Center, font);
        Stretch(this.titleText.rectTransform, new Vector2(0f, 0.56f), new Vector2(1f, 1f), new Vector2(32f, 4f), new Vector2(-32f, -10f));
        this.titleText.fontStyle = FontStyles.Bold;

        this.bodyText = CreateText(this.panel, "Message", 22f, TextAlignmentOptions.Center, font);
        Stretch(this.bodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.62f), new Vector2(38f, 9f), new Vector2(-38f, -4f));
        this.bodyText.textWrappingMode = TextWrappingModes.Normal;
    }

    public static void DismissActive()
    {
        SceneStoryOverlay existing = FindFirstObjectByType<SceneStoryOverlay>(FindObjectsInactive.Include);
        if (existing != null && existing.gameObject.activeSelf)
        {
            existing.StopAllCoroutines();
            if (existing.canvasGroup != null) existing.canvasGroup.alpha = 0f;
            existing.gameObject.SetActive(false);
        }
    }

    private static TMP_FontAsset GetDefaultFont()
    {
        if (TMP_Settings.defaultFontAsset != null)
        {
            return TMP_Settings.defaultFontAsset;
        }
        TextMeshProUGUI sampleText = FindFirstObjectByType<TextMeshProUGUI>();
        return sampleText != null ? sampleText.font : null;
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

    private static TextMeshProUGUI CreateText(Transform parent, string objectName, float fontSize,
        TextAlignmentOptions alignment, TMP_FontAsset font)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
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
