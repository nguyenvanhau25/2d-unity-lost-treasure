using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawn : Spawner
{
    [Header("Enemy Spawn")]
    protected static EnemySpawn instance;
    public static EnemySpawn Instance => instance;
    [SerializeField] protected int enemyLimit = 5;
    protected override void Awake()
    {
        base.Awake();
        if (EnemySpawn.instance != null) Debug.LogWarning("Only 1 EnemySpawn allow to exist");
        EnemySpawn.instance = this;
    }
    protected override void ResetValue()
    {
        base.ResetValue();
        this.spawnCount = this.enemyLimit;
    }

    public override Transform Spawn(Transform prefab, Vector3 spawnPos, Quaternion spawnRot)
    {
        Transform newEnemy = base.Spawn(prefab, spawnPos, spawnRot);
        if (newEnemy != null) this.AddHpBar2Obj(newEnemy);

        return newEnemy;
    }

    protected void AddHpBar2Obj(Transform newEnemy)
    {
        if (newEnemy == null || HpBarEnemySpawn.Instance == null) return;
        EnemyCtrl newEnemyCtrl = newEnemy.GetComponent<EnemyCtrl>();
        Transform newHpBar = HpBarEnemySpawn.Instance.Spawn(HpBarEnemySpawn.hpBarEnemy, newEnemy.position, Quaternion.identity);
        if (newHpBar == null) return;
        HpBarEnemy hpBarEnemy = newHpBar.GetComponent<HpBarEnemy>();
        if (hpBarEnemy == null) return;
        hpBarEnemy.SetObjCtrl(newEnemyCtrl);
        hpBarEnemy.SetFollowTarget(newEnemy);

        newHpBar.gameObject.SetActive(true);
    }
}
