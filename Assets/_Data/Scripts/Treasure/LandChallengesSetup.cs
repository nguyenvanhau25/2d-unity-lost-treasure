using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds the collectible and challenge objects required by each stage.</summary>
public class LandChallengesSetup : MonoBehaviour
{
    private static bool sceneHookRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoadedBootstrap()
    {
        if (!sceneHookRegistered)
        {
            sceneHookRegistered = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        EnsureSetupForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSetupForScene(scene);
    }

    private static void EnsureSetupForScene(Scene scene)
    {
        if (!scene.IsValid()) return;
        string sceneName = scene.name;
        if (sceneName == "MainMenu" || sceneName == "BeginScreen") return;

        if (FindAnyObjectByType<LandChallengesSetup>() == null)
        {
            new GameObject("LandChallengesSetup").AddComponent<LandChallengesSetup>();
        }
    }

    private void Start()
    {
        this.EnsureStageExitsActive();

        string activeScene = SceneManager.GetActiveScene().name;
        if (LostTreasureSceneHelper.IsForestStart(activeScene))
        {
            // Đặt Bệ Đá Mảnh Chìa Khóa 1 ngay chính giữa mặt đường dẫn tới cổng (x = 15.5, y = -0.05)
            SpawnFragmentAtPositionOrBeforeExit(
                0,
                "DarkForestScene",
                "KeyFragment_Forest",
                "Mảnh Chìa Khóa Rừng Xanh",
                new Vector3(15.5f, -0.05f, 0f));
        }
        else if (LostTreasureSceneHelper.IsDarkForest(activeScene))
        {
            SetupDarkForestScene();
        }
        else if (LostTreasureSceneHelper.IsAncientCave(activeScene))
        {
            SetupInsideCaveScene();
        }
        else if (LostTreasureSceneHelper.IsCursedTemple(activeScene))
        {
            if (FindAnyObjectByType<CursedTempleChallenge>() == null)
                gameObject.AddComponent<CursedTempleChallenge>();
        }
        else if (LostTreasureSceneHelper.IsTreasureSanctum(activeScene))
        {
            SetupSpecialSanctumScene();
        }
    }

    private void EnsureStageExitsActive()
    {
        string activeScene = SceneManager.GetActiveScene().name;

        // Ở màn Đền Cổ và màn cuối Tháp Kho Báu: tắt toàn bộ các cổng SceneTransition cũ còn sót lại để không hiện chữ trên map
        if (LostTreasureSceneHelper.IsTreasureSanctum(activeScene) || LostTreasureSceneHelper.IsCursedTemple(activeScene))
        {
            foreach (SceneTransition transition in FindObjectsByType<SceneTransition>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transition != null)
                {
                    transition.gameObject.SetActive(false);
                }
            }
            return;
        }

        foreach (SceneTransition transition in FindObjectsByType<SceneTransition>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transition == null) continue;
            if (LostTreasureSceneHelper.IsGuardianBattlefield(activeScene) && transition.gameObject.name == "SceneTransition_1")
                continue;

            Transform current = transition.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);
                current = current.parent;
            }
        }

        foreach (MapPieceTrigger trigger in FindObjectsByType<MapPieceTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (trigger == null) continue;
            Transform current = trigger.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);
                current = current.parent;
            }
        }
    }

    private void SetupDarkForestScene()
    {
        SpawnFragmentAtPositionOrBeforeExit(
            1,
            "InsideCave",
            "KeyFragment_DarkForest",
            "Mảnh Chìa Khóa Rừng Hắc Ám",
            new Vector3(41.8f, 26.2f, 0f));

        foreach (OrcAttack attack in FindObjectsByType<OrcAttack>(FindObjectsSortMode.None))
            attack.ApplyDarkForestBalance();

        // Early-stage traps right from the entrance (player spawns near (-28.4, -1.8))
        SpawnSpikeTrap(new Vector3(-23.2f, -1.8f, 0f), SpikeTrapType.Fixed, 1, 0f, 1.2f, 2.0f, 0.6f);
        SpawnSpikeTrap(new Vector3(-18.2f, -1.2f, 0f), SpikeTrapType.Periodic, 1, 0.2f, 1.15f, 2.2f, 0.65f);
        SpawnSpikeTrap(new Vector3(-13.5f, -2.2f, 0f), SpikeTrapType.StepTriggered, 1, 0f, 1.1f, 1.8f, 0.55f);
        SpawnSpikeTrap(new Vector3(-8.0f, -1.6f, 0f), SpikeTrapType.Periodic, 1, 1.0f, 1.15f, 2.3f, 0.65f);

        // Mid-stage traps combined with Orc patrols along the eastern trail
        SpawnSpikeTrap(new Vector3(10.5f, -1.4f, 0f), SpikeTrapType.StepTriggered, 1, 0f, 1.1f, 1.9f, 0.55f);
        SpawnSpikeTrap(new Vector3(19.0f, 1.8f, 0f), SpikeTrapType.Fixed, 1, 0f, 1.2f, 2.0f, 0.6f);
        SpawnSpikeTrap(new Vector3(27.0f, 2.2f, 0f), SpikeTrapType.Periodic, 1, 0.6f, 1.15f, 2.2f, 0.65f);

        // Northern hill climb traps before the cave entrance plateau
        SpawnSpikeTrap(new Vector3(34.8f, 14.5f, 0f), SpikeTrapType.StepTriggered, 1, 0f, 1.1f, 1.9f, 0.55f);
        SpawnSpikeTrap(new Vector3(38.8f, 22.5f, 0f), SpikeTrapType.Periodic, 1, 0.4f, 1.15f, 2.2f, 0.65f);
    }

    private void SetupInsideCaveScene()
    {
        SpawnFragmentAtPositionOrBeforeExit(
            2,
            "LastWar",
            "KeyFragment_InsideCave",
            "Mảnh Chìa Khóa Hang Động",
            new Vector3(37.5f, 28.2f, 0f));

        // Walkable S-curve corridors in InsideCave: Tier 1 (y ~ 7.2), Tier 2 (y ~ 18.0), Tier 3 (y ~ 28.0)
        SpawnSpikeTrap(new Vector3(6.5f, 7.2f, 0f), SpikeTrapType.Fixed, 1, 0f, 1.2f, 2.0f, 0.6f);
        SpawnSpikeTrap(new Vector3(15.5f, 7.2f, 0f), SpikeTrapType.Periodic, 1, 0.4f, 1.15f, 2.3f, 0.65f);
        SpawnSpikeTrap(new Vector3(15.0f, 18.0f, 0f), SpikeTrapType.StepTriggered, 1, 0f, 1.1f, 1.9f, 0.55f);
        SpawnSpikeTrap(new Vector3(7.5f, 18.0f, 0f), SpikeTrapType.Periodic, 1, 1.1f, 1.15f, 2.3f, 0.65f);
        SpawnSpikeTrap(new Vector3(8.5f, 28.0f, 0f), SpikeTrapType.StepTriggered, 1, 0f, 1.1f, 1.9f, 0.55f);
        SpawnSpikeTrap(new Vector3(18.5f, 28.0f, 0f), SpikeTrapType.Periodic, 1, 0.7f, 1.15f, 2.3f, 0.65f);
    }

    private void SetupSpecialSanctumScene()
    {
        GameObject legacyPrincess = GameObject.Find("Princess");
        if (legacyPrincess != null) legacyPrincess.SetActive(false);

        if (FindAnyObjectByType<MysteryTreasureChest>() != null) return;
        GameObject chest = new GameObject("MysteryTreasureChest");
        // Đặt Rương Kho Báu hiện rõ ngay trên Tháp Cổ Đại (Castle tại (0.36, 17.35))
        chest.transform.position = new Vector3(0.36f, 17.35f, 0f);
        BoxCollider2D collider = chest.AddComponent<BoxCollider2D>();
        collider.offset = new Vector2(0f, -1.25f);
        collider.size = new Vector2(4.2f, 3.8f);
        collider.isTrigger = true;
        chest.AddComponent<MysteryTreasureChest>();
    }

    private void SpawnSpikeTrap(
        Vector3 position,
        SpikeTrapType trapType,
        int damage,
        float delay,
        float activeDuration,
        float inactiveDuration,
        float warningDuration)
    {
        GameObject trapObject = new GameObject($"AncientSpikeTrap_{trapType}");
        trapObject.transform.position = position;
        BoxCollider2D collider = trapObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.65f, 0.75f);
        collider.isTrigger = true;
        AncientSpikeTrap trap = trapObject.AddComponent<AncientSpikeTrap>();
        trap.Configure(trapType, delay, damage, inactiveDuration, warningDuration, activeDuration);
    }

    private void SpawnFragmentAtPositionOrBeforeExit(int index, string destination, string objectName, string fragmentName, Vector3 preferredPosition)
    {
        if (LostTreasureManager.Instance.HasFragment(index)) return;
        foreach (KeyFragmentPickup existing in FindObjectsByType<KeyFragmentPickup>(FindObjectsSortMode.None))
            if (existing.FragmentIndex == index) return;

        CreateFragmentPickup(index, objectName, fragmentName, preferredPosition);
    }

    private static void CreateFragmentPickup(int index, string objectName, string fragmentName, Vector3 position)
    {
        GameObject fragment = new GameObject(objectName);
        fragment.transform.position = position;
        CircleCollider2D collider = fragment.AddComponent<CircleCollider2D>();
        collider.radius = 1.35f;
        collider.isTrigger = true;
        KeyFragmentPickup pickup = fragment.AddComponent<KeyFragmentPickup>();
        pickup.FragmentIndex = index;
        pickup.FragmentName = fragmentName;
    }
}
