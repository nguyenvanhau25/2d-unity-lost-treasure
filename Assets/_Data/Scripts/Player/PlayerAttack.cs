using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : PlayerAbstract
{
    [Header("Player Attack")]
    [SerializeField] protected bool attack;
    public bool Attack => attack;
    [SerializeField] protected float attackTime = 0.5f;
    [SerializeField] protected float attackTimeCounter = 0f;

    private void Update()
    {
        this.GetInputAttack();
        this.Attacking();
    }

    protected void GetInputAttack()
    {
        if (this.playerCtrl == null || this.playerCtrl.PlayerDamReceive == null || this.playerCtrl.PlayerMovement == null || InputManager.Instance == null)
            return;
        if (this.playerCtrl.PlayerDamReceive.IsDead) return;
        if (this.playerCtrl.PlayerDamReceive.IsHurt) return;
        if (this.playerCtrl.PlayerMovement.SwitchWeapon) return;
        this.attack = InputManager.Instance.InputAttack;
    }

    protected void Attacking()
    {
        if (!this.attack) return;
        this.attackTimeCounter += Time.deltaTime;
        if (this.attackTimeCounter < this.attackTime) return;
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("Sword");
        this.attackTimeCounter = 0f;

        this.attack = false;
        if (InputManager.Instance != null) InputManager.Instance.SetAttack(false);
    }
}
