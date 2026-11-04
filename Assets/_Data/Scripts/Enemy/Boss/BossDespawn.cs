using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDespawn : DespawnByTime
{
    [Header("After Defeat")]
    [Tooltip("Remove this boss from the scene after the death delay.")]
    [SerializeField] private bool deactivateBossOnDeath = true;
    [Tooltip("Optional object to activate after this boss dies, such as a door or scene transition.")]
    [SerializeField] private GameObject obj;
    [SerializeField] private bool activateObjectOnDeath = true;
    protected override void ResetValue()
    {
        base.ResetValue();
        this.delayTime = 3f;
    }
    public override void DespawnObj()
    {
        if (this.deactivateBossOnDeath)
        {
            transform.parent.gameObject.SetActive(false);
        }

        if (this.activateObjectOnDeath && this.obj != null)
        {
            this.obj.SetActive(true);
        }
    }
    protected override bool CanDespawn()
    {
        if (!BossCtrl.Instance.BossDamReceive.IsDead) return false;
        return base.CanDespawn();
    }
}
