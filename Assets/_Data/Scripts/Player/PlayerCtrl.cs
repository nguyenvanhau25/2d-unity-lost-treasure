using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCtrl : DinoBehaviourScript
{
    [Header("Player Ctrl")]
    protected static PlayerCtrl instance;
    public static PlayerCtrl Instance => instance;
    [SerializeField] protected Animator anim;
    [SerializeField] protected PlayerMovement playerMovement;
    [SerializeField] protected PlayerAttack playerAttack;
    [SerializeField] protected PlayerShooting playerShooting;
    [SerializeField] protected PlayerAttackArea playerAttackArea;
    [SerializeField] protected PlayerFlipDirect playerFlipDirect;
    [SerializeField] protected PlayerDash playerDash;
    [SerializeField] protected PlayerDamReceive playerDamReceive;
    [SerializeField] protected PlayerTreasureResonance playerTreasureResonance;
    public Animator Anim => anim;
    public PlayerMovement PlayerMovement => playerMovement;
    public PlayerAttack PlayerAttack => playerAttack;
    public PlayerShooting PlayerShooting => playerShooting;
    public PlayerAttackArea PlayerAttackArea => playerAttackArea;
    public PlayerFlipDirect PlayerFlipDirect => playerFlipDirect;
    public PlayerDash PlayerDash => playerDash;
    public PlayerDamReceive PlayerDamReceive => playerDamReceive;
    public PlayerTreasureResonance PlayerTreasureResonance => playerTreasureResonance;
    protected override void Awake()
    {
        base.Awake();
        if(PlayerCtrl.instance != null) Debug.Log("Only 1 PlayerCtrl allow to exist");
        PlayerCtrl.instance = this;
    }
    private void OnDestroy()
    {
        if (PlayerCtrl.instance == this) PlayerCtrl.instance = null;
    }
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadAnimator();
        this.LoadPlayerMovement();
        this.LoadPlayerAttack();
        this.LoadPlayerShooting();
        this.LoadPlayerAttackArea();
        this.LoadPlayerFlipDirect();
        this.LoadPlayerDash();
        this.LoadPlayerDamReceive();
        this.LoadPlayerTreasureResonance();
    }

    protected void LoadAnimator()
    {
        if (this.anim != null) return;
        this.anim = GetComponentInChildren<Animator>();
    }

    protected void LoadPlayerMovement()
    {
        if (this.playerMovement != null) return;
        this.playerMovement = GetComponentInChildren<PlayerMovement>();
    }
    protected void LoadPlayerAttack()
    {
        if (this.playerAttack != null) return;
        this.playerAttack = GetComponentInChildren<PlayerAttack>();
    }
    protected void LoadPlayerShooting()
    {
        if (this.playerShooting != null) return;
        this.playerShooting = GetComponentInChildren<PlayerShooting>();
    }
    protected void LoadPlayerAttackArea()
    {
        if (this.playerAttackArea != null) return;
        this.playerAttackArea = GetComponentInChildren<PlayerAttackArea>();
    }
    protected void LoadPlayerFlipDirect()
    {
        if (this.playerFlipDirect != null) return;
        this.playerFlipDirect = GetComponentInChildren<PlayerFlipDirect>();
    }
    protected void LoadPlayerDash()
    {
        if (this.playerDash != null) return;
        this.playerDash = GetComponentInChildren<PlayerDash>();
    }
    protected void LoadPlayerDamReceive()
    {
        if (this.playerDamReceive != null) return;
        this.playerDamReceive = GetComponentInChildren<PlayerDamReceive>();
    }
    protected void LoadPlayerTreasureResonance()
    {
        if (this.playerTreasureResonance != null) return;
        this.playerTreasureResonance = GetComponent<PlayerTreasureResonance>();
    }
}
