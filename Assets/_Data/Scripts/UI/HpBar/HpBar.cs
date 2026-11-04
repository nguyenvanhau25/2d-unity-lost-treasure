using UnityEngine;
using UnityEngine.UI;

public class HpBar : DinoBehaviourScript
{
    [Header("Hp Bar")]
    [SerializeField] protected SliderHp sliderHp;
    [SerializeField] protected float hp;
    [SerializeField] protected float hpMax;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadSliderHp();
    }
    protected void LoadSliderHp()
    {
        if (this.sliderHp != null) return;
        this.sliderHp = GetComponentInChildren<SliderHp>();
    }
    protected void FixedUpdate()
    {
        this.HpShowing();
    }
    protected virtual void HpShowing()
    {
        if (this.sliderHp == null) return;
        this.sliderHp.SetMaxHp(this.hpMax);
        this.sliderHp.SetCurrentHp(this.hp);
    }
}
