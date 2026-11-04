using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HpBarBoss : HpBar
{
    [Header("Boss Health Source")]
    [SerializeField] private BossDamReceive bossDamReceive;
    [Tooltip("Use this when the boss health bar is a regular Unity Slider instead of a SliderHp component.")]
    [SerializeField] private Slider fallbackSlider;

    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadBossDamageReceiver();
        this.LoadFallbackSlider();
    }

    private void LoadBossDamageReceiver()
    {
        if (this.bossDamReceive != null) return;
        this.bossDamReceive = GetComponentInParent<BossCtrl>()?.BossDamReceive;
    }

    private void LoadFallbackSlider()
    {
        if (this.fallbackSlider != null) return;
        this.fallbackSlider = GetComponentInChildren<Slider>();
    }

    protected override void HpShowing()
    {
        if (this.bossDamReceive == null) return;

        this.hp = this.bossDamReceive.Hp;
        this.hpMax = this.bossDamReceive.HPMax;

        if (this.sliderHp != null)
        {
            base.HpShowing();
            return;
        }

        if (this.fallbackSlider == null) return;
        this.fallbackSlider.maxValue = this.hpMax;
        this.fallbackSlider.value = this.hp;
    }
}
