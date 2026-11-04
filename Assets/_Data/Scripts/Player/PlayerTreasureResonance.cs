using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ultimate unlocked in LastWar and Special. Q releases three treasure pulses;
/// each pulse damages, slows and marks enemies. Three marks trigger an explosion.
/// </summary>
public class PlayerTreasureResonance : MonoBehaviour
{
    [Header("Treasure Resonance")]
    [SerializeField] protected float radius = 4.2f;
    [SerializeField] protected float cooldown = 15f;
    [SerializeField] protected float pulseInterval = 0.42f;
    [SerializeField] protected int pulseDamage = 2;
    [SerializeField] protected int detonationDamage = 5;
    [SerializeField] protected float slowMultiplier = 0.45f;
    [SerializeField] protected float slowDuration = 2.6f;

    [Header("Feedback")]
    [SerializeField] protected float resonanceSfxVolume = 1.35f;

    protected PlayerCtrl playerCtrl;
    protected float nextCastTime;
    protected bool unlocked;
    protected Image cooldownFill;
    protected Image skillIcon;
    protected TextMeshProUGUI cooldownText;
    protected TextMeshProUGUI keyText;
    protected Sprite uiSprite;
    protected TMP_FontAsset hudFont;

    public bool Unlocked => unlocked;
    public bool IsReady => unlocked && Time.time >= nextCastTime;
    public float Cooldown => cooldown;
    public float CooldownRemaining => Mathf.Max(0f, nextCastTime - Time.time);

    protected void Awake()
    {
        this.playerCtrl = GetComponent<PlayerCtrl>();
        string sceneName = SceneManager.GetActiveScene().name;
        this.unlocked = sceneName == SceneName.LastWar.ToString() || sceneName == "Special";
        if (!this.unlocked) enabled = false;
    }

    protected void Start()
    {
        if (FindFirstObjectByType<TreasureResonanceHud>() != null) return;
        this.CreateHud();
        StartCoroutine(this.ShowUnlockBanner());
    }

    protected void Update()
    {
        this.UpdateHud();
        if (InputManager.Instance == null || !InputManager.Instance.ConsumeTreasureResonance()) return;
        if (!this.CanCast()) return;
        StartCoroutine(this.CastTreasureResonance());
    }

    protected bool CanCast()
    {
        if (!this.IsReady || this.playerCtrl == null) return false;
        if (this.playerCtrl.PlayerDamReceive.IsDead || this.playerCtrl.PlayerDamReceive.IsHurt) return false;
        if (this.playerCtrl.PlayerDash.IsDashing) return false;
        return !this.playerCtrl.PlayerAttack.Attack && !this.playerCtrl.PlayerShooting.Shoot;
    }

    protected IEnumerator CastTreasureResonance()
    {
        this.nextCastTime = Time.time + this.cooldown;
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("ResonanceSkill", this.resonanceSfxVolume);

        for (int pulse = 0; pulse < 3; pulse++)
        {
            this.ReleasePulse(pulse);
            if (pulse < 2) yield return new WaitForSeconds(this.pulseInterval);
        }
    }

    protected void ReleasePulse(int pulseIndex)
    {
        Vector3 origin = transform.position;
        TreasureResonanceVfx.SpawnPulse(origin, this.radius, pulseIndex);

        // Prefer overlap queries for ordinary enemies, then also inspect active
        // receivers directly. Some copied bosses use colliders on a different
        // child/layer, so relying on Physics2D alone would show the pulse but
        // never apply damage to them.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, this.radius);
        HashSet<DamageReceiver> hitEnemies = new HashSet<DamageReceiver>();
        foreach (Collider2D hit in colliders)
        {
            DamageReceiver receiver = hit.GetComponentInParent<DamageReceiver>();
            this.HitReceiver(receiver, hitEnemies, pulseIndex);
        }

        foreach (DamageReceiver receiver in FindObjectsByType<DamageReceiver>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (Vector2.Distance(origin, receiver.transform.position) > this.radius) continue;
            this.HitReceiver(receiver, hitEnemies, pulseIndex);
        }

        HashSet<BossLevel1> hitLegacyBosses = new HashSet<BossLevel1>();
        foreach (BossLevel1 boss in FindObjectsByType<BossLevel1>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (boss.IsDead || Vector2.Distance(origin, boss.transform.position) > this.radius || !hitLegacyBosses.Add(boss)) continue;
            boss.ReceiveResonanceHit(this.pulseDamage);
        }
    }

