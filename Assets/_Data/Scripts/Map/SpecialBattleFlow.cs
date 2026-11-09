using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-specific two-wave finale for Special.
/// In "Lost Treasure", clearing both waves unlocks the ancient castle gate,
/// giving access to the Mystery Treasure Chest!
/// </summary>
public class SpecialBattleFlow : MonoBehaviour
{
    [Header("Wave 1 - Skeleton and BossEnemy")]
    [SerializeField] private Transform[] waveOneEnemies;
    [TextArea(2, 4)] [SerializeField] private string waveOneMessage = "Hộ vệ bóng tối cổ xưa đã thức tỉnh!\nHãy tiêu diệt chúng để giải phóng phong ấn!";

    [Header("Wave 2 - BossBringer and Boss")]
    [SerializeField] private Transform[] waveTwoEnemies;
    [TextArea(2, 4)] [SerializeField] private string waveTwoMessage = "Đợt 2 bắt đầu!\nNhững kẻ canh giữ tối thượng của kho báu đã xuất hiện!";

    [Header("Castle Ending")]
    [SerializeField] private SpecialCastleGate castleGate;
    [TextArea(2, 4)] [SerializeField] private string castleObjective = "Tất cả hộ vệ bóng tối đã bị đánh bại!\nTiến đến mở Rương Kho Báu Bí Ẩn!";
    [TextArea(2, 4)] [SerializeField] private string princessFinalLine = "Lời nguyền ngàn năm đã tan biến! Kho báu thất lạc đã tìm được chủ nhân xứng đáng.";
    [SerializeField] private string endingSceneName = "MainMenu";

    [Header("Timing")]
    [SerializeField, Min(0f)] private float startDelay = 0.5f;
    [SerializeField, Min(0.1f)] private float bannerDuration = 2.6f;
    [SerializeField, Min(0f)] private float phaseBreakDuration = 0.6f;
    [SerializeField, Min(0.1f)] private float endingDuration = 4.5f;

    private readonly List<DamageReceiver> trackedDamageReceivers = new List<DamageReceiver>();
    private readonly List<BossLevel1> trackedBossEnemies = new List<BossLevel1>();
    private bool castleReached;
    private bool battleFinished;

    public bool IsBattleFinished => this.battleFinished;

    private void Start()
    {
        this.SetWaveActive(this.waveOneEnemies, false);
        this.SetWaveActive(this.waveTwoEnemies, false);
        this.castleGate?.LockGate();
        StartCoroutine(this.RunBattle());
    }

    private IEnumerator RunBattle()
    {
        yield return new WaitForSeconds(this.startDelay);

        yield return this.ShowBanner("ĐỢT 1 - HỘ VỆ PHONG ẤN", this.waveOneMessage, new Color(1f, 0.76f, 0.24f));
        yield return this.ActivateAndWaitForClear(this.waveOneEnemies);

        yield return new WaitForSeconds(this.phaseBreakDuration);
        yield return this.ShowBanner("ĐỢT 2 - ĐẠI HỘ VỆ TỐI THƯỢNG", this.waveTwoMessage, new Color(1f, 0.35f, 0.38f));
        yield return this.ActivateAndWaitForClear(this.waveTwoEnemies);

        this.battleFinished = true;
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.StopMusic();
        this.castleGate?.OpenGate();
        yield return this.ShowBanner("CỔNG ĐỀN KHO BÁU ĐÃ MỞ", this.castleObjective, new Color(1f, 0.85f, 0.32f));
    }

    public bool ReachCastle()
    {
        if (!this.battleFinished || this.castleReached) return false;
        if (LostTreasureManager.Instance == null || !LostTreasureManager.Instance.IsMasterKeyCrafted)
        {
            StartCoroutine(this.ShowBanner("CHÌA KHÓA CHƯA HOÀN CHỈNH", "Hãy thu thập đủ bốn mảnh chìa khóa trước khi mở Rương Kho Báu.", new Color(1f, 0.45f, 0.35f)));
            return false;
        }

        this.castleReached = true;

        // Check if MysteryTreasureChest exists in scene
        MysteryTreasureChest chest = FindFirstObjectByType<MysteryTreasureChest>();
        if (chest != null)
        {
            chest.OpenChest();
        }
        else
        {
            StartCoroutine(this.RunFinale());
        }

        return true;
    }

