using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDamReceive : EnemyDamReceive
{
    [Header("Boss Hit Audio")]
    [SerializeField] private string hitSoundName = "SwordBlood";
    [SerializeField, Range(0f, 1f)] private float hitSoundVolume = 0.8f;

    public override void Deduct(int damage)
    {
        int hpBeforeHit = this.Hp;
        base.Deduct(damage);

        if (this.Hp >= hpBeforeHit || AudioManagerr.Instance == null) return;
        AudioManagerr.Instance.PlaySFX(this.hitSoundName, this.hitSoundVolume);
    }

    protected override void SetPosDead()
    {

    }

    protected override void OnDead()
    {
        base.OnDead();
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.StopMusic();
    }
}
