using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One reusable, scene-authored falling rock. Its rock and impact children
/// are placed in LastWar ahead of time so artists can adjust their sprites,
/// sorting and scale directly in the Inspector.
/// </summary>
public class BossFallingMeteor : MonoBehaviour
{
    private enum MeteorState { Idle, Falling, Impact }

    [Header("Scene Visuals")]
    [SerializeField] private SpriteRenderer rockRenderer;
    [SerializeField] private SpriteRenderer impactRenderer;
    [SerializeField] private CircleCollider2D rockCollider;

    [Header("Timing")]
    [SerializeField] private float fallDuration = 0.72f;
    [SerializeField] private float impactDuration = 0.34f;
    [SerializeField] private float fallHeight = 7.5f;

    [Header("Impact")]
    [SerializeField] private int damage = 2;
    [SerializeField] private float hitRadius = 0.62f;
    [SerializeField] private LayerMask hitMask = ~0;

    private MeteorState state;
    private float stateTimer;
    private float spinDirection;
    private Vector3 rockStartScale;
    private Vector3 impactStartScale;
    private readonly HashSet<PlayerDamReceive> damagedPlayers = new HashSet<PlayerDamReceive>();

    public bool IsAvailable => state == MeteorState.Idle;

    private void Awake()
    {
        rockStartScale = rockRenderer != null ? rockRenderer.transform.localScale : Vector3.one;
        impactStartScale = impactRenderer != null ? impactRenderer.transform.localScale : Vector3.one;
        HideAllVisuals();
    }

    public void Launch(Vector2 targetPosition)
    {
        if (rockRenderer == null)
        {
            Debug.LogWarning($"{name}: meteor scene references are missing.", this);
            return;
        }

        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        state = MeteorState.Falling;
        stateTimer = 0f;
        spinDirection = Random.value < 0.5f ? -1f : 1f;
        damagedPlayers.Clear();

        if (impactRenderer != null) impactRenderer.enabled = false;
        rockRenderer.enabled = true;
        if (rockCollider != null) rockCollider.enabled = true;
        rockRenderer.transform.localPosition = Vector3.up * fallHeight;
        rockRenderer.transform.localScale = rockStartScale * 0.72f;
        rockRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }

    public void CancelMeteor()
    {
        state = MeteorState.Idle;
        HideAllVisuals();
    }

    private void Update()
    {
        if (!Application.isPlaying || state == MeteorState.Idle) return;

        stateTimer += Time.deltaTime;

        switch (state)
        {
            case MeteorState.Falling:
                UpdateFall();
                break;
            case MeteorState.Impact:
                UpdateImpact();
                break;
        }
    }

    private void UpdateFall()
    {
        float progress = Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, fallDuration));
        float acceleratedProgress = 1f - (1f - progress) * (1f - progress);
        rockRenderer.transform.localPosition = Vector3.Lerp(Vector3.up * fallHeight, Vector3.zero, acceleratedProgress);
        rockRenderer.transform.localScale = rockStartScale * Mathf.Lerp(0.72f, 1.12f, acceleratedProgress);
        rockRenderer.transform.Rotate(0f, 0f, spinDirection * 540f * Time.deltaTime);
        DamagePlayersTouchedByRock();

        if (progress < 1f) return;

        state = MeteorState.Impact;
        stateTimer = 0f;
        rockRenderer.enabled = false;
        if (rockCollider != null) rockCollider.enabled = false;
        if (impactRenderer == null)
        {
            CancelMeteor();
            return;
        }
        impactRenderer.enabled = true;
        impactRenderer.color = new Color(1f, 0.72f, 0.2f, 1f);
        impactRenderer.transform.localScale = impactStartScale * 0.48f;
    }

    private void UpdateImpact()
    {
        float progress = Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, impactDuration));
        impactRenderer.transform.localScale = impactStartScale * Mathf.Lerp(0.48f, 1.55f, progress);
        impactRenderer.color = Color.Lerp(
            new Color(1f, 0.8f, 0.26f, 1f),
            new Color(0.55f, 0.08f, 0.01f, 0f),
            progress);

        if (progress < 1f) return;

        CancelMeteor();
    }

    private void DamagePlayersTouchedByRock()
    {
        Vector3 rockPosition = rockRenderer.transform.position;
        Vector2 impactPosition = new Vector2(rockPosition.x, rockPosition.y);
        Collider2D[] hits = Physics2D.OverlapCircleAll(impactPosition, hitRadius, hitMask);

        foreach (Collider2D hit in hits)
        {
            PlayerDamReceive player = hit.GetComponent<PlayerDamReceive>();
            if (player == null) player = hit.GetComponentInParent<PlayerDamReceive>();
            if (player == null || !damagedPlayers.Add(player)) continue;

            player.ReceiveMeteorHit(damage);
        }
    }

    private void HideAllVisuals()
    {
        if (rockRenderer != null) rockRenderer.enabled = false;
        if (impactRenderer != null) impactRenderer.enabled = false;
        if (rockCollider != null) rockCollider.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.22f, 0.04f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
