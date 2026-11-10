using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BtnRestart : BaseButton
{
    [Header("Btn Restart")]
    [SerializeField] protected SceneName sceneName;
    [SerializeField] protected ArrowSO arrowSO;
    [SerializeField] protected HpPotionSO hpPotionSO;
    [SerializeField] protected InventorySO inventorySO;
    [SerializeField] protected PlayerHpSO playerHpSO;
    [SerializeField] protected PlayerPos playerPos;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadArrowSO();
        this.LoadHpPotionSO();
        this.LoadInventorySO();
        this.LoadPlayerHpSO();
        this.LoadPlayerPos();
    }
    protected void LoadArrowSO()
    {
        if (this.arrowSO != null) return;
        this.arrowSO = Resources.Load<ArrowSO>("GameData/ArrowSO");
    }
    protected void LoadHpPotionSO()
    {
        if (this.hpPotionSO != null) return;
        this.hpPotionSO = Resources.Load<HpPotionSO>("GameData/HpPotionSO");
    }
    protected void LoadInventorySO()
    {
        if (this.inventorySO != null) return;
        this.inventorySO = Resources.Load<InventorySO>("GameData/InventorySO");
    }
    protected void LoadPlayerHpSO()
    {
        if (this.playerHpSO != null) return;
        this.playerHpSO = Resources.Load<PlayerHpSO>("GameData/PlayerHpSO");
    }
    protected void LoadPlayerPos()
    {
        if (this.playerPos != null) return;
        this.playerPos = Resources.Load<PlayerPos>("GameData/PlayerPos");
    }
    protected override void OnClick()
    {
        this.ResetValues();
        this.BackFirstScene();
    }
    protected void BackFirstScene()
    {
        SceneManager.LoadScene(LostTreasureSceneHelper.ResolveLoadableSceneName(sceneName));
    }
    protected void ResetValues()
    {
        if (LostTreasureManager.Instance != null && this.sceneName == SceneName.ForestStartScene)
        {
            LostTreasureManager.Instance.ResetProgress();
        }
        this.arrowSO.arrowCounts = this.arrowSO.defaultArrowCounts;
        this.hpPotionSO.hpPotionCount = this.hpPotionSO.defaultHpPotionCount;
        this.playerHpSO.currentHp = this.playerHpSO.maxHp;
        this.playerPos.intialValue = new Vector2(-16, 3);
    }
}
