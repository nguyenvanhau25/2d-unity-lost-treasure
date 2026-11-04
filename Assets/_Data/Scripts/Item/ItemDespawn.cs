using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDespawn : DespawnByDistance
{
    public override void DespawnObj()
    {
        if (ItemDropSpawn.Instance != null && transform.parent != null)
            ItemDropSpawn.Instance.Despawn(transform.parent);
    }
}
