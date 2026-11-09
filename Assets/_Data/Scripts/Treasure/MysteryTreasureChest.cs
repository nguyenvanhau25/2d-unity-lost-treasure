using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The climax objective for "Lost Treasure": The Mystery Treasure Chest.
/// Can only be unlocked once the player has crafted the Master Key (4/4 fragments).
/// Triggers the grand victory sequence with visual effects, fanfare, and lore resolution.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MysteryTreasureChest : MonoBehaviour
{
    [Header("Chest Settings")]
    [SerializeField] private string endingSceneName = "MainMenu";
    [SerializeField] private float endingDelay = 5.5f;

    [Header("Visual Colors")]
    [SerializeField] private Color chestGoldColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color lockedColor = new Color(0.6f, 0.6f, 0.65f);

    private SpriteRenderer spriteRenderer;
    private Collider2D chestCollider;
    private TextMeshPro promptText;
    private bool isPlayerNearby;
    private bool isOpened;
    private GameObject goldenAuraObj;

    private void Awake()
    {
        this.chestCollider = GetComponent<Collider2D>();
        this.chestCollider.isTrigger = true;

        this.spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (this.spriteRenderer == null)
        {
            this.spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            this.spriteRenderer.sprite = CreateChestSprite(false);
        }

        this.CreateWorldPrompt();
        this.CreateGoldenAura();
    }

    private void Update()
    {
        if (this.isOpened) return;

        bool hasMasterKey = LostTreasureManager.Instance.IsMasterKeyCrafted;

        if (this.goldenAuraObj != null)
        {
            this.goldenAuraObj.SetActive(hasMasterKey);
        }

        if (this.promptText != null)
        {
            if (this.isPlayerNearby)
            {
                if (hasMasterKey)
                {
                    this.promptText.text = "★ NHẤN [ E ] MỞ RƯƠNG KHO BÁU ★";
                    this.promptText.color = new Color(1f, 0.95f, 0.4f);
                }
                else
                {
                    int count = LostTreasureManager.Instance.TotalCollectedCount;
                    this.promptText.text = $"🔒 PHONG ẤN (Cần 4/4 Mảnh Chìa Khóa, hiện có: {count}/4)";
                    this.promptText.color = new Color(0.85f, 0.4f, 0.4f);
                }
            }
            else
            {
                this.promptText.text = "✦ RƯƠNG KHO BÁU BÍ ẨN ✦";
                this.promptText.color = hasMasterKey ? this.chestGoldColor : this.lockedColor;
            }
        }

        if (this.isPlayerNearby && hasMasterKey && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)))
        {
            this.OpenChest();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || other.GetComponentInParent<PlayerCtrl>() == null) return;
        this.isPlayerNearby = true;

        if (LostTreasureManager.Instance.IsMasterKeyCrafted && !this.isOpened)
        {
            // Auto-trigger opening if player touches chest directly!
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
        if (!LostTreasureManager.Instance.IsMasterKeyCrafted) return;
        this.isOpened = true;

        LostTreasureManager.Instance.UnlockTreasureChest();
        StartCoroutine(this.RunGrandClimaxSequence());
    }

    private IEnumerator RunGrandClimaxSequence()
    {
        this.FreezePlayer();

        if (this.promptText != null)
        {
            this.promptText.text = "★ ĐANG MỞ KHÓA KHO BÁU... ★";
            this.promptText.color = Color.yellow;
        }

        // 1. Shake and rumble
        Vector3 originPos = transform.position;
        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("SelectSound", 1.5f);
        }

        const float shakeTime = 1.2f;
        for (float t = 0; t < shakeTime; t += Time.deltaTime)
        {
            float offset = Mathf.Sin(t * 35f) * 0.08f;
            transform.position = originPos + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        transform.position = originPos;

        // 2. Chest pops open!
        if (this.spriteRenderer != null)
        {
            this.spriteRenderer.sprite = CreateChestSprite(true);
        }

        // 3. Victory fanfare
        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("ResonanceSkill", 1.8f);
        }

        // 4. Golden light rays effect
        StartCoroutine(this.EmitTreasureLightRays());

        // 5. Cinematic story banner
        SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();
        if (overlay != null)
        {
            yield return overlay.ShowMessage("BÍ MẬT ĐÃ ĐƯỢC GIẢI MÃ", "Chiếc Chìa Khóa Cổ Đại xoay chuyển... Ổ khóa nghìn năm đã mở tung!", 2.8f, new Color(1f, 0.85f, 0.3f));
            yield return overlay.ShowMessage("LOST TREASURE - CHIẾN THẮNG HUYỀN THOẠI", "Bạn đã thu thập đủ các mảnh chìa khóa, đánh bại hộ vệ hắc ám và tìm thấy Kho Báu Thất Lạc vĩ đại!\nVương quốc một lần nữa được chiếu sáng trong hòa bình và thịnh vượng.", 4.5f, new Color(1f, 0.95f, 0.5f));
        }

        yield return new WaitForSeconds(this.endingDelay);
        SceneManager.LoadScene(this.endingSceneName);
    }

    private IEnumerator EmitTreasureLightRays()
    {
        GameObject beam = new GameObject("TreasureLightBeam");
        beam.transform.SetParent(transform);
        beam.transform.localPosition = new Vector3(0, 1.2f, 0);

        SpriteRenderer sr = beam.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBeamSprite();
        sr.color = new Color(1f, 0.95f, 0.4f, 0.85f);
        sr.sortingOrder = 15;

        Vector3 startScale = new Vector3(0.5f, 0.1f, 1f);
        Vector3 maxScale = new Vector3(2.5f, 4.5f, 1f);

        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            beam.transform.localScale = Vector3.Lerp(startScale, maxScale, t / 2f);
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

    private void CreateWorldPrompt()
    {
        GameObject promptObj = new GameObject("ChestPrompt");
        promptObj.transform.SetParent(transform);
        promptObj.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        this.promptText = promptObj.AddComponent<TextMeshPro>();
        this.promptText.text = "✦ RƯƠNG KHO BÁU BÍ ẨN ✦";
        this.promptText.fontSize = 3.6f;
        this.promptText.alignment = TextAlignmentOptions.Center;
        this.promptText.color = this.chestGoldColor;
        this.promptText.sortingOrder = 12;
    }

    private void CreateGoldenAura()
    {
        this.goldenAuraObj = new GameObject("GoldenAura");
        this.goldenAuraObj.transform.SetParent(transform);
        this.goldenAuraObj.transform.localPosition = Vector3.zero;
        this.goldenAuraObj.transform.localScale = Vector3.one * 1.5f;

        SpriteRenderer sr = this.goldenAuraObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateChestSprite(false);
        sr.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        sr.sortingOrder = 2;
    }

    private static Sprite CreateChestSprite(bool isOpen)
    {
        Texture2D tex = new Texture2D(24, 20, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color wood = new Color(0.48f, 0.28f, 0.14f, 1f);
        Color gold = new Color(1f, 0.82f, 0.25f, 1f);
        Color lockCol = new Color(0.9f, 0.9f, 0.9f, 1f);
        Color inside = new Color(1f, 0.95f, 0.5f, 1f);

        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 24; x++)
            {
                if (x == 0 || x == 23 || y == 0 || y == 19 || x == 6 || x == 17)
                {
                    tex.SetPixel(x, y, gold);
                }
                else if (isOpen && y > 10 && x > 2 && x < 21)
                {
                    tex.SetPixel(x, y, inside); // Radiant open treasure interior
                }
                else
                {
                    tex.SetPixel(x, y, wood);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 24, 20), new Vector2(0.5f, 0.5f), 16);
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
