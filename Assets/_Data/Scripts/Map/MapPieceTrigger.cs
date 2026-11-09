using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Collects the Key/Map fragment when the player reaches it.
/// Automatically detects scene mapping and awards the corresponding Key Fragment
/// to the persistent LostTreasureManager.
/// </summary>
public sealed class MapPieceTrigger : MonoBehaviour
{
    [Header("Fragment Mapping")]
    [Tooltip("-1 for auto-detect based on Scene, or specify 0..3")]
    [SerializeField] private int fragmentIndexOverride = -1;
    [SerializeField, Min(0f)] private float collectDelay = 0.15f;

    private bool collected;

    private void Start()
    {
        int targetIndex = this.GetResolvedIndex();
        if (targetIndex >= 0 && LostTreasureManager.Instance.HasFragment(targetIndex))
        {
            gameObject.SetActive(false);
        }
    }

    private int GetResolvedIndex()
    {
        if (this.fragmentIndexOverride >= 0) return this.fragmentIndexOverride;
        return LostTreasureManager.GetCurrentSceneFragmentIndex();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (this.collected || other == null || other.GetComponentInParent<PlayerCtrl>() == null) return;

        this.collected = true;
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("Pickup");

        int targetIndex = this.GetResolvedIndex();
        if (targetIndex >= 0)
        {
            LostTreasureManager.Instance.CollectFragment(targetIndex);
        }

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
