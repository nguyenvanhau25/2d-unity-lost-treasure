using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Vật phẩm Kho Báu Thật (Vương Miện, Chén Thánh, Kim Cương, Thỏi Vàng, Đồng Xu Vàng)
/// bắn ra từ Rương Kho Báu trên Tháp Cổ Đại và rơi vung vãi xuống mặt đất phía dưới.
/// </summary>
public class SpawnedTreasureItem : MonoBehaviour
{
    private MysteryTreasureChest ownerChest;
    private string itemTitle;
    private bool isCollected;
    private bool canBePickedUp;
    private bool isMagnetizing;
    private Vector3 landedPosition;
    private float bobSeed;

    public bool IsCollected => this.isCollected;

    public void Initialize(MysteryTreasureChest chest, string title, Vector3 startPos, Vector3 targetPos, float flightDuration, float arcHeight)
    {
        this.ownerChest = chest;
        this.itemTitle = title;
        this.landedPosition = targetPos;
        this.bobSeed = Random.Range(0f, 10f);
        transform.position = startPos;
        StartCoroutine(this.FlyAndBounceRoutine(startPos, targetPos, flightDuration, arcHeight));
    }

    private IEnumerator FlyAndBounceRoutine(Vector3 startPos, Vector3 targetPos, float duration, float arcHeight)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = pos;
            yield return null;
        }

        // Nhịp nảy lần 1 khi rơi từ trên tháp chạm đất
        const float bounce1Duration = 0.28f;
        for (float b = 0f; b < bounce1Duration; b += Time.deltaTime)
        {
            float bt = b / bounce1Duration;
            Vector3 pos = targetPos;
            pos.y += Mathf.Sin(bt * Mathf.PI) * 0.48f;
            transform.position = pos;
            yield return null;
        }

        // Nhịp nảy nhẹ lần 2 trên mặt đất
        const float bounce2Duration = 0.18f;
        for (float b = 0f; b < bounce2Duration; b += Time.deltaTime)
        {
            float bt = b / bounce2Duration;
            Vector3 pos = targetPos;
            pos.y += Mathf.Sin(bt * Mathf.PI) * 0.18f;
            transform.position = pos;
            yield return null;
        }

        transform.position = targetPos;
        this.canBePickedUp = true;
    }

    private void Update()
    {
        if (this.isCollected || !this.canBePickedUp) return;

        if (this.isMagnetizing && PlayerCtrl.Instance != null)
        {
            Vector3 playerPos = PlayerCtrl.Instance.transform.position + new Vector3(0f, 0.5f, 0f);
            transform.position = Vector3.MoveTowards(transform.position, playerPos, 18f * Time.deltaTime);
            if (Vector3.Distance(transform.position, playerPos) <= 0.45f)
            {
                this.Collect();
            }
            return;
        }

        // Hiệu ứng lấp lánh nhẹ trên mặt đất
        float bob = Mathf.Sin(Time.time * 3.6f + this.bobSeed) * 0.06f;
        transform.position = this.landedPosition + new Vector3(0f, bob, 0f);
    }

    public void MagnetizeToPlayer()
    {
        if (this.isCollected) return;
        this.canBePickedUp = true;
        this.isMagnetizing = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!this.canBePickedUp || this.isCollected || other == null) return;
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerCtrl>() != null)
        {
            this.Collect();
        }
    }

    public void Collect()
    {
        if (this.isCollected) return;
        this.isCollected = true;

        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("SelectSound", 0.9f);
        }

        if (this.ownerChest != null)
        {
            this.ownerChest.OnTreasureItemCollected(this.itemTitle, transform.position);
        }

        Destroy(gameObject);
    }
}

