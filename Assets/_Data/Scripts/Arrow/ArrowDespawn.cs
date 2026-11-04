using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowDespawn : DespawnByDistance
{
    public override void DespawnObj()
    {
        if (transform.parent == null) return;
        if (ArrowPlayerSpawn.Instance != null)
            ArrowPlayerSpawn.Instance.Despawn(transform.parent);
        else if (ArrowEnemySpawn.Instance != null)
            ArrowEnemySpawn.Instance.Despawn(transform.parent);
    }
}
