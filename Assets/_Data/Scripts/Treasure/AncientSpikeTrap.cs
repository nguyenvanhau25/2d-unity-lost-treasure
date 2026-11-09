using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Ancient Spike Trap with prominent visuals, warning runes, and timing.
/// Players can Dash (Space) to pass without taking damage.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AncientSpikeTrap : MonoBehaviour
{
    [Header("Trap Timing")]
    [SerializeField] private float idleDuration = 1.8f;
    [SerializeField] private float warningDuration = 0.5f;
    [SerializeField] private float activeDuration = 0.9f;
    [SerializeField] private float retractDuration = 0.3f;
    [SerializeField] private float startDelay = 0f;

    [Header("Damage")]
    [SerializeField] private int trapDamage = 1;

    [Header("Visual Colors")]
    [SerializeField] private Color idleColor = new Color(0.6f, 0.6f, 0.65f, 0.85f);
    [SerializeField] private Color warningColor = new Color(1f, 0.55f, 0.15f, 1f);
    [SerializeField] private Color activeColor = new Color(1f, 0.2f, 0.2f, 1f);

    private SpriteRenderer spriteRenderer;
    private Collider2D trapCollider;
    private TextMeshPro warningLabel;
    private bool isDamaging;
    private readonly HashSet<PlayerDamReceive> damagedPlayersThisCycle = new HashSet<PlayerDamReceive>();

    public float StartDelay { get => this.startDelay; set => this.startDelay = value; }

    private void Awake()
    {
        this.trapCollider = GetComponent<Collider2D>();
        this.trapCollider.isTrigger = true;

        this.spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (this.spriteRenderer == null)
        {
            this.spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            this.spriteRenderer.sprite = CreateSpikeSprite();
        }
        this.spriteRenderer.sortingOrder = 15; // In front of ground and decorations

        this.CreateWarningPrompt();
    }

    private void Start()
    {
        StartCoroutine(this.TrapLoop());
    }

    private IEnumerator TrapLoop()
    {
        if (this.startDelay > 0f) yield return new WaitForSeconds(this.startDelay);

        while (true)
        {
            // State: Idle (Safe, spikes peeking)
            this.isDamaging = false;
            this.SetVisuals(this.idleColor, 0.45f);
            if (this.warningLabel != null) this.warningLabel.text = "⚠️ BẪY GAI";
            yield return new WaitForSeconds(this.idleDuration);

            // State: Warning (Flashing red/orange, shaking)
            if (this.warningLabel != null)
            {
                this.warningLabel.text = "⚡ CẨN THẬN!";
                this.warningLabel.color = this.warningColor;
            }
            Vector3 originPos = transform.position;
            float elapsedWarn = 0f;
            while (elapsedWarn < this.warningDuration)
            {
                elapsedWarn += Time.deltaTime;
                float shake = Mathf.Sin(elapsedWarn * 40f) * 0.04f;
                transform.position = originPos + new Vector3(shake, 0f, 0f);
                this.SetVisuals(Color.Lerp(this.idleColor, this.warningColor, elapsedWarn / this.warningDuration), 0.7f);
                yield return null;
            }
            transform.position = originPos;

            // State: Active (Spikes thrust upwards!)
            this.damagedPlayersThisCycle.Clear();
            this.isDamaging = true;
            this.SetVisuals(this.activeColor, 1.25f);
            if (this.warningLabel != null)
            {
                this.warningLabel.text = "💥 ĐÂM!";
                this.warningLabel.color = Color.red;
            }
            if (AudioManagerr.Instance != null)
            {
                AudioManagerr.Instance.PlaySFX("SwordSlash", 0.5f);
            }
            this.DamageOverlappingPlayers();
            yield return new WaitForSeconds(this.activeDuration);

            // State: Retracting
            this.isDamaging = false;
            this.SetVisuals(this.idleColor, 0.45f);
            yield return new WaitForSeconds(this.retractDuration);
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
        if (player != null)
        {
            // Dash grants invulnerability through traps!
            if (player.PlayerDash != null && player.PlayerDash.IsDashing)
            {
                return;
            }

            if (player.PlayerDamReceive != null && !player.PlayerDamReceive.IsDead
                && !player.PlayerDamReceive.IsHurt && this.damagedPlayersThisCycle.Add(player.PlayerDamReceive))
            {
                player.PlayerDamReceive.Deduct(this.trapDamage);
            }
        }
    }

    private void DamageOverlappingPlayers()
    {
        if (this.trapCollider == null) return;
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        Collider2D[] results = new Collider2D[4];
        int count = this.trapCollider.Overlap(filter, results);
        for (int i = 0; i < count; i++)
        {
            this.TryDamageEntity(results[i]);
        }
    }

    private void SetVisuals(Color color, float scaleY)
    {
        if (this.spriteRenderer != null) this.spriteRenderer.color = color;
        transform.localScale = new Vector3(1.2f, scaleY, 1f);
    }

    private void CreateWarningPrompt()
    {
        GameObject textObj = new GameObject("TrapWarningLabel");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);

        this.warningLabel = textObj.AddComponent<TextMeshPro>();
        this.warningLabel.text = "⚠️ BẪY GAI";
        this.warningLabel.fontSize = 2.4f;
        this.warningLabel.alignment = TextAlignmentOptions.Center;
        this.warningLabel.color = new Color(1f, 0.75f, 0.25f, 0.85f);
        this.warningLabel.sortingOrder = 16;
    }

    private static Sprite CreateSpikeSprite()
    {
        Texture2D tex = new Texture2D(32, 20, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);
        Color steel = new Color(0.85f, 0.88f, 0.92f, 1f);
        Color baseIron = new Color(0.25f, 0.28f, 0.32f, 1f);
        Color bloodTip = new Color(0.9f, 0.2f, 0.2f, 1f);

        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                // Base plate at bottom
                if (y < 4)
                {
                    tex.SetPixel(x, y, baseIron);
                    continue;
                }

                // 3 spikes across width
                int spikeX = x % 10;
                int slope = (spikeX < 5) ? spikeX * 4 : (9 - spikeX) * 4;
                if ((y - 4) <= slope)
                {
                    tex.SetPixel(x, y, (y > 15) ? bloodTip : steel);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 20), new Vector2(0.5f, 0f), 16);
    }
}
