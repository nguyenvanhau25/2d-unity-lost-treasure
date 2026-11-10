using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Thử thách: Ngôi Đền Cổ Bị Nguyền Rủa (The Cursed Temple) thay thế hoàn toàn nhiệm vụ bảo vệ công chúa cũ.
/// Quy trình thử thách:
/// 1. Người chơi tiến vào Ngôi Đền Cổ từ lối vào phía Nam (0, -2.46).
/// 2. Vượt qua hệ thống bẫy chông gai cổ đại (Cố định, Chu kỳ, Bẫy đạp) để kích hoạt 3 Cột Cổ Ngữ (Rune Pillars).
/// 3. Cánh cửa Phong Ấn Bắc Điện (y = 6.4) mở ra và đánh thức Guardian — Người Canh Giữ Ngôi Đền.
/// 4. Đánh bại Guardian để nhận Mảnh Chìa Khóa Đền Cổ (Mảnh thứ 4 -> hợp nhất Chìa Khóa Cổ Đại Hoàn Chỉnh)
///    và bước vào Bệ Thờ Bắc Điện (0, 10.2) để tiến tới Cấm Địa Kho Báu (Special).
/// </summary>
public sealed class CursedTempleChallenge : MonoBehaviour
{
    private bool guardianDefeated;
    private bool fragmentSpawned;
    private TextMeshPro altarStatusLabel;

    public bool IsComplete => this.guardianDefeated;

    private void Start()
    {
        this.EnsureSceneUiAndCleanLegacyPrincess();

        RescuePrincessStory story = FindAnyObjectByType<RescuePrincessStory>(FindObjectsInactive.Include);
        if (story == null)
        {
            GameObject altarTriggerObj = new GameObject("CursedTempleExitAltar");
            BoxCollider2D col = altarTriggerObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.6f, 2.2f);
            story = altarTriggerObj.AddComponent<RescuePrincessStory>();
        }

        // Đặt Bệ Thờ Cổ Đại ở chính giữa Bắc Điện trong phạm vi phòng đền (x in [-13.5, 13.5], y in [-3.8, 12.0])
        Vector3 altarPosition = new Vector3(0f, 10.2f, 0f);
        story.gameObject.SetActive(true);
        story.transform.position = altarPosition;

        BoxCollider2D storyBox = story.GetComponent<BoxCollider2D>();
        if (storyBox != null)
        {
            storyBox.isTrigger = true;
            storyBox.offset = Vector2.zero;
            storyBox.size = new Vector2(2.8f, 2.2f);
        }

        this.PrepareExitAltar(story, altarPosition);

        if (LostTreasureManager.Instance != null && LostTreasureManager.Instance.HasFragment(3))
        {
            this.guardianDefeated = true;
            this.UpdateAltarPrompt(true);
            return;
        }