    private void HitReceiver(DamageReceiver receiver, HashSet<DamageReceiver> hitEnemies, int pulseIndex)
    {
        if (receiver == null || receiver is PlayerDamReceive || receiver.IsDead || !hitEnemies.Add(receiver)) return;

        receiver.Deduct(this.pulseDamage);
        EnemyDamReceive enemy = receiver as EnemyDamReceive;
        if (enemy == null) return;

        EnemyCtrl enemyCtrl = enemy.GetComponentInParent<EnemyCtrl>();
        if (enemyCtrl == null) return;

        BossResonanceImpact bossImpact = enemyCtrl.GetComponent<BossResonanceImpact>();
        if (bossImpact != null) bossImpact.PlayPulseImpact(pulseIndex);

        TreasureResonanceMark mark = enemyCtrl.GetComponent<TreasureResonanceMark>();
        if (mark == null) mark = enemyCtrl.gameObject.AddComponent<TreasureResonanceMark>();
        mark.AddMark(enemy, this.detonationDamage, this.slowMultiplier, this.slowDuration);
    }

    protected void CreateHud()
    {
        Canvas canvas = this.FindHudCanvas();
        if (canvas == null) return;

        Image sampleImage = FindFirstObjectByType<Image>();
        this.uiSprite = sampleImage != null ? sampleImage.sprite : null;
        TextMeshProUGUI sampleText = FindFirstObjectByType<TextMeshProUGUI>();
        this.hudFont = sampleText != null ? sampleText.font : TMP_Settings.defaultFontAsset;

        GameObject root = new GameObject("TreasureResonanceHUD", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.anchoredPosition = new Vector2(-36f, 42f);
        rootRect.sizeDelta = new Vector2(206f, 76f);

        Image background = this.CreateImage(root.transform, "Backdrop", new Color(0.055f, 0.025f, 0.12f, 0.88f));
        this.Stretch(background.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));

        this.skillIcon = this.CreateImage(root.transform, "ResonanceIcon", new Color(1f, 0.72f, 0.16f, 1f));
        this.Stretch(this.skillIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -25f), new Vector2(64f, 25f));

        this.cooldownFill = this.CreateImage(root.transform, "Cooldown", new Color(0.27f, 0.06f, 0.48f, 0.78f));
        this.cooldownFill.type = Image.Type.Filled;
        this.cooldownFill.fillMethod = Image.FillMethod.Radial360;
        this.cooldownFill.fillOrigin = 2;
        this.Stretch(this.cooldownFill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -25f), new Vector2(64f, 25f));

        this.CreateText(root.transform, "SkillName", "TREASURE\nRESONANCE", 16, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(73f, 4f), new Vector2(-13f, -5f), new Color(1f, 0.82f, 0.31f));
        this.keyText = this.CreateText(root.transform, "Key", "[ Q ] READY", 15, TextAlignmentOptions.Left,
            new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(73f, 7f), new Vector2(-13f, -4f), new Color(0.86f, 0.67f, 1f));
        this.cooldownText = this.CreateText(root.transform, "CooldownText", "", 18, TextAlignmentOptions.Center,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -15f), new Vector2(64f, 15f), Color.white);
    }

    protected Canvas FindHudCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return canvas;
        return canvases.Length > 0 ? canvases[0] : null;
    }

    protected Image CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = this.uiSprite;
        image.color = color;
        return image;
    }

    protected TextMeshProUGUI CreateText(Transform parent, string objectName, string value, float size,
        TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = this.hudFont;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        this.Stretch(text.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
        return text;
    }

    protected void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    protected void UpdateHud()
    {
        if (this.cooldownFill == null) return;
        float remaining = Mathf.Max(0f, this.nextCastTime - Time.time);
        float progress = remaining / this.cooldown;
        this.cooldownFill.fillAmount = progress;
        this.skillIcon.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 2.7f) * 0.035f);
        if (remaining <= 0f)
        {
            this.keyText.text = "[ Q ] READY";
            this.keyText.color = new Color(0.86f, 0.67f, 1f);
            this.cooldownText.text = "";
            return;
        }
        this.keyText.text = "[ Q ] RECHARGING";
        this.keyText.color = new Color(0.72f, 0.63f, 0.8f);
        this.cooldownText.text = Mathf.CeilToInt(remaining).ToString();
    }

    protected IEnumerator ShowUnlockBanner()
    {
        Canvas canvas = this.FindHudCanvas();
        if (canvas == null || this.hudFont == null) yield break;

        GameObject banner = new GameObject("TreasureResonanceUnlocked", typeof(RectTransform), typeof(CanvasGroup));
        banner.transform.SetParent(canvas.transform, false);
        RectTransform rect = banner.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -58f);
        rect.sizeDelta = new Vector2(460f, 68f);
        Image background = this.CreateImage(banner.transform, "BannerGlow", new Color(0.16f, 0.035f, 0.28f, 0.92f));
        this.Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        this.CreateText(banner.transform, "BannerText", "SKILL UNLOCKED  •  TREASURE RESONANCE  [Q]", 19,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f), new Color(1f, 0.8f, 0.28f));

        CanvasGroup group = banner.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        for (float time = 0f; time < 0.35f; time += Time.deltaTime)
        {
            group.alpha = time / 0.35f;
            yield return null;
        }
        group.alpha = 1f;
        yield return new WaitForSeconds(2.4f);
        for (float time = 0f; time < 0.55f; time += Time.deltaTime)
        {
            group.alpha = 1f - time / 0.55f;
            yield return null;
        }
        Destroy(banner);
    }

    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.91f, 0.57f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, this.radius);
    }
}

