using UnityEngine;

/// <summary>
/// Drives the scene-authored meteor pool during the boss' second attack.
/// The warning collider is deliberately sampled as geometry only; it never
/// deals damage itself. Every damage event comes from a landed meteor.
/// </summary>
public class BossMeteorRain : MonoBehaviour
{
    private enum MeteorTargetPattern
    {
        DangerZoneRandom,
        PlayerCurrentPosition,
        PlayerStartRing
    }

    [Header("References")]
    [SerializeField] private BossAttack2 bossAttack2;
    [SerializeField] private PolygonCollider2D dangerZone;
    [SerializeField] private BossFallingMeteor[] meteorPool;

    [Header("Targeting")]
    [Tooltip("Danger Zone preserves LastWar. Player Start Ring locks the player's position at the start of the attack, then distributes meteors around a circle.")]
    [SerializeField] private MeteorTargetPattern targetPattern = MeteorTargetPattern.DangerZoneRandom;
    [SerializeField] private Transform playerTarget;

    [Header("Player Start Ring")]
    [SerializeField, Min(0f)] private float playerRingRadius = 2.75f;
    [SerializeField, Min(0f)] private float playerRingThickness = 0.35f;
    [SerializeField, Min(1)] private int playerRingPointCount = 16;

    [Header("Meteor Rain")]
    [SerializeField] private float spawnInterval = 0.18f;
    [SerializeField] private float edgePadding = 0.38f;
    [SerializeField] private int randomAttempts = 24;

    private bool isRaining;
    private float nextSpawnTime;
    private int nextMeteorIndex;
    private Vector2 playerPositionAtRainStart;
    private float ringStartAngle;
    private int nextRingPointIndex;

    private void Awake()
    {
        FindPlayerTarget();
    }

    public void StartRain(BossAttack2 attack2 = null)
    {
        if (attack2 != null) bossAttack2 = attack2;
        FindPlayerTarget();
        CaptureRainStartPosition();
        isRaining = true;
        nextSpawnTime = Time.time + 0.12f;
    }

    public void StopRain()
    {
        isRaining = false;
        if (meteorPool == null) return;

        foreach (BossFallingMeteor meteor in meteorPool)
        {
            if (meteor != null) meteor.CancelMeteor();
        }
    }

    private void OnDisable()
    {
        StopRain();
    }

    private void Update()
    {
        if (!Application.isPlaying || !isRaining || bossAttack2 == null || !bossAttack2.Attack2)
        {
            return;
        }

        if (Time.time < nextSpawnTime) return;

        SpawnMeteor();
        nextSpawnTime = Time.time + Mathf.Max(0.08f, spawnInterval);
    }

    private void SpawnMeteor()
    {
        BossFallingMeteor meteor = GetAvailableMeteor();
        if (meteor == null) return;

        meteor.Launch(GetMeteorTargetPosition());
    }

    private BossFallingMeteor GetAvailableMeteor()
    {
        if (meteorPool == null || meteorPool.Length == 0) return null;

        for (int i = 0; i < meteorPool.Length; i++)
        {
            int index = (nextMeteorIndex + i) % meteorPool.Length;
            BossFallingMeteor candidate = meteorPool[index];
            if (candidate == null || !candidate.IsAvailable) continue;

            nextMeteorIndex = (index + 1) % meteorPool.Length;
            return candidate;
        }

        return null;
    }

    private Vector2 GetMeteorTargetPosition()
    {
        switch (targetPattern)
        {
            case MeteorTargetPattern.PlayerCurrentPosition:
                FindPlayerTarget();
                return playerTarget != null ? playerTarget.position : transform.position;

            case MeteorTargetPattern.PlayerStartRing:
                return GetPointOnPlayerStartRing();
        }

        return GetPointInsideDangerZone();
    }

    private void CaptureRainStartPosition()
    {
        playerPositionAtRainStart = playerTarget != null ? playerTarget.position : transform.position;
        ringStartAngle = Random.Range(0f, 360f);
        nextRingPointIndex = 0;
    }

    private Vector2 GetPointOnPlayerStartRing()
    {
        int pointCount = Mathf.Max(1, playerRingPointCount);
        float angle = ringStartAngle + 360f * (nextRingPointIndex % pointCount) / pointCount;
        nextRingPointIndex++;

        float radius = Mathf.Max(0f, playerRingRadius + Random.Range(-playerRingThickness, playerRingThickness));
        Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        return playerPositionAtRainStart + direction * radius;
    }

    private Vector2 GetPointInsideDangerZone()
    {
        if (dangerZone == null) return transform.position;

        Bounds bounds = GetDangerZoneWorldBounds();
        float padding = Mathf.Max(0f, edgePadding);

        for (int i = 0; i < randomAttempts; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(bounds.min.x + padding, bounds.max.x - padding),
                Random.Range(bounds.min.y + padding, bounds.max.y - padding));

            if (ContainsWorldPoint(candidate)) return candidate;
        }

        return bounds.center;
    }

    private void FindPlayerTarget()
    {
        if (playerTarget != null) return;

        PlayerCtrl player = FindAnyObjectByType<PlayerCtrl>();
        if (player != null) playerTarget = player.transform;
    }

    private Bounds GetDangerZoneWorldBounds()
    {
        Vector2[] polygon = dangerZone.points;
        if (polygon == null || polygon.Length == 0)
        {
            return new Bounds(dangerZone.transform.position, Vector3.zero);
        }

        Vector3 firstPoint = dangerZone.transform.TransformPoint(polygon[0] + dangerZone.offset);
        Vector3 min = firstPoint;
        Vector3 max = firstPoint;

        foreach (Vector2 vertex in polygon)
        {
            Vector3 worldVertex = dangerZone.transform.TransformPoint(vertex + dangerZone.offset);
            min = Vector3.Min(min, worldVertex);
            max = Vector3.Max(max, worldVertex);
        }

        return new Bounds((min + max) * 0.5f, max - min);
    }

    // PolygonCollider2D.OverlapPoint does not work while its collider is
    // disabled, so test the polygon manually. The collider stays disabled to
    // guarantee the red warning area cannot damage the player by itself.
    private bool ContainsWorldPoint(Vector2 worldPoint)
    {
        Vector3 localPoint = dangerZone.transform.InverseTransformPoint(worldPoint);
        Vector2 point = new Vector2(localPoint.x, localPoint.y);
        Vector2[] polygon = dangerZone.points;
        bool inside = false;

        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i] + dangerZone.offset;
            Vector2 b = polygon[j] + dangerZone.offset;
            bool crosses = (a.y > point.y) != (b.y > point.y);
            if (crosses && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
