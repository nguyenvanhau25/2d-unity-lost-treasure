using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class BossBringer : DamageReceiver
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Transform firePos;
    [SerializeField] private Transform enemySpawnPos;
    // SkeletonHUD stores the playable Skeleton as a child Transform, not as
    // the prefab root GameObject. Keeping this as Transform avoids Unity's
    // GameObject cast error when the child is spawned.
    [SerializeField] private Transform skeletonPrefab;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Rigidbody2D rigid;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Tilemap walkableTilemap;

    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 150;
    [SerializeField, Min(0f)] private float hurtDuration = 0.3f;
    [SerializeField, Min(0)] private int spellHealAmount = 20;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
    [SerializeField, Min(0f)] private float detectRange = 8f;
    [SerializeField, Min(0f)] private float patrolDistance = 3f;
    [SerializeField, Min(0f)] private float attackRange = 1.25f;
    [SerializeField, Min(0.05f)] private float pathRefreshInterval = 0.25f;
    [SerializeField, Min(1)] private int pathSearchRadius = 24;
    [SerializeField] private float distance;

    [Header("Attacks")]
    [SerializeField, Min(0)] private int attackDamage = 4;
    [SerializeField, Min(0)] private int bulletDamage = 3;
    [SerializeField, Min(0f)] private float bulletSpeed = 8f;
    [SerializeField, Min(0)] private int skeletonCount = 2;
    [SerializeField, Min(0f)] private float attackHitTime = 0.45f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.5f;
    [SerializeField] private GameObject healEffect;
    private Transform target;
    private Vector3 patrolCenter;
    private Vector3 patrolTarget;
    private bool hasDetectedPlayer;
    private bool patrolMovingRight = true;
    private bool specialQueued;
    private bool isActing;
    private int nextSpecial;
    private string currentAnimation;
    private readonly List<Vector3> path = new List<Vector3>();
    private int pathIndex;
    private float nextPathRefreshTime;

    protected override void Awake()
    {
        base.Awake();
        this.animator = this.animator != null ? this.animator : GetComponent<Animator>();
        this.healthBar = this.healthBar != null ? this.healthBar : GetComponentInChildren<Slider>();
        this.firePos = this.firePos != null ? this.firePos : transform.Find("FirePos");
        this.enemySpawnPos = this.enemySpawnPos != null ? this.enemySpawnPos : transform.Find("SpawnPos");
        this.patrolCenter = transform.position;
        this.patrolTarget = this.GetNextPatrolTarget();
        if (healEffect != null) healEffect.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (this.bodyCollider != null) this.bodyCollider.enabled = true;
    }

    protected override void Start()
    {
        base.Start();
        bool isSpecialScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Special";
        this.attackDamage = 1;
        this.bulletDamage = 1;
        this.maxHealth = isSpecialScene ? 18 : Mathf.Min(this.maxHealth, 24);
        this.hp = this.maxHealth;
        this.hpMax = this.maxHealth;
        this.spellHealAmount = Mathf.Min(this.spellHealAmount, 3);
        this.moveSpeed = Mathf.Min(this.moveSpeed, isSpecialScene ? 1.85f : 2.0f);
        this.bulletSpeed = Mathf.Min(this.bulletSpeed, 6.5f);
        this.skeletonCount = 1;
        this.attackHitTime = Mathf.Max(this.attackHitTime, 0.65f);
        this.attackCooldown = Mathf.Max(this.attackCooldown, 1.6f);

        GameObject player = GameObject.Find("Player");
        if (player != null) this.target = player.transform;
        this.UpdateHealthBar();
        this.PlayAnimation("Idle");
    }

    protected override void Update()
    {
        base.Update();
        if (this.isDead || this.target == null || this.isActing) return;
        if (this.isHurt)
        {
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, this.target.position);
        if (!this.hasDetectedPlayer && distanceToPlayer <= this.detectRange)
            this.hasDetectedPlayer = true;

        if (!this.hasDetectedPlayer)
        {
            this.Patrol();
            return;
        }
        if (distanceToPlayer <= this.attackRange)
        {
            StartCoroutine(this.Attack());
            return;
        }

        if (this.specialQueued)
        {
            this.specialQueued = false;
            StartCoroutine(this.UseSpecial());
            return;
        }
        else
        {     
            if(distanceToPlayer - this.attackRange >= distance && hasDetectedPlayer)
            {
                StartCoroutine(this.UseSpecial());
                return;
            }
        }

        
        this.MoveTo(this.target.position);
    }

    public override void Deduct(int damage)
    {
        if (damage <= 0 || this.isDead || this.isHurt) return;

        int oldHealth = this.hp;
        base.Deduct(damage);
        if (this.hp >= oldHealth) return;

        this.UpdateHealthBar();
        if (!this.isDead)
        {
            this.specialQueued = true;
            animator.SetTrigger("Hurt");
        }
    }

    protected override void Reborn()
    {
        this.hpMax = this.maxHealth;
        this.hurtTime = this.hurtDuration;
        base.Reborn();
        this.UpdateHealthBar();
    }

    protected override void OnDead()
    {
        this.StopAllCoroutines();
        this.isActing = true;
        if (this.bodyCollider != null) this.bodyCollider.enabled = false;
        if (this.rigid != null) this.rigid.simulated = false;
        this.PlayAnimation("Death");
        Destroy(gameObject, 1f);
    }

    protected override void Hurt()
    {
        if (this.rigid == null) return;
        this.rigid.linearVelocity = Vector2.zero;
        this.rigid.angularVelocity = 0f;
    }

    private void Patrol()
    {
        if (Vector2.Distance(transform.position, this.patrolTarget) < 0.1f)
            this.patrolTarget = this.GetNextPatrolTarget();
        this.MoveTo(this.patrolTarget);
    }

    private Vector3 GetNextPatrolTarget()
    {
        // Alternate between the two ends. Basing this only on the current
        // position can choose the same blocked tile forever on a cave path.
        float side = this.patrolMovingRight ? 1f : -1f;
        this.patrolMovingRight = !this.patrolMovingRight;
        return this.GetNearestWalkablePosition(this.patrolCenter + Vector3.right * side * this.patrolDistance);
    }

    private void MoveTo(Vector3 destination)
    {
        if (this.walkableTilemap == null)
        {
            this.MoveDirectly(destination);
            this.animator.SetBool("Moving", true);
            return;
        }

        if (Time.time >= this.nextPathRefreshTime || this.pathIndex >= this.path.Count)
        {
            this.BuildPath(destination);
            this.nextPathRefreshTime = Time.time + this.pathRefreshInterval;
        }

        if (this.pathIndex >= this.path.Count)
        {
            this.animator.SetBool("Moving", false);
            return;
        }

        Vector3 nextWaypoint = this.path[this.pathIndex];

        if (Vector2.Distance(transform.position, nextWaypoint) <= 0.15f)
        {
            this.pathIndex++;
            return;
        }

        this.animator.SetBool("Moving", true);
        this.MoveDirectly(nextWaypoint);
    }

    private void MoveDirectly(Vector3 destination)
    {
        Vector3 nextPosition = Vector2.MoveTowards(transform.position, destination, this.moveSpeed * Time.deltaTime);
        if (this.rigid != null)
        {
            this.rigid.linearVelocity = Vector2.zero;
            this.rigid.angularVelocity = 0f;
            this.rigid.position = nextPosition;
        }
        else transform.position = nextPosition;
        this.Flip(destination.x - transform.position.x);
    }

    private IEnumerator Attack()
    {
        this.isActing = true;
        //this.PlayAnimation("Attack");
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(this.attackHitTime);

        if (!this.isDead && this.target != null && Vector2.Distance(transform.position, this.target.position) <= this.attackRange + 0.25f)
        {
            PlayerDamReceive player = this.target.GetComponentInChildren<PlayerDamReceive>();
            if (player != null) player.Deduct(this.attackDamage);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, 1f - this.attackHitTime) + this.attackCooldown);
        this.isActing = false;
    }

    private IEnumerator UseSpecial()
    {
        this.isActing = true;
        if (this.nextSpecial == 0)
        {
            // this.PlayAnimation("Cast");
            if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("SpellBossBringer");
            animator.SetTrigger("Cast");
            Invoke("HeafEffectActive", 0.4f);
            Invoke("HeafEffectUnActive", 1f);
            this.Add(this.spellHealAmount);
            this.UpdateHealthBar();
            yield return new WaitForSeconds(0.45f);
            this.SummonSkeletons();
            yield return new WaitForSeconds(0.45f);
        }
        else
        {
            this.TeleportToPlayer();
            //  this.PlayAnimation("Spell");
            animator.SetTrigger("Spell");
            if (this.healthBar != null) this.healthBar.gameObject.SetActive(false);
            yield return new WaitForSeconds(1.5f);
            if (this.healthBar != null) this.healthBar.gameObject.SetActive(true);
            this.FireBullet();
        }
        yield return new WaitForSeconds(1.5f);
        this.nextSpecial = 1 - this.nextSpecial;
        this.isActing = false;
    }

    private void HeafEffectUnActive()
    {
        if (healEffect != null) healEffect.SetActive(false);
    }
    private void HeafEffectActive()
    {
        if (healEffect != null) healEffect.SetActive(true);
    }


    private void SummonSkeletons()
    {
        if (this.skeletonPrefab == null || this.enemySpawnPos == null) return;
        for (int i = 0; i < this.skeletonCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.45f;
            Transform skeleton = Instantiate(this.skeletonPrefab, this.enemySpawnPos.position + (Vector3)offset, Quaternion.identity);
            EnemyFollow enemyFollow = skeleton.GetComponentInChildren<EnemyFollow>();
            if (enemyFollow != null) enemyFollow.SetAlwaysFollowPlayer(true);
        }
    }

    private void TeleportToPlayer()
    {
        if (this.target == null) return;
        Vector3 desiredPosition = this.target.position;
        Vector3 safePosition = this.GetNearestWalkablePosition(desiredPosition);
        if (this.rigid != null)
        {
            this.rigid.linearVelocity = Vector2.zero;
            this.rigid.angularVelocity = 0f;
            this.rigid.position = safePosition;
        }
        else transform.position = safePosition;
        this.path.Clear();
        this.pathIndex = 0;
        this.Flip(this.target.position.x - transform.position.x);
    }

    private void FireBullet()
    {
        if (this.bulletPrefab == null || this.target == null || this.firePos == null) return;
        GameObject bullet = Instantiate(this.bulletPrefab, this.firePos.position, Quaternion.identity);
        BossBringerProjectile projectile = bullet.GetComponent<BossBringerProjectile>();
        if (projectile == null)
        {
            Debug.LogWarning("BossBringerBullet needs the BossBringerProjectile component in its Inspector.", bullet);
            Destroy(bullet);
            return;
        }
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("BossBringerShoot");
        projectile.Launch(this.target.position - this.firePos.position, this.bulletSpeed, this.bulletDamage);
    }

    private void Flip(float direction)
    {
        if (Mathf.Approximately(direction, 0f)) return;
        Vector3 scale = transform.localScale;
        // Bringer's source sprite faces left at positive scale.
        scale.x = direction > 0f ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    private void PlayAnimation(string animationName)
    {
        if (this.animator == null || this.currentAnimation == animationName) return;
        this.currentAnimation = animationName;
        this.animator.Play(animationName, 0, 0f);
    }

    private void UpdateHealthBar()
    {
        if (this.healthBar == null) return;
        this.healthBar.maxValue = this.maxHealth;
        this.healthBar.value = this.hp;
    }

    private Vector3 GetNearestWalkablePosition(Vector3 position)
    {
        if (this.walkableTilemap == null) return position;

        Vector3Int origin = this.walkableTilemap.WorldToCell(position);
        if (this.walkableTilemap.HasTile(origin)) return this.walkableTilemap.GetCellCenterWorld(origin);

        for (int radius = 1; radius <= this.pathSearchRadius; radius++)
        {
            Vector3Int closestCell = default;
            float closestDistance = float.MaxValue;
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius) continue;
                    Vector3Int cell = origin + new Vector3Int(x, y, 0);
                    if (!this.walkableTilemap.HasTile(cell)) continue;

                    float distance = ((Vector2)this.walkableTilemap.GetCellCenterWorld(cell) - (Vector2)position).sqrMagnitude;
                    if (distance >= closestDistance) continue;
                    closestDistance = distance;
                    closestCell = cell;
                }
            }

            if (closestDistance < float.MaxValue) return this.walkableTilemap.GetCellCenterWorld(closestCell);
        }

        return transform.position;
    }

    private void BuildPath(Vector3 destination)
    {
        this.path.Clear();
        this.pathIndex = 0;

        Vector3Int start = this.walkableTilemap.WorldToCell(transform.position);
        Vector3Int goal = this.walkableTilemap.WorldToCell(this.GetNearestWalkablePosition(destination));
        if (!this.walkableTilemap.HasTile(start)) start = this.walkableTilemap.WorldToCell(this.GetNearestWalkablePosition(transform.position));
        if (!this.walkableTilemap.HasTile(start) || !this.walkableTilemap.HasTile(goal)) return;

        List<Vector3Int> open = new List<Vector3Int> { start };
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, int> cost = new Dictionary<Vector3Int, int> { { start, 0 } };
        Vector3Int[] directions = { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };
        int searchedNodes = 0;

        while (open.Count > 0 && searchedNodes++ < 1200)
        {
            int bestIndex = 0;
            int bestScore = int.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                int score = cost[open[i]] + this.CellDistance(open[i], goal);
                if (score < bestScore)
                {
                    bestIndex = i;
                    bestScore = score;
                }
            }

            Vector3Int current = open[bestIndex];
            open.RemoveAt(bestIndex);
            if (current == goal)
            {
                this.CreatePath(cameFrom, current, start);
                return;
            }

            foreach (Vector3Int direction in directions)
            {
                Vector3Int next = current + direction;
                if (!this.walkableTilemap.HasTile(next)) continue;
                int nextCost = cost[current] + 1;
                if (cost.TryGetValue(next, out int savedCost) && nextCost >= savedCost) continue;
                cost[next] = nextCost;
                cameFrom[next] = current;
                if (!open.Contains(next)) open.Add(next);
            }
        }
    }

    private void CreatePath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current, Vector3Int start)
    {
        List<Vector3> reversePath = new List<Vector3>();
        while (current != start)
        {
            reversePath.Add(this.walkableTilemap.GetCellCenterWorld(current));
            current = cameFrom[current];
        }
        reversePath.Reverse();
        this.path.AddRange(reversePath);
    }

    private int CellDistance(Vector3Int first, Vector3Int second)
    {
        return Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y);
    }
}
