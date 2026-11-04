using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFollow : EnemyAbstract
{
    [Header("Enemy Follow")]
    [SerializeField] protected Transform target;
    public Transform Target => target;


    [SerializeField] private bool alwaysFollowPlayer;
    public bool AlwaysFollowPlayer => this.alwaysFollowPlayer;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadTarget();
    }

    protected void LoadTarget()
    {
        if (this.target != null) return;
        GameObject player = GameObject.Find("Player");
        if (player != null) this.target = player.transform;
    }
    protected void FixedUpdate()
    {
        this.Following();
    }
    protected void Following()
    {
        if (this.target == null || this.enemyCtrl == null || this.enemyCtrl.EnemyDamReceive == null) return;
        if (this.enemyCtrl.EnemyDamReceive.IsDead) return;
        if (this.enemyCtrl.EnemyDamReceive.IsHurt) return;
        if (alwaysFollowPlayer)
        {
            FollowPlayer();
            return;
        }
        else
        {
            if (this.enemyCtrl.EnemyDetect == null || this.enemyCtrl.Rigid == null || this.enemyCtrl.EnemyMovement == null) return;
            if (!this.enemyCtrl.EnemyDetect.Detect) return;
            this.enemyCtrl.Rigid.MovePosition(Vector2.MoveTowards
            (transform.parent.position, this.target.position, this.enemyCtrl.EnemyMovement.Speed * Time.fixedDeltaTime));
        }
    }
    protected void FollowPlayer()
    {
        if (this.enemyCtrl == null || this.enemyCtrl.Rigid == null || this.enemyCtrl.EnemyMovement == null || this.target == null)
            return;
        this.enemyCtrl.Rigid.MovePosition(
            Vector2.MoveTowards(
                transform.parent.position,
                this.target.position,
                this.enemyCtrl.EnemyMovement.Speed * Time.fixedDeltaTime
            )
        );
    }

    public void SetAlwaysFollowPlayer(bool value)
    {
        this.alwaysFollowPlayer = value;
    }
}
