using UnityEngine;
using UnityEngine.UI;

public class HpBarEnemy : HpBar
{
    [Header("Hp Bar Enemy")]
    [SerializeField] protected EnemyCtrl enemyCtrl;
    [SerializeField] protected FollowTarget followTarget;
    [SerializeField] protected Spawner spawner;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadFollowTarget();
        this.LoadSpawner();
    }
    protected void LoadFollowTarget()
    {
        if (this.followTarget != null) return;
        this.followTarget = GetComponent<FollowTarget>();
    }
    protected void LoadSpawner()
    {
        if (this.spawner != null) return;
        GameObject spawnerObject = GameObject.Find("HpBarEnemySpawn");
        if (spawnerObject != null) this.spawner = spawnerObject.GetComponent<Spawner>();
    }
    protected override void HpShowing()
    {
        if (this.enemyCtrl == null) return;

        if (this.enemyCtrl.EnemyDamReceive == null) return;
        bool isDead = this.enemyCtrl.EnemyDamReceive.IsDeaded();
        if (isDead)
        {
            if (this.spawner != null) this.spawner.Despawn(transform);
            return;
        }

        this.hp = this.enemyCtrl.EnemyDamReceive.Hp;
        this.hpMax = this.enemyCtrl.EnemyDamReceive.HPMax;

        base.HpShowing();
    }

    public void SetObjCtrl(EnemyCtrl enemyCtrl)
    {
        this.enemyCtrl = enemyCtrl;
    }
    public void SetFollowTarget(Transform target)
    {
        if (this.followTarget != null) this.followTarget.SetTarget(target);
    }
}
