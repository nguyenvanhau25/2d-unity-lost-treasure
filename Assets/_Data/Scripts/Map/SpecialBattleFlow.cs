using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-specific two-wave finale for Special. The serialized arrays deliberately
/// point to objects in the Special scene only, so other levels are untouched.
/// </summary>
public class SpecialBattleFlow : MonoBehaviour
{
    [Header("Wave 1 - Skeleton and BossEnemy")]
    [SerializeField] private Transform[] waveOneEnemies;
    [TextArea(2, 4)] [SerializeField] private string waveOneMessage = "Yêu quái đã xuất hiện!\nHãy tiêu diệt chúng ngay.";

    [Header("Wave 2 - BossBringer and Boss")]
    [SerializeField] private Transform[] waveTwoEnemies;
    [TextArea(2, 4)] [SerializeField] private string waveTwoMessage = "Đợt 2 bắt đầu.\nNhững kẻ canh giữ cuối cùng đã xuất hiện!";

    [Header("Castle Ending")]
    [SerializeField] private SpecialCastleGate castleGate;
    [TextArea(2, 4)] [SerializeField] private string castleObjective = "Tất cả yêu quái đã bị đánh bại.\nTiến đến cổng lâu đài.";
    [TextArea(2, 4)] [SerializeField] private string princessFinalLine = "Cuối cùng lời nguyền đã tan biến. Vương quốc này được tự do rồi.";
    [SerializeField] private string endingSceneName = "MainMenu";

    [Header("Timing")]
    [SerializeField, Min(0f)] private float startDelay = 0.5f;
    [SerializeField, Min(0.1f)] private float bannerDuration = 2.3f;
    [SerializeField, Min(0f)] private float phaseBreakDuration = 0.6f;
    [SerializeField, Min(0.1f)] private float endingDuration = 4f;

    private readonly List<DamageReceiver> trackedDamageReceivers = new List<DamageReceiver>();
    private readonly List<BossLevel1> trackedBossEnemies = new List<BossLevel1>();
    private bool castleReached;
    private bool battleFinished;

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

        yield return this.ShowBanner("ĐỢT 1", this.waveOneMessage, new Color(1f, 0.76f, 0.24f));
        yield return this.ActivateAndWaitForClear(this.waveOneEnemies);

        yield return new WaitForSeconds(this.phaseBreakDuration);
        yield return this.ShowBanner("ĐỢT 2", this.waveTwoMessage, new Color(1f, 0.35f, 0.38f));
        yield return this.ActivateAndWaitForClear(this.waveTwoEnemies);

        this.battleFinished = true;
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.StopMusic();
        this.castleGate?.OpenGate();
        yield return this.ShowBanner("CỔNG LÂU ĐÀI ĐÃ MỞ", this.castleObjective, new Color(1f, 0.82f, 0.32f));
    }

    public void ReachCastle()
    {
        if (!this.battleFinished || this.castleReached) return;
        this.castleReached = true;
        StartCoroutine(this.RunFinale());
    }

    private IEnumerator RunFinale()
    {
        FreezePlayer();
        yield return this.ShowBanner("CÔNG CHÚA", this.princessFinalLine, new Color(1f, 0.82f, 0.35f));
        yield return this.ShowBanner("FRAGMENTS OF TREASURE", "Bạn đã giải cứu công chúa và khôi phục vương quốc.", new Color(0.9f, 0.7f, 1f));
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

            foreach (DamageReceiver receiver in enemyRoot.GetComponentsInChildren<DamageReceiver>(true))
            {
                if (!receiver.gameObject.activeInHierarchy || this.trackedDamageReceivers.Contains(receiver)) continue;
                this.trackedDamageReceivers.Add(receiver);
            }

            foreach (BossLevel1 bossEnemy in enemyRoot.GetComponentsInChildren<BossLevel1>(true))
            {
                if (!bossEnemy.gameObject.activeInHierarchy || this.trackedBossEnemies.Contains(bossEnemy)) continue;
                this.trackedBossEnemies.Add(bossEnemy);
            }
        }

        if (this.trackedDamageReceivers.Count == 0 && this.trackedBossEnemies.Count == 0)
            Debug.LogError("SpecialBattleFlow could not find any active enemy health components for this wave.", this);
    }

    private bool IsTrackedWaveCleared()
    {
        if (this.trackedDamageReceivers.Count == 0 && this.trackedBossEnemies.Count == 0) return false;

        foreach (DamageReceiver receiver in this.trackedDamageReceivers)
        {
            if (receiver != null && receiver.gameObject.activeInHierarchy && !receiver.IsDead) return false;
        }

        foreach (BossLevel1 bossEnemy in this.trackedBossEnemies)
        {
            if (bossEnemy != null && bossEnemy.gameObject.activeInHierarchy && !bossEnemy.IsDead) return false;
        }

        return true;
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
