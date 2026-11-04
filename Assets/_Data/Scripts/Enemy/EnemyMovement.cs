using UnityEngine;
public class EnemyMovement : EnemyAbstract
{
    [Header("Enemy Movment")]
    [SerializeField] protected Transform homePoint;
    [SerializeField] protected float speed = 2f;
    [SerializeField] protected float distance;
    [SerializeField] protected int distanceMaxX = 4;
    [SerializeField] protected int distanceMinX = -4;
    [SerializeField] protected int distanceMaxY = 4;
    [SerializeField] protected int distanceMinY = -4;
    [SerializeField] protected float moveTime = 2f;
    [SerializeField] protected float moveTimeCounter = 0f;
    [SerializeField] protected bool isMoving;
    protected float treasureSlowMultiplier = 1f;
    protected float treasureSlowEndTime;
    protected Vector2 wayPoint;
    public Vector2 WayPoint => wayPoint;
    public bool IsMoving => isMoving;
    public float Speed => this.GetCurrentSpeed();
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadHomePoint();
    }
    protected void LoadHomePoint()
    {
        if (this.homePoint != null) return;
        GameObject homePointObject = GameObject.Find("HomePoint");
        if (homePointObject != null) this.homePoint = homePointObject.transform;
    }
    protected void FixedUpdate()
    {
        this.PlayMoving();
    }
    protected void PlayMoving()
    {
        if (this.enemyCtrl == null || this.enemyCtrl.EnemyDamReceive == null || this.enemyCtrl.EnemyFollow == null ||
            this.enemyCtrl.EnemyDetect == null || this.enemyCtrl.Rigid == null) return;
        if (this.enemyCtrl.EnemyDamReceive.IsDead) return;
        if (this.enemyCtrl.EnemyDamReceive.IsHurt) return;
        if (this.enemyCtrl.EnemyFollow.AlwaysFollowPlayer) return;
        if (this.enemyCtrl.EnemyDetect.Detect)
        {
            this.isMoving = true;
            return;
        }
        this.Moving();
        this.StopMoving();
    }
    protected void Moving()
    {
        if (this.enemyCtrl == null || this.enemyCtrl.Rigid == null || transform.parent == null) return;
        this.isMoving = true;
        this.enemyCtrl.Rigid.MovePosition(Vector2.MoveTowards
        (transform.parent.position, this.wayPoint, this.GetCurrentSpeed() * Time.fixedDeltaTime));
    }
    
    protected void StopMoving()
    {
        if (transform.parent == null) return;
        this.distance = Vector2.Distance(transform.parent.position, this.wayPoint);
        if (this.distance < 0.5f)
        {
            this.isMoving = false;
            this.moveTimeCounter += Time.deltaTime;
            if (this.moveTimeCounter < this.moveTime) return;
            this.moveTimeCounter = 0;
            this.SetNewDestination();
        }
    }
    public void SetNewDestination()
    {
        if (this.homePoint == null) return;
        Vector2 homePosition = (Vector2)homePoint.position;
        this.wayPoint = homePosition + new Vector2(
            Random.Range(this.distanceMinX, this.distanceMaxX),
            Random.Range(this.distanceMinY, this.distanceMaxY)
        );
    }
    public void ApplyTreasureSlow(float multiplier, float duration)
    {
        this.treasureSlowMultiplier = Mathf.Min(this.treasureSlowMultiplier, Mathf.Clamp(multiplier, 0.1f, 1f));
        this.treasureSlowEndTime = Mathf.Max(this.treasureSlowEndTime, Time.time + duration);
    }
    protected float GetCurrentSpeed()
    {
        if (Time.time >= this.treasureSlowEndTime)
        {
            this.treasureSlowMultiplier = 1f;
            return this.speed;
        }
        return this.speed * this.treasureSlowMultiplier;
    }

    public void SetSpeed(float value)
    {
        this.speed = value;
    }
}
