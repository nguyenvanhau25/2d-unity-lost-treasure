using System.Collections.Generic;
using UnityEngine;

public abstract class Spawner : DinoBehaviourScript
{
    [Header("Spawner")]
    [SerializeField] protected Transform holder;
    [SerializeField] protected List<Transform> prefabs;
    [SerializeField] protected List<Transform> poolObj;
    [SerializeField] protected int spawnCount = 0;
    public int SpawnCount => spawnCount;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadPrefab();
        this.LoadHolder();
    }
    protected void LoadPrefab()
    {
        if (this.prefabs == null) this.prefabs = new List<Transform>();
        if (this.prefabs.Count > 0) return;
        Transform prefab = transform.Find("Prefab");
        if (prefab == null) return;
        foreach (Transform child in prefab)
        {
            this.prefabs.Add(child);
        }
        this.HidePrefab();
    }
    protected void HidePrefab()
    {
        if (this.prefabs == null) return;
        foreach (Transform prefab in this.prefabs)
        {
            if (prefab != null) prefab.gameObject.SetActive(false);
        }
    }
    protected void LoadHolder()
    {
        if (this.holder != null) return;
        this.holder = transform.Find("Holder");
    }
    protected Transform GetPrefabByName(string prefabName)
    {
        if (this.prefabs == null) return null;
        foreach (Transform prefab in this.prefabs)
        {
            if (prefab == null || prefab.name != prefabName) continue;
            return prefab;
        }
        return null;
    }
    protected Transform GetObjFromPool(Transform prefab)
    {
        if (prefab == null) return null;
        if (this.poolObj == null) this.poolObj = new List<Transform>();
        for (int index = this.poolObj.Count - 1; index >= 0; index--)
        {
            Transform obj = this.poolObj[index];
            if (obj == null)
            {
                this.poolObj.RemoveAt(index);
                continue;
            }
            if (obj.name != prefab.name) continue;
            this.poolObj.RemoveAt(index);
            return obj;
        }

        Transform newPrefab = Instantiate(prefab);
        newPrefab.name = prefab.name;
        return newPrefab;
    }
    public Transform Spawn(string prefabName, Vector3 spawnPos, Quaternion spawnRot)
    {
        Transform prefab = this.GetPrefabByName(prefabName);
        if (prefab == null)
        {
            return null;
        }
        return Spawn(prefab, spawnPos, spawnRot);
    }
    public virtual Transform Spawn(Transform prefab, Vector3 spawnPos, Quaternion spawnRot)
    {
        Transform newPrefab = this.GetObjFromPool(prefab);
        if (newPrefab == null) return null;
        newPrefab.SetPositionAndRotation(spawnPos, spawnRot);
        newPrefab.SetParent(this.holder != null ? this.holder : transform);
        newPrefab.gameObject.SetActive(true);
        this.spawnCount--;
        return newPrefab;
    }
    public void Despawn(Transform obj)
    {
        if (obj == null) return;
        if (this.poolObj == null) this.poolObj = new List<Transform>();
        if (!this.poolObj.Contains(obj)) this.poolObj.Add(obj);
        obj.gameObject.SetActive(false);
    }
    public Transform GetRandomPrefab()
    {
        if (this.prefabs == null || this.prefabs.Count == 0) return null;
        int rand = Random.Range(0, this.prefabs.Count);
        return this.prefabs[rand];
    }
}