public static class TreasureResonanceVfx
{
    private static Sprite glowSprite;

    public static Sprite GlowSprite
    {
        get
        {
            if (glowSprite != null) return glowSprite;
            const int size = 48;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float alpha = Mathf.Clamp01(1f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            glowSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size);
            glowSprite.hideFlags = HideFlags.HideAndDontSave;
            return glowSprite;
        }
    }

    public static void SpawnPulse(Vector3 position, float radius, int pulseIndex)
    {
        GameObject effect = new GameObject("Treasure Resonance Pulse");
        effect.transform.position = position;
        TreasureResonancePulse pulse = effect.AddComponent<TreasureResonancePulse>();
        pulse.Initialize(radius, pulseIndex);
    }

    public static void SpawnDetonation(Vector3 position)
    {
        GameObject effect = new GameObject("Treasure Mark Detonation");
        effect.transform.position = position;
        effect.AddComponent<TreasureResonanceDetonation>();
    }

    public static void SpawnBossImpact(Vector3 position, bool detonation, int pulseIndex)
    {
        GameObject effect = new GameObject(detonation ? "Boss Resonance Detonation" : "Boss Resonance Impact");
        effect.transform.position = position;
        TreasureResonanceBossImpact impact = effect.AddComponent<TreasureResonanceBossImpact>();
        impact.Initialize(detonation, pulseIndex);
    }
}

public class TreasureResonancePulse : MonoBehaviour
{
    protected const int ringSegments = 48;
    protected LineRenderer ring;
    protected SpriteRenderer[] shards;
    protected Material material;
    protected float radius;
    protected const float lifetime = 0.6f;
    protected float elapsed;
    protected Color color;

    public void Initialize(float pulseRadius, int pulseIndex)
    {
        this.radius = pulseRadius;
        this.color = pulseIndex % 2 == 0 ? new Color(1f, 0.67f, 0.16f) : new Color(0.72f, 0.27f, 1f);
        this.CreateRing();
        this.CreateShards();
    }

    protected void CreateRing()
    {
        this.ring = gameObject.AddComponent<LineRenderer>();
        this.ring.useWorldSpace = false;
        this.ring.loop = true;
        this.ring.positionCount = ringSegments;
        this.ring.widthMultiplier = 0.065f;
        this.ring.numCapVertices = 4;
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        this.material = new Material(shader);
        this.ring.material = this.material;
        this.ring.sortingOrder = 60;
    }

