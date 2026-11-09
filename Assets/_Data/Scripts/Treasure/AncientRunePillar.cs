using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// An ancient rune pillar that reacts to player attacks or proximity.
/// Lighting up the runes is required to solve environmental puzzles.
/// </summary>
public class AncientRunePillar : MonoBehaviour
{
    [Header("Rune Settings")]
    [SerializeField] private int runeId = 0;
    [SerializeField] private bool isActivated;
    [SerializeField] private Color inactiveColor = new Color(0.4f, 0.45f, 0.5f, 0.8f);
    [SerializeField] private Color activeColor = new Color(0.3f, 0.9f, 1f, 1f);

    [Header("Puzzle Link")]
    [SerializeField] private AncientRunePuzzle puzzleMaster;

    private SpriteRenderer spriteRenderer;
    private TextMeshPro runeSymbolText;

    public int RuneId => this.runeId;
    public bool IsActivated => this.isActivated;

    public void Configure(int id, AncientRunePuzzle puzzle)
    {
        this.runeId = id;
        this.puzzleMaster = puzzle;
    }

    private void Awake()
    {
        this.spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (this.spriteRenderer == null)
        {
            this.spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            this.spriteRenderer.sprite = CreatePillarSprite();
        }

        this.CreateRuneSymbol();
        this.UpdateVisuals();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (this.isActivated) return;

        // Activated by player melee attack, arrow, or dash
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerAttackArea>() != null || other.GetComponent<ArrowCtrl>() != null)
        {
            this.ActivateRune();
        }
    }

    public void ActivateRune()
    {
        if (this.isActivated) return;
        this.isActivated = true;

        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("SelectSound", 1.2f);
        }

        this.UpdateVisuals();
        StartCoroutine(this.PlayActivationPulse());

        if (this.puzzleMaster != null)
        {
            this.puzzleMaster.OnPillarActivated(this);
        }
    }

    public void ResetRune()
    {
        this.isActivated = false;
        this.UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        Color targetColor = this.isActivated ? this.activeColor : this.inactiveColor;
        if (this.spriteRenderer != null) this.spriteRenderer.color = targetColor;
        if (this.runeSymbolText != null)
        {
            this.runeSymbolText.color = this.isActivated ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            this.runeSymbolText.text = this.isActivated ? "✦" : "ᛟ";
        }
    }

    private IEnumerator PlayActivationPulse()
    {
        Vector3 orig = transform.localScale;
        for (float t = 0; t < 0.25f; t += Time.deltaTime)
        {
            float scale = 1f + Mathf.Sin(t / 0.25f * Mathf.PI) * 0.15f;
            transform.localScale = orig * scale;
            yield return null;
        }
        transform.localScale = orig;
    }

    private void CreateRuneSymbol()
    {
        GameObject textObj = new GameObject("RuneSymbol");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 0.2f, 0);

        this.runeSymbolText = textObj.AddComponent<TextMeshPro>();
        this.runeSymbolText.text = "ᛟ";
        this.runeSymbolText.fontSize = 3.5f;
        this.runeSymbolText.alignment = TextAlignmentOptions.Center;
        this.runeSymbolText.color = this.inactiveColor;
        this.runeSymbolText.sortingOrder = 5;
    }

    private static Sprite CreatePillarSprite()
    {
        Texture2D tex = new Texture2D(16, 32, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color stone = new Color(0.35f, 0.38f, 0.42f, 1f);
        Color border = new Color(0.2f, 0.22f, 0.25f, 1f);

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                if (x == 0 || x == 15 || y == 0 || y == 31)
                    tex.SetPixel(x, y, border);
                else
                    tex.SetPixel(x, y, stone);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 32), new Vector2(0.5f, 0.5f), 16);
    }
}