/// <summary>
/// Rương Kho Báu Thất Lạc (Mystery Treasure Chest) đặt nổi bật TRÊN Tháp Cổ Đại (SortingLayer = "Effects").
/// Không hiển thị chữ thừa trên map.
/// Khi mở rương, 4 mảnh chìa khóa ghép lại trên tháp, mở tung nắp rương và phun trào Kho Báu Thật
/// rơi vung vãi xuống mặt đất phía dưới để người chơi nhặt trước khi về Màn Hình Chính.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MysteryTreasureChest : MonoBehaviour
{
    private const string TopSortingLayer = "Effects";

    [Header("Chest Settings")]
    [SerializeField] private string endingSceneName = "MainMenu";

    private SpriteRenderer spriteRenderer;
    private Collider2D chestCollider;
    private SpecialBattleFlow battleFlow;
    private bool isPlayerNearby;
    private bool isOpened;
    private bool treasureRevealed;
    private bool returningToMenu;
    private GameObject goldenAuraObj;

    private readonly List<SpawnedTreasureItem> spawnedTreasures = new List<SpawnedTreasureItem>();
    private int totalTreasureCount;
    private int collectedTreasureCount;
    private TextMeshProUGUI bottomBarStatusText;
    private TextMeshProUGUI bottomBarSubText;

    private void Awake()
    {
        this.chestCollider = GetComponent<Collider2D>();
        this.chestCollider.isTrigger = true;
        if (this.chestCollider is BoxCollider2D box)
        {
            box.offset = new Vector2(0f, -1.25f);
            box.size = new Vector2(4.2f, 3.8f);
        }

        this.CreateStoneAltarBase();

        this.spriteRenderer = GetComponent<SpriteRenderer>();
        if (this.spriteRenderer == null)
        {
            this.spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        this.spriteRenderer.sprite = CreateChestSprite(false);
        this.spriteRenderer.sortingLayerName = TopSortingLayer;
        this.spriteRenderer.sortingOrder = 52;

        transform.localScale = Vector3.one * 2.1f;
        this.CreateGoldenAura();
    }

    private void Start()
    {
        this.battleFlow = FindFirstObjectByType<SpecialBattleFlow>();
    }

    private bool AreGuardiansCleared()
    {
        if (this.battleFlow == null)
            this.battleFlow = FindFirstObjectByType<SpecialBattleFlow>();
        return this.battleFlow == null || this.battleFlow.IsBattleFinished;
    }

    private void Update()
    {
        if (this.treasureRevealed && !this.returningToMenu)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                this.VacuumAllRemainingTreasures();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                this.ConfirmReturnToMainMenu();
            }
            return;
        }

        if (this.isOpened) return;

        bool guardiansCleared = this.AreGuardiansCleared();
        bool hasMasterKey = LostTreasureManager.Instance.IsMasterKeyCrafted;
        bool canUnlockNow = guardiansCleared && hasMasterKey;

        if (this.goldenAuraObj != null)
        {
            this.goldenAuraObj.SetActive(true);
            float pulse = canUnlockNow
                ? 1.65f + Mathf.Sin(Time.time * 4.0f) * 0.22f
                : 1.25f + Mathf.Sin(Time.time * 2.0f) * 0.08f;
            this.goldenAuraObj.transform.localScale = new Vector3(pulse, pulse * 0.75f, 1f);
        }

        if (this.isPlayerNearby && canUnlockNow && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)))
        {
            this.OpenChest();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || other.GetComponentInParent<PlayerCtrl>() == null) return;
        this.isPlayerNearby = true;

        if (this.AreGuardiansCleared() && LostTreasureManager.Instance.IsMasterKeyCrafted && !this.isOpened)
        {
            this.OpenChest();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null || other.GetComponentInParent<PlayerCtrl>() == null) return;
        this.isPlayerNearby = false;
    }

    public void OpenChest()
    {
        if (this.isOpened) return;
        if (!this.AreGuardiansCleared()) return;
        if (!LostTreasureManager.Instance.IsMasterKeyCrafted) return;
        this.isOpened = true;

        LostTreasureManager.Instance.UnlockTreasureChest();
        StartCoroutine(this.RunGrandClimaxSequence());
    }

    private IEnumerator RunGrandClimaxSequence()
    {
        SceneStoryOverlay.DismissActive();
        this.FreezePlayer();

        // Đặt nhân vật đứng ở sân đất phía dưới chân Tháp Cổ Đại để nhìn rõ rương trên tháp và kho báu rơi xuống đất
        if (PlayerCtrl.Instance != null)
        {
            Vector3 groundStandPos = new Vector3(transform.position.x, transform.position.y - 3.6f, 0f);
            if (Vector2.Distance(PlayerCtrl.Instance.transform.position, groundStandPos) > 3.2f)
            {
                PlayerCtrl.Instance.transform.position = groundStandPos;
            }
        }

        // BƯỚC 1: 4 MẢNH CHÌA KHÓA HỘI TỤ TRÊN ĐỈNH THÁP & MỞ KHÓA RƯƠNG
        yield return StartCoroutine(this.PlayKeyAssemblyCutsceneAboveChest());

        // BƯỚC 2: RƯƠNG TRÊN THÁP RUNG LẮC & BẬT MỞ NẮP VÀNG
        Vector3 originPos = transform.position;
        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("SelectSound", 1.4f);
        }

        const float shakeTime = 0.8f;
        for (float t = 0; t < shakeTime; t += Time.deltaTime)
        {
            float offset = Mathf.Sin(t * 44f) * 0.1f;
            transform.position = originPos + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        transform.position = originPos;

        if (this.spriteRenderer != null)
        {
            this.spriteRenderer.sprite = CreateChestSprite(true);
        }

        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("ResonanceSkill", 1.8f);
        }

        StartCoroutine(this.EmitTreasureLightRays());

        // BƯỚC 3: KHO BÁU THẬT BẮN TỪ RƯƠNG TRÊN THÁP VÀ RƠI VUNG VÃI XUỐNG MẶT ĐẤT PHÍA DƯỚI!
        yield return StartCoroutine(this.SpawnRealTreasureHoardToGround());

        // BƯỚC 4: MỞ KHÓA DI CHUYỂN ĐỂ NGƯỜI CHƠI CHẠY NHẶT KHO BÁU DƯỚI ĐẤT RỒI MỚI VỀ MÀN HÌNH CHÍNH
        this.UnfreezePlayer();
        this.treasureRevealed = true;
        this.BuildTreasureCelebrationBottomBar();
    }

    private IEnumerator PlayKeyAssemblyCutsceneAboveChest()
    {
        Vector3 centerAboveChest = transform.position + new Vector3(0f, 1.45f, 0f);
        Vector3[] startOffsets =
        {
            new Vector3(-2.2f, 0.8f, 0f),
            new Vector3(-1.0f, 2.2f, 0f),
            new Vector3(1.0f, 2.2f, 0f),
            new Vector3(2.2f, 0.8f, 0f)
        };

        Sprite keySprite = KeyFragmentPickup.CreateProceduralKeySprite();
        GameObject[] fragObjs = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            fragObjs[i] = new GameObject($"AssemblingFragment_{i + 1}");
            fragObjs[i].transform.position = transform.position + startOffsets[i];
            fragObjs[i].transform.localScale = Vector3.one * 1.55f;
            SpriteRenderer sr = fragObjs[i].AddComponent<SpriteRenderer>();
            sr.sprite = keySprite;
            sr.color = new Color(1f, 0.9f, 0.3f, 1f);
            sr.sortingLayerName = TopSortingLayer;
            sr.sortingOrder = 60;

            if (AudioManagerr.Instance != null)
            {
                AudioManagerr.Instance.PlaySFX("SelectSound", 0.9f + i * 0.1f);
            }
            yield return new WaitForSeconds(0.16f);
        }

        const float convergeDuration = 0.95f;
        for (float t = 0f; t < convergeDuration; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / convergeDuration);
            for (int i = 0; i < 4; i++)
            {
                if (fragObjs[i] == null) continue;
                Vector3 from = transform.position + startOffsets[i];
                Vector3 pos = Vector3.Lerp(from, centerAboveChest, p);
                pos.y += Mathf.Sin(p * Mathf.PI) * 0.45f;
                fragObjs[i].transform.position = pos;
                fragObjs[i].transform.Rotate(0f, 0f, 360f * Time.deltaTime);
            }
            yield return null;
        }

        for (int i = 0; i < 4; i++)
        {
            if (fragObjs[i] != null) Destroy(fragObjs[i]);
        }

        GameObject masterKeyObj = new GameObject("AssembledMasterKeyVisual");
        masterKeyObj.transform.position = centerAboveChest;
        masterKeyObj.transform.localScale = Vector3.one * 2.5f;
        SpriteRenderer mkRenderer = masterKeyObj.AddComponent<SpriteRenderer>();
        mkRenderer.sprite = keySprite;
        mkRenderer.color = new Color(1f, 0.96f, 0.45f, 1f);
        mkRenderer.sortingLayerName = TopSortingLayer;
        mkRenderer.sortingOrder = 62;

        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("StartSound", 1.3f);
        }

        yield return new WaitForSeconds(0.4f);

        Vector3 lockPos = transform.position + new Vector3(0f, 0.1f, 0f);
        const float insertDuration = 0.5f;
        for (float t = 0f; t < insertDuration; t += Time.deltaTime)
        {
            float p = t / insertDuration;
            masterKeyObj.transform.position = Vector3.Lerp(centerAboveChest, lockPos, p);
            masterKeyObj.transform.localScale = Vector3.Lerp(Vector3.one * 2.5f, Vector3.one * 1.15f, p);
            yield return null;
        }

        Destroy(masterKeyObj);
    }

    /// <summary>
    /// Bắn toàn bộ Kho Báu Thật từ Rương trên Tháp Cổ Đại (y = 17.35) rơi vung vãi xuống sân đất phía dưới (y = 11.2 .. 15.0).
    /// </summary>
    private IEnumerator SpawnRealTreasureHoardToGround()
    {
        this.spawnedTreasures.Clear();
        this.totalTreasureCount = 0;
        this.collectedTreasureCount = 0;

        Vector3 chestTopPos = transform.position + new Vector3(0f, 0.4f, 0f);

        // 1. Vương Miện Hoàng Gia Cổ Đại rơi xuống chính giữa sân đất dưới chân tháp
        this.SpawnSingleTreasure(
            "👑 Vương Miện Hoàng Gia Cổ Đại",
            CreateCrownSprite(),
            new Color(1f, 0.92f, 0.28f, 1f),
            chestTopPos,
            transform.position + new Vector3(0f, -3.1f, 0f),
            1.85f,
            0.78f,
            2.4f);

        yield return new WaitForSeconds(0.10f);

        // 2. Hai Chén Thánh Hoàng Kim rơi xuống hai bên sân đất dưới tháp
        this.SpawnSingleTreasure(
            "🏆 Chén Thánh Hoàng Kim",
            CreateChaliceSprite(),
            new Color(1f, 0.86f, 0.25f, 1f),
            chestTopPos,
            transform.position + new Vector3(-2.3f, -3.3f, 0f),
            1.65f,
            0.75f,
            2.2f);

        this.SpawnSingleTreasure(
            "🏆 Cúp Ngọc Bích Cổ Đại",
            CreateChaliceSprite(),
            new Color(0.4f, 1f, 0.85f, 1f),
            chestTopPos,
            transform.position + new Vector3(2.3f, -3.3f, 0f),
            1.65f,
            0.75f,
            2.2f);

        yield return new WaitForSeconds(0.10f);

        // 3. 8 Viên Đại Bảo Ngọc (Kim Cương, Hồng Ngọc, Ngọc Lục Bảo, Thạch Anh Tím) rơi vung vãi xuống mặt đất
        Vector3[] gemGroundOffsets =
        {
            new Vector3(-1.25f, -2.75f, 0f),
            new Vector3(1.25f, -2.75f, 0f),
            new Vector3(-3.35f, -3.65f, 0f),
            new Vector3(3.35f, -3.65f, 0f),
            new Vector3(-1.85f, -4.45f, 0f),
            new Vector3(1.85f, -4.45f, 0f),
            new Vector3(-0.75f, -5.15f, 0f),
            new Vector3(0.75f, -5.15f, 0f)
        };
        Color[] gemColors =
        {
            new Color(0.35f, 0.92f, 1f, 1f),
            new Color(1f, 0.28f, 0.42f, 1f),
            new Color(0.25f, 0.98f, 0.52f, 1f),
            new Color(0.82f, 0.45f, 1f, 1f),
            new Color(1f, 0.72f, 0.22f, 1f),
            new Color(0.45f, 0.98f, 0.95f, 1f),
            new Color(1f, 0.42f, 0.75f, 1f),
            new Color(0.55f, 1f, 0.38f, 1f)
        };
        string[] gemNames =
        {
            "💎 Đại Kim Cương Vĩnh Hằng",
            "♦ Hồng Ngọc Huyết Long",
            "❖ Ngọc Lục Bảo Rừng Sao",
            "✦ Thạch Anh Tím Đền Cổ",
            "★ Hoàng Ngọc Thái Dương",
            "💎 Lam Ngọc Đại Dương",
            "♦ Chu Ngọc Hoàng Gia",
            "❖ Bích Ngọc Ngàn Năm"
        };

        Sprite gemSprite = CreateGemSprite();
        for (int i = 0; i < gemGroundOffsets.Length; i++)
        {
            this.SpawnSingleTreasure(
                gemNames[i],
                gemSprite,
                gemColors[i],
                chestTopPos,
                transform.position + gemGroundOffsets[i],
                1.45f,
                Random.Range(0.68f, 0.88f),
                Random.Range(1.9f, 2.6f));
        }

        yield return new WaitForSeconds(0.10f);

        // 4. 8 Thỏi Vàng Hoàng Gia rơi xuống khắp bãi đất trước tháp
        Sprite ingotSprite = CreateGoldIngotSprite();
        Vector3[] ingotGroundOffsets =
        {
            new Vector3(-0.65f, -3.55f, 0f),
            new Vector3(0.65f, -3.55f, 0f),
            new Vector3(-2.85f, -2.85f, 0f),
            new Vector3(2.85f, -2.85f, 0f),
            new Vector3(-2.65f, -4.85f, 0f),
            new Vector3(2.65f, -4.85f, 0f),
            new Vector3(-1.35f, -5.65f, 0f),
            new Vector3(1.35f, -5.65f, 0f)
        };
        for (int i = 0; i < ingotGroundOffsets.Length; i++)
        {
            this.SpawnSingleTreasure(
                "🪙 Thỏi Vàng Hoàng Gia",
                ingotSprite,
                new Color(1f, 0.86f, 0.24f, 1f),
                chestTopPos,
                transform.position + ingotGroundOffsets[i],
                1.4f,
                Random.Range(0.65f, 0.85f),
                Random.Range(1.8f, 2.5f));
        }

        // 5. 22 Đồng Xu Vàng Cổ Đại phun từ trên tháp rơi vung vãi khắp mặt đất phía dưới
        Sprite coinSprite = CreateGoldCoinSprite();
        for (int i = 0; i < 22; i++)
        {
            float offsetX = Random.Range(-4.4f, 4.4f);
            float offsetY = Random.Range(-6.1f, -2.4f);
            Vector3 coinGroundTarget = transform.position + new Vector3(offsetX, offsetY, 0f);

            this.SpawnSingleTreasure(
                "🪙 Đồng Vàng Cổ Đại",
                coinSprite,
                new Color(1f, 0.9f, 0.28f, 1f),
                chestTopPos,
                coinGroundTarget,
                1.25f,
                Random.Range(0.60f, 0.92f),
                Random.Range(1.8f, 2.9f));

            if (i % 4 == 0)
            {
                if (AudioManagerr.Instance != null)
                {
                    AudioManagerr.Instance.PlaySFX("SelectSound", 0.8f);
                }
                yield return new WaitForSeconds(0.04f);
            }
        }

        yield return new WaitForSeconds(0.45f);
    }

    private void SpawnSingleTreasure(
        string title,
        Sprite sprite,
        Color color,
        Vector3 startPos,
        Vector3 targetPos,
        float scale,
        float duration,
        float arcHeight)
    {
        GameObject obj = new GameObject($"TreasureLoot_{this.totalTreasureCount + 1}");
        obj.transform.localScale = Vector3.one * scale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = TopSortingLayer;
        sr.sortingOrder = 55;

        GameObject glowObj = new GameObject("TreasureGlow");
        glowObj.transform.SetParent(obj.transform, false);
        glowObj.transform.localScale = Vector3.one * 1.45f;
        SpriteRenderer glowSr = glowObj.AddComponent<SpriteRenderer>();
        glowSr.sprite = CreateGoldCoinSprite();
        glowSr.color = new Color(1f, 0.92f, 0.35f, 0.34f);
        glowSr.sortingLayerName = TopSortingLayer;
        glowSr.sortingOrder = 54;

        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.52f;

        SpawnedTreasureItem item = obj.AddComponent<SpawnedTreasureItem>();
        item.Initialize(this, title, startPos, targetPos, duration, arcHeight);

        this.spawnedTreasures.Add(item);
        this.totalTreasureCount++;
    }

    public void OnTreasureItemCollected(string itemTitle, Vector3 worldPos)
    {
        this.collectedTreasureCount++;
        this.SpawnFloatingPopup(itemTitle, worldPos);
        this.RefreshBottomBarStatus();
    }

    private void SpawnFloatingPopup(string text, Vector3 worldPos)
    {
        GameObject popupObj = new GameObject("TreasurePickupPopup");
        popupObj.transform.position = worldPos + new Vector3(0f, 0.4f, 0f);
        TextMeshPro tmp = popupObj.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = $"+ {text}";
        tmp.fontSize = 2.0f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.94f, 0.35f, 1f);
        tmp.sortingLayerID = SortingLayer.NameToID(TopSortingLayer);
        tmp.sortingOrder = 65;
        StartCoroutine(this.AnimateFloatingPopup(popupObj, tmp));
    }

    private IEnumerator AnimateFloatingPopup(GameObject popupObj, TextMeshPro tmp)
    {
        Vector3 start = popupObj.transform.position;
        const float duration = 0.85f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (popupObj == null) yield break;
            float p = t / duration;
            popupObj.transform.position = start + new Vector3(0f, p * 0.95f, 0f);
            Color c = tmp.color;
            c.a = 1f - p;
            tmp.color = c;
            yield return null;
        }
        if (popupObj != null) Destroy(popupObj);
    }

    public void VacuumAllRemainingTreasures()
    {
        for (int i = 0; i < this.spawnedTreasures.Count; i++)
        {
            if (this.spawnedTreasures[i] != null && !this.spawnedTreasures[i].IsCollected)
            {
                this.spawnedTreasures[i].MagnetizeToPlayer();
            }
        }
    }

    public void ConfirmReturnToMainMenu()
    {
        if (this.returningToMenu) return;
        this.returningToMenu = true;
        SceneManager.LoadScene(LostTreasureSceneHelper.ResolveLoadableSceneName(this.endingSceneName));
    }

    private void BuildTreasureCelebrationBottomBar()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObj = new GameObject("TreasureCelebrationCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        GameObject frameObj = new GameObject("CelebrationBottomFrame");
        frameObj.transform.SetParent(canvasObj.transform, false);
        RectTransform frameRect = frameObj.AddComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0f);
        frameRect.anchorMax = new Vector2(0.5f, 0f);
        frameRect.pivot = new Vector2(0.5f, 0f);
        frameRect.anchoredPosition = new Vector2(0f, 18f);
        frameRect.sizeDelta = new Vector2(1260f, 165f);
        Image frameImg = frameObj.AddComponent<Image>();
        frameImg.color = new Color(0.92f, 0.74f, 0.26f, 0.98f);

        GameObject innerObj = new GameObject("CelebrationInner");
        innerObj.transform.SetParent(frameObj.transform, false);
        RectTransform innerRect = innerObj.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(4f, 4f);
        innerRect.offsetMax = new Vector2(-4f, -4f);
        Image innerImg = innerObj.AddComponent<Image>();
        innerImg.color = new Color(0.08f, 0.09f, 0.15f, 0.94f);

        this.bottomBarStatusText = CreateUIText(
            innerObj.transform,
            "StatusTitle",
            "",
            font,
            25f,
            FontStyles.Bold,
            new Color(1f, 0.9f, 0.32f),
            new Vector2(0f, 46f),
            new Vector2(1200f, 38f));

        this.bottomBarSubText = CreateUIText(
            innerObj.transform,
            "SubTitle",
            "Kho Báu Thất Lạc đã rơi xuống dưới chân Tháp Cổ Đại! Hãy đi nhặt kho báu trước khi trở về Màn Hình Chính.",
            font,
            18f,
            FontStyles.Normal,
            new Color(0.92f, 0.94f, 0.98f),
            new Vector2(0f, 10f),
            new Vector2(1180f, 30f));

        GameObject vacuumBtnObj = new GameObject("VacuumTreasureButton");
        vacuumBtnObj.transform.SetParent(innerObj.transform, false);
        RectTransform vacRect = vacuumBtnObj.AddComponent<RectTransform>();
        vacRect.anchorMin = new Vector2(0.5f, 0.5f);
        vacRect.anchorMax = new Vector2(0.5f, 0.5f);
        vacRect.anchoredPosition = new Vector2(-250f, -42f);
        vacRect.sizeDelta = new Vector2(440f, 48f);
        Image vacImg = vacuumBtnObj.AddComponent<Image>();
        vacImg.color = new Color(0.16f, 0.42f, 0.62f, 0.98f);
        Button vacBtn = vacuumBtnObj.AddComponent<Button>();
        vacBtn.onClick.AddListener(this.VacuumAllRemainingTreasures);

        CreateUIText(
            vacuumBtnObj.transform,
            "Label",
            "🧲 THU THẬP TẤT CẢ KHO BÁU [Phím E]",
            font,
            19f,
            FontStyles.Bold,
            new Color(1f, 0.96f, 0.78f),
            Vector2.zero,
            new Vector2(420f, 42f));

        GameObject finishBtnObj = new GameObject("FinishReturnButton");
        finishBtnObj.transform.SetParent(innerObj.transform, false);
        RectTransform finRect = finishBtnObj.AddComponent<RectTransform>();
        finRect.anchorMin = new Vector2(0.5f, 0.5f);
        finRect.anchorMax = new Vector2(0.5f, 0.5f);
        finRect.anchoredPosition = new Vector2(250f, -42f);
        finRect.sizeDelta = new Vector2(460f, 48f);
        Image finImg = finishBtnObj.AddComponent<Image>();
        finImg.color = new Color(0.76f, 0.52f, 0.12f, 0.98f);
        Button finBtn = finishBtnObj.AddComponent<Button>();
        finBtn.onClick.AddListener(this.ConfirmReturnToMainMenu);

        CreateUIText(
            finishBtnObj.transform,
            "Label",
            "🏆 HOÀN THÀNH & VỀ MÀN HÌNH CHÍNH [ENTER]",
            font,
            19f,
            FontStyles.Bold,
            Color.white,
            Vector2.zero,
            new Vector2(445f, 42f));

        this.RefreshBottomBarStatus();
    }

    private void RefreshBottomBarStatus()
    {
        if (this.bottomBarStatusText == null) return;
        if (this.collectedTreasureCount >= this.totalTreasureCount && this.totalTreasureCount > 0)
        {
            this.bottomBarStatusText.text =
                $"👑 ĐÃ THU THẬP TRỌN VẸN KHO BÁU THẤT LẠC ({this.collectedTreasureCount}/{this.totalTreasureCount} BÁU VẬT)! 👑";
            if (this.bottomBarSubText != null)
            {
                this.bottomBarSubText.text =
                    "Chúc mừng nhà thám hiểm huyền thoại! Nhấn [ENTER] hoặc nút bên dưới khi bạn sẵn sàng trở về Màn Hình Chính.";
                this.bottomBarSubText.color = new Color(0.45f, 1f, 0.65f);
            }
        }
        else
        {
            this.bottomBarStatusText.text =
                $"✦ KHO BÁU THẤT LẠC ĐÃ RƠI XUỐNG ĐẤT — ĐÃ NHẶT: {this.collectedTreasureCount} / {this.totalTreasureCount} BÁU VẬT ✦";
        }
    }

    private static TextMeshProUGUI CreateUIText(
        Transform parent,
        string name,
        string content,
        TMP_FontAsset font,
        float fontSize,
        FontStyles style,
        Color color,
        Vector2 anchoredPos,
        Vector2 sizeDelta)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private IEnumerator EmitTreasureLightRays()
    {
        GameObject beam = new GameObject("TreasureLightBeam");
        beam.transform.SetParent(transform, false);
        beam.transform.localPosition = new Vector3(0, 0.35f, 0);

        SpriteRenderer sr = beam.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBeamSprite();
        sr.color = new Color(1f, 0.95f, 0.4f, 0.65f);
        sr.sortingLayerName = TopSortingLayer;
        sr.sortingOrder = 51;

        Vector3 startScale = new Vector3(0.5f, 0.1f, 1f);
        Vector3 maxScale = new Vector3(2.8f, 3.6f, 1f);

        for (float t = 0; t < 1.2f; t += Time.deltaTime)
        {
            beam.transform.localScale = Vector3.Lerp(startScale, maxScale, t / 1.2f);
            yield return null;
        }
    }

    private void FreezePlayer()
    {
        PlayerCtrl player = PlayerCtrl.Instance;
        if (player == null || player.PlayerMovement == null || player.PlayerMovement._Rb == null) return;

        player.PlayerMovement._Rb.linearVelocity = Vector2.zero;
        player.PlayerMovement._Rb.constraints = RigidbodyConstraints2D.FreezeAll;
        player.PlayerMovement.SetHorizontal(0f);
        player.PlayerMovement.SetVertical(0f);
    }

    private void UnfreezePlayer()
    {
        PlayerCtrl player = PlayerCtrl.Instance;
        if (player == null || player.PlayerMovement == null || player.PlayerMovement._Rb == null) return;

        player.PlayerMovement._Rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        player.PlayerMovement.enabled = true;
        if (player.PlayerAttack != null) player.PlayerAttack.enabled = true;
    }

    private void CreateStoneAltarBase()
    {
        GameObject pedestalObj = new GameObject("TreasureAltarBase");
        pedestalObj.transform.SetParent(transform, false);
        pedestalObj.transform.localPosition = new Vector3(0f, -0.38f, 0f);
        pedestalObj.transform.localScale = new Vector3(1.45f, 1.15f, 1f);

        SpriteRenderer sr = pedestalObj.AddComponent<SpriteRenderer>();
        sr.sprite = KeyFragmentPickup.CreatePedestalSprite();
        sr.sortingLayerName = TopSortingLayer;
        sr.sortingOrder = 48;
    }

    private void CreateGoldenAura()
    {
        this.goldenAuraObj = new GameObject("GoldenAura");
        this.goldenAuraObj.transform.SetParent(transform, false);
        this.goldenAuraObj.transform.localPosition = Vector3.zero;
        this.goldenAuraObj.transform.localScale = new Vector3(1.5f, 1.1f, 1f);

        SpriteRenderer sr = this.goldenAuraObj.AddComponent<SpriteRenderer>();
        sr.sprite = KeyFragmentPickup.CreateAuraSprite();
        sr.color = new Color(1f, 0.88f, 0.25f, 0.65f);
        sr.sortingLayerName = TopSortingLayer;
        sr.sortingOrder = 49;
    }

    private static Sprite CreateChestSprite(bool isOpen)
    {
        Texture2D tex = new Texture2D(28, 24, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color woodDark = new Color(0.36f, 0.20f, 0.09f, 1f);
        Color wood = new Color(0.54f, 0.31f, 0.14f, 1f);
        Color gold = new Color(1f, 0.84f, 0.24f, 1f);
        Color goldBright = new Color(1f, 0.96f, 0.58f, 1f);
        Color ruby = new Color(1f, 0.22f, 0.35f, 1f);
        Color cyanGem = new Color(0.3f, 0.95f, 1f, 1f);
        Color emerald = new Color(0.25f, 0.95f, 0.48f, 1f);

        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 28; x++)
            {
                bool isOuterBorder = x <= 1 || x >= 26 || y <= 1 || y >= 22;
                bool isStraps = (x >= 6 && x <= 8) || (x >= 19 && x <= 21);
                bool isLock = x >= 12 && x <= 15 && y >= 7 && y <= 12;

                if (isOuterBorder)
                {
                    tex.SetPixel(x, y, gold);
                }
                else if (isOpen && y >= 11 && y <= 20 && x >= 2 && x <= 25)
                {
                    if ((x + y) % 7 == 0)
                        tex.SetPixel(x, y, ruby);
                    else if ((x * 3 + y) % 8 == 0)
                        tex.SetPixel(x, y, cyanGem);
                    else if ((x + y * 2) % 9 == 0)
                        tex.SetPixel(x, y, emerald);
                    else if ((x + y) % 2 == 0)
                        tex.SetPixel(x, y, goldBright);
                    else
                        tex.SetPixel(x, y, gold);
                }
                else if (!isOpen && isLock)
                {
                    tex.SetPixel(x, y, goldBright);
                }
                else if (isStraps || y == 10)
                {
                    tex.SetPixel(x, y, gold);
                }
                else
                {
                    tex.SetPixel(x, y, (y % 4 == 0) ? woodDark : wood);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 28, 24), new Vector2(0.5f, 0.5f), 16);
    }

    private static Sprite CreateCrownSprite()
    {
        Texture2D tex = new Texture2D(20, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color gold = new Color(1f, 0.85f, 0.22f, 1f);
        Color highlight = new Color(1f, 0.97f, 0.65f, 1f);
        Color ruby = new Color(1f, 0.2f, 0.35f, 1f);
        Color sapphire = new Color(0.25f, 0.88f, 1f, 1f);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 20; x++)
            {
                bool baseBand = y >= 2 && y <= 6 && x >= 2 && x <= 17;
                bool leftPeak = x >= 2 && x <= 6 && y >= 7 && y <= 13 - Mathf.Abs(x - 4) * 2;
                bool midPeak = x >= 7 && x <= 12 && y >= 7 && y <= 14 - Mathf.Abs(x - 9) * 2;
                bool rightPeak = x >= 13 && x <= 17 && y >= 7 && y <= 13 - Mathf.Abs(x - 15) * 2;

                if (y == 4 && (x == 5 || x == 14))
                    tex.SetPixel(x, y, sapphire);
                else if (y == 4 && (x == 9 || x == 10))
                    tex.SetPixel(x, y, ruby);
                else if (baseBand || leftPeak || midPeak || rightPeak)
                    tex.SetPixel(x, y, (y >= 11 || y == 2) ? highlight : gold);
                else
                    tex.SetPixel(x, y, clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 20, 16), new Vector2(0.5f, 0.5f), 16);
    }

    private static Sprite CreateChaliceSprite()
    {
        Texture2D tex = new Texture2D(16, 20, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color gold = new Color(1f, 0.84f, 0.24f, 1f);
        Color shine = new Color(1f, 0.96f, 0.68f, 1f);
        Color gem = new Color(1f, 0.25f, 0.4f, 1f);

        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool baseFoot = y >= 1 && y <= 3 && x >= 3 && x <= 12;
                bool stem = y >= 4 && y <= 9 && x >= 6 && x <= 9;
                bool cup = y >= 10 && y <= 17 && x >= 3 && x <= 12;

                if (y == 13 && (x == 7 || x == 8))
                    tex.SetPixel(x, y, gem);
                else if (baseFoot || stem || cup)
                    tex.SetPixel(x, y, (x == 5 || y == 17) ? shine : gold);
                else
                    tex.SetPixel(x, y, clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 20), new Vector2(0.5f, 0.5f), 16);
    }

    private static Sprite CreateGemSprite()
    {
        Texture2D tex = new Texture2D(14, 14, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Vector2 center = new Vector2(6.5f, 6.5f);

        for (int y = 0; y < 14; y++)
        {
            for (int x = 0; x < 14; x++)
            {
                float manhattan = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                if (manhattan <= 5.5f)
                {
                    bool facet = x <= 6 && y >= 7 && manhattan <= 3.5f;
                    tex.SetPixel(x, y, facet ? Color.white : new Color(0.9f, 0.9f, 0.9f, 1f));
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 14, 14), new Vector2(0.5f, 0.5f), 16);
    }

    private static Sprite CreateGoldIngotSprite()
    {
        Texture2D tex = new Texture2D(16, 10, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color darkGold = new Color(0.82f, 0.62f, 0.14f, 1f);
        Color gold = new Color(1f, 0.85f, 0.25f, 1f);
        Color brightGold = new Color(1f, 0.96f, 0.65f, 1f);

        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int margin = y / 2;
                if (y >= 1 && y <= 8 && x >= 1 + margin && x <= 14 - margin)
                {
                    tex.SetPixel(x, y, y >= 6 ? brightGold : (y <= 2 ? darkGold : gold));
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 10), new Vector2(0.5f, 0.5f), 16);
    }

    private static Sprite CreateGoldCoinSprite()
    {
        Texture2D tex = new Texture2D(12, 12, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color rim = new Color(0.88f, 0.66f, 0.14f, 1f);
        Color gold = new Color(1f, 0.88f, 0.26f, 1f);
        Color shine = new Color(1f, 0.98f, 0.72f, 1f);
        Vector2 c = new Vector2(5.5f, 5.5f);

        for (int y = 0; y < 12; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                if (d <= 4.8f)
                {
                    if (d >= 3.8f) tex.SetPixel(x, y, rim);
                    else if (x == 4 && y >= 5 && y <= 7) tex.SetPixel(x, y, shine);
                    else tex.SetPixel(x, y, gold);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f), 16);
    }

    private static Sprite CreateBeamSprite()
    {
        Texture2D tex = new Texture2D(16, 32, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color beam = new Color(1f, 0.95f, 0.4f, 0.9f);
        Color fade = new Color(1f, 0.95f, 0.4f, 0f);

        for (int y = 0; y < 32; y++)
        {
            float alpha = 1f - (float)y / 32f;
            Color c = Color.Lerp(fade, beam, alpha);
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 32), new Vector2(0.5f, 0f), 16);
    }
}
