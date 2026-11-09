using System.Collections;
using UnityEngine;

public sealed class CursedTempleChallenge : MonoBehaviour
{
    private bool guardianDefeated;
    private bool fragmentSpawned;

    public bool IsComplete => this.guardianDefeated;

    private void Start()
    {
        RescuePrincessStory story = FindAnyObjectByType<RescuePrincessStory>();
        if (story == null)
        {
            Debug.LogError("Cursed Temple requires its Guardian encounter trigger.", this);
            enabled = false;
            return;
        }

        Collider2D storyCollider = story.GetComponent<Collider2D>();
        Vector3 altarPosition = storyCollider != null ? storyCollider.bounds.center : story.transform.position;
        this.PrepareExitAltar(story, altarPosition);

        if (LostTreasureManager.Instance.HasFragment(3))
        {
            this.guardianDefeated = true;
            return;
        }

        this.BuildRunePuzzle(altarPosition);
        StartCoroutine(this.ShowTempleObjective());
    }

    private IEnumerator ShowTempleObjective()
    {
        yield return null;
        SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();
        if (overlay != null)
            yield return overlay.ShowMessage("THỬ THÁCH ĐỀN CỔ", "Kích hoạt ba trụ rune bằng đòn đánh, hạ Guardian rồi nhặt mảnh chìa khóa.", 3f, new Color(0.35f, 0.85f, 1f));
    }

    private void PrepareExitAltar(RescuePrincessStory story, Vector3 position)
    {
        foreach (SpriteRenderer renderer in story.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.enabled = false;
        foreach (Animator animator in story.GetComponentsInChildren<Animator>(true))
            animator.enabled = false;

        GameObject altar = new GameObject("CursedTempleExitAltarVisual");
        altar.transform.position = position;
        SpriteRenderer altarRenderer = altar.AddComponent<SpriteRenderer>();
        altarRenderer.sprite = CreateGateSprite();
        altarRenderer.color = new Color(0.95f, 0.72f, 0.34f, 1f);
        altarRenderer.sortingOrder = 11;
    }

    private void BuildRunePuzzle(Vector3 altarPosition)
    {
        this.CreateSpikeTrap(altarPosition + new Vector3(-9f, -0.8f, 0f), 0f);
        this.CreateSpikeTrap(altarPosition + new Vector3(-6.5f, -0.8f, 0f), 1.2f);

        GameObject guardianObject = new GameObject("CursedTempleGuardian");
        guardianObject.transform.position = altarPosition + Vector3.right * 5f;
        guardianObject.SetActive(false);

        SpriteRenderer guardianRenderer = guardianObject.AddComponent<SpriteRenderer>();
        guardianRenderer.sprite = CreateGuardianSprite();
        guardianRenderer.sortingOrder = 12;
        guardianObject.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, 1.8f);
        CursedTempleGuardian guardian = guardianObject.AddComponent<CursedTempleGuardian>();
        guardian.Configure(this);

        GameObject sealedGate = new GameObject("CursedTempleSealedGate");
        sealedGate.transform.position = altarPosition + Vector3.right * 3f;
        SpriteRenderer gateRenderer = sealedGate.AddComponent<SpriteRenderer>();
        gateRenderer.sprite = CreateGateSprite();
        gateRenderer.sortingOrder = 10;
        BoxCollider2D gateCollider = sealedGate.AddComponent<BoxCollider2D>();
        gateCollider.size = new Vector2(1.2f, 2.5f);

        AncientRunePillar[] pillars = new AncientRunePillar[3];
        for (int index = 0; index < pillars.Length; index++)
        {
            GameObject pillarObject = new GameObject($"CursedTempleRune_{index + 1}");
            pillarObject.transform.position = altarPosition + new Vector3(-5f + index * 2f, -1.2f, 0f);
            BoxCollider2D pillarCollider = pillarObject.AddComponent<BoxCollider2D>();
            pillarCollider.size = new Vector2(1.2f, 2.2f);
            pillarCollider.isTrigger = true;
            pillars[index] = pillarObject.AddComponent<AncientRunePillar>();
        }

        AncientRunePuzzle puzzle = gameObject.AddComponent<AncientRunePuzzle>();
        puzzle.Configure(pillars, guardianObject, sealedGate);
    }

    private void CreateSpikeTrap(Vector3 position, float startDelay)
    {
        GameObject trapObject = new GameObject("CursedTempleSpikeTrap");
        trapObject.transform.position = position;
        BoxCollider2D trapCollider = trapObject.AddComponent<BoxCollider2D>();
        trapCollider.size = new Vector2(1.8f, 0.8f);
        trapCollider.isTrigger = true;
        AncientSpikeTrap trap = trapObject.AddComponent<AncientSpikeTrap>();
        trap.StartDelay = startDelay;
    }

    public void OnGuardianDefeated(Vector3 position)
    {
        if (this.guardianDefeated) return;
        this.guardianDefeated = true;

        if (LostTreasureManager.Instance.HasFragment(3) || this.fragmentSpawned) return;
        this.fragmentSpawned = true;

        GameObject fragmentObject = new GameObject("KeyFragment_CursedTemple");
        fragmentObject.transform.position = position + Vector3.up * 0.5f;
        CircleCollider2D fragmentCollider = fragmentObject.AddComponent<CircleCollider2D>();
        fragmentCollider.radius = 0.9f;
        fragmentCollider.isTrigger = true;
        KeyFragmentPickup pickup = fragmentObject.AddComponent<KeyFragmentPickup>();
        pickup.FragmentIndex = 3;
        pickup.FragmentName = "Mảnh Chìa Khóa Đền Cổ";
    }

    private static Sprite CreateGuardianSprite()
    {
        Texture2D texture = new Texture2D(24, 32, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color shadow = new Color(0.12f, 0.09f, 0.2f, 1f);
        Color armor = new Color(0.42f, 0.28f, 0.62f, 1f);
        Color gold = new Color(1f, 0.78f, 0.24f, 1f);

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 24; x++)
            {
                bool body = x >= 4 && x <= 19 && y >= 3 && y <= 23;
                bool head = x >= 6 && x <= 17 && y >= 20 && y <= 29;
                bool horns = y >= 27 && ((x >= 3 && x <= 7) || (x >= 16 && x <= 20));
                bool eyes = y >= 23 && y <= 24 && (x == 8 || x == 15);
                texture.SetPixel(x, y, horns || eyes ? gold : body || head ? armor : body && (x < 6 || x > 17) ? shadow : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 24, 32), new Vector2(0.5f, 0f), 16);
    }

    private static Sprite CreateGateSprite()
    {
        Texture2D texture = new Texture2D(20, 36, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color stone = new Color(0.22f, 0.2f, 0.28f, 1f);
        Color rune = new Color(0.3f, 0.8f, 0.9f, 1f);

        for (int y = 0; y < 36; y++)
        {
            for (int x = 0; x < 20; x++)
            {
                bool frame = x < 3 || x > 16 || y < 3 || y > 32;
                bool sigil = x == 9 || x == 10 || y == 18 && x >= 6 && x <= 13;
                texture.SetPixel(x, y, sigil ? rune : stone);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 20, 36), new Vector2(0.5f, 0f), 16);
    }
}