    private IEnumerator RunFinale()
    {
        FreezePlayer();
        yield return this.ShowBanner("HỘ THẦN KHO BÁU", this.princessFinalLine, new Color(1f, 0.82f, 0.35f));
        yield return this.ShowBanner("LOST TREASURE - CHIẾN THẮNG HUYỀN THOẠI", "Bạn đã mở Rương Kho Báu Bí Ẩn và giải cứu vương quốc!", new Color(0.95f, 0.85f, 0.4f));
        yield return new WaitForSeconds(this.endingDuration);
        SceneManager.LoadScene(this.endingSceneName);
    }

    private IEnumerator ActivateAndWaitForClear(Transform[] wave)
    {
        this.SetWaveActive(wave, true);
        yield return null;
        this.TrackWaveEnemies(wave);

        while (!this.IsTrackedWaveCleared())
            yield return null;
    }

    private void SetWaveActive(Transform[] wave, bool isActive)
    {
        if (wave == null) return;
        foreach (Transform enemyRoot in wave)
            if (enemyRoot != null) enemyRoot.gameObject.SetActive(isActive);
    }

    private void TrackWaveEnemies(Transform[] wave)
    {
        this.trackedDamageReceivers.Clear();
        this.trackedBossEnemies.Clear();

        if (wave == null) return;
        foreach (Transform enemyRoot in wave)
        {
            if (enemyRoot == null) continue;

            DamageReceiver[] receivers = enemyRoot.GetComponentsInChildren<DamageReceiver>(true);
            foreach (DamageReceiver receiver in receivers)
                if (receiver != null && !this.trackedDamageReceivers.Contains(receiver))
                    this.trackedDamageReceivers.Add(receiver);

            BossLevel1[] bossEnemies = enemyRoot.GetComponentsInChildren<BossLevel1>(true);
            foreach (BossLevel1 boss in bossEnemies)
                if (boss != null && !this.trackedBossEnemies.Contains(boss))
                    this.trackedBossEnemies.Add(boss);
        }
    }

    private bool IsTrackedWaveCleared()
    {
        for (int i = this.trackedDamageReceivers.Count - 1; i >= 0; i--)
        {
            DamageReceiver receiver = this.trackedDamageReceivers[i];
            if (receiver == null)
            {
                this.trackedDamageReceivers.RemoveAt(i);
                continue;
            }

            if (!receiver.gameObject.activeInHierarchy || receiver.IsDead)
                this.trackedDamageReceivers.RemoveAt(i);
        }

        for (int i = this.trackedBossEnemies.Count - 1; i >= 0; i--)
        {
            BossLevel1 boss = this.trackedBossEnemies[i];
            if (boss == null)
            {
                this.trackedBossEnemies.RemoveAt(i);
                continue;
            }

            if (!boss.gameObject.activeInHierarchy || boss.IsDead)
                this.trackedBossEnemies.RemoveAt(i);
        }

        return this.trackedDamageReceivers.Count == 0 && this.trackedBossEnemies.Count == 0;
    }

    private IEnumerator ShowBanner(string title, string message, Color accent)
    {
        SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();
        if (overlay == null)
        {
            yield return new WaitForSeconds(this.bannerDuration);
            yield break;
        }

        yield return overlay.ShowMessage(title, message, this.bannerDuration, accent);
    }

    private static void FreezePlayer()
    {
        PlayerCtrl player = PlayerCtrl.Instance;
        if (player == null || player.PlayerMovement == null || player.PlayerMovement._Rb == null) return;

        player.PlayerMovement._Rb.linearVelocity = Vector2.zero;
        player.PlayerMovement._Rb.constraints = RigidbodyConstraints2D.FreezeAll;
        player.PlayerMovement.SetHorizontal(0f);
        player.PlayerMovement.SetVertical(0f);
    }
}
