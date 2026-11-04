using UnityEngine;
using UnityEngine.UI;

public class HpBarPlayer : HpBar
{
    // [Header("Hp Bar Player")]
    protected override void HpShowing()
    {
        if (PlayerCtrl.Instance == null || PlayerCtrl.Instance.PlayerDamReceive == null) return;
        this.hp = PlayerCtrl.Instance.PlayerDamReceive.Hp;
        this.hpMax = PlayerCtrl.Instance.PlayerDamReceive.HPMax;

        base.HpShowing();
    }

}
