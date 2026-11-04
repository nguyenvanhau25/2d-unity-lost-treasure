using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSender : DinoBehaviourScript
{
    [Header("Damage Sender")]
    [SerializeField] protected int damage = 1;
    protected bool canSendDamage = false;
    public int Damage => damage;

    protected virtual void SendToTransform(Transform collider)
    {
        DamageReceiver damageReceiver = collider.GetComponent<DamageReceiver>();
        if (damageReceiver == null) return;
        this.canSendDamage = true;
        this.SendToDamReceive(damageReceiver);
    }

    protected void SendToDamReceive(DamageReceiver damageReceiver)
    {
        damageReceiver.Deduct(this.damage);
        // Player and the Dark Forest boss play their own configurable hit SFX
        // from their damage receiver. Keep the shared sound for other targets.
        if (damageReceiver is PlayerDamReceive || damageReceiver is BossDamReceive) return;
        if (AudioManagerr.Instance != null)
            AudioManagerr.Instance.PlaySFX("SwordBlood", 0.5f);
    }
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        this.SendToTransform(other.transform);
    }
}
