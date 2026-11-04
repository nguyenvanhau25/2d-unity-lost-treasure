using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetArrowFromInvent : GetItemFromInvent
{
    protected override void GetItemInventory()
    {
        foreach (ItemInventory item in this.inventory.Items)
        {
            if (item.itemProfile.itemCode != ItemCode.Arrow || item.itemCount <= 0) continue;
            item.itemCount--;
            if (ArrowPlayerSpawn.Instance != null) ArrowPlayerSpawn.Instance.AddArrow(1);
        }
    }
}

