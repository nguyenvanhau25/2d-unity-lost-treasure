using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps the castle gate solid until both Special waves are cleared. Once open,
/// the same collider becomes a trigger so the player can enter the castle.
/// </summary>
public class SpecialCastleGate : MonoBehaviour
{
    [SerializeField] private SpecialBattleFlow battleFlow;
    [SerializeField] private BoxCollider2D gateCollider;
    [SerializeField] private SpriteRenderer castleRenderer;
    [SerializeField, Min(0.1f)] private float castlePulseDuration = 1.1f;

    private bool unlocked;
    private bool castleReached;

    public bool IsUnlocked => this.unlocked;

    private void Awake()
    {
        if (this.gateCollider == null) this.gateCollider = GetComponent<BoxCollider2D>();
        this.LockGate();
    }

    public void LockGate()
    {
        this.unlocked = false;
        this.castleReached = false;
        if (this.gateCollider != null)
        {
            this.gateCollider.enabled = true;
            this.gateCollider.isTrigger = false;
        }
    }

    public void OpenGate()
    {
        if (this.unlocked) return;
        this.unlocked = true;
        if (this.gateCollider != null) this.gateCollider.isTrigger = true;
        StartCoroutine(this.PulseCastle());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!this.unlocked || this.castleReached || other == null) return;
        if (other.GetComponentInParent<PlayerCtrl>() == null) return;

        this.castleReached = true;
        this.battleFlow?.ReachCastle();
    }

    private IEnumerator PulseCastle()
    {
        if (this.castleRenderer == null) yield break;

        Color originalColor = this.castleRenderer.color;
        Transform castleTransform = this.castleRenderer.transform;
        Vector3 originalScale = castleTransform.localScale;
        Color glowColor = new Color(1f, 0.84f, 0.4f, originalColor.a);

        for (float time = 0f; time < this.castlePulseDuration; time += Time.deltaTime)
        {
            float pulse = Mathf.PingPong(time * 2.8f, 1f);
            this.castleRenderer.color = Color.Lerp(originalColor, glowColor, pulse);
            castleTransform.localScale = originalScale * (1f + pulse * 0.025f);
            yield return null;
        }

        this.castleRenderer.color = originalColor;
        castleTransform.localScale = originalScale;
    }
}
