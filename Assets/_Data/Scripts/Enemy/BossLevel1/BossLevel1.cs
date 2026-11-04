using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Dark Forest boss. Inherits DamageReceiver so player arrows can use the
/// regular DamageSender path, while sword hits use their own damage setting.
/// </summary>
public class BossLevel1 : MonoBehaviour
{
    [Header("Movement and attacks")]
    [SerializeField, Min(0f)] private float speed = 3f;
    [SerializeField, Min(0.01f)] private float damageInterval = 0.5f;
    [SerializeField] private GameObject bulletEnemy;
    [SerializeField] private Transform firePos;
    [SerializeField] private Transform enemySpanwPos;
    [SerializeField, Min(0.1f)] private float timeRandomSkill = 2f;
    [SerializeField, Min(0f)] private float bulletSpeedNormal = 20f;
    [SerializeField, Min(0f)] private float bulletSpeedAround = 10f;
    [SerializeField, Min(1)] private int bulletsAroundCount = 12;
    [SerializeField] private GameObject miniEmeny;
    [SerializeField] private PlayerDamReceive player;
    [FormerlySerializedAs("dmgSender"), SerializeField, Min(0)] private int contactDamage = 3;
    [SerializeField, Min(0)] private int healAmount = 5;

    [Header("Health and player damage")]
    [SerializeField] private int currentHealth;
    [FormerlySerializedAs("hPBoss"), SerializeField, Min(1)] private int maxHealth = 50;
    [SerializeField, Min(0)] private int slashDamage = 2;
    [SerializeField, Min(0)] private int arrowDamage = 1;
    [SerializeField] private Slider hPSlider;

