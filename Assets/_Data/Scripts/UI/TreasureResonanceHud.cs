using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-owned HUD for Treasure Resonance. In edit mode it creates the default
/// child hierarchy once, then leaves it available for visual adjustment.
/// </summary>
[ExecuteAlways]
public class TreasureResonanceHud : MonoBehaviour
{
    [SerializeField] protected Image cooldownFill;
    [SerializeField] protected Image skillIcon;
    [SerializeField] protected TextMeshProUGUI cooldownText;
    [SerializeField] protected TextMeshProUGUI keyText;
    protected PlayerTreasureResonance skill;
    protected bool isBuilding;

    protected void OnEnable()
    {
        if (!Application.isPlaying) this.EnsureDefaultLayout();
    }

    protected void Start()
    {
        if (!Application.isPlaying) return;
        this.EnsureDefaultLayout();
        StartCoroutine(this.ShowUnlockBanner());
    }

    protected void Update()
    {
        if (!Application.isPlaying) return;
        if (this.skill == null) this.skill = FindFirstObjectByType<PlayerTreasureResonance>();
        if (this.skill == null || this.cooldownFill == null || this.skillIcon == null || this.cooldownText == null || this.keyText == null)
            return;

        float remaining = this.skill.CooldownRemaining;
        this.cooldownFill.fillAmount = remaining / this.skill.Cooldown;
        this.skillIcon.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 2.7f) * 0.035f);
        if (remaining <= 0f)
        {
            this.keyText.text = "[ Q ] READY";
            this.keyText.color = new Color(0.86f, 0.67f, 1f);
            this.cooldownText.text = "";
            return;
        }
        this.keyText.text = "[ Q ] RECHARGING";
        this.keyText.color = new Color(0.72f, 0.63f, 0.8f);
        this.cooldownText.text = Mathf.CeilToInt(remaining).ToString();
    }

    [ContextMenu("Rebuild Default Layout")]
    public void RebuildDefaultLayout()
    {
        if (Application.isPlaying) return;
        this.isBuilding = true;
        foreach (Transform child in transform) DestroyImmediate(child.gameObject);
        this.cooldownFill = null;
        this.skillIcon = null;
        this.cooldownText = null;
        this.keyText = null;
        this.EnsureDefaultLayout();
        this.isBuilding = false;
    }

    protected void EnsureDefaultLayout()
    {
        if (this.isBuilding) return;
        if (transform.Find("Backdrop") != null)
        {
            this.CacheExistingLayout();
            return;
        }
        this.isBuilding = true;
        this.ConfigureRoot();

        Image sampleImage = FindFirstObjectByType<Image>();
        Sprite sprite = sampleImage != null ? sampleImage.sprite : null;
        TextMeshProUGUI sampleText = FindFirstObjectByType<TextMeshProUGUI>();
        TMP_FontAsset font = sampleText != null ? sampleText.font : TMP_Settings.defaultFontAsset;

        Image background = this.CreateImage("Backdrop", sprite, new Color(0.055f, 0.025f, 0.12f, 0.88f));
        this.Stretch(background.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));

        this.skillIcon = this.CreateImage("ResonanceIcon", sprite, new Color(1f, 0.72f, 0.16f, 1f));
        this.Stretch(this.skillIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -25f), new Vector2(64f, 25f));

        this.cooldownFill = this.CreateImage("Cooldown", sprite, new Color(0.27f, 0.06f, 0.48f, 0.78f));
        this.cooldownFill.type = Image.Type.Filled;
        this.cooldownFill.fillMethod = Image.FillMethod.Radial360;
        this.cooldownFill.fillOrigin = 2;
        this.Stretch(this.cooldownFill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -25f), new Vector2(64f, 25f));

        this.CreateText("SkillName", "TREASURE\nRESONANCE", 16, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(73f, 4f), new Vector2(-13f, -5f), new Color(1f, 0.82f, 0.31f), font);
        this.keyText = this.CreateText("Key", "[ Q ] READY", 15, TextAlignmentOptions.Left,
            new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(73f, 7f), new Vector2(-13f, -4f), new Color(0.86f, 0.67f, 1f), font);
        this.cooldownText = this.CreateText("CooldownText", "", 18, TextAlignmentOptions.Center,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -15f), new Vector2(64f, 15f), Color.white, font);
        this.isBuilding = false;
    }

    private void CacheExistingLayout()
    {
        Transform icon = transform.Find("ResonanceIcon");
        Transform cooldown = transform.Find("Cooldown");
        Transform cooldownLabel = transform.Find("CooldownText");
        Transform key = transform.Find("Key");
        if (this.skillIcon == null && icon != null) this.skillIcon = icon.GetComponent<Image>();
        if (this.cooldownFill == null && cooldown != null) this.cooldownFill = cooldown.GetComponent<Image>();
        if (this.cooldownText == null && cooldownLabel != null) this.cooldownText = cooldownLabel.GetComponent<TextMeshProUGUI>();
        if (this.keyText == null && key != null) this.keyText = key.GetComponent<TextMeshProUGUI>();
    }

    protected void ConfigureRoot()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-36f, 42f);
        rect.sizeDelta = new Vector2(206f, 76f);
    }

    protected Image CreateImage(string objectName, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(transform, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    protected TextMeshProUGUI CreateText(string objectName, string value, float size, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color, TMP_FontAsset font)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        this.Stretch(text.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
        return text;
    }

    protected void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    protected IEnumerator ShowUnlockBanner()
    {
        yield return new WaitForSeconds(0.15f);
        Transform titleObject = transform.Find("SkillName");
        if (titleObject == null) yield break;
        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        if (title == null) yield break;
        Color original = title.color;
        for (float time = 0f; time < 0.75f; time += Time.deltaTime)
        {
            title.color = Color.Lerp(original, Color.white, Mathf.PingPong(time * 3f, 1f));
            yield return null;
        }
        title.color = original;
    }
}
