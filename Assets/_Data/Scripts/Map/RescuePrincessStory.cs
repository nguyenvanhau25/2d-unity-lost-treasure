using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Replaces the first rescue win popup with the story bridge into Special.
/// It is only active in RecusePrincess because that scene's Princess owns this
/// component; every other Princess keeps the old win behaviour.
/// </summary>
public class RescuePrincessStory : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string specialSceneName = "Special";

    [Header("Story Text")]
    [TextArea(2, 4)] [SerializeField] private string princessLine = "Cảm ơn ngươi... nhưng kho báu vẫn chưa thực sự yên bình.";
    [TextArea(2, 4)] [SerializeField] private string abductionLine = "Một luồng bóng tối cuốn công chúa vào cánh cổng bí ẩn!";
    [TextArea(2, 4)] [SerializeField] private string objectiveLine = "Mục tiêu mới: Đến vùng đất đặc biệt và giải cứu công chúa.";
    [SerializeField, Min(0.1f)] private float messageDuration = 2.3f;
    [SerializeField, Min(0.1f)] private float disappearDuration = 0.5f;

    private bool started;

    public bool BeginRescue()
    {
        if (this.started) return true;
        this.started = true;
        StartCoroutine(this.RunStory());
        return true;
    }

    private IEnumerator RunStory()
    {
        FreezePlayer();
        SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();

        yield return Show(overlay, "CÔNG CHÚA", this.princessLine, new Color(1f, 0.79f, 0.3f));
        yield return FadePrincessAway();
        yield return Show(overlay, "LỜI NGUYỀN THỨC TỈNH", this.abductionLine, new Color(1f, 0.34f, 0.34f));
        yield return Show(overlay, "MỤC TIÊU MỚI", this.objectiveLine, new Color(0.88f, 0.68f, 1f));

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

    private IEnumerator FadePrincessAway()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders) collider.enabled = false;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) originalColors[i] = renderers[i].color;

        for (float time = 0f; time < this.disappearDuration; time += Time.unscaledDeltaTime)
        {
            float alpha = 1f - time / this.disappearDuration;
            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = originalColors[i];
                color.a *= alpha;
                renderers[i].color = color;
            }
            yield return null;
        }

        foreach (SpriteRenderer spriteRenderer in renderers) spriteRenderer.enabled = false;
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
