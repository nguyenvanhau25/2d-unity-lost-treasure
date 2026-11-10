using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrcAttack : EnemyAttack
{
    [Header("Orc Attack")]
    [SerializeField] protected Transform enemyDamSender;
    [SerializeField] protected float orcDisAttackLimit = 1.65f;
    [SerializeField] protected float orcAttackTime = 0.4f;
    [SerializeField] protected float orcAttackCoolDown = 2.8f;

    protected override void Start()
    {
        base.Start();
        this.orcDisAttackLimit = Mathf.Min(this.orcDisAttackLimit, 1.65f);
        this.orcAttackCoolDown = Mathf.Max(this.orcAttackCoolDown, 2.8f);
        this.disAttackLimit = this.orcDisAttackLimit;
        this.attackCoolDown = this.orcAttackCoolDown;
    }

    public void ApplyDarkForestBalance()
    {
        this.orcDisAttackLimit = 1.35f;
        this.orcAttackTime = 0.3f;
        this.orcAttackCoolDown = 3.2f;
        this.disAttackLimit = this.orcDisAttackLimit;
        this.attackTime = this.orcAttackTime;
        this.attackCoolDown = this.orcAttackCoolDown;
    }
    protected override void ResetValue()
    {
        base.ResetValue();
        this.disAttackLimit = this.orcDisAttackLimit;
        this.attackTime = this.orcAttackTime;
        this.attackCoolDown = this.orcAttackCoolDown;
    }
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadEnemyDamSender();
    }
    protected void LoadEnemyDamSender()
    {
        if (this.enemyDamSender != null) return;
        if (transform.parent != null)
            this.enemyDamSender = transform.parent.Find("EnemyDamSender");
    }
    protected override void Attack()
    {
        if (this.enemyDamSender != null) this.enemyDamSender.gameObject.SetActive(true);
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("EnemyAttack");
        base.Attack();
    }
    protected override void StopAttack()
    {
        if (this.attackTimeCounter <= 0f && this.isAttack)
        {
            this.isAttack = false;
            if (this.enemyDamSender != null) this.enemyDamSender.gameObject.SetActive(false);
        }
    }
}
