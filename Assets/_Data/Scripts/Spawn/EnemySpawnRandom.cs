using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnRandom : DinoBehaviourScript
{
    [Header("Enemy Spawn Random")]
    [SerializeField] protected EnemySpawnCtrl enemySpawnCtrl;
    [SerializeField, Min(0f)] protected float delaySpawnTime = 3f;
    [SerializeField] protected float spawnTimer = 0f;
    [Header("Dark Forest boss gate")]
    [SerializeField] protected GameObject bossToUnlock;
    [SerializeField, Min(0f)] protected float bossRevealDelay = 1.5f;
    protected int livingWaveEnemies;
    protected float clearTimer;
    public bool bossReleased;

    protected override void Awake()
    {
        base.Awake();
        if (this.bossToUnlock != null)
            this.bossToUnlock.SetActive(false);
    }
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadEnemySpawnCtrl();
    }
    protected void LoadEnemySpawnCtrl()
    {
        if (this.enemySpawnCtrl != null) return;
        this.enemySpawnCtrl = GetComponent<EnemySpawnCtrl>();
    }
    protected void Update()
    {
        this.EnemySpawning();
        this.CheckWaveCleared();
    }
    protected void EnemySpawning()
    {
        if (this.enemySpawnCtrl == null || this.enemySpawnCtrl.EnemySpawn == null || this.enemySpawnCtrl.EnemySpawnPoint == null)
            return;
        if (!this.RandomSpawnLimit()) return;

        this.spawnTimer += Time.deltaTime;
        if (this.spawnTimer < this.delaySpawnTime) return;
        this.spawnTimer = 0f;

        Transform spawnPoint = this.enemySpawnCtrl.EnemySpawnPoint.GetRandomPoint();
        if (spawnPoint == null) return;
        Vector3 spawnPointPos = spawnPoint.position;
        Quaternion spawPointRot = spawnPoint.rotation;
        Transform enemy = this.enemySpawnCtrl.EnemySpawn.GetRandomPrefab();
        Transform newEnemy = this.enemySpawnCtrl.EnemySpawn.Spawn(enemy, spawnPointPos, spawPointRot);
        if (newEnemy == null) return;
        newEnemy.gameObject.SetActive(true);
        EnemyDamReceive damageReceiver = newEnemy.GetComponentInChildren<EnemyDamReceive>();
        if (damageReceiver == null)
        {
            Debug.LogError($"Spawned enemy '{newEnemy.name}' has no EnemyDamReceive; boss gate cannot track the wave.", newEnemy);
            return;
        }
        damageReceiver.Died += this.OnWaveEnemyDied;
        this.livingWaveEnemies++;
    }

    protected void OnWaveEnemyDied(EnemyDamReceive enemy)
    {
        enemy.Died -= this.OnWaveEnemyDied;
        this.livingWaveEnemies = Mathf.Max(0, this.livingWaveEnemies - 1);
        this.CheckWaveCleared();
    }

    protected void CheckWaveCleared()
    {
        if (this.bossReleased || this.bossToUnlock == null || this.enemySpawnCtrl == null || this.enemySpawnCtrl.EnemySpawn == null)
            return;

        if (this.enemySpawnCtrl.EnemySpawn.SpawnCount > 0 || this.livingWaveEnemies > 0)
        {
            this.clearTimer = 0f;
            return;
        }

        this.clearTimer += Time.deltaTime;
        if (this.clearTimer < this.bossRevealDelay) return;

        this.bossReleased = true;
        this.bossToUnlock.SetActive(true);
        Debug.Log("Enemy wave cleared: releasing the Dark Forest boss.", this.bossToUnlock);
    }

    protected bool RandomSpawnLimit()
    {
        if (this.enemySpawnCtrl == null || this.enemySpawnCtrl.EnemySpawn == null) return false;
        if (this.enemySpawnCtrl.EnemySpawn.SpawnCount <= 0) return false;
        return true;
    }
}
