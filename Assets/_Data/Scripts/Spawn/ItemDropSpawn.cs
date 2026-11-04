using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDropSpawn : Spawner
{
    [Header("Item Drop Spawn")]
    protected static ItemDropSpawn instance;
    public static ItemDropSpawn Instance => instance;
    protected override void Awake()
    {
        base.Awake();
        if (ItemDropSpawn.instance != null) Debug.LogWarning("Only 1 ItemDropSpawn allow to exist");
        ItemDropSpawn.instance = this;
    }

    public void Drop(List<DropRate> dropList, Vector3 dropPos, Quaternion dropRot)
    {
        if (dropList == null || dropList.Count == 0 || dropList[0].itemSO == null) return;
        ItemCode itemCode = dropList[0].itemSO.itemCode;
        
        Transform itemDrop = this.Spawn(itemCode.ToString(), dropPos, dropRot);
        if (itemDrop == null) return;
        itemDrop.gameObject.SetActive(true);
    }
}
