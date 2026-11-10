using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAttack2 : DinoBehaviourScript
{
    [SerializeField] protected BossAttackCtrl bossAttackCtrl;

    [Header("Attack Position")]
    [Tooltip("When enabled, the boss walks to Center Point before starting the meteor rain. Disable this for bosses placed in the Special scene.")]
    [SerializeField] protected bool moveToCenterForAttack = true;
    [SerializeField] protected Transform centerPoint;

    [Header("Attack 2 Visual")]
    [Tooltip("Show the red warning zone used by the LastWar version of this attack.")]
    [SerializeField] protected bool showWarningZone = true;
    [Tooltip("Place the warning zone on the player's position once, when this attack begins. The warning then stays there so the player can escape it.")]
    [SerializeField] protected bool centerWarningOnPlayerAtAttackStart;
    [SerializeField] protected Transform warningTarget;
    [SerializeField] protected PolygonCollider2D collide;
    [SerializeField] protected SpriteRenderer sprite;
    [SerializeField] protected BossMeteorRain meteorRain;
    [SerializeField] protected float disToCenter;
    [SerializeField] protected bool attack2;
    public bool Attack2 => attack2;
    public bool isWorking2 = true;
    private Vector3 initialLocalPosition;

    protected override void Awake()
    {
        base.Awake();
        this.initialLocalPosition = transform.localPosition;
    }

    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadBossAttackCtrl();
        this.LoadCenterPoint();
        this.LoadWarningTarget();
        this.LoadCollider();
        this.LoadSpriteRenderer();
        this.LoadMeteorRain();
    }
    protected void LoadCenterPoint()
    {
        if (!this.moveToCenterForAttack || this.centerPoint != null) return;

        GameObject bossPoint = GameObject.Find("BossPoint");
        if (bossPoint == null)
        {
            Debug.LogWarning("BossAttack2 needs a Center Point when Move To Center For Attack is enabled.", this);
            return;
        }

        this.centerPoint = bossPoint.transform;
    }
    protected void LoadBossAttackCtrl()
    {
        if (this.bossAttackCtrl != null) return;
        this.bossAttackCtrl = GetComponentInParent<BossAttackCtrl>();
    }
    protected void LoadWarningTarget()
    {
        if (!this.centerWarningOnPlayerAtAttackStart || this.warningTarget != null) return;

        GameObject player = GameObject.Find("Player");
        if (player != null) this.warningTarget = player.transform;
    }
    protected void LoadCollider()
    {
        if (this.collide != null) return;
        this.collide = GetComponent<PolygonCollider2D>();
    }
    protected void LoadSpriteRenderer()
    {
        if (this.sprite != null) return;
        this.sprite = GetComponentInChildren<SpriteRenderer>();
    }
    protected void LoadMeteorRain()
    {
        if (this.meteorRain != null) return;
        this.meteorRain = GetComponentInChildren<BossMeteorRain>(true);
    }
    public void Attacking2()
    {
        if (!isWorking2 && this.CanRunAttack())
        {
            StartCoroutine(DoWork2());
        }
    }
    protected IEnumerator PlayAttack2Sound()
    {
        for (int i = 0; i < 5; i++)
        {
            if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("Attack2");
            yield return new WaitForSeconds(1);
        }
    }
    protected IEnumerator DoWork2()
    {
        if (!this.CanRunAttack()) yield break;

        this.isWorking2 = true;
        this.SetWarningZoneVisible(false);
        this.MoveWarningZoneToPlayerStartPosition();
        if (this.moveToCenterForAttack && this.centerPoint != null)
        {
            this.bossAttackCtrl.isMoving = true;
            this.disToCenter = this.bossAttackCtrl.DistanceToTarget(this.centerPoint.position);
            while (this.disToCenter > 0.01f)
            {
                this.disToCenter = this.bossAttackCtrl.DistanceToTarget(this.centerPoint.position);
                if (this.bossAttackCtrl.BossCtrl?.BossFlipDirect != null)
                    this.bossAttackCtrl.BossCtrl.BossFlipDirect.Flipping(this.centerPoint);
                if (transform.parent?.parent != null)
                    transform.parent.parent.position = this.bossAttackCtrl.MoveToTarget(this.centerPoint.position);
                yield return null;
            }
        }

        this.bossAttackCtrl.isMoving = false;
        this.bossAttackCtrl.angry = true;
        this.SetWarningZoneColor();
        this.SetWarningZoneVisible(this.showWarningZone);
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("MonsterBreath");

        yield return new WaitForSeconds(5);

        this.SetWarningZoneVisible(false);
        this.bossAttackCtrl.angry = false;
        this.attack2 = true;
        this.SetWarningZoneColor();
        this.SetWarningZoneVisible(this.showWarningZone);
        // The red area is visual warning only. Damage is now dealt by the
        // individual falling meteors when they land on the player.
        if (this.collide != null) this.collide.enabled = false;
        this.meteorRain?.StartRain(this);
        yield return StartCoroutine(PlayAttack2Sound());

        this.meteorRain?.StopRain();
        this.attack2 = false;
        this.SetWarningZoneVisible(false);
        if (this.sprite != null) this.sprite.color = Color.white;
        this.RestoreWarningZonePosition();
        if (this.collide != null) this.collide.enabled = false;

        yield return new WaitForSeconds(1.8f);
        this.bossAttackCtrl.attackCount = 0;
        this.isWorking2 = true;

        // Chuyển kĩ năng 
        if (this.bossAttackCtrl.BossAttack1 != null) this.bossAttackCtrl.BossAttack1.isWorking1 = false;
        int phaseTwoThreshold = this.bossAttackCtrl.BossCtrl?.BossDamReceive != null
            ? Mathf.Max(10, this.bossAttackCtrl.BossCtrl.BossDamReceive.HPMax / 2)
            : 14;
        if (this.bossAttackCtrl.BossCtrl?.BossDamReceive != null && this.bossAttackCtrl.BossCtrl.BossDamReceive.Hp <= phaseTwoThreshold)
            this.isWorking2 = false;

    }

    private void SetWarningZoneVisible(bool isVisible)
    {
        if (this.sprite != null) this.sprite.enabled = isVisible;
    }

    private void SetWarningZoneColor()
    {
        if (this.sprite != null) this.sprite.color = new Color(1f, 0.26f, 0.08f, 0.13f);
    }

    private void MoveWarningZoneToPlayerStartPosition()
    {
        if (!this.centerWarningOnPlayerAtAttackStart || this.warningTarget == null) return;

        Vector3 position = this.warningTarget.position;
        position.z = transform.position.z;
        transform.position = position;
    }

    private void RestoreWarningZonePosition()
    {
        if (this.centerWarningOnPlayerAtAttackStart)
        {
            transform.localPosition = this.initialLocalPosition;
        }
    }

    protected void OnDisable()
    {
        this.SetWarningZoneVisible(false);
        this.RestoreWarningZonePosition();
    }

    private bool CanRunAttack()
    {
        return this.bossAttackCtrl != null && this.bossAttackCtrl.BossCtrl != null &&
               this.meteorRain != null && this.collide != null;
    }

}
