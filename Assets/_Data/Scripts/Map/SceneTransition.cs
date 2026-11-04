using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : DinoBehaviourScript
{
    [SerializeField] protected SceneName sceneName;
    [SerializeField] protected Vector2 playerPos;
    [SerializeField] protected PlayerPos playerStorage;
    [SerializeField] protected PlayerHpSO playerHpSO;
    [SerializeField] protected ArrowSO arrowSO;
    [SerializeField] protected Inventory inventory;
    [SerializeField] protected Animator transAnim;
    protected bool isTransitioning;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadPlayerPos();
        this.LoadPlayerHpSO();
        this.LoadArrowSO();
        this.LoadInventory();
        this.LoadAnimator();
    }
    protected void LoadPlayerPos()
    {
        if (this.playerStorage != null) return;
        this.playerStorage = Resources.Load<PlayerPos>("GameData/PlayerPos");
    }
    protected void LoadPlayerHpSO()
    {
        if (this.playerHpSO != null) return;
        this.playerHpSO = Resources.Load<PlayerHpSO>("GameData/PlayerHpSO");
    }
    protected void LoadArrowSO()
    {
        if (this.arrowSO != null) return;
        this.arrowSO = Resources.Load<ArrowSO>("GameData/ArrowSO");
    }
    protected void LoadInventory()
    {
        if (this.inventory != null) return;
        PlayerCtrl player = PlayerCtrl.Instance;
        if (player != null) this.inventory = player.GetComponentInChildren<Inventory>();
    }
    protected void LoadAnimator()
    {
        if (this.transAnim != null) return;
        this.transAnim = GetComponentInChildren<Animator>();
    }
    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (this.isTransitioning || other == null || other.GetComponentInParent<PlayerCtrl>() == null) return;
        this.isTransitioning = true;
        this.SetHp();
        this.SetPos();
        this.SetInventory();
        this.SetArrowCount();
        this.NextLevel();
        if (AudioManagerr.Instance != null && AudioManagerr.Instance.FootStepSource != null)
            AudioManagerr.Instance.FootStepSource.enabled = false;
    }
    protected void SetHp()
    {
        if (this.playerHpSO != null && PlayerCtrl.Instance != null && PlayerCtrl.Instance.PlayerDamReceive != null)
            this.playerHpSO.currentHp = PlayerCtrl.Instance.PlayerDamReceive.Hp;
    }
    protected void SetPos()
    {
        if (this.playerStorage != null) this.playerStorage.intialValue = playerPos;
    }
    protected void SetInventory()
    {
        if (this.inventory == null) return;
        this.inventory.ClearInventory();
        this.inventory.SaveItems();
    }
    protected void SetArrowCount()
    {
        if (this.arrowSO != null && ArrowPlayerSpawn.Instance != null)
            this.arrowSO.arrowCounts = ArrowPlayerSpawn.Instance.SpawnCount;
    }
    protected void NextLevel()
    {
        StartCoroutine(this.LoadLevel());
    }
    IEnumerator LoadLevel()
    {
        this.StopPlayerMovement();
        if (this.transAnim != null) this.transAnim.SetTrigger("End");
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene(this.sceneName.ToString());
    }
    protected void StopPlayerMovement()
    {
        PlayerCtrl player = PlayerCtrl.Instance;
        if (player == null) return;

        if (player.PlayerMovement != null)
        {
            if (player.PlayerMovement._Rb != null)
                player.PlayerMovement._Rb.constraints = RigidbodyConstraints2D.FreezeAll;
            player.PlayerMovement.enabled = false;
            player.PlayerMovement.SetHorizontal(0);
            player.PlayerMovement.SetVertical(0);
        }
        if (player.PlayerAttack != null) player.PlayerAttack.enabled = false;
    }
}
