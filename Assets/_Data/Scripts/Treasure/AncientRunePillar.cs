using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Bia Đá Câu Đố Cổ Ngữ (Ancient Riddle Tablet) đặt ngay trước Cửa Phong Ấn trong Ngôi Đền Cổ Bị Nguyền Rủa.
/// Khi người chơi chạm vào hoặc tấn công Bia Đá, bảng giải 3 câu đố sẽ mở ra để phá vỡ phong ấn Bắc Điện.
/// </summary>
public class AncientRunePillar : MonoBehaviour
{
    [Header("Rune Settings")]
    [SerializeField] private int runeId = 0;
    [SerializeField] private bool isActivated;
    [SerializeField] private Color inactiveColor = new Color(0.38f, 0.86f, 1f, 0.96f);
    [SerializeField] private Color activeColor = new Color(0.28f, 1f, 0.72f, 1f);

    [Header("Puzzle Link")]
    [SerializeField] private AncientRunePuzzle puzzleMaster;

    private SpriteRenderer spriteRenderer;
    private TextMeshPro runeSymbolText;
    private TextMeshPro statusLabel;
    private int solvedCount;
    private int totalRequired = 3;

    public int RuneId => this.runeId;
    public bool IsActivated => this.isActivated;

    public void Configure(int id, AncientRunePuzzle puzzle)
    {
        this.runeId = id;
        this.puzzleMaster = puzzle;
        this.UpdateVisuals();
    }

    public void SetSolvedProgress(int solved, int total)
    {
        this.solvedCount = solved;
        this.totalRequired = Mathf.Max(1, total);
        this.isActivated = this.solvedCount >= this.totalRequired;
        this.UpdateVisuals();
    }

    private void Awake()
    {
        this.spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (this.spriteRenderer == null)
        {
            this.spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            this.spriteRenderer.sprite = CreatePillarSprite();
        }
        this.spriteRenderer.sortingOrder = 15;
        transform.localScale = new Vector3(1.35f, 1.35f, 1f);

        this.CreateRuneSymbol();
        this.UpdateVisuals();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (this.isActivated || other == null) return;

        // Mở bảng Giải 3 Câu Đố khi người chơi tiến tới chạm vào, đánh kiếm hoặc bắn tên vào Bia Đá
        if (other.CompareTag("Player")
            || other.GetComponentInParent<PlayerCtrl>() != null
            || other.GetComponentInParent<PlayerAttackArea>() != null
            || other.GetComponentInParent<ArrowCtrl>() != null)
        {
            this.ActivateRune();
        }
    }

    public void ActivateRune()
    {
        if (this.isActivated) return;

        StartCoroutine(this.PlayActivationPulse());

        if (this.puzzleMaster != null)
        {
            this.puzzleMaster.OnPillarActivated(this);
        }
    }

    public void ResetRune()
    {
        this.isActivated = false;
        this.solvedCount = 0;
        this.UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        Color targetColor = this.isActivated ? this.activeColor : this.inactiveColor;
        if (this.spriteRenderer != null)
        {
            this.spriteRenderer.color = targetColor;
        }

        if (this.runeSymbolText != null)
        {
            this.runeSymbolText.color = this.isActivated
                ? new Color(0.35f, 1f, 0.72f, 1f)
                : new Color(1f, 0.9f, 0.35f, 1f);
            this.runeSymbolText.text = this.isActivated ? "✦" : "📜";
        }

        if (this.statusLabel != null)
        {
            if (this.isActivated)
            {
                this.statusLabel.text = $"✦ BIA ĐÁ CỔ NGỮ [{this.solvedCount}/{this.totalRequired}]\n<size=1.8>[ĐÃ GIẢI ĐỦ 3 CÂU ĐỐ — CỬA ĐÃ MỞ!]</size>";
                this.statusLabel.color = new Color(0.35f, 1f, 0.85f, 1f);
            }
            else
            {
                this.statusLabel.text = $"📜 BIA ĐÁ CÂU ĐỐ PHONG ẤN [{this.solvedCount}/{this.totalRequired}]\n<size=1.9>[Chạm vào hoặc nhấn phím E để giải 3 câu đố mở cửa]</size>";
                this.statusLabel.color = new Color(1f, 0.9f, 0.38f, 1f);
            }
        }
    }

    private IEnumerator PlayActivationPulse()
    {
        Vector3 orig = new Vector3(1.35f, 1.35f, 1f);
        for (float t = 0; t < 0.24f; t += Time.deltaTime)
        {
            float scale = 1f + Mathf.Sin(t / 0.24f * Mathf.PI) * 0.16f;
            transform.localScale = orig * scale;
            yield return null;
        }
        transform.localScale = orig;
    }

    private void CreateRuneSymbol()
    {
        GameObject textObj = new GameObject("RuneSymbol");
        textObj.transform.SetParent(transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0.18f, 0f);

        this.runeSymbolText = textObj.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            this.runeSymbolText.font = TMP_Settings.defaultFontAsset;
        }
        this.runeSymbolText.text = "📜";
        this.runeSymbolText.fontSize = 3.6f;
        this.runeSymbolText.alignment = TextAlignmentOptions.Center;
        this.runeSymbolText.color = new Color(1f, 0.9f, 0.35f, 1f);
        this.runeSymbolText.sortingOrder = 17;

        GameObject labelObj = new GameObject("RuneStatusLabel");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 1.75f, 0f);

        this.statusLabel = labelObj.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            this.statusLabel.font = TMP_Settings.defaultFontAsset;
        }
        this.statusLabel.fontSize = 2.1f;
        this.statusLabel.alignment = TextAlignmentOptions.Center;
        this.statusLabel.sortingOrder = 18;
    }

    private static Sprite CreatePillarSprite()
    {
        Texture2D tex = new Texture2D(24, 36, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color stone = new Color(0.28f, 0.34f, 0.46f, 1f);
        Color goldBorder = new Color(0.95f, 0.78f, 0.28f, 1f);
        Color runeGlow = new Color(0.38f, 0.92f, 1f, 1f);

        for (int y = 0; y < 36; y++)
        {
            for (int x = 0; x < 24; x++)
            {
                if (x <= 1 || x >= 22 || y <= 1 || y >= 34 || y == 5 || y == 30)
                    tex.SetPixel(x, y, goldBorder);
                else if ((x >= 10 && x <= 13) && y >= 9 && y <= 26)
                    tex.SetPixel(x, y, runeGlow);
                else
                    tex.SetPixel(x, y, stone);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 24, 36), new Vector2(0.5f, 0.5f), 16);
    }
}