    protected void CreateShards()
    {
        this.shards = new SpriteRenderer[10];
        for (int i = 0; i < this.shards.Length; i++)
        {
            GameObject shard = new GameObject("CrystalShard");
            shard.transform.SetParent(transform, false);
            shard.transform.localScale = new Vector3(0.12f, 0.28f, 1f);
            SpriteRenderer renderer = shard.AddComponent<SpriteRenderer>();
            renderer.sprite = TreasureResonanceVfx.GlowSprite;
            renderer.color = this.color;
            renderer.sortingOrder = 61;
            this.shards[i] = renderer;
        }
    }

    protected void Update()
    {
        this.elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(this.elapsed / lifetime);
        float currentRadius = Mathf.Lerp(0.25f, this.radius, 1f - Mathf.Pow(1f - progress, 3f));
        this.ring.widthMultiplier = Mathf.Lerp(0.12f, 0.012f, progress);
        Color faded = this.color;
        faded.a = (1f - progress) * 0.9f;
        this.ring.startColor = faded;
        this.ring.endColor = faded;

        for (int i = 0; i < ringSegments; i++)
        {
            float angle = Mathf.PI * 2f * i / ringSegments;
            this.ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * currentRadius, Mathf.Sin(angle) * currentRadius));
        }
        for (int i = 0; i < this.shards.Length; i++)
        {
            float angle = Mathf.PI * 2f * i / this.shards.Length + this.elapsed * 2.2f;
            Transform shard = this.shards[i].transform;
            shard.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * currentRadius * 0.68f;
            shard.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg + 90f + this.elapsed * 250f);
            Color shardColor = this.color;
            shardColor.a = faded.a;
            this.shards[i].color = shardColor;
        }
        if (progress >= 1f) Destroy(gameObject);
    }

    protected void OnDestroy()
    {
        if (this.material != null) Destroy(this.material);
    }
}

public class TreasureResonanceDetonation : MonoBehaviour
{
    protected SpriteRenderer core;
    protected SpriteRenderer[] sparks;
    protected float elapsed;
    protected const float lifetime = 0.48f;

    protected void Awake()
    {
        GameObject coreObject = new GameObject("TreasureBurstCore");
        coreObject.transform.SetParent(transform, false);
        this.core = coreObject.AddComponent<SpriteRenderer>();
        this.core.sprite = TreasureResonanceVfx.GlowSprite;
        this.core.color = new Color(1f, 0.72f, 0.18f, 1f);
        this.core.sortingOrder = 75;

        this.sparks = new SpriteRenderer[12];
        for (int i = 0; i < this.sparks.Length; i++)
        {
            GameObject spark = new GameObject("TreasureSpark");
            spark.transform.SetParent(transform, false);
            spark.transform.localScale = new Vector3(0.08f, 0.22f, 1f);
            this.sparks[i] = spark.AddComponent<SpriteRenderer>();
            this.sparks[i].sprite = TreasureResonanceVfx.GlowSprite;
            this.sparks[i].color = i % 2 == 0 ? new Color(1f, 0.75f, 0.19f, 1f) : new Color(0.78f, 0.32f, 1f, 1f);
            this.sparks[i].sortingOrder = 76;
        }
    }

    protected void Update()
    {
        this.elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(this.elapsed / lifetime);
        float scale = Mathf.Lerp(0.3f, 1.9f, Mathf.Sin(progress * Mathf.PI * 0.5f));
        this.core.transform.localScale = Vector3.one * scale;
        Color coreColor = this.core.color;
        coreColor.a = 1f - progress;
        this.core.color = coreColor;

        for (int i = 0; i < this.sparks.Length; i++)
        {
            float angle = Mathf.PI * 2f * i / this.sparks.Length + progress * 1.2f;
            Transform spark = this.sparks[i].transform;
            spark.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Lerp(0.15f, 1.25f, progress);
            spark.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
            Color sparkColor = this.sparks[i].color;
            sparkColor.a = 1f - progress;
            this.sparks[i].color = sparkColor;
        }
        if (progress >= 1f) Destroy(gameObject);
    }
}

