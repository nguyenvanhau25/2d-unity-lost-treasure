using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Story bridge connecting the Ancient Shrine to the final Secret Sanctum (Special).
/// The Ancient Lore Guardian warns that the Mystery Treasure Chest was pulled into the Special realm.
/// </summary>
public class RescuePrincessStory : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string specialSceneName = "Special";

    [Header("Story Text")]
    [TextArea(2, 4)] [SerializeField] private string guardianLine = "Ngươi đã vượt qua những cạm bẫy cổ xưa và tìm ra Đền Thờ Bị Nguyền Rủa. Guardian đã nhận ra bản lĩnh của ngươi.";
    [TextArea(2, 4)] [SerializeField] private string abductionLine = "Phong ấn đền thờ đang tan vỡ. Cánh cửa đến Cấm Địa cuối cùng đã mở.";
    [TextArea(2, 4)] [SerializeField] private string objectiveLine = "Tiến vào Cấm Địa, đánh bại các hộ vệ cuối cùng và dùng Chìa Khóa Hoàn Chỉnh để mở Rương Kho Báu Thất Lạc.";
    [SerializeField, Min(0.1f)] private float messageDuration = 2.8f;

    private bool started;
    private bool waitingForTempleCompletion;
    private bool playerNearby;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != "RecusePrincess") return;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;
        foreach (Animator animator in GetComponentsInChildren<Animator>(true)) animator.enabled = false;
    }

    public bool BeginTempleTransition()
    {
        if (this.started) return true;
        CursedTempleChallenge templeChallenge = FindAnyObjectByType<CursedTempleChallenge>();
        if (templeChallenge == null || !templeChallenge.IsComplete)
        {
            if (!this.waitingForTempleCompletion)
                StartCoroutine(this.ShowTempleHint("Kích hoạt ba trụ rune để mở phong ấn và đối mặt với Guardian."));
            this.waitingForTempleCompletion = true;
            return false;
        }

        if (!LostTreasureManager.Instance.HasFragment(3))
        {
            if (!this.waitingForTempleCompletion)
                StartCoroutine(this.ShowTempleHint("Guardian đã gục ngã. Hãy nhặt Mảnh Chìa Khóa Đền Cổ."));
            this.waitingForTempleCompletion = true;
            return false;
        }

        this.waitingForTempleCompletion = false;
        this.started = true;
        StartCoroutine(this.RunStory());
        return true;
    }

    private void Update()
    {
        if (!this.playerNearby || !this.waitingForTempleCompletion) return;
        this.BeginTempleTransition();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || other.GetComponentInParent<PlayerCtrl>() == null) return;
        this.playerNearby = true;
        this.BeginTempleTransition();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null || other.GetComponentInParent<PlayerCtrl>() == null) return;
        this.playerNearby = false;
    }

    private IEnumerator ShowTempleHint(string message)
    {
        SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();
        if (overlay != null)
            yield return overlay.ShowMessage("THỬ THÁCH ĐỀN CỔ", message, 2.2f, new Color(0.35f, 0.85f, 1f));
    }

    private IEnumerator RunStory()
    {
        FreezePlayer();
        SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();

        yield return Show(overlay, "GUARDIAN ĐỀN CỔ", this.guardianLine, new Color(1f, 0.85f, 0.3f));
        yield return Show(overlay, "PHONG ẤN ĐỀN THỜ", this.abductionLine, new Color(1f, 0.35f, 0.35f));
        yield return Show(overlay, "MỤC TIÊU CUỐI CÙNG", this.objectiveLine, new Color(0.9f, 0.75f, 1f));

        SceneManager.LoadScene(this.specialSceneName);
    }

    private IEnumerator Show(SceneStoryOverlay overlay, string title, string message, Color accent)
    {
        if (overlay == null)
        {
            yield return new WaitForSeconds(this.messageDuration);
            yield break;
        }

        yield return overlay.ShowMessage(title, message, this.messageDuration, accent);
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
