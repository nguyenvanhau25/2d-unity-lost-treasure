using UnityEngine;
public class PlayerMovement : PlayerAbstract
{
    [Header("Player Movement")]
    [SerializeField] protected Rigidbody2D _rb;
    [SerializeField] protected int speed = 5;
    [SerializeField] protected float horizontal;
    [SerializeField] protected float vertical;
    [SerializeField] protected bool switchWeapon;
    [SerializeField] protected PlayerPos currentPlayer;
    public Rigidbody2D _Rb => _rb;
    public float Horizontal => horizontal;
    public float Vertical => vertical;
    public bool SwitchWeapon => switchWeapon;
    protected override void Start()
    {
        base.Start();
     //   transform.parent.position = currentPlayer.intialValue;
    }
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadRigidbody();
    }
    private void FixedUpdate()
    {
        this.GetInput();
        this.Moving();
        this.ChangeWeapon();
    }
    protected void LoadRigidbody()
    {
        if (this._rb != null) return;
        this._rb = GetComponentInParent<Rigidbody2D>();
    }
    public void SetHorizontal(float horizontal)
    {
        this.horizontal = horizontal;
    }
    public void SetVertical(float vertical)
    {
        this.vertical = vertical;
    }
    protected void PlayFootStepSound()
    {
        if (AudioManagerr.Instance != null && AudioManagerr.Instance.FootStepSource != null)
            AudioManagerr.Instance.FootStepSource.enabled = true;
    }
    protected void StopFootStepSound()
    {
        if (AudioManagerr.Instance != null && AudioManagerr.Instance.FootStepSource != null)
            AudioManagerr.Instance.FootStepSource.enabled = false;
    }
    protected void GetInput()
    {
        if (InputManager.Instance == null) return;
        this.horizontal = InputManager.Instance.InputHorizontal;
        this.vertical = InputManager.Instance.InputVertical;
        if (this.horizontal != 0 || this.vertical != 0) this.PlayFootStepSound();
        else this.StopFootStepSound();
    }
    protected void Moving()
    {
        if (this.playerCtrl == null || this.playerCtrl.PlayerDamReceive == null || this._rb == null) return;
        if (this.playerCtrl.PlayerDamReceive.IsDead) return;
        if (this.playerCtrl.PlayerDash != null && this.playerCtrl.PlayerDash.IsDashing) return;
        this._rb.linearVelocity = new Vector3(this.horizontal * this.speed, this.vertical * this.speed, 0f);

    }
    protected void ChangeWeapon()
    {
        if (this.playerCtrl == null || this.playerCtrl.PlayerDamReceive == null || InputManager.Instance == null) return;
        if (this.playerCtrl.PlayerDamReceive.IsHurt) return;
        if (this.TerminateCondition()) return;
        this.switchWeapon = InputManager.Instance.InputSwitchWeapon;
    }
    public bool TerminateCondition()
    {
        if (this.playerCtrl == null) return false;
        bool isMeleeAttacking = this.playerCtrl.PlayerAttack != null && this.playerCtrl.PlayerAttack.Attack;
        bool isRangedAttacking = this.playerCtrl.PlayerShooting != null && this.playerCtrl.PlayerShooting.Shoot;
        return isMeleeAttacking || isRangedAttacking;
    }
}
