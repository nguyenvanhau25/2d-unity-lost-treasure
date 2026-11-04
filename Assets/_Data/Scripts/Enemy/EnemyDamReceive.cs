using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDamReceive : DamageReceiver
{
    public event System.Action<EnemyDamReceive> Died;
    [Header("Enemy Dam Receive")]
    [SerializeField] protected EnemyCtrl enemyCtrl;
    [SerializeField] protected CapsuleCollider2D collide;
    [SerializeField] protected float enemyHurtTime = 0.5f;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadEnemyCtrl();
        this.LoadCollide();
    }
    protected void LoadEnemyCtrl()
    {
        if (this.enemyCtrl != null) return;
        this.enemyCtrl = GetComponentInParent<EnemyCtrl>();
    }
    protected void LoadCollide()
    {
        if (this.collide != null) return;
        this.collide = GetComponent<CapsuleCollider2D>();
    }
    protected override void ResetValue()
    {
        base.ResetValue();
        this.Reborn();
    }
    protected override void Reborn()
    {
        this.hurtTime = this.enemyHurtTime;
        if (this.enemyCtrl != null && this.enemyCtrl.EnemySO != null)
            this.hpMax = this.enemyCtrl.EnemySO.hpMax;
        if (this.collide != null) this.collide.enabled = true;
        this.SetPosDead();
        base.Reborn();
    }
    protected override void OnDead()
    {
        this.Died?.Invoke(this);
        if (this.collide != null) this.collide.enabled = false;
        if (this.enemyCtrl != null && this.enemyCtrl.Rigid != null)
            this.enemyCtrl.Rigid.constraints = RigidbodyConstraints2D.FreezeAll;
        this.OnDeadDrop();
    }
    protected void OnDeadDrop()
    {
        if (this.enemyCtrl == null || this.enemyCtrl.EnemySO == null || ItemDropSpawn.Instance == null) return;
        Vector3 dropPos = transform.position;
        Quaternion dropRot = transform.rotation;
        ItemDropSpawn.Instance.Drop(this.enemyCtrl.EnemySO.dropList, dropPos, dropRot);
    }
    protected override void StopHurt()
    {
        base.StopHurt();
        this.SetPosDead();
    }
    protected override void Hurt()
    {
        if (this.enemyCtrl != null && this.enemyCtrl.Rigid != null)
            this.enemyCtrl.Rigid.constraints = RigidbodyConstraints2D.FreezeAll;
    }
    protected virtual void SetPosDead()
    {
        if (this.enemyCtrl == null || this.enemyCtrl.Rigid == null) return;
        this.enemyCtrl.Rigid.constraints = RigidbodyConstraints2D.None;
        this.enemyCtrl.Rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
    protected void OnCollisionStay2D(Collision2D other)
    {
        if (other == null || !other.gameObject.CompareTag("Enemy") || this.enemyCtrl == null) return;

        // BossCtrl inherits EnemyCtrl but uses BossMovement, not EnemyMovement.
        // Regular enemies keep their old behaviour; bosses simply ignore this
        // random-walk redirection when they touch another enemy.
        EnemyMovement enemyMovement = this.enemyCtrl.EnemyMovement;
        if (enemyMovement != null) enemyMovement.SetNewDestination();
    }
}