        this.BuildCursedTempleEncounter(altarPosition);
        StartCoroutine(this.ShowTempleObjective());
    }

    private void EnsureSceneUiAndCleanLegacyPrincess()
    {
        // Đảm bảo Canvas HUD trong RecusePrincess được bật nhưng tắt WinMenu cũ
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allCanvases.Length; i++)
        {
            Canvas c = allCanvases[i];
            if (c != null && c.name.Trim() == "Canvas" && !c.gameObject.activeSelf)
            {
                Transform winMenu = c.transform.Find("UIMid/WinMenu");
                if (winMenu != null) winMenu.gameObject.SetActive(false);

                Transform winToggle = c.transform.Find("UIToggle/WinToggle");
                if (winToggle != null) winToggle.gameObject.SetActive(false);

                c.gameObject.SetActive(true);
            }
        }

        // Vô hiệu hóa hoàn toàn thành phần va chạm công chúa cũ nếu còn sót lại
        PlayerCollidePrincess[] legacyPrincessColliders = FindObjectsByType<PlayerCollidePrincess>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < legacyPrincessColliders.Length; i++)
        {
            if (legacyPrincessColliders[i] != null)
            {
                legacyPrincessColliders[i].gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator ShowTempleObjective()
    {
        yield return new WaitForSeconds(0.25f);
        SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();
        if (overlay != null)
        {
            yield return overlay.ShowMessage(
                "NGÔI ĐỀN CỔ BỊ NGUYỀN RỦA",
                "Tiến tới Bia Đá Cổ Ngữ ở giữa sảnh đền, giải 3 Câu Đố Cổ Đại để mở Cửa Phong Ấn, đánh bại Guardian và thu thập Mảnh Chìa Khóa Thứ Tư!",
                3.2f,
                new Color(0.35f, 0.88f, 1f, 1f));
        }
    }

    private void PrepareExitAltar(RescuePrincessStory story, Vector3 position)
    {
        foreach (SpriteRenderer renderer in story.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.enabled = false;
        }
        foreach (Animator animator in story.GetComponentsInChildren<Animator>(true))
        {
            animator.enabled = false;
        }

        GameObject altar = new GameObject("CursedTempleExitAltarVisual");
        altar.transform.position = position;
        altar.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        SpriteRenderer altarRenderer = altar.AddComponent<SpriteRenderer>();
        altarRenderer.sprite = CreateAltarPortalSprite();
        altarRenderer.color = new Color(1f, 0.82f, 0.32f, 1f);
        altarRenderer.sortingOrder = 11;

        GameObject labelObj = new GameObject("AltarPortalLabel");
        labelObj.transform.SetParent(altar.transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 1.85f, 0f);
        this.altarStatusLabel = labelObj.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            this.altarStatusLabel.font = TMP_Settings.defaultFontAsset;
        }
        this.altarStatusLabel.fontSize = 2.0f;
        this.altarStatusLabel.alignment = TextAlignmentOptions.Center;
        this.altarStatusLabel.sortingOrder = 16;
        this.UpdateAltarPrompt(false);
    }

    private void UpdateAltarPrompt(bool unlocked)
    {
        if (this.altarStatusLabel == null) return;
        if (unlocked)
        {
            this.altarStatusLabel.text = "⛩ CỔNG DỊCH CHUYỂN CẤM ĐỊA KHO BÁU\n<size=1.6>[Nhặt Mảnh Chìa Khóa rồi bước vào Bệ Thờ]</size>";
            this.altarStatusLabel.color = new Color(1f, 0.9f, 0.35f, 1f);
        }
        else
        {
            this.altarStatusLabel.text = "⛩ BỆ THỜ CỔ ĐẠI (ĐANG PHONG ẤN)";
            this.altarStatusLabel.color = new Color(0.75f, 0.8f, 0.9f, 0.85f);
        }
    }

    private void BuildCursedTempleEncounter(Vector3 altarPosition)
    {
        // 1. Hệ thống bẫy chông gai bảo vệ Ngôi Đền Cổ (kết hợp cả 3 loại bẫy với lối đi hợp lý)
        this.CreateSpikeTrap(new Vector3(-4.0f, 0.4f, 0f), SpikeTrapType.Periodic, 0f);
        this.CreateSpikeTrap(new Vector3(4.0f, 0.4f, 0f), SpikeTrapType.Periodic, 1.1f);
        this.CreateSpikeTrap(new Vector3(-7.4f, -0.8f, 0f), SpikeTrapType.StepTriggered, 0f);
        this.CreateSpikeTrap(new Vector3(7.4f, -0.8f, 0f), SpikeTrapType.StepTriggered, 0f);
        this.CreateSpikeTrap(new Vector3(-3.6f, 3.8f, 0f), SpikeTrapType.Fixed, 0f);
        this.CreateSpikeTrap(new Vector3(3.6f, 3.8f, 0f), SpikeTrapType.Fixed, 0f);

        // 2. Guardian — Người Canh Giữ Ngôi Đền Cổ (ẩn cho tới khi giải xong 3 Cột Cổ Ngữ)
        GameObject guardianObject = new GameObject("CursedTempleGuardian");
        guardianObject.transform.position = new Vector3(0f, 8.0f, 0f);
        guardianObject.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        guardianObject.SetActive(false);

        SpriteRenderer guardianRenderer = guardianObject.AddComponent<SpriteRenderer>();
        guardianRenderer.sprite = CreateGuardianSprite();
        guardianRenderer.sortingOrder = 14;
        BoxCollider2D guardianCollider = guardianObject.AddComponent<BoxCollider2D>();
        guardianCollider.size = new Vector2(1.35f, 1.95f);
        guardianCollider.offset = new Vector2(0f, 0.95f);
        CursedTempleGuardian guardian = guardianObject.AddComponent<CursedTempleGuardian>();
        guardian.Configure(this);

        // 3. Cánh Cửa Phong Ấn Bắc Điện (chắn ngang lối lên Bệ Thờ tại y = 6.3)
        GameObject sealedGate = new GameObject("CursedTempleSealedGate");
        sealedGate.transform.position = new Vector3(0f, 6.3f, 0f);

        SpriteRenderer gateRenderer = sealedGate.AddComponent<SpriteRenderer>();
        gateRenderer.sprite = CreateGateSprite();
        gateRenderer.sortingOrder = 12;

        // Hàng rào năng lượng phong ấn chạy ngang hành lang Bắc Điện để người chơi bắt buộc phải giải đố
        BoxCollider2D gateCollider = sealedGate.AddComponent<BoxCollider2D>();
        gateCollider.size = new Vector2(28f, 1.1f);
        gateCollider.offset = new Vector2(0f, 0.9f);

        for (int side = -1; side <= 1; side += 2)
        {
            for (int seg = 1; seg <= 3; seg++)
            {
                GameObject barrierPillar = new GameObject($"SealBarrierPillar_{side}_{seg}");
                barrierPillar.transform.SetParent(sealedGate.transform, false);
                barrierPillar.transform.localPosition = new Vector3(side * seg * 3.2f, 0f, 0f);
                SpriteRenderer bpRenderer = barrierPillar.AddComponent<SpriteRenderer>();
                bpRenderer.sprite = CreateGateSprite();
                bpRenderer.color = new Color(0.45f, 0.85f, 1f, 0.85f);
                bpRenderer.sortingOrder = 11;
            }
        }

        GameObject gateLabelObj = new GameObject("SealedGateLabel");
        gateLabelObj.transform.SetParent(sealedGate.transform, false);
        gateLabelObj.transform.localPosition = new Vector3(0f, 2.65f, 0f);
        TextMeshPro gateLabel = gateLabelObj.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            gateLabel.font = TMP_Settings.defaultFontAsset;
        }
        gateLabel.fontSize = 2.4f;
        gateLabel.alignment = TextAlignmentOptions.Center;
        gateLabel.color = new Color(0.4f, 0.95f, 1f, 1f);
        gateLabel.sortingOrder = 16;

        // 4. Bia Đá Giải Đố Phong Ấn đặt ngay chính giữa sảnh đền (0, 3.2) ngay trước Cửa Phong Ấn (0, 6.3)
        // Người chơi chỉ cần tiến thẳng lên chạm vào Bia Đá và giải 3 câu đố bất kỳ để mở Cửa Phong Ấn!
        GameObject tabletObject = new GameObject("CursedTempleRiddleTablet");
        tabletObject.transform.position = new Vector3(0f, 3.2f, 0f);
        BoxCollider2D tabletCollider = tabletObject.AddComponent<BoxCollider2D>();
        tabletCollider.size = new Vector2(2.6f, 2.6f);
        tabletCollider.isTrigger = true;
        AncientRunePillar mainTablet = tabletObject.AddComponent<AncientRunePillar>();

        AncientRunePillar[] pillars = new AncientRunePillar[] { mainTablet };

        AncientRunePuzzle puzzle = gameObject.AddComponent<AncientRunePuzzle>();
        puzzle.Configure(pillars, guardianObject, sealedGate, gateLabel);
    }

    private void CreateSpikeTrap(Vector3 position, SpikeTrapType trapType, float startDelay)
    {
        GameObject trapObject = new GameObject($"CursedTempleTrap_{trapType}");
        trapObject.transform.position = position;
        BoxCollider2D trapCollider = trapObject.AddComponent<BoxCollider2D>();
        trapCollider.size = new Vector2(1.5f, 0.8f);
        trapCollider.isTrigger = true;
        AncientSpikeTrap trap = trapObject.AddComponent<AncientSpikeTrap>();
        trap.Configure(trapType, startDelay, damage: 1, idle: 2.1f, warn: 0.65f, active: 0.85f);
    }

    public void OnGuardianDefeated(Vector3 position)
    {
        if (this.guardianDefeated) return;
        this.guardianDefeated = true;
        this.UpdateAltarPrompt(true);

        if ((LostTreasureManager.Instance != null && LostTreasureManager.Instance.HasFragment(3)) || this.fragmentSpawned)
        {
            return;
        }
        this.fragmentSpawned = true;

        // Đảm bảo mảnh chìa khóa rơi ở vị trí dễ nhặt ngay trước Bệ Thờ Bắc Điện
        Vector3 spawnPos = new Vector3(
            Mathf.Clamp(position.x, -6f, 6f),
            Mathf.Clamp(position.y, 4.5f, 7.6f),
            0f);

        GameObject fragmentObject = new GameObject("KeyFragment_CursedTemple");
        fragmentObject.transform.position = spawnPos;
        CircleCollider2D fragmentCollider = fragmentObject.AddComponent<CircleCollider2D>();
        fragmentCollider.radius = 1.0f;
        fragmentCollider.isTrigger = true;
        KeyFragmentPickup pickup = fragmentObject.AddComponent<KeyFragmentPickup>();
        pickup.FragmentIndex = 3;
        pickup.FragmentName = "Mảnh Chìa Khóa Đền Cổ";
    }

    private static Sprite CreateGuardianSprite()
    {
        Texture2D texture = new Texture2D(32, 36, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color shadow = new Color(0.14f, 0.1f, 0.24f, 1f);
        Color armor = new Color(0.46f, 0.3f, 0.68f, 1f);
        Color gold = new Color(1f, 0.82f, 0.26f, 1f);
        Color cyanCore = new Color(0.25f, 0.95f, 1f, 1f);

        for (int y = 0; y < 36; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                bool body = x >= 6 && x <= 25 && y >= 3 && y <= 25;
                bool shoulders = x >= 3 && x <= 28 && y >= 16 && y <= 24;
                bool head = x >= 9 && x <= 22 && y >= 23 && y <= 32;
                bool horns = y >= 29 && ((x >= 5 && x <= 9) || (x >= 22 && x <= 26));
                bool eyes = y >= 27 && y <= 28 && ((x >= 11 && x <= 13) || (x >= 18 && x <= 20));
                bool chestRune = x >= 14 && x <= 17 && y >= 13 && y <= 19;

                if (eyes || chestRune)
                    texture.SetPixel(x, y, cyanCore);
                else if (horns || (shoulders && (y == 24 || x == 3 || x == 28)))
                    texture.SetPixel(x, y, gold);
                else if (head || body || shoulders)
                    texture.SetPixel(x, y, (x <= 7 || x >= 24) ? shadow : armor);
                else
                    texture.SetPixel(x, y, clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 32, 36), new Vector2(0.5f, 0f), 16);
    }

    private static Sprite CreateGateSprite()
    {
        Texture2D texture = new Texture2D(24, 36, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color stone = new Color(0.22f, 0.22f, 0.3f, 1f);
        Color rune = new Color(0.3f, 0.88f, 1f, 1f);
        Color gold = new Color(0.95f, 0.78f, 0.28f, 1f);

        for (int y = 0; y < 36; y++)
        {
            for (int x = 0; x < 24; x++)
            {
                bool border = x < 2 || x > 21 || y < 2 || y > 33;
                bool sigil = (x == 11 || x == 12) && y >= 6 && y <= 30 || (y == 18 && x >= 6 && x <= 17);
                texture.SetPixel(x, y, sigil ? rune : border ? gold : stone);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 24, 36), new Vector2(0.5f, 0f), 16);
    }

    private static Sprite CreateAltarPortalSprite()
    {
        Texture2D texture = new Texture2D(32, 36, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color stone = new Color(0.28f, 0.26f, 0.36f, 1f);
        Color gold = new Color(1f, 0.82f, 0.28f, 1f);
        Color portalGlow = new Color(0.35f, 0.92f, 1f, 0.95f);

        for (int y = 0; y < 36; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                bool baseSteps = y < 6 && x >= 2 && x <= 29;
                bool pillars = y >= 6 && y <= 30 && ((x >= 4 && x <= 7) || (x >= 24 && x <= 27));
                bool arch = y >= 28 && y <= 34 && x >= 4 && x <= 27;
                bool innerPortal = y >= 6 && y <= 28 && x >= 9 && x <= 22;

                if (innerPortal)
                    texture.SetPixel(x, y, portalGlow);
                else if (arch || (baseSteps && y == 5))
                    texture.SetPixel(x, y, gold);
                else if (baseSteps || pillars)
                    texture.SetPixel(x, y, stone);
                else
                    texture.SetPixel(x, y, clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 32, 36), new Vector2(0.5f, 0f), 16);
    }
}
