using System.Collections;
using TMPro;
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
    public string DestinationSceneName => LostTreasureSceneHelper.ResolveLoadableSceneName(this.sceneName);

    protected override void Start()
    {
        base.Start();
        string activeScene = SceneManager.GetActiveScene().name;
        if (LostTreasureSceneHelper.IsTreasureSanctum(activeScene) || LostTreasureSceneHelper.IsCursedTemple(activeScene))
        {
            gameObject.SetActive(false);
            return;
        }

        if (this.sceneName == SceneName.MainMenu || this.sceneName == SceneName.BeginScreen) return;

        GameObject marker = new GameObject("StageExitMarker");
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = Vector3.up * 2.2f;
        TextMeshPro label = marker.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
        label.text = $"CỔNG SANG\n{this.GetDestinationLabel()}\nĐi vào cổng để tiếp tục";
        label.fontSize = 2.2f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.86f, 0.42f);
        label.sortingLayerID = SortingLayer.NameToID("Effects");
        label.sortingOrder = 30;
        marker.transform.localScale = Vector3.one * 0.55f;
    }

    private string GetDestinationLabel()
    {
        switch (this.sceneName)
        {
            case SceneName.DarkForestScene: return "Rừng Hắc Ám";
            case SceneName.AncientCaveScene: return "Hang Động Cổ Xưa";
            case SceneName.GuardianBattlefieldScene: return "Chiến Trường Cổ Đại";
            case SceneName.CursedTempleScene: return "Ngôi Đền Bị Nguyền Rủa";
            case SceneName.TreasureSanctumScene: return "Tháp Kho Báu Tối Thượng";
            default: return this.sceneName.ToString();
        }
    }
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
    protected float lastWarningTime = -10f;

    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (this.isTransitioning || other == null || other.GetComponentInParent<PlayerCtrl>() == null) return;

        int requiredFragment = LostTreasureManager.GetCurrentSceneFragmentIndex();
        if (requiredFragment >= 0 && LostTreasureManager.Instance != null && !LostTreasureManager.Instance.HasFragment(requiredFragment))
        {
            if (Time.time - this.lastWarningTime > 2.5f)
            {
                this.lastWarningTime = Time.time;
                SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();
                if (overlay != null)
                {
                    StartCoroutine(overlay.ShowMessage(
                        "CHƯA THU THẬP MẢNH CHÌA KHÓA!",
                        "Hãy nhặt [Mảnh Chìa Khóa Cổ Đại] phát sáng trên Bệ Đá Vàng ngay trước cổng rồi mới bước qua cổng!",
                        2.4f,
                        new Color(1f, 0.78f, 0.28f, 1f)));
                }
            }
            return;
        }

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
        {
            int maxHp = Mathf.Max(this.playerHpSO.maxHp, PlayerCtrl.Instance.PlayerDamReceive.HPMax);
            this.playerHpSO.maxHp = maxHp;
            this.playerHpSO.currentHp = maxHp;
        }
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
        SceneManager.LoadScene(LostTreasureSceneHelper.ResolveLoadableSceneName(this.sceneName));
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