    [Header("Hit feedback")]
    [SerializeField] private SpriteRenderer hitRenderer;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField, Min(0f)] private float hitFlashDuration = 0.14f;
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private string hurtTriggerName = "Hurt";
    [SerializeField] private string deadBoolName = "Dead";
    [SerializeField] private string hitSoundName = "SwordBlood";
    [SerializeField, Range(0f, 1f)] private float hitSoundVolume = 0.8f;


    [SerializeField] private GameObject healEffect;

    private float damageTimer;
    private bool isDead;
    public bool IsDead => this.isDead;
    private Color originalHitColor = Color.white;
    private Coroutine hitFlashRoutine;

    private void Awake()
    {
        if (this.hitRenderer == null) this.hitRenderer = GetComponent<SpriteRenderer>();
        if (this.bossAnimator == null) this.bossAnimator = GetComponent<Animator>();
        if (this.healEffect != null) this.healEffect.SetActive(false);
    }

    private void OnEnable()
    {
        this.currentHealth = Mathf.Max(1, this.maxHealth);
        this.isDead = false;
        this.damageTimer = 0f;
    }

    private void Start()
    {
        if (this.hPSlider != null)
        {
            this.hPSlider.maxValue = this.maxHealth;
            this.hPSlider.value = this.currentHealth;
        }
        if (this.hitRenderer != null) this.originalHitColor = this.hitRenderer.color;
        StartCoroutine(this.RandomSkill());
    }

    private void Update()
    {
        if (this.isDead) return;
        this.MoveTowardsPlayer();
        if (this.damageTimer > 0f) this.damageTimer -= Time.deltaTime;
    }

    private void MoveTowardsPlayer()
    {
        if (this.player == null) return;
        transform.position = Vector2.MoveTowards(transform.position, this.player.transform.position, this.speed * Time.deltaTime);
        if (transform.position.x < this.player.transform.position.x)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (transform.position.x > this.player.transform.position.x)
            transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    private void TakeDamage(int damage)
    {


        if (damage <= 0 || this.isDead) return;

        this.currentHealth = Mathf.Max(0, this.currentHealth - damage);
          if (this.hPSlider != null) this.hPSlider.value = this.currentHealth;
        if (this.bossAnimator != null && this.HasAnimatorParameter(this.hurtTriggerName, 
            AnimatorControllerParameterType.Trigger))
            this.bossAnimator.SetTrigger(this.hurtTriggerName);
        if (this.hitRenderer != null && this.hitFlashDuration > 0f)
        {
            if (this.hitFlashRoutine != null) StopCoroutine(this.hitFlashRoutine);
            this.hitFlashRoutine = StartCoroutine(this.FlashOnHit());
        }
        if (AudioManagerr.Instance != null)
            AudioManagerr.Instance.PlaySFX(this.hitSoundName, this.hitSoundVolume);

        if (this.currentHealth == 0) this.Die();
    }

    public void ReceiveArrowHit()
    {
        this.TakeDamage(this.arrowDamage);
    }

    /// <summary>Used by Treasure Resonance without changing normal sword/arrow damage.</summary>
    public void ReceiveResonanceHit(int damage)
    {
        this.TakeDamage(damage);
    }

    private IEnumerator FlashOnHit()
    {
        this.hitRenderer.color = this.hitFlashColor;
        yield return new WaitForSeconds(this.hitFlashDuration);
        if (this.hitRenderer != null) this.hitRenderer.color = this.originalHitColor;
        this.hitFlashRoutine = null;
    }

    private void Die()
    {
        this.isDead = true;
        if (this.hPSlider != null) this.hPSlider.value = 0f;
        if (this.bossAnimator != null && this.HasAnimatorParameter(this.deadBoolName, AnimatorControllerParameterType.Bool))
            this.bossAnimator.SetBool(this.deadBoolName, true);
        if (this.hitFlashRoutine != null)
        {
            StopCoroutine(this.hitFlashRoutine);
            this.hitFlashRoutine = null;
        }
        if (this.hitRenderer != null) this.hitRenderer.color = this.originalHitColor;
        AudioManagerr.Instance?.StopMusic();
        Destroy(gameObject, 1f);
    }


    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (string.IsNullOrEmpty(parameterName) || this.bossAnimator == null) return false;
        foreach (AnimatorControllerParameter parameter in this.bossAnimator.parameters)
            if (parameter.name == parameterName && parameter.type == type) return true;
        return false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
       
        if (collision.CompareTag("PlayerAttackArea"))
            this.TakeDamage(this.slashDamage);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (this.isDead || this.damageTimer > 0f || this.player == null) return;
        if (!collision.transform.root.CompareTag("Player")) return;
        this.player.Deduct(this.contactDamage);
        this.damageTimer = this.damageInterval;
    }

    private void ShotNormal()
    {
        if (this.player == null || this.bulletEnemy == null || this.firePos == null) return;
        Vector3 directionToPlayer = (this.player.transform.position - this.firePos.position).normalized;
        GameObject bullet = Instantiate(this.bulletEnemy, this.firePos.position, Quaternion.identity);
        EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
        if (enemyBullet == null) enemyBullet = bullet.AddComponent<EnemyBullet>();
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("BossShoot");
        enemyBullet.SetMovementDirection(directionToPlayer * this.bulletSpeedNormal);
    }

    private void ShotAround()
    {
        if (this.bulletEnemy == null || this.firePos == null) return;
        float angleStep = 360f / Mathf.Max(1, this.bulletsAroundCount);
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("BossShoot");
        for (int i = 0; i < this.bulletsAroundCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            GameObject bullet = Instantiate(this.bulletEnemy, this.firePos.position, Quaternion.identity);
            EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
            if (enemyBullet == null) enemyBullet = bullet.AddComponent<EnemyBullet>();
            enemyBullet.SetMovementDirection(direction * this.bulletSpeedAround);
        }
    }

    private void CreateMiniEnemy()
    {
        if (this.miniEmeny == null) return;

        Vector3 spawnPosition = this.enemySpanwPos != null
            ? this.enemySpanwPos.position
            : this.transform.position;
        GameObject spawnedEnemy = Instantiate(this.miniEmeny, spawnPosition, Quaternion.identity);
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("CreateMini");
        EnemyFollow enemyFollow = spawnedEnemy.GetComponentInChildren<EnemyFollow>();
        if (enemyFollow != null) enemyFollow.SetAlwaysFollowPlayer(true);

        EnemyMovement enemyMovement = spawnedEnemy.GetComponentInChildren<EnemyMovement>();
        if (enemyMovement != null) enemyMovement.SetSpeed(5f);
    }

    private void Heal()
    {
        if (this.currentHealth == maxHealth)
        {
            return;
        } 
        if (this.healEffect != null) this.healEffect.SetActive(true);
        this.currentHealth = Mathf.Min(this.maxHealth, this.currentHealth + this.healAmount);
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("HealBossLevel1");
        if (this.hPSlider != null) this.hPSlider.value = this.currentHealth;
        Invoke("SetAvtiveHealEffect", 0.5f);
    }

    private void SetAvtiveHealEffect()
    {
        if (this.healEffect != null) this.healEffect.SetActive(false);
    }

    private IEnumerator RandomSkill()
    {
        while (!this.isDead)
        {
            yield return new WaitForSeconds(this.timeRandomSkill);
            if (this.isDead || this.player == null) continue;
            switch (Random.Range(0, 4))
            {
                case 0:
                    ShotNormal();
                    break;
                case 1:
                    ShotAround();
                    break;
                case 2:
                    CreateMiniEnemy();
                    break;
                case 3:                  
                    Heal();
                    break;
                default:
                    break;
            }
        
        }
    }

    
}
