using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMovement : DinoBehaviourScript
{
    [SerializeField] protected Transform target;
    [SerializeField] protected BossAttack bossAttack;
    [SerializeField] protected float distance;
    [SerializeField] protected float distanceToTarget;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadTarget();
        this.LoadBossAttack();
    }
    protected void LoadTarget()
    {
        if (this.target != null) return;
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null) this.target = playerObject.transform;
    }
    protected void LoadBossAttack()
    {
        if (this.bossAttack != null) return;
        if (transform.parent != null)
            this.bossAttack = transform.parent.GetComponentInChildren<BossAttack>();
    }
    protected void Update()
    {
        this.Move();
    }
    public bool CanMove()
    {
        if (this.target == null || transform.parent == null) return false;
        this.distance = Vector2.Distance(transform.parent.position, target.position);
        if (this.distance <= this.distanceToTarget) return true;
        return false;
    }
    protected void Move()
    {
        if (!this.CanMove() || this.bossAttack == null) return;
        this.bossAttack.enabled = true;
    }
}
