using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowPlayerSpawn : Spawner
{
    [Header("Arrow Player Spawn")]
    [SerializeField] protected ArrowSO arrowSO;
    protected static ArrowPlayerSpawn instance;
    public static ArrowPlayerSpawn Instance => instance;
    protected static string arrowOne = "Arrow_1";
    public string ArrowOne => arrowOne;
    protected override void Awake()
    {
        base.Awake();
        if (ArrowPlayerSpawn.instance != null) Debug.LogWarning("Only 1 ArrowPlayerSpawn allow to exist");
        ArrowPlayerSpawn.instance = this;
    }
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadArrowSO();
        if (this.arrowSO != null) this.spawnCount = this.arrowSO.arrowCounts;
    }
    protected void LoadArrowSO()
    {
        if (this.arrowSO != null) return;
        this.arrowSO = Resources.Load<ArrowSO>("GameData/ArrowSO");
    }
    public void AddArrow(int count)
    {
        this.spawnCount += count;
    }
}
