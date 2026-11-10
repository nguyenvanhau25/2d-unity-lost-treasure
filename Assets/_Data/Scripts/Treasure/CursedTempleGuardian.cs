using TMPro;
using UnityEngine;

/// <summary>
/// Guardian — Kẻ Canh Giữ Ngôi Đền Cổ Bị Nguyền Rủa.
/// Sau khi người chơi giải đố 3 Cột Cổ Ngữ để mở Cửa Phong Ấn, Guardian sẽ thức tỉnh.
/// Có thanh máu trực quan, đòn lao (Lunge) có cảnh báo trước và khoảng nghỉ để người chơi phản công.
/// Khi bị đánh bại sẽ rơi ra Mảnh Chìa Khóa Đền Cổ (mảnh thứ 4).
/// </summary>
public sealed class CursedTempleGuardian : DamageReceiver
{
    [Header("Guardian Balance")]
    [SerializeField] private int guardianHealth = 18;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float contactCooldown = 1.6f;
    [SerializeField] private float movementSpeed = 1.6f;
    [SerializeField] private float lungeSpeed = 4.2f;
    [SerializeField] private float attackInterval = 3.4f;

    private CursedTempleChallenge challenge;
    private SpriteRenderer spriteRenderer;
    private TextMeshPro statusLabel;
    private float nextContactDamageTime;
    private float attackTimer;
    private bool isWarningLunge;
    private bool isLunging;
    private bool isRecovering;
    private float stateTimer;
    private Vector2 lungeDirection;

    public void Configure(CursedTempleChallenge templeChallenge)
    {
        this.challenge = templeChallenge;
    }

    protected override void Awake()
    {
        base.Awake();
        gameObject.tag = "Enemy";

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        this.spriteRenderer = GetComponent<SpriteRenderer>();
        this.hpMax = this.guardianHealth;
        this.hp = this.guardianHealth;
        this.hurtTime = 0.28f;

        this.CreateGuardianHudLabel();
        this.UpdateHudLabel();
    }

    protected override void Reborn()
    {
        this.hpMax = this.guardianHealth;
        base.Reborn();
        this.UpdateHudLabel();
    }

    protected override void Update()
    {
        base.Update();
        if (this.isDead || PlayerCtrl.Instance == null) return;

        Transform player = PlayerCtrl.Instance.transform;
        Vector2 toPlayer = player.position - transform.position;

        if (this.spriteRenderer != null && Mathf.Abs(toPlayer.x) > 0.1f)
        {
            this.spriteRenderer.flipX = toPlayer.x < 0f;
        }

        // State machine: Normal Chase -> Telegraph Warning (0.65s) -> Lunge (0.45s) -> Vulnerable Recovery (1.1s)
        if (this.isWarningLunge)
        {
            this.stateTimer -= Time.deltaTime;
            float flash = (Mathf.Sin(Time.time * 28f) + 1f) * 0.5f;
            if (this.spriteRenderer != null && !this.isHurt)
            {
                this.spriteRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.72f, 0.15f, 1f), flash);
            }

