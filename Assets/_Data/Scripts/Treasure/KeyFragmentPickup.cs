using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// A collectable Key Fragment in the world.
/// Features a floating hover animation, glowing aura, proximity prompt,
/// and optional guardian enemy requirement.
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
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float rotateSpeed = 0f;
    [SerializeField] private Color fragmentGlowColor = new Color(1f, 0.85f, 0.25f);

    private Vector3 initialPosition;
    private bool isCollected;
    private SpriteRenderer spriteRenderer;
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

        this.spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (this.spriteRenderer == null)
        {
            this.spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Auto ensure sprite exists
        if (this.spriteRenderer.sprite == null)
        {
            this.spriteRenderer.sprite = CreateProceduralKeySprite();
            this.spriteRenderer.color = this.fragmentGlowColor;
        }

        this.CreateWorldPrompt();
    }

    private void Start()
    {
        // If this fragment was already collected previously in the run, deactivate it
        if (LostTreasureManager.Instance.HasFragment(this.fragmentIndex))
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

        // Hover animation
        float newY = this.initialPosition.y + Mathf.Sin(Time.time * this.bobSpeed) * this.bobHeight;
        transform.position = new Vector3(this.initialPosition.x, newY, this.initialPosition.z);

        if (this.rotateSpeed != 0f)
        {
            transform.Rotate(Vector3.forward * (this.rotateSpeed * Time.deltaTime));
        }

        // Check if guardians are defeated
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
            this.promptText.text = isUnlocked ? $"✦ {this.fragmentName} ✦" : "🔒 ĐANG ĐƯỢC BẢO VỆ";
            this.promptText.color = isUnlocked ? this.fragmentGlowColor : new Color(0.8f, 0.3f, 0.3f);
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
        Vector3 targetPos = startPos + Vector3.up * 1.5f;
        Vector3 startScale = transform.localScale;

        const float duration = 0.45f;
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

    private void CreateWorldPrompt()
    {
        GameObject promptObj = new GameObject("PromptText");
        promptObj.transform.SetParent(transform);
        promptObj.transform.localPosition = new Vector3(0f, 0.75f, 0f);

        this.promptText = promptObj.AddComponent<TextMeshPro>();
        this.promptText.text = $"✦ {this.fragmentName} ✦";
        this.promptText.fontSize = 3.2f;
        this.promptText.alignment = TextAlignmentOptions.Center;
        this.promptText.color = this.fragmentGlowColor;
        this.promptText.sortingOrder = 10;
    }

    private void CreateShieldVisual()
    {
        this.shieldVfx = new GameObject("ShieldVfx");
        this.shieldVfx.transform.SetParent(transform);
        this.shieldVfx.transform.localPosition = Vector3.zero;
        this.shieldVfx.transform.localScale = Vector3.one * 1.4f;

        SpriteRenderer sr = this.shieldVfx.AddComponent<SpriteRenderer>();
        sr.sprite = CreateProceduralKeySprite();
        sr.color = new Color(0.4f, 0.6f, 1f, 0.45f);
        sr.sortingOrder = 9;
    }

    private static Sprite CreateProceduralKeySprite()
    {
        // 16x16 diamond shape
        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);
        Color gold = new Color(1f, 0.88f, 0.25f, 1f);
        Color bright = new Color(1f, 1f, 0.8f, 1f);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int dist = Mathf.Abs(x - 8) + Mathf.Abs(y - 8);
                if (dist <= 6)
                {
                    texture.SetPixel(x, y, dist <= 2 ? bright : gold);
                }
                else
                {
                    texture.SetPixel(x, y, clear);
                }
            }
        }
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }
}
