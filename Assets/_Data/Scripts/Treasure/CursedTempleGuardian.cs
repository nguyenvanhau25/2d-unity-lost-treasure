using UnityEngine;

public sealed class CursedTempleGuardian : DamageReceiver
{
    [SerializeField] private int guardianHealth = 20;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float contactCooldown = 1.4f;
    [SerializeField] private float movementSpeed = 1.4f;

    private CursedTempleChallenge challenge;
    private SpriteRenderer spriteRenderer;
    private float nextContactDamageTime;

    public void Configure(CursedTempleChallenge templeChallenge)
    {
        this.challenge = templeChallenge;
    }

    protected override void Awake()
    {
        base.Awake();
        this.spriteRenderer = GetComponent<SpriteRenderer>();
        this.hpMax = this.guardianHealth;
        this.hurtTime = 0.35f;
    }

    protected override void Reborn()
    {
        this.hpMax = this.guardianHealth;
        base.Reborn();
    }

    protected override void Update()
    {
        base.Update();
        if (this.isDead || PlayerCtrl.Instance == null) return;

        Transform player = PlayerCtrl.Instance.transform;
        if (Vector2.Distance(transform.position, player.position) > 1.5f)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, this.movementSpeed * Time.deltaTime);
        }
    }

    public override void Deduct(int damage)
    {
        if (damage <= 0 || this.isDead || this.isHurt) return;
        base.Deduct(damage);
        if (this.spriteRenderer != null && !this.isDead)
            this.spriteRenderer.color = new Color(1f, 0.45f, 0.5f, 1f);
    }

    protected override void StopHurt()
    {
        base.StopHurt();
        if (!this.isHurt && this.spriteRenderer != null)
            this.spriteRenderer.color = Color.white;
    }

    protected override void OnDead()
    {
        Collider2D guardianCollider = GetComponent<Collider2D>();
        if (guardianCollider != null) guardianCollider.enabled = false;
        if (this.spriteRenderer != null) this.spriteRenderer.color = new Color(1f, 0.85f, 0.35f, 0.65f);
        this.challenge?.OnGuardianDefeated(transform.position);
        Destroy(gameObject, 0.5f);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (this.isDead || Time.time < this.nextContactDamageTime) return;
        PlayerCtrl player = collision.collider.GetComponentInParent<PlayerCtrl>();
        if (player == null || player.PlayerDamReceive == null) return;

        player.PlayerDamReceive.Deduct(this.contactDamage);
        this.nextContactDamageTime = Time.time + this.contactCooldown;
    }
}
