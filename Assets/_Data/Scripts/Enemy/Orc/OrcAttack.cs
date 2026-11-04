using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrcAttack : EnemyAttack
{
    [Header("Orc Attack")]
    [SerializeField] protected Transform enemyDamSender;
    [SerializeField] protected float orcDisAttackLimit = 2f;
    [SerializeField] protected float orcAttackTime = 0.5f;
    [SerializeField] protected float orcAttackCoolDown = 2f;
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
