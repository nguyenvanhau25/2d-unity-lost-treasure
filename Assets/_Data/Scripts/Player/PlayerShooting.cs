using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : PlayerAbstract
{
    [Header("Player Shooting")]
    [SerializeField] protected bool shoot;
    public bool Shoot => shoot;
    [SerializeField] protected float shootTime = 0.5f;
    [SerializeField] protected float shootTimeCounter = 0f;


    private void Update()
    {
        this.GetInputShoot();
        this.Shooting();
    }

    protected void GetInputShoot()
    {
        if (ArrowPlayerSpawn.Instance == null || this.playerCtrl == null || InputManager.Instance == null) return;
        if (ArrowPlayerSpawn.Instance.SpawnCount <= 0) return;
        if (this.playerCtrl.PlayerDamReceive.IsDead) return;
        if (this.playerCtrl.PlayerDamReceive.IsHurt) return;
        if (!this.playerCtrl.PlayerMovement.SwitchWeapon) return;
        this.shoot = InputManager.Instance.InputAttack;
    }

    protected void Shooting()
    {
        if (!this.shoot) return;

        this.shootTimeCounter += Time.deltaTime;
        if (this.shootTimeCounter < this.shootTime) return;
        this.shootTimeCounter = 0f;

        this.Shooted();

        this.shoot = false;
        if (InputManager.Instance != null) InputManager.Instance.SetAttack(false);
    }
    protected void Shooted()
    {
        if (ArrowPlayerSpawn.Instance == null || transform.parent == null) return;
        Vector3 spawPos = transform.parent.position;
        Quaternion spawRot = transform.parent.rotation;
        Transform new_Arrow = ArrowPlayerSpawn.Instance.Spawn(this.GetArrowName(), spawPos, spawRot);
        if (new_Arrow == null) return;

        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("Arrow");

        this.SetRotationArrow(new_Arrow);
    }
    protected string GetArrowName()
    {
        return ArrowPlayerSpawn.Instance.ArrowOne;
    }
    protected void SetRotationArrow(Transform new_Arrow)
    {
        if (new_Arrow == null || this.playerCtrl == null || this.playerCtrl.PlayerFlipDirect == null) return;
        if (this.playerCtrl.PlayerFlipDirect.Left) new_Arrow.rotation = Quaternion.Euler(0, 0, 180);
        if (this.playerCtrl.PlayerFlipDirect.Up) new_Arrow.rotation = Quaternion.Euler(0, 0, 90);
        if (this.playerCtrl.PlayerFlipDirect.Down) new_Arrow.rotation = Quaternion.Euler(0, 0, -90);
    }
}
