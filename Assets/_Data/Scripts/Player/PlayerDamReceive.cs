using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDamReceive : DamageReceiver
{
    [Header("Player Dam Receive")]
    [SerializeField] protected int playerHpMax = 10;
    [SerializeField] protected float playerHurtTime = 0.5f;
    [SerializeField] protected string hitSoundName = "SwordBlood";
    [SerializeField, Range(0f, 1f)] protected float hitSoundVolume = 0.8f;
    [SerializeField] protected float meteorStunTime = 0.55f;
    [SerializeField] protected CapsuleCollider2D collide;
    [SerializeField] protected PlayerCtrl playerCtrl;
    [SerializeField] protected PlayerHpSO playerHpSO;
    protected override void Start()
    {
        base.Start();
        if (this.playerHpSO != null)
        {
            this.playerHpSO.maxHp = Mathf.Max(this.playerHpSO.maxHp, 35);
            if (this.playerHpSO.currentHp <= 0) this.playerHpSO.currentHp = this.playerHpSO.maxHp;
            this.hp = this.playerHpSO.currentHp;
            this.playerHpMax = this.playerHpSO.maxHp;
            this.hpMax = this.playerHpMax;
        }
        else
        {
            this.hp = 35;
            this.playerHpMax = 35;
            this.hpMax = 35;
        }
        this.hurtTime = 0.85f;
    }
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadCollider();
        this.LoadPlayerCtrl();
        this.LoadPlayerHpSO();
    }
    protected override void ResetValue()
    {
        base.ResetValue();
        this.Reborn();
    }
    protected void LoadCollider()
    {
        if (this.collide != null) return;
        this.collide = GetComponent<CapsuleCollider2D>();
    }
    protected void LoadPlayerCtrl()
    {
        if (this.playerCtrl != null) return;
        this.playerCtrl = GetComponentInParent<PlayerCtrl>();
    }
    protected void LoadPlayerHpSO()
    {
        if (this.playerHpSO != null) return;
        this.playerHpSO = Resources.Load<PlayerHpSO>("GameData/PlayerHpSO");
    }
    protected override void Reborn()
    {
        if (this.playerHpSO == null)
        {
            this.hp = Mathf.Max(1, this.hp);
            return;
        }
        this.hpMax = this.playerHpSO.maxHp;
        this.hurtTime = this.playerHurtTime;
        this.hp = this.playerHpSO.currentHp;
        this.playerHpMax = this.playerHpSO.maxHp;
    }
    protected override void OnDead()
    {
        this.StopMovement();
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.ClockMusic();
        if (RestartToggle.Instance != null) RestartToggle.Instance.RestartGameMenu();
    }

    public override void Deduct(int damage)
    {
        int previousHp = this.hp;
        base.Deduct(damage);
        if (this.hp >= previousHp) return;

        if (AudioManagerr.Instance != null)
            AudioManagerr.Instance.PlaySFX(this.hitSoundName, this.hitSoundVolume);
    }

    /// <summary>
    /// Reuses the player's existing Hurt animation/state when a falling rock
    /// actually hits them. IsHurt blocks movement, attacks and shooting, so it
    /// also provides a brief, readable stun and prevents meteor hit spam.
    /// </summary>
    public bool ReceiveMeteorHit(int damage)
    {
        if (this.isDead || this.isHurt) return false;

        this.hurtTime = this.meteorStunTime;
        this.hurtTimeCounter = 0f;
        this.Deduct(damage);

        return true;
    }
    protected void StopMovement()
    {
        if (this.collide != null) this.collide.enabled = false;
        if (transform.parent == null) return;
        Vector3 posDead = transform.parent.position;
        posDead.z = 1;
        transform.parent.position = posDead;
        if (this.playerCtrl != null && this.playerCtrl.PlayerMovement != null && this.playerCtrl.PlayerMovement._Rb != null)
            this.playerCtrl.PlayerMovement._Rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }
}
