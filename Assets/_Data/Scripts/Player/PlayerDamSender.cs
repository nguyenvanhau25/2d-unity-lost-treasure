using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDamSender : DamageSender
{
    [Header("Player DamSender")]
    [SerializeField] protected PolygonCollider2D collide;
    public PolygonCollider2D Collide => collide;

    protected override void Start()
    {
        base.Start();
        this.damage = 5; // Buff sword strike to 5 damage
    }

    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadCollider();
    }
    protected void LoadCollider()
    {
        if (this.collide != null) return;
        this.collide = GetComponent<PolygonCollider2D>();
        this.collide.isTrigger = true;
    }
}
