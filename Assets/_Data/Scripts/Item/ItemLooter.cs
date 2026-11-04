using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(CircleCollider2D))]
public class ItemLooter : DinoBehaviourScript
{
    [SerializeField] protected Inventory inventory;
    [SerializeField] protected CircleCollider2D collide;

    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadInventory();
        this.LoadCollider();
    }
    protected void LoadInventory()
    {
        if (this.inventory != null) return;
        this.inventory = GetComponentInParent<Inventory>();
    }
    protected void LoadCollider()
    {
        if (this.collide != null) return;
        this.collide = GetComponent<CircleCollider2D>();
        this.collide.isTrigger = true;
        this.collide.radius = 0.5f;
    }
    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (this.inventory == null) return;
        ItemPickupable itemPickupable = other.GetComponent<ItemPickupable>();
        if (itemPickupable == null) return;
        ItemCode itemCode = itemPickupable.GetItemCode();
        if (this.inventory.PickupItem(itemCode, 1))
        {
            itemPickupable.Picked();
            if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("Pickup");
        }
    }
}
