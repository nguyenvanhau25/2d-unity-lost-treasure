using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHpBarCtrl : DinoBehaviourScript
{
    [SerializeField] protected Canvas canvasBoss;
    [SerializeField] protected BossDamReceive bossDamReceive;
    [SerializeField] protected BossMovement bossMovement;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadCanvasBoss();
        this.LoadBossComponents();
    }
    protected override void Start()
    {
        base.Start();
        if (this.canvasBoss != null) this.canvasBoss.gameObject.SetActive(false);
    }
    protected void LoadCanvasBoss()
    {
        if (this.canvasBoss != null) return;
        GameObject hpBarObject = GameObject.Find("HpBarBoss");
        if (hpBarObject != null) this.canvasBoss = hpBarObject.GetComponent<Canvas>();
    }
    protected void LoadBossComponents()
    {
        BossCtrl bossCtrl = GetComponentInParent<BossCtrl>();
        if (bossCtrl == null) return;

        if (this.bossDamReceive == null) this.bossDamReceive = bossCtrl.BossDamReceive;
        if (this.bossMovement == null) this.bossMovement = bossCtrl.BossMovement;
    }
    protected void Update()
    {
        this.ControlHpBar();
    }

    protected void ControlHpBar()
    {
        if (this.canvasBoss == null || this.bossDamReceive == null || this.bossMovement == null) return;

        if (this.bossDamReceive.IsDeaded())
        {
            this.canvasBoss.gameObject.SetActive(false);
            return;
        }

        if (!this.bossMovement.CanMove()) return;
        this.canvasBoss.gameObject.SetActive(true);
    }
}