/// <summary>Layered gold-and-violet burst reserved for Treasure Resonance hits on the final boss.</summary>
public class TreasureResonanceBossImpact : MonoBehaviour
{
    private const int segments = 36;
    private LineRenderer outerRing;
    private LineRenderer innerRing;
    private SpriteRenderer core;
    private SpriteRenderer[] shards;
    private Material material;
    private float elapsed;
    private float lifetime;
    private bool detonation;
    private Color primary;

    public void Initialize(bool isDetonation, int pulseIndex)
    {
        detonation = isDetonation;
        lifetime = detonation ? 0.72f : 0.38f;
        primary = pulseIndex % 2 == 0 ? new Color(1f, 0.64f, 0.1f, 1f) : new Color(0.72f, 0.2f, 1f, 1f);
        CreateRings();
        CreateCoreAndShards();
    }

    private void CreateRings()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        material = new Material(shader);
        outerRing = CreateRing("OuterSeal", 0.11f);
        innerRing = CreateRing("InnerSeal", 0.055f);
    }

    private LineRenderer CreateRing(string ringName, float width)
    {
        GameObject ringObject = new GameObject(ringName);
        ringObject.transform.SetParent(transform, false);
        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = segments;
        ring.widthMultiplier = width;
        ring.numCapVertices = 3;
        ring.material = material;
        ring.sortingOrder = 92;
        return ring;
    }

    private void CreateCoreAndShards()
    {
        GameObject coreObject = new GameObject("ResonanceSealCore");
        coreObject.transform.SetParent(transform, false);
        core = coreObject.AddComponent<SpriteRenderer>();
        core.sprite = TreasureResonanceVfx.GlowSprite;
        core.sortingOrder = 93;

        int count = detonation ? 22 : 12;
        shards = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            GameObject shard = new GameObject("BossResonanceShard");
            shard.transform.SetParent(transform, false);
            shard.transform.localScale = new Vector3(0.08f, detonation ? 0.32f : 0.2f, 1f);
            SpriteRenderer renderer = shard.AddComponent<SpriteRenderer>();
            renderer.sprite = TreasureResonanceVfx.GlowSprite;
            renderer.sortingOrder = 94;
            shards[i] = renderer;
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / lifetime);
        float power = detonation ? 2.6f : 1.35f;
        float radius = Mathf.Lerp(0.18f, power, 1f - Mathf.Pow(1f - progress, 3f));
        float inverseRadius = Mathf.Lerp(power * 0.76f, 0.1f, progress);
        Color secondary = new Color(0.68f, 0.24f, 1f, 1f);
        Color outerColor = Color.Lerp(primary, secondary, progress * 0.65f);
        outerColor.a = (1f - progress) * 0.95f;
        Color innerColor = Color.Lerp(secondary, new Color(1f, 0.84f, 0.28f, 1f), progress);
        innerColor.a = (1f - progress) * 0.88f;

        DrawRing(outerRing, radius, elapsed * 1.8f, outerColor);
        DrawRing(innerRing, inverseRadius, -elapsed * 4.4f, innerColor);
        outerRing.widthMultiplier = Mathf.Lerp(0.12f, 0.01f, progress);
        innerRing.widthMultiplier = Mathf.Lerp(0.07f, 0.008f, progress);

        core.color = Color.Lerp(primary, Color.white, Mathf.Sin(progress * Mathf.PI));
        core.color = new Color(core.color.r, core.color.g, core.color.b, (1f - progress) * 0.9f);
        core.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, detonation ? 1.8f : 0.9f, Mathf.Sin(progress * Mathf.PI * 0.5f));

        for (int i = 0; i < shards.Length; i++)
        {
            float angle = Mathf.PI * 2f * i / shards.Length + elapsed * (detonation ? 3f : 1.8f);
            Transform shard = shards[i].transform;
            shard.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * Mathf.Lerp(0.55f, 1.15f, progress);
            shard.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
            Color color = i % 2 == 0 ? primary : secondary;
            color.a = (1f - progress) * 0.95f;
            shards[i].color = color;
        }

        if (progress >= 1f) Destroy(gameObject);
    }

    private void DrawRing(LineRenderer ring, float radius, float rotation, Color color)
    {
        ring.startColor = color;
        ring.endColor = color;
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments + rotation;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
