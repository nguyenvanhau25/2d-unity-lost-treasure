using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// A collectable Key Fragment in the world.
/// Features a carved stone pedestal, glowing golden halo, floating key animation,
/// high sortingOrder above all tilemaps, proximity prompt, and optional guardian requirement.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class KeyFragmentPickup : MonoBehaviour
{
    [Header("Fragment Identity")]
    [Tooltip("0 = Forest, 1 = DarkForest, 2 = Cave, 3 = Cursed Temple")]
    [SerializeField] private int fragmentIndex = 0;
    [SerializeField] private string fragmentName = "Mảnh Chìa Khóa Cổ";
    [TextArea(2, 4)]
    [SerializeField] private string collectStoryLine = "";

    [Header("Requirements (Optional)")]
    [Tooltip("Enemies that must be defeated before this fragment can be collected")]
    [SerializeField] private GameObject[] guardianEnemies;
    [Tooltip("Rune puzzle that must be completed to unlock this fragment")]
    [SerializeField] private AncientRunePuzzle runePuzzle;

    [Header("Visual Effects")]
    [SerializeField] private float bobSpeed = 3.2f;
    [SerializeField] private float bobHeight = 0.22f;
    [SerializeField] private float rotateSpeed = 0f;
    [SerializeField] private Color fragmentGlowColor = new Color(1f, 0.88f, 0.25f, 1f);

    private Vector3 initialPosition;
    private bool isCollected;
    private SpriteRenderer spriteRenderer;
    private Transform keyVisualTransform;
    private SpriteRenderer auraRenderer;
    private Collider2D pickupCollider;
    private TextMeshPro promptText;
    private GameObject shieldVfx;

    public int FragmentIndex { get => this.fragmentIndex; set => this.fragmentIndex = value; }
    public string FragmentName { get => this.fragmentName; set => this.fragmentName = value; }

    private void Awake()
    {
        this.initialPosition = transform.position;
        this.pickupCollider = GetComponent<Collider2D>();
        this.pickupCollider.isTrigger = true;
        if (this.pickupCollider is CircleCollider2D circle)
        {
            circle.radius = Mathf.Max(circle.radius, 1.35f);
        }

        // Stone pedestal at base position so player clearly sees the pickup spot on the road
        this.CreatePedestalAndAura();

        // Floating key visual child
        GameObject keyVisualObj = new GameObject("KeyFragmentVisual");
        keyVisualObj.transform.SetParent(transform, false);
        keyVisualObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        keyVisualObj.transform.localScale = Vector3.one * 1.55f;
        this.keyVisualTransform = keyVisualObj.transform;

        this.spriteRenderer = keyVisualObj.AddComponent<SpriteRenderer>();
        this.spriteRenderer.sprite = CreateProceduralKeySprite();
        this.spriteRenderer.color = this.fragmentGlowColor;
        this.spriteRenderer.sortingLayerName = "Effects";
        this.spriteRenderer.sortingOrder = 26;

        this.CreateWorldPrompt();
    }

    private void Start()
    {
        this.initialPosition = transform.position;

        if (LostTreasureManager.Instance != null && LostTreasureManager.Instance.HasFragment(this.fragmentIndex))
        {
            gameObject.SetActive(false);
            return;
        }

        if (this.guardianEnemies != null && this.guardianEnemies.Length > 0)
        {
            this.CreateShieldVisual();
        }
    }

    private void Update()
    {
        if (this.isCollected) return;

        // Hover animation on the key visual while pedestal stays grounded
        if (this.keyVisualTransform != null)
        {
            float bobOffset = 0.38f + Mathf.Sin(Time.time * this.bobSpeed) * this.bobHeight;
            this.keyVisualTransform.localPosition = new Vector3(0f, bobOffset, 0f);

            if (this.rotateSpeed != 0f)
            {
                this.keyVisualTransform.Rotate(Vector3.forward * (this.rotateSpeed * Time.deltaTime));
            }
        }

        if (this.auraRenderer != null)
        {
            float pulse = 0.45f + Mathf.PingPong(Time.time * 0.8f, 0.35f);
            this.auraRenderer.color = new Color(1f, 0.88f, 0.28f, pulse);
        }

        bool isUnlocked = this.IsRequirementFulfilled();
        if (this.pickupCollider != null)
        {
            this.pickupCollider.enabled = isUnlocked;
        }

        if (this.shieldVfx != null)
        {
            this.shieldVfx.SetActive(!isUnlocked);
        }

        if (this.promptText != null)
        {
            int displayIndex = Mathf.Clamp(this.fragmentIndex + 1, 1, 4);
            this.promptText.text = isUnlocked
                ? $"🔑 {this.fragmentName} [{displayIndex}/4]\n<size=2.1>[Chạm vào bệ đá để nhặt]</size>"
                : "🔒 ĐANG ĐƯỢC PHONG ẤN";
            this.promptText.color = isUnlocked ? this.fragmentGlowColor : new Color(0.95f, 0.4f, 0.4f);
        }
    }

    public bool IsRequirementFulfilled()
    {
        if (this.runePuzzle != null && !this.runePuzzle.IsPuzzleSolved)
        {
            return false;
        }

        if (this.guardianEnemies != null && this.guardianEnemies.Length > 0)
        {
            for (int i = 0; i < this.guardianEnemies.Length; i++)
            {
                if (this.guardianEnemies[i] != null && this.guardianEnemies[i].activeInHierarchy)
                {
                    DamageReceiver receiver = this.guardianEnemies[i].GetComponentInChildren<DamageReceiver>();
                    if (receiver != null && !receiver.IsDead) return false;
                }
            }
        }

        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (this.isCollected) return;
        if (other == null || other.GetComponentInParent<PlayerCtrl>() == null) return;
        if (!this.IsRequirementFulfilled()) return;

        this.Collect();
    }

    public void Collect()
    {
        if (this.isCollected) return;
        this.isCollected = true;

        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("Pickup", 1.2f);
        }

        LostTreasureManager.Instance.CollectFragment(this.fragmentIndex, this.fragmentName, this.collectStoryLine);
        StartCoroutine(this.PlayCollectAnimation());
    }

    private IEnumerator PlayCollectAnimation()
    {
        if (this.pickupCollider != null) this.pickupCollider.enabled = false;
        if (this.promptText != null) Destroy(this.promptText.gameObject);
        if (this.shieldVfx != null) Destroy(this.shieldVfx);

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * 1.4f;
        Vector3 startScale = transform.localScale;

        const float duration = 0.4f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float norm = t / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, norm);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, norm);
            yield return null;
        }

        TreasureMapUI.Instance?.Pulse();
        gameObject.SetActive(false);
    }

    private void CreatePedestalAndAura()
    {
        GameObject pedestalObj = new GameObject("KeyStonePedestal");
        pedestalObj.transform.SetParent(transform, false);
        pedestalObj.transform.localPosition = new Vector3(0f, -0.15f, 0f);
        pedestalObj.transform.localScale = new Vector3(1.5f, 1.3f, 1f);

        SpriteRenderer pedestalSr = pedestalObj.AddComponent<SpriteRenderer>();
        pedestalSr.sprite = CreatePedestalSprite();
        pedestalSr.sortingLayerName = "Effects";
        pedestalSr.sortingOrder = 23;

        GameObject auraObj = new GameObject("KeyGoldenAura");
        auraObj.transform.SetParent(transform, false);
        auraObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        auraObj.transform.localScale = Vector3.one * 2.1f;

        this.auraRenderer = auraObj.AddComponent<SpriteRenderer>();
        this.auraRenderer.sprite = CreateAuraSprite();
        this.auraRenderer.color = new Color(1f, 0.88f, 0.28f, 0.55f);
        this.auraRenderer.sortingLayerName = "Effects";
        this.auraRenderer.sortingOrder = 24;
    }

    private void CreateWorldPrompt()
    {
        GameObject promptObj = new GameObject("PromptText");
        promptObj.transform.SetParent(transform, false);
        promptObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);

        this.promptText = promptObj.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            this.promptText.font = TMP_Settings.defaultFontAsset;
        }
        int displayIndex = Mathf.Clamp(this.fragmentIndex + 1, 1, 4);
        this.promptText.text = $"🔑 {this.fragmentName} [{displayIndex}/4]\n<size=2.1>[Chạm vào bệ đá để nhặt]</size>";
        this.promptText.fontSize = 3.0f;
        this.promptText.alignment = TextAlignmentOptions.Center;
        this.promptText.color = this.fragmentGlowColor;
        this.promptText.sortingLayerID = SortingLayer.NameToID("Effects");
        this.promptText.sortingOrder = 30;
    }

    private void CreateShieldVisual()
    {
        this.shieldVfx = new GameObject("ShieldVfx");
        this.shieldVfx.transform.SetParent(transform, false);
        this.shieldVfx.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        this.shieldVfx.transform.localScale = Vector3.one * 1.8f;

        SpriteRenderer sr = this.shieldVfx.AddComponent<SpriteRenderer>();
        sr.sprite = CreateAuraSprite();
        sr.color = new Color(0.35f, 0.65f, 1f, 0.5f);
        sr.sortingOrder = 25;
    }

    public static Sprite CreateProceduralKeySprite()
    {
        Texture2D texture = new Texture2D(24, 24, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color darkOutline = new Color(0.38f, 0.22f, 0.04f, 1f);
        Color gold = new Color(1f, 0.84f, 0.20f, 1f);
        Color highlight = new Color(1f, 0.98f, 0.72f, 1f);
        Color gem = new Color(0.25f, 0.95f, 1f, 1f);

        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 24; x++)
            {
                texture.SetPixel(x, y, clear);

                // Key bow (ring at top: y = 14..22, x = 7..16)
                int dx = x - 11;
                int dy = y - 18;
                int distSq = dx * dx + dy * dy;
                if (distSq <= 25 && distSq >= 6)
                {
                    texture.SetPixel(x, y, distSq >= 18 ? darkOutline : (y >= 19 ? highlight : gold));
                }
                else if (distSq < 6 && distSq <= 2)
                {
                    texture.SetPixel(x, y, gem);
                }

                // Key shaft (vertical stem: x = 10..13, y = 3..14)
                if (x >= 10 && x <= 13 && y >= 3 && y <= 14)
                {
                    bool edge = x == 10 || x == 13 || y == 3;
                    texture.SetPixel(x, y, edge ? darkOutline : (x == 11 ? highlight : gold));
                }

                // Key teeth (right side prongs: x = 14..17, y = 4..6 and y = 8..9)
                if (x >= 14 && x <= 17 && ((y >= 4 && y <= 6) || (y >= 8 && y <= 9)))
                {
                    bool edge = x == 17 || y == 4 || y == 8;
                    texture.SetPixel(x, y, edge ? darkOutline : gold);
                }
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f), 16);
    }

    public static Sprite CreatePedestalSprite()
    {
        Texture2D tex = new Texture2D(28, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color stoneDark = new Color(0.24f, 0.25f, 0.30f, 1f);
        Color stoneMid = new Color(0.42f, 0.44f, 0.50f, 1f);
        Color goldTrim = new Color(0.95f, 0.78f, 0.26f, 1f);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 28; x++)
            {
                bool bottomStep = y >= 0 && y <= 4 && x >= 2 && x <= 25;
                bool midStep = y >= 5 && y <= 9 && x >= 5 && x <= 22;
                bool topAltar = y >= 10 && y <= 13 && x >= 7 && x <= 20;

                if (topAltar)
                {
                    tex.SetPixel(x, y, y == 13 ? goldTrim : stoneMid);
                }
                else if (midStep)
                {
                    tex.SetPixel(x, y, (x == 5 || x == 22 || y == 5) ? stoneDark : stoneMid);
                }
                else if (bottomStep)
                {
                    tex.SetPixel(x, y, (y == 0 || x == 2 || x == 25) ? stoneDark : stoneMid);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 28, 16), new Vector2(0.5f, 0.5f), 16);
    }

    public static Sprite CreateAuraSprite()
    {
        Texture2D tex = new Texture2D(24, 24, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(11.5f, 11.5f);
        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 24; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / 11.5f;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha * 0.85f;
                tex.SetPixel(x, y, new Color(1f, 0.9f, 0.35f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f), 16);
    }
}
