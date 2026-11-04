using System.Collections;
using UnityEngine;

/// <summary>
/// Collects the LastWar map fragment when the player reaches it. The component
/// deliberately has no required Inspector fields so it restores old scenes
/// that already contain this GUID without extra manual setup.
/// </summary>
public sealed class MapPieceTrigger : MonoBehaviour
{
    [SerializeField, Min(0f)] private float collectDelay = 0.15f;
    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (this.collected || other == null || other.GetComponentInParent<PlayerCtrl>() == null) return;

        this.collected = true;
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("Pickup");
        StartCoroutine(this.Collect());
    }

    private IEnumerator Collect()
    {
        Vector3 initialScale = transform.localScale;
        for (float elapsed = 0f; elapsed < this.collectDelay; elapsed += Time.deltaTime)
        {
            float progress = this.collectDelay <= 0f ? 1f : elapsed / this.collectDelay;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);
            yield return null;
        }

        TreasureMapUI.Instance?.Pulse();
        gameObject.SetActive(false);
    }
}
