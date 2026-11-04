using System.Collections;
using UnityEngine;

/// <summary>
/// Lightweight feedback hook for the LastWar treasure-map UI. It restores the
/// scene component that was missing from the project and gives the map a short
/// pulse whenever a fragment is collected.
/// </summary>
public sealed class TreasureMapUI : MonoBehaviour
{
    public static TreasureMapUI Instance { get; private set; }

    private Vector3 originalScale;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        Instance = this;
        this.originalScale = transform.localScale;
    }

    public void Pulse()
    {
        if (!gameObject.activeInHierarchy) return;
        if (this.pulseRoutine != null) StopCoroutine(this.pulseRoutine);
        this.pulseRoutine = StartCoroutine(this.PlayPulse());
    }

    private IEnumerator PlayPulse()
    {
        const float duration = 0.26f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float pulse = Mathf.Sin(elapsed / duration * Mathf.PI);
            transform.localScale = this.originalScale * (1f + pulse * 0.12f);
            yield return null;
        }

        transform.localScale = this.originalScale;
        this.pulseRoutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