            if (this.stateTimer <= 0f)
            {
                this.isWarningLunge = false;
                this.isLunging = true;
                this.stateTimer = 0.45f;
                this.lungeDirection = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : Vector2.down;
                if (AudioManagerr.Instance != null)
                {
                    AudioManagerr.Instance.PlaySFX("SwordSlash", 0.55f);
                }
            }
            this.UpdateHudLabel();
            return;
        }

        if (this.isLunging)
        {
            this.stateTimer -= Time.deltaTime;
            Vector3 nextPos = transform.position + (Vector3)(this.lungeDirection * (this.lungeSpeed * Time.deltaTime));
            nextPos.x = Mathf.Clamp(nextPos.x, -12.5f, 12.5f);
            nextPos.y = Mathf.Clamp(nextPos.y, -3.2f, 11.2f);
            transform.position = nextPos;

            if (this.stateTimer <= 0f)
            {
                this.isLunging = false;
                this.isRecovering = true;
                this.stateTimer = 1.15f; // Vulnerability window for player to counter-attack
                if (this.spriteRenderer != null && !this.isHurt)
                {
                    this.spriteRenderer.color = new Color(0.7f, 0.85f, 1f, 1f);
                }
            }
            this.UpdateHudLabel();
            return;
        }

        if (this.isRecovering)
        {
            this.stateTimer -= Time.deltaTime;
            if (this.stateTimer <= 0f)
            {
                this.isRecovering = false;
                if (this.spriteRenderer != null && !this.isHurt)
                {
                    this.spriteRenderer.color = Color.white;
                }
            }
            this.UpdateHudLabel();
            return;
        }

        // Normal pursuit
        if (toPlayer.magnitude > 1.1f)
        {
            Vector3 nextPos = Vector2.MoveTowards(transform.position, player.position, this.movementSpeed * Time.deltaTime);
            nextPos.x = Mathf.Clamp(nextPos.x, -12.5f, 12.5f);
            nextPos.y = Mathf.Clamp(nextPos.y, -3.2f, 11.2f);
            transform.position = nextPos;
        }

        this.attackTimer += Time.deltaTime;
        if (this.attackTimer >= this.attackInterval && toPlayer.magnitude <= 7.5f)
        {
            this.attackTimer = 0f;
            this.isWarningLunge = true;
            this.stateTimer = 0.65f;
        }

        this.UpdateHudLabel();
    }

    public override void Deduct(int damage)
    {
        if (damage <= 0 || this.isDead || this.isHurt) return;
        base.Deduct(damage);

        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("SwordBlood", 0.65f);
        }

        if (this.spriteRenderer != null && !this.isDead)
        {
            this.spriteRenderer.color = new Color(1f, 0.38f, 0.42f, 1f);
        }
        this.UpdateHudLabel();
    }

    protected override void StopHurt()
    {
        base.StopHurt();
        if (!this.isHurt && this.spriteRenderer != null)
        {
            this.spriteRenderer.color = this.isRecovering ? new Color(0.7f, 0.85f, 1f, 1f) : Color.white;
        }
    }

    protected override void OnDead()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = false;
        }

        if (this.spriteRenderer != null)
        {
            this.spriteRenderer.color = new Color(1f, 0.85f, 0.35f, 0.65f);
        }

        if (this.statusLabel != null)
        {
            this.statusLabel.text = "✦ GUARDIAN ĐÃ NGÃ XUỐNG ✦";
            this.statusLabel.color = new Color(1f, 0.88f, 0.3f, 1f);
        }

        this.challenge?.OnGuardianDefeated(transform.position);
        Destroy(gameObject, 0.55f);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision != null)
        {
            this.TryDealContactDamage(collision.collider);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        this.TryDealContactDamage(other);
    }

    private void TryDealContactDamage(Collider2D other)
    {
        if (this.isDead || this.isRecovering || other == null || Time.time < this.nextContactDamageTime) return;

        PlayerCtrl player = other.GetComponentInParent<PlayerCtrl>();
        if (player == null || player.PlayerDamReceive == null || player.PlayerDamReceive.IsDead) return;

        if (player.PlayerDash != null && player.PlayerDash.IsDashing) return;

        player.PlayerDamReceive.Deduct(this.contactDamage);
        this.nextContactDamageTime = Time.time + this.contactCooldown;
    }

    private void CreateGuardianHudLabel()
    {
        GameObject labelObj = new GameObject("GuardianHudLabel");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 2.35f, 0f);

        this.statusLabel = labelObj.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            this.statusLabel.font = TMP_Settings.defaultFontAsset;
        }
        this.statusLabel.fontSize = 2.5f;
        this.statusLabel.alignment = TextAlignmentOptions.Center;
        this.statusLabel.color = new Color(1f, 0.85f, 0.32f, 1f);
        this.statusLabel.sortingOrder = 18;
    }

    private void UpdateHudLabel()
    {
        if (this.statusLabel == null || this.isDead) return;

        string stateHint = "";
        if (this.isWarningLunge)
        {
            stateHint = "\n<size=2.0><color=#FF9922>⚠ SẮP LAO TỚI — HÃY NÉ!</color></size>";
        }
        else if (this.isRecovering)
        {
            stateHint = "\n<size=2.0><color=#55FFCC>✦ ĐANG SƠ HỞ — TẤN CÔNG NGAY!</color></size>";
        }

        this.statusLabel.text = $"⚔ GUARDIAN ĐỀN CỔ [{Mathf.Max(0, this.hp)}/{this.hpMax} HP]{stateHint}";
    }
}
