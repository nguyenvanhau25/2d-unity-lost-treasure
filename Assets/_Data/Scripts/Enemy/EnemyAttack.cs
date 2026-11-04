using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyAttack : EnemyAbstract
{
    [Header("Enemy Attack")]
    [SerializeField] protected bool isAttack;
    [SerializeField] protected float distanceAttack;
    [SerializeField] protected float disAttackLimit = 2f;
    [SerializeField] protected float attackTime = 0.5f;
    [SerializeField] protected float attackTimeCounter = 0f;
    [SerializeField] protected float attackCoolDown = 2f;
    [SerializeField] protected float attackCoolDownCounter = 0f;
    public bool IsAttack => isAttack;
    protected void Update()
    {
        this.Attacking();
    }
    protected virtual void Attacking()
    {
        if (this.enemyCtrl == null || this.enemyCtrl.EnemyDamReceive == null ||
            this.enemyCtrl.EnemyDetect == null || this.enemyCtrl.EnemyFollow == null) return;
        if (this.enemyCtrl.EnemyDamReceive.IsDead) return;
        if (!this.enemyCtrl.EnemyDetect.Detect)
        {
            this.isAttack = false;
            return;
        }
        if (this.enemyCtrl.EnemyDamReceive.IsHurt)
        {
            this.SetAttackTime();
            return;
        }
        if (this.CanAttack()) this.Attack();
        this.StopAttack();
        this.TimeCooldown();
    }
    protected bool CanAttack()
    {
        if (this.enemyCtrl == null || this.enemyCtrl.EnemyFollow == null || this.enemyCtrl.EnemyFollow.Target == null || transform.parent == null)
            return false;
        this.distanceAttack = Vector3.Distance(transform.parent.position, this.enemyCtrl.EnemyFollow.Target.position);
        if (this.distanceAttack <= this.disAttackLimit && this.attackCoolDownCounter <= 0f)
            return true;
        return false;
    }
    protected virtual void Attack()
    {
        this.isAttack = true;
        this.SetAttackTime();
    }
    protected virtual void StopAttack()
    {
        if (this.attackTimeCounter <= 0f && this.isAttack)
        {
            this.isAttack = false;
        }
    }
    protected void TimeCooldown()
    {
        if (this.attackTimeCounter <= 0f && this.attackCoolDownCounter <= 0f) return;
        this.attackCoolDownCounter -= Time.deltaTime;
        this.attackTimeCounter -= Time.deltaTime;
    }
    protected void SetAttackTime()
    {
        this.attackTimeCounter = this.attackTime;
        this.attackCoolDownCounter = this.attackCoolDown + this.attackTime;
    }
}
