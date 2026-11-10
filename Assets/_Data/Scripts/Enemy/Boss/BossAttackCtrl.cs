using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAttackCtrl : DinoBehaviourScript
{
    [SerializeField] protected BossCtrl bossCtrl;
    public BossCtrl BossCtrl => bossCtrl;
    [SerializeField] protected BossAttack1 bossAttack1;
    public BossAttack1 BossAttack1 => bossAttack1;
    [SerializeField] protected BossAttack2 bossAttack2;
    public BossAttack2 BossAttack2 => bossAttack2;
    [SerializeField] protected BossAttack3 bossAttack3;
    public BossAttack3 BossAttack3 => bossAttack3;
    public bool angry;
    public bool isMoving = false;
    public int attackCount = 0;
    public float speed = 7.2f;

    protected override void Start()
    {
        base.Start();
        bool isSpecialScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Special";
        this.speed = Mathf.Min(this.speed, isSpecialScene ? 6.5f : 7.5f);
    }
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadBossCtrl();
        this.LoadBossAttack1();
        this.LoadBossAttack2();
        this.LoadBossAttack3();
    }
    protected void LoadBossCtrl()
    {
        if (this.bossCtrl != null) return;
        this.bossCtrl = GetComponentInParent<BossCtrl>();
    }
    protected void LoadBossAttack1()
    {
        if (this.bossAttack1 != null) return;
        this.bossAttack1 = GetComponentInChildren<BossAttack1>();
    }
    protected void LoadBossAttack2()
    {
        if (this.bossAttack2 != null) return;
        this.bossAttack2 = GetComponentInChildren<BossAttack2>();
    }
    protected void LoadBossAttack3()
    {
        if (this.bossAttack3 != null) return;
        this.bossAttack3 = GetComponentInChildren<BossAttack3>();
    }
    public float DistanceToTarget(Vector2 target)
    {
        return Vector2.Distance(transform.position, target);
    }
    public Vector2 MoveToTarget(Vector2 target)
    {
        return Vector2.MoveTowards(transform.position, target, this.speed * Time.deltaTime);
    }
}
