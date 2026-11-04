using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyDetectPlayer : EnemyAbstract
{
    [Header("Enemy Detect Player")]
    [SerializeField] protected bool detect = false;
    [SerializeField] protected float directionRange = 6f;
    public bool Detect => detect;
    protected void FixedUpdate()
    {
        this.DetectPlayer();
    }
    protected void DetectPlayer()
    {
        if (this.enemyCtrl == null || this.enemyCtrl.EnemyFollow == null || this.enemyCtrl.EnemyFollow.Target == null)
        {
            this.detect = false;
            return;
        }
        if (!this.CanDetect()) return;
        Vector3 playerPos = this.enemyCtrl.EnemyFollow.Target.position;

        if (!this.CheckDetect(playerPos)) return;

        RaycastHit2D ray = Physics2D.Raycast(transform.position, playerPos - transform.position);

        if (ray.collider == null) return;

        if (ray.collider.CompareTag("Player") || ray.collider.CompareTag("PlayerAttackArea")
        || ray.collider.CompareTag("AttackArea")) this.detect = true;
        else this.detect = false;

        Color color = this.detect ? Color.green : Color.red;
        Debug.DrawRay(transform.position, playerPos - transform.position, color);
    }
    protected bool CanDetect()
    {
        if (this.enemyCtrl == null || this.enemyCtrl.EnemyFollow == null || this.enemyCtrl.EnemyFollow.Target == null)
        {
            this.detect = false;
            return false;
        }
        if (this.enemyCtrl.EnemyFollow.Target.position.z == 1)//Check Player Die
        {
            this.detect = false;
            return false;
        }
        return true;
    }
    protected bool CheckDetect(Vector3 playerPos)
    {
      
        float distanceToPlayer = Vector2.Distance(transform.position, playerPos);
        if (distanceToPlayer > this.directionRange)
        {
            this.detect = false;
            return false;
        }
       
        return true;
    }

}
