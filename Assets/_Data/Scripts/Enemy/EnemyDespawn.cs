using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDespawn : DespawnByTime
{
    [SerializeField] protected EnemyCtrl enemyCtrl;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadEnemyCtrl();
    }
    protected void LoadEnemyCtrl()
    {
        if (this.enemyCtrl != null) return;
        this.enemyCtrl = GetComponentInParent<EnemyCtrl>();
    }
    public override void DespawnObj()
    {
        if (EnemySpawn.Instance == null)
        {
            base.DespawnObj();
            return;
        }
        if (transform.parent != null) EnemySpawn.Instance.Despawn(transform.parent);
    }
    protected override bool CanDespawn()
    {
        if (this.enemyCtrl == null || this.enemyCtrl.EnemyDamReceive == null) return false;
        if (!this.enemyCtrl.EnemyDamReceive.IsDead) return false;
        return base.CanDespawn();
    }
}
