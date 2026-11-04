using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackArea : PlayerAbstract
{
    [Header("Player Attack Area")]
    [SerializeField] protected Transform hitBox1;
    [SerializeField] protected Transform hitBox2;
    [SerializeField] protected Transform hitBox3;
    [SerializeField] protected Transform hitBox4;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadHitBox();
    }
    protected void LoadHitBox()
    {
        if (this.hitBox1 != null && this.hitBox2 != null && this.hitBox3 != null && this.hitBox4 != null) return;
        this.hitBox1 = transform.Find("PlayerAttackArea_1");
        this.hitBox2 = transform.Find("PlayerAttackArea_2");
        this.hitBox3 = transform.Find("PlayerAttackArea_3");
        this.hitBox4 = transform.Find("PlayerAttackArea_4");
    }
    private void Update()
    {
        this.OnAttackArea();
        this.OffAttackArea();
    }

    protected void OnAttackArea()
    {
        if (this.playerCtrl == null || this.playerCtrl.PlayerMovement == null || this.playerCtrl.PlayerFlipDirect == null || this.playerCtrl.PlayerAttack == null)
            return;
        if (this.playerCtrl.PlayerMovement.SwitchWeapon) return;
        this.GetOnAttackArea(this.playerCtrl.PlayerFlipDirect.Right, this.hitBox1);
        this.GetOnAttackArea(this.playerCtrl.PlayerFlipDirect.Left, this.hitBox2);
        this.GetOnAttackArea(this.playerCtrl.PlayerFlipDirect.Down, this.hitBox3);
        this.GetOnAttackArea(this.playerCtrl.PlayerFlipDirect.Up, this.hitBox4);
    }
    protected void GetOnAttackArea(bool _bool, Transform _hitBox)
    {
        if (_bool && _hitBox != null && this.playerCtrl.PlayerAttack.Attack)
            _hitBox.gameObject.SetActive(true);
    }
    protected void OffAttackArea()
    {
        if (this.playerCtrl == null || this.playerCtrl.PlayerMovement == null || this.playerCtrl.PlayerAttack == null)
            return;
        if (this.playerCtrl.PlayerMovement.SwitchWeapon) return;
        this.GetOffAttackArea(this.hitBox1);
        this.GetOffAttackArea(this.hitBox2);
        this.GetOffAttackArea(this.hitBox3);
        this.GetOffAttackArea(this.hitBox4);
    }

    protected void GetOffAttackArea(Transform hitbox)
    {
        if (this.playerCtrl.PlayerAttack.Attack) return;
        if (hitbox != null) hitbox.gameObject.SetActive(false);
    }
}
