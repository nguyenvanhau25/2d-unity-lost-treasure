using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum SpikeTrapType
{
    Fixed = 0,          // Chông gai cố định (luôn nhô lên, người chơi cần né hoặc Dash qua)
    Periodic = 1,       // Chông gai bật lên theo chu kỳ (ẩn -> cảnh báo -> đâm lên -> thu về)
    StepTriggered = 2   // Bẫy kích hoạt khi người chơi bước qua (đạp lên -> cảnh báo ngắn -> đâm lên)
}

/// <summary>
/// Hệ thống Bẫy Chông Gai Cổ Đại hỗ trợ 3 chế độ hoạt động:
/// 1. Fixed (Cố định)
/// 2. Periodic (Theo chu kỳ)
/// 3. StepTriggered (Kích hoạt khi bước qua)
/// Có hiệu ứng hình ảnh trực quan, cảnh báo rõ ràng và gây sát thương hợp lý.
/// Người chơi có thể quan sát nhịp bẫy, đi vòng qua khoảng trống hoặc dùng Dash (Space) để vượt qua.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AncientSpikeTrap : MonoBehaviour
{
    [Header("Trap Configuration")]
    [SerializeField] private SpikeTrapType trapType = SpikeTrapType.Periodic;
    [SerializeField] private float idleDuration = 2.0f;
    [SerializeField] private float warningDuration = 0.6f;
    [SerializeField] private float activeDuration = 0.85f;
    [SerializeField] private float retractDuration = 0.4f;
    [SerializeField] private float startDelay = 0f;

    [Header("Damage")]
    [SerializeField] private int trapDamage = 1;
    [SerializeField] private float fixedDamageCooldown = 1.15f;

    [Header("Visual Colors")]
    [SerializeField] private Color idleColor = new Color(0.58f, 0.62f, 0.68f, 0.88f);
    [SerializeField] private Color stepReadyColor = new Color(0.75f, 0.62f, 0.28f, 0.92f);
    [SerializeField] private Color warningColor = new Color(1f, 0.6f, 0.12f, 1f);
    [SerializeField] private Color activeColor = new Color(1f, 0.22f, 0.22f, 1f);
    [SerializeField] private Color fixedColor = new Color(0.92f, 0.28f, 0.22f, 1f);

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer basePlateRenderer;
    private Collider2D trapCollider;
    private TextMeshPro warningLabel;
    private bool isDamaging;
    private bool stepSequenceRunning;
    private float lastFixedDamageTime = -10f;
    private Vector3 initialPosition;
    private readonly HashSet<PlayerDamReceive> damagedPlayersThisCycle = new HashSet<PlayerDamReceive>();

    public SpikeTrapType TrapType { get => this.trapType; set => this.trapType = value; }
    public float StartDelay { get => this.startDelay; set => this.startDelay = value; }
    public int TrapDamage { get => this.trapDamage; set => this.trapDamage = Mathf.Max(1, value); }

    public void Configure(SpikeTrapType type, float delay = 0f, int damage = 1, float idle = 2.0f, float warn = 0.6f, float active = 0.85f)
    {
        this.trapType = type;
        this.startDelay = delay;
        this.trapDamage = Mathf.Max(1, damage);
        this.idleDuration = Mathf.Max(0.5f, idle);
        this.warningDuration = Mathf.Max(0.35f, warn);
        this.activeDuration = Mathf.Max(0.4f, active);
    }

    private void Awake()
    {
        this.initialPosition = transform.position;
        this.trapCollider = GetComponent<Collider2D>();
        this.trapCollider.isTrigger = true;

        BoxCollider2D box = this.trapCollider as BoxCollider2D;
        if (box != null)
        {
            box.size = new Vector2(1.45f, 0.78f);
            box.offset = new Vector2(0f, 0.36f);
        }

        this.CreateBasePlate();

        this.spriteRenderer = GetComponent<SpriteRenderer>();
        if (this.spriteRenderer == null)
        {
            this.spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        this.spriteRenderer.sprite = CreateSpikeSprite();
        this.spriteRenderer.sortingOrder = 15;

        this.CreateWarningPrompt();
    }

    private void Start()
    {
        this.initialPosition = transform.position;
        StopAllCoroutines();

        switch (this.trapType)
        {
            case SpikeTrapType.Fixed:
                StartCoroutine(this.FixedTrapLoop());
                break;
            case SpikeTrapType.StepTriggered:
                this.EnterStepReadyState();
                break;
            case SpikeTrapType.Periodic:
            default:
                StartCoroutine(this.PeriodicTrapLoop());
                break;
        }
    }

    private IEnumerator FixedTrapLoop()
    {
        this.isDamaging = true;
        this.damagedPlayersThisCycle.Clear();

        if (this.warningLabel != null)
        {
            this.warningLabel.text = "⚔ CHÔNG CỐ ĐỊNH";
            this.warningLabel.color = new Color(1f, 0.42f, 0.32f, 0.95f);
        }

        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime * 3.2f;
            float pulse = (Mathf.Sin(timer) + 1f) * 0.5f;
            Color c = Color.Lerp(this.fixedColor, new Color(1f, 0.5f, 0.3f, 1f), pulse * 0.35f);
            this.SetVisuals(c, 1.05f + pulse * 0.08f);
            yield return null;
        }
    }

    private IEnumerator PeriodicTrapLoop()
    {
        if (this.startDelay > 0f)
        {
            this.isDamaging = false;
            this.SetVisuals(this.idleColor, 0.38f);
            yield return new WaitForSeconds(this.startDelay);
        }

        while (true)
        {
            // 1. Idle (Safe retracted spikes)
            this.isDamaging = false;
            this.SetVisuals(this.idleColor, 0.38f);
            if (this.warningLabel != null)
            {
                this.warningLabel.text = "⚙ BẪY CHU KỲ";
                this.warningLabel.color = new Color(0.85f, 0.88f, 0.92f, 0.85f);
            }
            yield return new WaitForSeconds(this.idleDuration);

            // 2. Warning (Trembling & glowing orange)
            yield return this.PlayWarningPhase("⚠ SẮP BẬT!", this.warningDuration);

            // 3. Active (Spikes thrust upwards)
            yield return this.PlayActivePhase("💥 CHÔNG BẬT!", this.activeDuration);

            // 4. Retracting
            this.isDamaging = false;
            this.SetVisuals(this.idleColor, 0.38f);
            yield return new WaitForSeconds(this.retractDuration);
        }
    }

    private void EnterStepReadyState()
    {
        this.stepSequenceRunning = false;
        this.isDamaging = false;
        transform.position = this.initialPosition;
        this.SetVisuals(this.stepReadyColor, 0.42f);
        if (this.warningLabel != null)
        {
            this.warningLabel.text = "👣 BẪY ĐẠP";
            this.warningLabel.color = new Color(1f, 0.82f, 0.32f, 0.9f);
        }
    }

    private IEnumerator StepTriggeredSequence()
    {
        this.stepSequenceRunning = true;

        // Reaction window so player can walk out or Dash after stepping on the plate
        yield return this.PlayWarningPhase("⚡ KÍCH HOẠT!", this.warningDuration);

        // Spikes thrust upwards
        yield return this.PlayActivePhase("💥 ĐÂM LÊN!", this.activeDuration);

        // Cooldown before resetting pressure plate
        this.isDamaging = false;
        this.SetVisuals(this.idleColor, 0.35f);
        if (this.warningLabel != null)
        {
            this.warningLabel.text = "⏳ THU CHÔNG...";
            this.warningLabel.color = this.idleColor;
        }
        yield return new WaitForSeconds(Mathf.Max(0.8f, this.retractDuration + 0.5f));

        this.EnterStepReadyState();
    }

    private IEnumerator PlayWarningPhase(string labelText, float duration)
    {
        if (this.warningLabel != null)
        {
            this.warningLabel.text = labelText;
            this.warningLabel.color = this.warningColor;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float shake = Mathf.Sin(elapsed * 45f) * 0.045f;
            transform.position = this.initialPosition + new Vector3(shake, 0f, 0f);
            this.SetVisuals(Color.Lerp(this.idleColor, this.warningColor, t), Mathf.Lerp(0.45f, 0.72f, t));
            yield return null;
        }

        transform.position = this.initialPosition;
    }

    private IEnumerator PlayActivePhase(string labelText, float duration)
    {
        this.damagedPlayersThisCycle.Clear();
        this.isDamaging = true;
        this.SetVisuals(this.activeColor, 1.22f);

        if (this.warningLabel != null)
        {
            this.warningLabel.text = labelText;
            this.warningLabel.color = new Color(1f, 0.25f, 0.25f, 1f);
        }

        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("SwordSlash", 0.38f);
        }

        this.DamageOverlappingPlayers();
        yield return new WaitForSeconds(duration);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (this.trapType == SpikeTrapType.StepTriggered && !this.stepSequenceRunning)
        {
            PlayerCtrl player = other != null ? other.GetComponentInParent<PlayerCtrl>() : null;
            if (player != null && player.PlayerDamReceive != null && !player.PlayerDamReceive.IsDead)
            {
                StartCoroutine(this.StepTriggeredSequence());
                return;
            }
        }

        if (this.isDamaging)
        {
            this.TryDamageEntity(other);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!this.isDamaging) return;
        this.TryDamageEntity(other);
    }

    private void TryDamageEntity(Collider2D other)
    {
        if (other == null) return;

        PlayerCtrl player = other.GetComponentInParent<PlayerCtrl>();
        if (player == null || player.PlayerDamReceive == null || player.PlayerDamReceive.IsDead)
        {
            return;
        }

        // Dash grants invulnerability through all spike traps
        if (player.PlayerDash != null && player.PlayerDash.IsDashing)
        {
            return;
        }

        if (player.PlayerDamReceive.IsHurt)
        {
            return;
        }

        if (this.trapType == SpikeTrapType.Fixed)
        {
            if (Time.time - this.lastFixedDamageTime < this.fixedDamageCooldown)
            {
                return;
            }
            this.lastFixedDamageTime = Time.time;
            player.PlayerDamReceive.Deduct(this.trapDamage);
            return;
        }

        if (this.damagedPlayersThisCycle.Add(player.PlayerDamReceive))
        {
            player.PlayerDamReceive.Deduct(this.trapDamage);
        }
    }

    private void DamageOverlappingPlayers()
    {
        if (this.trapCollider == null) return;
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        Collider2D[] results = new Collider2D[6];
        int count = this.trapCollider.Overlap(filter, results);
        for (int i = 0; i < count; i++)
        {
            this.TryDamageEntity(results[i]);
        }
    }

    private void SetVisuals(Color color, float scaleY)
    {
        if (this.spriteRenderer != null)
        {
            this.spriteRenderer.color = color;
        }

        if (this.basePlateRenderer != null)
        {
            this.basePlateRenderer.color = new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 0.92f);
        }

        transform.localScale = new Vector3(1.18f, scaleY, 1f);
    }

    private void CreateBasePlate()
    {
        Transform existingBase = transform.Find("TrapBasePlate");
        if (existingBase != null)
        {
            this.basePlateRenderer = existingBase.GetComponent<SpriteRenderer>();
            return;
        }

        GameObject baseObj = new GameObject("TrapBasePlate");
        baseObj.transform.SetParent(transform, false);
        baseObj.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        this.basePlateRenderer = baseObj.AddComponent<SpriteRenderer>();
        this.basePlateRenderer.sprite = CreateBasePlateSprite();
        this.basePlateRenderer.sortingOrder = 14;
    }

    private void CreateWarningPrompt()
    {
        Transform existingLabel = transform.Find("TrapWarningLabel");
        if (existingLabel != null)
        {
            this.warningLabel = existingLabel.GetComponent<TextMeshPro>();
            return;
        }

        GameObject textObj = new GameObject("TrapWarningLabel");
        textObj.transform.SetParent(transform, false);
        textObj.transform.localPosition = new Vector3(0f, 1.05f, 0f);

        this.warningLabel = textObj.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            this.warningLabel.font = TMP_Settings.defaultFontAsset;
        }
        this.warningLabel.text = "⚙ BẪY GAI";
        this.warningLabel.fontSize = 2.3f;
        this.warningLabel.alignment = TextAlignmentOptions.Center;
        this.warningLabel.color = new Color(1f, 0.78f, 0.28f, 0.9f);
        this.warningLabel.sortingOrder = 16;
    }

    private static Sprite CreateBasePlateSprite()
    {
        Texture2D tex = new Texture2D(36, 8, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color darkStone = new Color(0.18f, 0.2f, 0.25f, 0.95f);
        Color rim = new Color(0.42f, 0.45f, 0.52f, 0.95f);
        Color slot = new Color(0.06f, 0.06f, 0.08f, 1f);

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 36; x++)
            {
                if (x == 0 || x == 35 || y == 0 || y == 7)
                {
                    tex.SetPixel(x, y, rim);
                }
                else if (y >= 3 && y <= 5 && (x % 9 >= 2 && x % 9 <= 6))
                {
                    tex.SetPixel(x, y, slot);
                }
                else
                {
                    tex.SetPixel(x, y, darkStone);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 36, 8), new Vector2(0.5f, 0.5f), 16);
    }

    private static Sprite CreateSpikeSprite()
    {
        Texture2D tex = new Texture2D(36, 22, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);
        Color steel = new Color(0.86f, 0.9f, 0.95f, 1f);
        Color edge = new Color(0.48f, 0.54f, 0.62f, 1f);
        Color baseIron = new Color(0.24f, 0.27f, 0.32f, 1f);
        Color bloodTip = new Color(0.95f, 0.22f, 0.2f, 1f);

        for (int y = 0; y < 22; y++)
        {
            for (int x = 0; x < 36; x++)
            {
                if (y < 4)
                {
                    tex.SetPixel(x, y, baseIron);
                    continue;
                }

                // 4 distinct spikes across width
                int spikeX = x % 9;
                int halfWidth = 4 - (y - 4) / 5;
                int distFromCenter = Mathf.Abs(spikeX - 4);
                if (distFromCenter <= halfWidth && y < 21)
                {
                    if (y >= 16)
                        tex.SetPixel(x, y, bloodTip);
                    else if (distFromCenter == halfWidth)
                        tex.SetPixel(x, y, edge);
                    else
                        tex.SetPixel(x, y, steel);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 36, 22), new Vector2(0.5f, 0f), 16);
    }
}
