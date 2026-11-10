using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowDamSender : DamageSender
{
    [Header("Arrow Dam Sender")]
    [SerializeField] protected ArrowCtrl arrowCtrl;
    public ArrowCtrl ArrowCtrl => arrowCtrl;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.damage = 4; // Buff arrow to 4 damage
        this.LoadArrowCtrl();
    }
    protected void LoadArrowCtrl()
    {
        if (this.arrowCtrl != null) return;
        this.arrowCtrl = GetComponentInParent<ArrowCtrl>();
    }
    protected override void SendToTransform(Transform collider)
    {
        AncientRunePillar runePillar = collider.GetComponent<AncientRunePillar>();
        if (runePillar == null) runePillar = collider.GetComponentInParent<AncientRunePillar>();
        if (runePillar != null)
        {
            runePillar.ActivateRune();
            this.arrowCtrl.ArrowDespawn.DespawnObj();
            return;
        }

        BossLevel1 darkForestBoss = collider.GetComponent<BossLevel1>();
        if (darkForestBoss == null) darkForestBoss = collider.GetComponentInParent<BossLevel1>();

        if (darkForestBoss != null)
        {
            darkForestBoss.ReceiveArrowHit();
            this.canSendDamage = true;
        }
        else
        {
            base.SendToTransform(collider);
            if (!this.canSendDamage && collider.CompareTag("Enemy"))
            {
                DamageReceiver receiver = collider.GetComponentInChildren<DamageReceiver>();
                if (receiver != null && !(receiver is PlayerDamReceive))
                {
                    this.canSendDamage = true;
                    this.SendToDamReceive(receiver);
                }
            }
        }

        if (!this.canSendDamage) return;

        this.arrowCtrl.ArrowDespawn.DespawnObj();
        this.canSendDamage = false;
    }
    protected override void OnTriggerEnter2D(Collider2D collider)
    {
        if (this.arrowCtrl.Shooter != null && collider.transform.root == this.arrowCtrl.Shooter.root)
            return;
        SendToTransform(collider.transform);
    }


}
