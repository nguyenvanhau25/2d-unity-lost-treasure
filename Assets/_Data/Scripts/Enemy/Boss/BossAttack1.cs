using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAttack1 : DinoBehaviourScript
{
    [SerializeField] protected BossAttackCtrl bossAttackCtrl;
    [SerializeField] protected Transform player;
    [SerializeField] protected PolygonCollider2D collide1;
    [SerializeField] protected BoxCollider2D collide2;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected float disToPlayer;
    [SerializeField] protected bool attack1;
    public Transform Player => player;
    public bool Attack1 => attack1;
    public bool isWorking1 = false;

    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadPlayer();
        this.LoadBossAttackCtrl();
        this.LoadPolygonCollider();
        this.LoadBoxCollider();
        this.LoadSpriteRenderer();
    }
    protected void LoadPlayer()
    {
        if (this.player != null) return;
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null) this.player = playerObject.transform;
    }
    protected void LoadBossAttackCtrl()
    {
        if (this.bossAttackCtrl != null) return;
        this.bossAttackCtrl = GetComponentInParent<BossAttackCtrl>();
    }
    protected void LoadPolygonCollider()
    {
        if (this.collide1 != null) return;
        this.collide1 = GetComponent<PolygonCollider2D>();
    }
    protected void LoadBoxCollider()
    {
        if (this.collide2 != null) return;
        GameObject blockPlayer = GameObject.Find("Blockplayer");
        if (blockPlayer != null) this.collide2 = blockPlayer.GetComponent<BoxCollider2D>();
    }
    protected void LoadSpriteRenderer()
    {
        if (this.spriteRenderer != null) return;
        GameObject blockPlayer = GameObject.Find("Blockplayer");
        if (blockPlayer != null) this.spriteRenderer = blockPlayer.GetComponent<SpriteRenderer>();
    }
    public void Attacking1()
    {
        if (!isWorking1 && this.CanRunAttack())
        {
            StartCoroutine(DoWork1());
        }
    }
    protected IEnumerator DoWork1()
    {
        if (!this.CanRunAttack()) yield break;

        this.spriteRenderer.enabled = true;
        this.collide2.enabled = true;
        this.isWorking1 = true;
        this.bossAttackCtrl.angry = true;
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("MonsterBreath");

        yield return new WaitForSeconds(5);
        if (this.player == null || this.bossAttackCtrl == null) { this.isWorking1 = false; yield break; }
        this.disToPlayer = this.bossAttackCtrl.DistanceToTarget(this.player.position);
        this.bossAttackCtrl.angry = false;
        this.bossAttackCtrl.isMoving = true;

        while (disToPlayer >= 1)
        {
            this.disToPlayer = this.bossAttackCtrl.DistanceToTarget(this.player.position);
            if (this.bossAttackCtrl.BossCtrl?.BossFlipDirect != null)
                this.bossAttackCtrl.BossCtrl.BossFlipDirect.Flipping(this.player);
            if (transform.parent?.parent != null)
                transform.parent.parent.position = this.bossAttackCtrl.MoveToTarget(this.player.position);
            yield return null;
        }
        this.bossAttackCtrl.isMoving = false;

        yield return new WaitForSeconds(0.2f);
        this.attack1 = true;
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("EnemyAttack");
        this.collide1.enabled = true;

        yield return new WaitForSeconds(0.2f);
        this.attack1 = false;
        this.collide1.enabled = false;

        yield return new WaitForSeconds(1);
        this.isWorking1 = false;
        this.bossAttackCtrl.attackCount += 1;

        //Chuyển kĩ năng
        if (this.bossAttackCtrl.attackCount == 1)
        {
            this.isWorking1 = true;
            this.bossAttackCtrl.BossAttack2.isWorking2 = false;
        }

        if (this.bossAttackCtrl.BossCtrl?.BossDamReceive != null && this.bossAttackCtrl.BossCtrl.BossDamReceive.Hp <= 30)
        {
            this.isWorking1 = false;
            this.bossAttackCtrl.BossAttack2.isWorking2 = false;
        }
    }

    private bool CanRunAttack()
    {
        return this.bossAttackCtrl != null && this.player != null &&
               this.collide1 != null && this.collide2 != null && this.spriteRenderer != null;
    }
}
