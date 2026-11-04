using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerAbstract : DinoBehaviourScript
{
    [Header("Player Abstract")]
    [SerializeField] protected PlayerCtrl playerCtrl;
    public PlayerCtrl PlayerCtrl => playerCtrl;

    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadPlayerCtrl();
    }

    protected void LoadPlayerCtrl()
    {
        if (this.playerCtrl != null) return;
        if (transform.parent != null)
            this.playerCtrl = transform.parent.GetComponent<PlayerCtrl>();
    }
}
